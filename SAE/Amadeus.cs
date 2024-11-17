//#define __dbg


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Net;


using SAE.AmadeusPRD;
using System.Web.Services.Description;
using System.Globalization;
using SAE.Properties;

namespace SAE {
  public class Amadeus : AmadeusSOAP {
    private const int numConnPoints = 1; // количество пересадок (для GetAirportsForChanges)

    private string amplitudeApiKey, amplitudeUserId, pcc;

    public Amadeus(
      string username,
      string passwordBase64,
      string pcc,
      string xmlPath,
      string amplitudeApiKey,
      string amplitudeUserId,
      bool logXml,
      bool useProxy,
      string urlProxy,
      string logProxy,
      string parProxy)
      : base(username, passwordBase64, pcc, xmlPath, logXml, useProxy, urlProxy, logProxy, parProxy) {

      this.amplitudeApiKey = amplitudeApiKey;
      this.amplitudeUserId = amplitudeUserId;
      this.pcc = pcc;
    }

    void SendAmplitudeEvent(AmplitudeEvent ae) {
      string json;
      var ds = new System.Runtime.Serialization.Json.DataContractJsonSerializer(ae.GetType());
      using (var ms = new MemoryStream()) {
        ds.WriteObject(ms, ae);
        json = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Position);
      }

      string url = @"https://api.amplitude.com/httpapi?" +
        "api_key=" + System.Web.HttpUtility.UrlEncode(amplitudeApiKey, Encoding.UTF8) + "&" +
        "event=" + System.Web.HttpUtility.UrlEncode(json, Encoding.UTF8);

      var webReq = (HttpWebRequest)WebRequest.Create(url);
      webReq.Headers.Add("Pragma", "no-cache");
      webReq.Headers.Add("Cache-Control", "no-cache, no-store");
      try {
        using (var webRsp = (HttpWebResponse)webReq.GetResponse()) {
        }
      }
      catch {
      }
    }

    // ********* GetDirectFlightsOnDate ***************************************************************
       
     public Flight[] GetDirectFlightsOnDate(string origin, string destination, DateTime departureDate) {

 #if __dbg
       var listDbgE = new List<string>() {
       /*
         @"D:\ProjectsAgenda\SAE\air_multiavail_rsp_20190426_160813_434_1.xml", 
         @"D:\ProjectsAgenda\SAE\1_air_multiavail_scroll_rsp_20190426_160813_810_1.xml",
         @"D:\ProjectsAgenda\SAE\2_air_multiavail_scroll_rsp_20190426_160815_163_1.xml",
         @"D:\ProjectsAgenda\SAE\3_air_multiavail_scroll_rsp_20190426_160817_525_1.xml",
         @"D:\ProjectsAgenda\SAE\4_air_multiavail_scroll_rsp_20190426_160817_879_1.xml",
         @"D:\ProjectsAgenda\SAE\5_air_multiavail_scroll_rsp_20190426_160819_222_1.xml"
       */
         @"C:\Users\zzz\Downloads\air_multiavail_rsp_20200414_201839_365_4.xml",
       }.GetEnumerator();
#endif

      int t1 = Environment.TickCount;
       var resultFlights = new List<Flight>();

       var amavReq = FirstScheduleReq(origin, destination, departureDate);
       string subdir = "GetDirectFlightsOnDate";
       int fc = SerializeXml(amavReq, subdir, "air_multiavail_req");
       int numScrolls = 0;

       using (var am = CreateAWS(null)) {
 #if __dbg
         Air_MultiAvailabilityReply amavRsp;
         listDbgE.MoveNext();
         DeserializeXml(out amavRsp, listDbgE.Current);
 #else
         Air_MultiAvailabilityReply amavRsp = am.Air_MultiAvailability(amavReq);        
 #endif
         SerializeXml(amavRsp, subdir, "air_multiavail_rsp", am.SessionValue, fc);
       
         while (AddDirectFlightsOnDate(resultFlights, amavRsp, departureDate)) {
           if (numScrolls == 30) {
             // на всякий случай ограничиваем количество листаний 30-ю
             break;
           }
           ++numScrolls;
 #if __dbg
           listDbgE.MoveNext();
           DeserializeXml(out amavRsp, listDbgE.Current);
 #else
           IncrementSession(am, false);
           amavRsp = am.Air_MultiAvailability(NextScheduleReq());          
 #endif
           SerializeXml(amavRsp, subdir, numScrolls + "_air_multiavail_scroll_rsp", am.SessionValue, fc);
         }
         
 #if !__dbg
         SignOut(am, subdir, fc);
 #endif
       }

       SerializeXml(resultFlights, subdir, "result", null, fc);

 #if !__dbg      
       if (!String.IsNullOrEmpty(amplitudeApiKey) && !String.IsNullOrEmpty(amplitudeUserId)) {
         var ampEv = new GetDirectFlightsOnDateEvent() {
           user_id = amplitudeUserId,
           event_properties = new GetDirectFlightsOnDate_EP() {
             origin = origin,
             destination = destination,
             time_execute = (Environment.TickCount - t1 + 500) / 1000,
             total_variants = resultFlights.Count,
             air_multiavail_req_count = numScrolls + 1,
             gate = pcc,
           },
         };

         SendAmplitudeEvent(ampEv);
       }
 #endif
          
       return resultFlights.ToArray();
     }
  
     private bool AddDirectFlightsOnDate(List<Flight> flights, Air_MultiAvailabilityReply amavRsp, DateTime departureDate) {

       // проверка на ошибку
       if (amavRsp.errorOrWarningSection != null && amavRsp.errorOrWarningSection.Length != 0) {
         var errTxt = new StringBuilder("Ошибка GetDirectFlightsOnDate(): ");
         foreach (var err in amavRsp.errorOrWarningSection) {
           if (err.textInformation != null &&
             err.textInformation.freeText != null) {

             foreach (string s in err.textInformation.freeText) {
               errTxt.Append(" ").Append(s);
             }
           }
         }
         throw new GDSErrorException(errTxt.ToString());
       }

       // если нет инфы по перелетам
       if (amavRsp.singleCityPairInfo == null ||
         amavRsp.singleCityPairInfo.Length == 0 ||
         amavRsp.singleCityPairInfo[0].flightInfo == null ||
         amavRsp.singleCityPairInfo[0].flightInfo.Length == 0) {
         return false;
       }
       /* закомментировано 16.04.2020 т.к. из-за этой проверки стали отсеиваться нормальные рейсы
       // если есть ошибка (тип 001 и EC - значит ошибка, а не предупреждение или что-то другое)
       if (amavRsp.singleCityPairInfo[0].cityPairErrorOrWarning != null &&
         amavRsp.singleCityPairInfo[0].cityPairErrorOrWarning.Any(
           _ews => _ews.cityPairErrorOrWarningInfo.error.type == "001" || _ews.cityPairErrorOrWarningInfo.error.type == "EC")) {
         return false;
       }
       */
       bool shouldScroll = true;
       foreach (var f in amavRsp.singleCityPairInfo[0].flightInfo) {

         if (!f.basicFlightInfo.productTypeDetail.Any(_pi => _pi == "D") || // если непрямой
           // или если начались перелеты на другую дату
             f.basicFlightInfo.flightDetails.departureDate != departureDate.ToString("ddMMyy")) {
           shouldScroll = false;
           break;
         }

         // если перелет с посадками
         if (f.additionalFlightInfo[0].flightDetails.numberOfStops != "0") {
           continue;
         }

         int infoOnClassesLength = f.infoOnClasses != null ? f.infoOnClasses.Length : 0;
         var bicNum = new List<string>(infoOnClassesLength);
         for (int i = 0; i < infoOnClassesLength; ++i) {
           uint ns;
           if (!uint.TryParse(f.infoOnClasses[i].productClassDetail.availabilityStatus, out ns)) {
             ns = 0;
           }
           bicNum.Add(f.infoOnClasses[i].productClassDetail.serviceClass + ns);
         }

         flights.Add(new Flight() {
           DepartureDateTime = AmavDateTime(f.basicFlightInfo.flightDetails.departureDate, f.basicFlightInfo.flightDetails.departureTime),
           ArrivalDateTime = AmavDateTime(f.basicFlightInfo.flightDetails.arrivalDate, f.basicFlightInfo.flightDetails.arrivalTime),

           Origin = f.basicFlightInfo.departureLocation.cityAirport,
           Destination = f.basicFlightInfo.arrivalLocation.cityAirport,

           MarketingCarrier = f.basicFlightInfo.marketingCompany.identifier,
           OperatingCarrier = f.basicFlightInfo.operatingCompany == null || String.IsNullOrEmpty(f.basicFlightInfo.operatingCompany.identifier) ?
             f.basicFlightInfo.marketingCompany.identifier : f.basicFlightInfo.operatingCompany.identifier,

           FlightNumber = f.basicFlightInfo.flightIdentification.number,

           Duration = int.Parse(f.additionalFlightInfo[0].flightDetails.legDuration.Substring(0, 2)) * 60 +
             int.Parse(f.additionalFlightInfo[0].flightDetails.legDuration.Substring(2, 2)),

           Equipment = f.additionalFlightInfo[0].flightDetails.typeOfAircraft,

           DepartureTerminal = f.additionalFlightInfo[0].departureStation != null && !String.IsNullOrEmpty(f.additionalFlightInfo[0].departureStation.terminal) ?
             f.additionalFlightInfo[0].departureStation.terminal : null,

           ArrivalTerminal = f.additionalFlightInfo[0].arrivalStation != null && !String.IsNullOrEmpty(f.additionalFlightInfo[0].arrivalStation.terminal) ?
             f.additionalFlightInfo[0].arrivalStation.terminal : null,

           NumSeatsForBookingClass = bicNum.ToArray(),
         });

       }

       return shouldScroll;
     }
   
     private static DateTime AmavDateTime(string d, string t) {
       if (t != "2400") {
         return DateTime.ParseExact(d.Insert(4, "20") + t, "ddMMyyyyHHmm", null);
       }
       else {
         return DateTime.ParseExact(d.Insert(4, "20") + "0000", "ddMMyyyyHHmm", null).AddDays(1);
       }
     }
    
     private Air_MultiAvailability FirstScheduleReq(string origin, string destination, DateTime departureDate) {
       return new Air_MultiAvailability() {
         messageActionDetails = new MessageActionDetailsType() {
           functionDetails = new MessageFunctionBusinessDetailsType() {
             actionCode = "48",             
           },
         },
         requestSection = new[] {
           new Air_MultiAvailabilityRequestSection() {
             
             availabilityProductInfo = new AvailabilityProductionInfoType(){
               availabilityDetails = new[] {
                 new ProductDateTimeType() {
                   departureDate = departureDate.ToString("ddMMyy"),
                 }
               },
               departureLocationInfo = new LocationDetailsType() {
                 cityAirport = origin,
               },
               arrivalLocationInfo = new LocationDetailsType() {
                 cityAirport = destination,
               }
             },
             availabilityOptions=new AvailabilityOptionsType() {
               typeOfRequest="TN",               
             },
    /*        
             availabilityOptions = new AvailabilityOptionsType() {               
               productTypeDetails = new ProductTypeDetailsType() {
                 typeOfRequest = "TN",
               },
               optionInfo = new[] {
                 new  SelectionDetailsInformationType_276635C {
                  
                   type = "NDC",
                   arguments = new[] {"OD"},
                 },
               },
             },*/
           },
         }
       };
     }
    
    private Air_MultiAvailability NextScheduleReq() {
      return new Air_MultiAvailability() {
        messageActionDetails = new MessageActionDetailsType() {
          functionDetails = new MessageFunctionBusinessDetailsType() {
            actionCode = "55"
          }
        }
      };
    }


        // ********* GetAirportsForChanges ***************************************************************
        public GetAirportsForChanges_Result GetAirportsForChanges(string origin, string destination, DateTime departureDate, string[] allowedCarriers, int radiusKM)
        {
            int t1 = Environment.TickCount;
            var airpForChng = new HashSet<string>();
            var origins = new HashSet<string>();
            var destinations = new HashSet<string>();
            string subdir = "GetAirportsForChanges";
            var req = MasterPricerRQ(origin, destination, departureDate, allowedCarriers, radiusKM);
            int fc = SerializeXml(req, subdir, "master_pricer_req");
            using (var am = CreateAWS(null, true))
            {
                var rsp = am.Fare_MasterPricerTravelBoardSearch(req);
                SerializeXml(rsp, subdir, "master_pricer_rsp", am.SessionValue, fc);

                if (rsp.errorMessage != null)
                {
                    string errCode = rsp.errorMessage.applicationError.applicationErrorDetail.error;
                    if (
                      errCode == "866" /*NO FARE FOUND FOR REQUESTED ITINERARY*/ ||
                      errCode == "931" /*NO ITINERARY FOUND FOR REQUESTED SEGMENT*/ ||
                      errCode == "996" /*NO JOURNEY FOUND FOR REQUESTED ITINERARY*/ ||
                      errCode == "977" /*No available flight found for the requested segment*/ )
                    {
                        return GetAirportsForChanges_Result.Empty();
                    }
                    throw new GDSErrorException("Ошибка GetAirportsForChanges(): " +
                      String.Join(". ", rsp.errorMessage.errorMessageText.description));
                }


                if (rsp.flightIndex != null)
                {
                    foreach (var fi in rsp.flightIndex)
                    {
                        foreach (var gf in fi.groupOfFlights)
                        {

                            for (int i = 0; i < gf.flightDetails.Length && gf.flightDetails.Length > 1 &&
                              allowedCarriers != null && allowedCarriers.Length > 0; ++i)
                            {
                                // хотя в ответе master pricer'а по идее должны быть только маркетинговые а/к
                                // которые входят в allowedCarriers, на всякий случай проверяем, чтобы были только разрешенные а/к,
                                //
                                // ограничение в запросе не работает для оперирующих (невозможно ограничить мастерпрайсер по оперирующим)
                                // поэтому вручную откидываем те у который оперирубщий не входит в allowedCarriers
                                var companyId = gf.flightDetails[i].flightInformation.companyId;
                                if ((allowedCarriers.All(_ac => _ac != companyId.marketingCarrier) ||
                                    allowedCarriers.All(_ac => _ac != companyId.operatingCarrier)) && companyId.operatingCarrier != null)
                                {
                                    goto __nextgof;
                                }
                            }

                            /*
                            // test              
                            if (gf.flightDetails.Any(_fd => 
                                allowedCarriers.All(_ac => _ac != _fd.flightInformation.companyId.marketingCarrier) || 
                                allowedCarriers.All(_ac => _ac != _fd.flightInformation.companyId.operatingCarrier))) {
                              throw new Exception("1");
                            }
                            */

                            for (int i = 1; i < gf.flightDetails.Length; ++i)
                            {// отбрасываем open jaw 
                                if (gf.flightDetails[i - 1].flightInformation.location[1].locationId != gf.flightDetails[i].flightInformation.location[0].locationId)
                                {
                                    goto __nextgof;
                                }
                            }
                            if (gf.flightDetails.Length != numConnPoints + 1)
                            {
                                goto __nextgof; // ограничиваем колво пересадок числом numConnPoints (На всякий случай. В ответе на мастер прайсер и не должно быть больше)
                            }
                            for (int i = 1; i < gf.flightDetails.Length; ++i)
                            {
                                airpForChng.Add(gf.flightDetails[i].flightInformation.location[0].locationId);
                            }
                            origins.Add(gf.flightDetails[0].flightInformation.location[0].locationId);
                            destinations.Add(gf.flightDetails[gf.flightDetails.Length - 1].flightInformation.location[1].locationId);
                        __nextgof:;
                        }
                    }
                }
            }

            var result = new GetAirportsForChanges_Result()
            {
                AirportsForChanges = airpForChng.ToArray(),
                Origins = origins.ToArray(),
                Destinations = destinations.ToArray(),
            };

            SerializeXml(result, subdir, "result", null, fc);

            if (!String.IsNullOrEmpty(amplitudeApiKey) && !String.IsNullOrEmpty(amplitudeUserId))
            {
                var ampEv = new GetAirportsForChangesEvent()
                {
                    user_id = amplitudeUserId,
                    event_properties = new GetAirportsForChanges_EP()
                    {
                        origin = origin,
                        destination = destination,
                        time_execute = (Environment.TickCount - t1 + 500) / 1000,
                        aircompanies = allowedCarriers,
                        total_transfer_location = airpForChng.Count,
                        radius = radiusKM,
                        total_from = origins.Count,
                        total_to = destinations.Count,
                        gate = pcc,
                    },
                };
                SendAmplitudeEvent(ampEv);
            }

            return result;
        }

        public Flight[] GetDirectFlightsCryptic(string origin, string destination, DateTime departureDate)
        {
            List<Flight> resultFlights = new List<Flight>();
            int t1 = Environment.TickCount;

            string strdate = departureDate.ToString("ddMMM", CultureInfo.GetCultureInfo("en-US")).ToUpper();

            var req = new Command_Cryptic()
            {
                messageAction = new Command_CrypticMessageAction()
                {
                    messageFunctionDetails = new Command_CrypticMessageActionMessageFunctionDetails()
                    {
                        messageFunction = "M"
                    },
                },
                longTextString = new Command_CrypticLongTextString()
                {
                    textStringDetails = "SN" + strdate + origin + destination + "/AYY",
                },
            };

            var req2 = new Command_Cryptic()
            {
                messageAction = new Command_CrypticMessageAction()
                {
                    messageFunctionDetails = new Command_CrypticMessageActionMessageFunctionDetails()
                    {
                        messageFunction = "M"
                    },
                },
                longTextString = new Command_CrypticLongTextString()
                {
                    textStringDetails = "MD",
                },
            };

            string subdir = "GetDirectFlightCryptic";
            int numScrolls = 0;

            using (var am = CreateAWS(null, false))
            {
                var rsp = am.Command_Cryptic(req);
                string response = rsp.longTextString.textStringDetails;
                Flight[] flights = ParseFlights(response, departureDate);
                resultFlights.AddRange(flights);
                int cntflight = flights.Length;

                int fc = SerializeXml(req, subdir, "command_cryptic_req");
                SerializeXml(rsp, subdir, "command_cryptic_rsp", am.SessionValue, fc);

                while (cntflight >= 9)
                {
                    IncrementSession(am, false);
                    var rsp2 = am.Command_Cryptic(req2);
                    response = rsp2.longTextString.textStringDetails;
                    flights = ParseFlights(response, departureDate);
                    resultFlights.AddRange(flights);
                    cntflight = flights.Length;
                    numScrolls++;

                    SerializeXml(rsp2, subdir, "command_cryptic_rsp", am.SessionValue, fc);
                }

                IncrementSession(am, true);
                am.Security_SignOut(new Security_SignOut() { });
                //am.Command_Cryptic(req2);
            }

            if (!string.IsNullOrEmpty(amplitudeApiKey) && !string.IsNullOrEmpty(amplitudeUserId))
            {
                var ampEv = new GetDirectFlightsOnDateEvent()
                {
                    user_id = amplitudeUserId,
                    event_properties = new GetDirectFlightsOnDate_EP()
                    {
                        origin = origin,
                        destination = destination,
                        time_execute = (Environment.TickCount - t1 + 500) / 1000,
                        total_variants = resultFlights.Count,
                        air_multiavail_req_count = numScrolls + 1,
                        gate = pcc,
                    },
                };

                SendAmplitudeEvent(ampEv);
            }

            // выбор оперирующего перевозчика
            foreach (Flight r in resultFlights)
            {
                if (string.IsNullOrEmpty(r.OperatingCarrier) || r.OperatingCarrier == "??")
                {
                    var tmp = resultFlights.FirstOrDefault(x => x.Origin == r.Origin && x.DepartureDateTime == r.DepartureDateTime && !string.IsNullOrEmpty(x.OperatingCarrier) && x.OperatingCarrier != "??");
                    if (tmp != null)
                    {
                        r.OperatingCarrier = tmp.OperatingCarrier;
                    }
                    else
                    {
                        r.OperatingCarrier = r.MarketingCarrier;
                    }
                }
            }

            return resultFlights.ToArray();
        }

        private Flight[] ParseFlights(string response, DateTime dt)
        {
            List<Flight> resultFlights = new List<Flight>();

            List<string> listFlight = response.Split('\n').ToList();
            listFlight.RemoveAt(0);
            listFlight.RemoveAt(0);

            List<int> fordel = new List<int>();
            for (int i = 0; i <= listFlight.Count - 1; i++)
            {
                string s = listFlight[i];
                if (s.IndexOf("PLANNED") >= 0) fordel.Insert(0, i);
                if (s.IndexOf("TILL") >= 0) fordel.Insert(0, i);
                if (s == "A") fordel.Insert(0, i);
            }
            foreach (int i in fordel)
            {
                listFlight.RemoveAt(i);
            }

            List<Flight> suspectList = new List<Flight>();

            bool RealyAddFlight = false;
            bool suspect = false;

            foreach (string firstString in listFlight)
            {
                if (!string.IsNullOrEmpty(firstString.Trim()))
                {
                    var numpp = firstString.Substring(0, 2).Trim();
                    int indexFlight = -1;
                    bool number = int.TryParse(numpp, out indexFlight);

                    if (!string.IsNullOrEmpty(numpp) && number)
                    {
                        Flight f = new Flight();
                        List<string> classes = new List<string>();

                        var aknum = firstString.Substring(2, 9);
                        var operate = aknum.Substring(0, 2).Trim();
                        var symsuspect = aknum.Substring(2, 1);
                        var market = aknum.Substring(3, 2).Trim();
                        var num = aknum.Substring(5, 4).Trim();
                        if (string.IsNullOrEmpty(operate))
                        {
                            operate = "??";
                            if (symsuspect == ":")
                            {
                                suspect = true;
                            }
                        }

                        f.MarketingCarrier = market;
                        f.OperatingCarrier = operate;
                        f.FlightNumber = num.TrimStart('0');

                        bool fltCancelled = false;
                        var strclass = firstString.Substring(11, 22).Trim();
                        if (strclass == "FLT CANCELLED")
                        {
                            fltCancelled = true;
                        }
                        classes.AddRange(RemoveEmptyString(strclass.Split(' ')));

                        var portterminal = firstString.Substring(35, 11);

                        f.Origin = portterminal.Substring(0, 3);
                        f.DepartureTerminal = portterminal.Substring(3, 2).Trim();
                        f.Destination = portterminal.Substring(6, 3);
                        f.ArrivalTerminal = portterminal.Substring(9, 2).Trim();

                        var times = firstString.Substring(47, 15);
                        var t1 = times.Substring(0, 5);
                        var t2 = times.Substring(8);

                        var midday1 = t1.Substring(4, 1);
                        var midday2 = t2.Substring(4, 1);

                        var h1 = int.Parse(t1.Substring(0, 2).Trim());
                        var h2 = int.Parse(t2.Substring(0, 2).Trim());
                        if (midday1 == "P" && h1 < 12) h1 = h1 + 12;
                        if (midday2 == "P" && h2 < 12) h2 = h2 + 12;
                        if (midday1 == "A" && h1 == 12) h1 = 0;
                        if (midday2 == "A" && h2 == 12) h2 = 0;

                        f.DepartureDateTime = new DateTime(dt.Year, dt.Month, dt.Day, h1, int.Parse(t1.Substring(2, 2).Trim()), 0);
                        f.ArrivalDateTime = new DateTime(dt.Year, dt.Month, dt.Day, h2, int.Parse(t2.Substring(2, 2).Trim()), 0);

                        if (t2.IndexOf("+1") >= 0)
                        {
                            f.ArrivalDateTime = f.ArrivalDateTime.AddDays(1);
                        }

                        var equip = firstString.Substring(65, 3);
                        f.Equipment = equip;

                        if (firstString.Length < 75)
                        {
                            fltCancelled = true;
                        }
                        else
                        {
                            var dura = firstString.Substring(74).Trim();
                            string[] dur = dura.Split(':');

                            f.Duration = int.Parse(dur[0]) * 60 + int.Parse(dur[1]);
                        }

                        f.NumSeatsForBookingClass = CorrectNumSeats(classes.ToArray());

                        if (!fltCancelled)
                        {
                            if (suspect)
                            {
                                suspectList.Add(f);
                            }
                            else
                            {
                                resultFlights.Add(f);
                            }
                            RealyAddFlight = true;
                        }
                    }
                    else
                    {
                        if (RealyAddFlight)
                        {
                            Flight last = null;
                            if (!suspect && resultFlights.Count > 0)
                            {
                                last = resultFlights.Last();
                            }
                            else if (suspect && suspectList.Count > 0)
                            {
                                last = suspectList.Last();
                            }
                            List<string> classes = last.NumSeatsForBookingClass.ToList();
                            var classes2 = RemoveEmptyString(firstString.Trim().Split(' '));
                            classes.AddRange(CorrectNumSeats(classes2));
                            last.NumSeatsForBookingClass = classes.ToArray();
                            RealyAddFlight = false;
                            suspect = false;
                        }
                    }
                }
            }

            // Проверка сомнительных рейсов и удаление
            foreach (var f in suspectList)
            {
                //var suspectindex = resultFlights.FindIndex(x => x.FlightNumber == f.FlightNumber && x.DepartureDateTime == f.DepartureDateTime && x.MarketingCarrier == f.MarketingCarrier && x.OperatingCarrier == f.OperatingCarrier && x.Origin == f.Origin);
                var existchange = resultFlights.Exists(x => x.Origin == f.Origin && x.DepartureDateTime == f.DepartureDateTime);
                if (!existchange)
                {
                    resultFlights.Add(f);
                }
            }

            return resultFlights.ToArray();
        }

        private string[] CorrectNumSeats(string[] classes)
        {
            List<string> result = new List<string>();
            foreach (string s in classes)
            {
                if (s.Length == 1)
                {
                    result.Add(s + "0");
                }
                else if (s.Length == 2)
                {
                    string secchar = s[1].ToString();
                    int ii = 0;
                    bool success = int.TryParse(secchar, out ii);
                    if (success)
                    {
                        result.Add(s);
                    }
                    else
                    {
                        result.Add(s[0] + "0");
                    }
                }
            }
            return result.ToArray();
        }
        private string[] RemoveEmptyString(string[] arrstr)
        {
            List<string> result = new List<string>();
            for (int i = 0; i <= arrstr.Length - 1; i++)
            {
                if (!string.IsNullOrEmpty(arrstr[i]))
                {
                    result.Add(arrstr[i]);
                }
            }
            return result.ToArray();
        }

        // ********* GetDateTimeInAirport ***************************************************************
        public DateTime GetDateTimeInAirport(string airport) {
      var req = new Command_Cryptic() {
        messageAction = new Command_CrypticMessageAction() {
          messageFunctionDetails = new Command_CrypticMessageActionMessageFunctionDetails() {
            messageFunction = "M"
          },
        },
        longTextString = new Command_CrypticLongTextString() {
          textStringDetails = "DD " + airport.Trim().ToUpper(),
        },
      };

      string subdir = "GetDateTimeInAirport";
      int fc = SerializeXml(req, subdir, "command_cryptic_req");

      DateTime result;
      using (var am = CreateAWS(null, true)) {
        var rsp = am.Command_Cryptic(req);
        SerializeXml(rsp, subdir, "command_cryptic_rsp", am.SessionValue, fc);

        Match m;
        if (rsp.longTextString == null || rsp.longTextString.textStringDetails == null ||
          !(m = Regex.Match(rsp.longTextString.textStringDetails,
              @" TIME +IS +(\d{4})\/\d{4}[AP] +ON +[A-Z]{3}(\d{2}[A-Z]{3}\d{2})\s+",
              RegexOptions.IgnoreCase)).Success) {

          throw new GDSErrorException("Не удалось получить ожидаемый ответ на команду DD");
        }
        result = DateTime.ParseExact(
          m.Groups[1].Value + m.Groups[2].Value,
          "HHmmddMMMyy",
          new System.Globalization.CultureInfo("en-US"));
      }
      if (!String.IsNullOrEmpty(amplitudeApiKey) && !String.IsNullOrEmpty(amplitudeUserId)) {
        var ampEv = new GetDateTimeInAirportEvent() {
          user_id = amplitudeUserId,
          event_properties = new GetDateTimeInAirport_EP() {
            airport = airport,
          }
        };
        SendAmplitudeEvent(ampEv);
      }
      return result;
    }

    private Fare_MasterPricerTravelBoardSearch MasterPricerRQ(string origin, string destination, DateTime departureDate, string[] allowedCarriers, int radiusKM) {
      var req = new Fare_MasterPricerTravelBoardSearch();

      req.numberOfUnit = new[] { 
          new NumberOfUnitDetailsType_260583C(){
            numberOfUnits="1",
            typeOfUnit="PX",
          },
          new NumberOfUnitDetailsType_260583C(){
            numberOfUnits="200",
            typeOfUnit="RC",// макс количество рекомендаций 
          }
        };

      req.paxReference = new[] {
        new TravellerReferenceInformationType(){
          ptc=new[] {"ADT"},
          traveller=new[] {
            new TravellerDetailsType(){
              @ref="1",
            }
          },
        }
      };

      req.fareOptions = new Fare_MasterPricerTravelBoardSearchFareOptions() {
        pricingTickInfo = new PricingTicketingDetailsType() {
          pricingTicketing = new[] { "RU", "ET", "NSD", "TAC", "RP" },
        },
      };

      req.travelFlightInfo = new TravelFlightInformationType_185853S() {//new TravelFlightInformationType_199258S() {
        cabinId = new CabinIdentificationType_233500C() {
          cabin = new[] { "Y" }
        },
      };

      //req.travelFlightInfo.flightDetail = new[] { "C" }; // с пересадками

      if (allowedCarriers != null && allowedCarriers.Length > 0) {
        req.travelFlightInfo.companyIdentity = new[] {
          new CompanyIdentificationType_233548C() {
            carrierQualifier = "M",
            carrierId = allowedCarriers,
          },
        };
      }

      req.itinerary = new[] {
        new Fare_MasterPricerTravelBoardSearchItinerary() {
          requestedSegmentRef = new OriginAndDestinationRequestType1() {
            segRef = "1",
          },
          departureLocalization = new DepartureLocationType() {
            /*depMultiCity = new[] {
              new MultiCityOptionType() {
                locationId = origin,
              },
            },
            */
            departurePoint = new ArrivalLocationDetailsType_120834C() {
              distance = radiusKM > 0 ? radiusKM.ToString() : null,
              distanceUnit = radiusKM > 0 ? "K" : null,
              locationId = origin,
            },
          },
          arrivalLocalization = new ArrivalLocalizationType() {
            /*arrivalMultiCity = new[] {
              new MultiCityOptionType() {
                locationId = destination,
              },
            }
             */
            arrivalPointDetails = new ArrivalLocationDetailsType() {
              distance = radiusKM > 0 ? radiusKM.ToString() : null,
              distanceUnit = radiusKM > 0 ? "K" : null,
              locationId = destination,
            }
          },
          timeDetails = new DateAndTimeInformationType_181295S() {
            firstDateTimeDetail = new DateAndTimeDetailsTypeI() {
              date = departureDate.ToString("ddMMyy"),
            },
          },

          flightInfo =  new TravelFlightInformationType_165053S() {
              unitNumberDetail = new [] {new NumberOfUnitDetailsTypeI() {
                numberOfUnits = numConnPoints.ToString(),
                typeOfUnit = "C"
              }
            }
          },
/*
          flightInfo= new TravelFlightInformationType_199585S(){
            companyIdentity=new[]{new CompanyIdentificationType_120719C(){
              carrierQualifier="M",
              carrierId=allowedCarriers,
            }}
          }
*/
        }
      };

      return req;
    }
  }

  public class GDSErrorException : Exception {
    public GDSErrorException(string message)
      : base(message) {
    }
  }
}
