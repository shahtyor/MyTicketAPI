using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Net;
using System.IO;
using System.Runtime.ConstrainedExecution;

namespace Test {
  class Program {
    static string PwdDigest(byte[] nonce, string created, byte[] pwd) {
      using (var sha1 = new System.Security.Cryptography.SHA1Managed()) {
        var createdba = Encoding.UTF8.GetBytes(created);
        var pwdba = sha1.ComputeHash(pwd);
        var ba = new byte[nonce.Length + createdba.Length + pwdba.Length];

        Array.Copy(nonce, ba, nonce.Length);
        Array.Copy(createdba, 0, ba, nonce.Length, createdba.Length);
        Array.Copy(pwdba, 0, ba, nonce.Length + createdba.Length, pwdba.Length);

        return Convert.ToBase64String(sha1.ComputeHash(ba));
      }
    }
        static void Main(string[] args)
        {
            DateTime start = DateTime.Now;
            //Console.WriteLine(Convert.ToBase64String(Encoding.UTF8.GetBytes("a0Gk8T1=")));
            //Console.WriteLine(Convert.ToBase64String(Encoding.UTF8.GetBytes("TVL2157")));
            //Console.WriteLine(Convert.ToBase64String(Encoding.ASCII.GetBytes("TVL2157")));
            //Console.WriteLine(PwdDigest(Convert.FromBase64String("c2VjcmV0bm9uY2UxMDExMQ=="), "2015-09-30T14:12:15Z", Encoding.UTF8.GetBytes("AMADEUS")));


            //var am = new SAE.Amadeus("WSER1ETM", "TU82dERyIzc=", "KGDR228AQ", "xml", amplitudeApiKey, amplitudeUserId, false);
            var amRU = new SAE.Amadeus("WSS7TS7T", "YTBHazhUMT0=", "MOWR228SG", "xml", amplitudeApiKey, amplitudeUserId, true, false, null, null, null);
            //var amUS = new SAE.Amadeus("WSRUITPA", "NHI/SFJnWDdxWSQj", "ATL1S2157", "xml", amplitudeApiKey, amplitudeUserId, false, true, "http://srv2.globalreservation.com:3128", "staff_airlines", "hZgWt4FrYcnVm9qD");
            var amUS = new SAE.Amadeus("WSRUITPA", "NHI/SFJnWDdxWSQj", "ATL1S2484", "xml", amplitudeApiKey, amplitudeUserId, true, true, "http://srv2.globalreservation.com:3128", "staff_airlines", "hZgWt4FrYcnVm9qD");
            var amKZ = new SAE.Amadeus("WSGTFSTA", "YXMjSjJrUj9hU2c/", "ALAKZ28HZ", "xml", amplitudeApiKey, amplitudeUserId, true, false, null, null, null);
            //am.GetDirectFlightsOnDate("SDU", "CGH", new DateTime(2024, 1, 10));
            //am.GetAirportsForChanges("MOW", "OVB", new DateTime(2019, 12, 8), null, 0);

            //var f0 = AlexAm.GetDirectFlightsOnDate("ORD", "BOS", new DateTime(2023, 7, 12));
            var searchdt = DateTime.Today.AddDays(10);

            /*Console.WriteLine("WSS7TS7T/MOWR228SG");
            Console.WriteLine();

            Console.Write("1. GetDateTimeInAirport(CDG)" + Environment.NewLine + Environment.NewLine);
            try
            {
                var t = amRU.GetDateTimeInAirport("CDG");
                Console.Write(Newtonsoft.Json.JsonConvert.SerializeObject(t));
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message + "..." + ex.StackTrace);
            }

            Console.Write(Environment.NewLine + Environment.NewLine + "2. GetAirportsForChanges(NYC, LAX, " + searchdt.ToString() + ", null, 100)" + Environment.NewLine + Environment.NewLine);
            try
            {
                var p = amRU.GetAirportsForChanges("NYC", "LAX", searchdt, null, 100);
                Console.Write(Newtonsoft.Json.JsonConvert.SerializeObject(p));
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message + "..." + ex.StackTrace);
            }

            Console.Write(Environment.NewLine + Environment.NewLine + "3. GetDirectFlightsOnDate(BER, PAR, " + searchdt.ToString() + ")" + Environment.NewLine + Environment.NewLine);
            try
            {
                var f = amRU.GetDirectFlightsOnDate("BER", "PAR", DateTime.Today.AddDays(10));
                Console.Write(Newtonsoft.Json.JsonConvert.SerializeObject(f));
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message + "..." + ex.StackTrace);
            }

            Console.Write(Environment.NewLine + Environment.NewLine + "4. GetDirectFlightsCryptic(BER, PAR, " + searchdt.ToString() + ")" + Environment.NewLine + Environment.NewLine);
            try
            {
                var s = amRU.GetDirectFlightsCryptic("LON", "MAD", DateTime.Today.AddDays(10));
                Console.Write(Newtonsoft.Json.JsonConvert.SerializeObject(s));
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message + "..." + ex.StackTrace);
            }*/

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("WSRUITPA/ATL1S2157");
            Console.WriteLine();

            /*Console.Write("1. GetDateTimeInAirport(CDG)" + Environment.NewLine + Environment.NewLine);
            try
            {
                var t = amUS.GetDateTimeInAirport("CDG");
                Console.Write(Newtonsoft.Json.JsonConvert.SerializeObject(t));
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message + "..." + ex.StackTrace);
            }

            Console.Write(Environment.NewLine + Environment.NewLine + "2. GetAirportsForChanges(NYC, LAX, " + searchdt.ToString() + ", null, 100)" + Environment.NewLine + Environment.NewLine);
            try
            {
                var p = amUS.GetAirportsForChanges("NYC", "LAX", searchdt, null, 100);
                Console.Write(Newtonsoft.Json.JsonConvert.SerializeObject(p));
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message + "..." + ex.StackTrace);
            }

            Console.Write(Environment.NewLine + Environment.NewLine + "3. GetDirectFlightsOnDate(BER, PAR, " + searchdt.ToString() + ")" + Environment.NewLine + Environment.NewLine);
            try
            {
                var f = amUS.GetDirectFlightsOnDate("BER", "PAR", DateTime.Today.AddDays(10));
                Console.Write(Newtonsoft.Json.JsonConvert.SerializeObject(f));
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message + "..." + ex.StackTrace);
            }*/

            Console.Write(Environment.NewLine + Environment.NewLine + "4. GetDirectFlightsCryptic(BER, PAR, " + searchdt.ToString() + ")" + Environment.NewLine + Environment.NewLine);
            try
            {
                var s = amUS.GetDirectFlightsCryptic("GRU", "LAX", DateTime.Today);
                Console.Write(Newtonsoft.Json.JsonConvert.SerializeObject(s));
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message + "..." + ex.StackTrace);
            }

            /*Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("WSGTFSTA/ALAKZ28HZ");
            Console.WriteLine();*/

            /*Console.Write("1. GetDateTimeInAirport(CDG)" + Environment.NewLine + Environment.NewLine);
            try
            {
                var t = amKZ.GetDateTimeInAirport("CDG");
                Console.Write(Newtonsoft.Json.JsonConvert.SerializeObject(t));
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message + "..." + ex.StackTrace);
            }

            Console.Write(Environment.NewLine + Environment.NewLine + "2. GetAirportsForChanges(NYC, LAX, " + searchdt.ToString() + ", null, 100)" + Environment.NewLine + Environment.NewLine);
            try
            {
                var p = amKZ.GetAirportsForChanges("NYC", "LAX", searchdt, null, 100);
                Console.Write(Newtonsoft.Json.JsonConvert.SerializeObject(p));
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message + "..." + ex.StackTrace);
            }

            Console.Write(Environment.NewLine + Environment.NewLine + "3. GetDirectFlightsOnDate(BER, PAR, " + searchdt.ToString() + ")" + Environment.NewLine + Environment.NewLine);
            try
            {
                var f = amKZ.GetDirectFlightsOnDate("BER", "PAR", DateTime.Today.AddDays(10));
                Console.Write(Newtonsoft.Json.JsonConvert.SerializeObject(f));
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message + "..." + ex.StackTrace);
            }

            Console.Write(Environment.NewLine + Environment.NewLine + "4. GetDirectFlightsCryptic(BER, PAR, " + searchdt.ToString() + ")" + Environment.NewLine + Environment.NewLine);
            try
            {
                var s = amKZ.GetDirectFlightsCryptic("LON", "MAD", DateTime.Today.AddDays(10));
                Console.Write(Newtonsoft.Json.JsonConvert.SerializeObject(s));
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message + "..." + ex.StackTrace);
            }*/

            //Console.WriteLine(f);
            /*string acs = "AC-XK-4N-8T-TS-AS-QX-G4-5T-MO-WX-CX-9M-DE-OU-DL-EW-YB-JB-2L-FI-6H-XE-M5-LG-ND-KS-JV-7H-S4-YR-HI-XO-NK-LX-HV-4T-X3-WS-WF-8P";
            string[] arac = acs.Split('-');
            var f = am.GetAirportsForChanges("YYC", "PHL", new DateTime(2023, 10, 24), arac, 0);
            DateTime end = DateTime.Now;
            int timesec = (int)(end - start).TotalSeconds;*/
            Console.ReadKey();

            //string dt = "1954FRI11JAN19".Insert(7, " "); 
            //DateTime d=DateTime.ParseExact(dt, "HHmmddd ddMMMyy", new System.Globalization.CultureInfo("en-US"));


            /*
                  var ae = new GetDirectFlightsOnDateEvent() {
                    user_id = amplitudeUserId,
                    event_properties = new GetDirectFlightsOnDate_EP() {
                      origin = "MOW",
                      destination = "OVB",
                      time_execute = 23,
                      total_variants = 101,
                      air_multiavail_req_count = 2
                    },
                  };

                  var ae2 = new GetAirportsForChangesEvent() {
                    user_id = amplitudeUserId,
                    event_properties = new GetAirportsForChanges_EP() {
                      origin = "LED",
                      destination = "BER",
                      time_execute = 32,
                      aircompanies = new[] { "SU", "U6", "UN" },
                      total_transfer_location = 111
                    }
                  };

                  SendAmplitudeEvent(ae);
             */
        }

    private static string amplitudeApiKey = "95be547554caecf51c57c691bafb2640", amplitudeUserId = "shahtyor@mail.ru";

    static void SendAmplitudeEvent(AmplitudeEvent ae) {
      string json;
      var ds = new System.Runtime.Serialization.Json.DataContractJsonSerializer(ae.GetType());
      using (var ms = new MemoryStream()) {
        ds.WriteObject(ms, ae);
        json = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Position);
      }

      string url = @"https://api.amplitude.com/httpapi?" +
        "api_key=" + System.Web.HttpUtility.UrlEncode(amplitudeApiKey, Encoding.UTF8) + "&" +
        "event=" + System.Web.HttpUtility.UrlEncode(json, Encoding.UTF8);

      Console.WriteLine(System.Web.HttpUtility.UrlEncode(json, Encoding.UTF8));

      var webReq = (HttpWebRequest)WebRequest.Create(url);
      webReq.Headers.Add("Pragma", "no-cache");
      webReq.Headers.Add("Cache-Control", "no-cache, no-store");
      using (var webRsp = (HttpWebResponse)webReq.GetResponse()) {
        Console.WriteLine(webRsp.StatusCode);
      }

    }

  }

  [DataContract]
  class AmplitudeEvent {
    [DataMember]
    public string user_id;
    [DataMember]
    protected string event_type;
  }

  [DataContract]
  class GetAirportsForChangesEvent : AmplitudeEvent {
    public GetAirportsForChangesEvent() {
      event_type = "GetAirportsForChanges";
    }
    [DataMember]
    public GetAirportsForChanges_EP event_properties;
  }

  [DataContract]
  class GetDirectFlightsOnDateEvent : AmplitudeEvent {
    public GetDirectFlightsOnDateEvent() {
      event_type = "GetDirectFlightsOnDate";
    }
    [DataMember]
    public GetDirectFlightsOnDate_EP event_properties;
  }

  [DataContract]
  public class EventProperties {
    [DataMember]
    public string origin;
    [DataMember]
    public string destination;
    [DataMember]
    public int time_execute;
  }

  [DataContract]
  public class GetAirportsForChanges_EP : EventProperties {
    [DataMember]
    public string[] aircompanies;
    [DataMember]
    public int total_transfer_location;
  }

  [DataContract]
  public class GetDirectFlightsOnDate_EP : EventProperties {
    [DataMember]
    public int total_variants;
    [DataMember]
    public int air_multiavail_req_count;
  }

}
