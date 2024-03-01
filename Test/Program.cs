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
            //var AlexAmOld = new SAE.Amadeus("WSS7TS7T", "YTBHazhUMT0=", "MOWR228SG", "xml", amplitudeApiKey, amplitudeUserId, false, false, null, null, null);
            var AlexAm = new SAE.Amadeus("WSGTFSTA", "UGJtc1I9eDdZSGtL", "ALAKZ28HZ", "c:\\xmlcrypt", amplitudeApiKey, amplitudeUserId, true, false, null, null, null);
            //var am = new SAE.Amadeus("WSRUITPA", "NHI/SFJnWDdxWSQj", "ATL1S2157", "xml", amplitudeApiKey, amplitudeUserId, false, true, "http://srv2.globalreservation.com:3128", "staff_airlines", "hZgWt4FrYcnVm9qD");
            //var am = new SAE.Amadeus("WSRUITPA", "NHI/SFJnWDdxWSQj", "ATL1S2484", "xml", amplitudeApiKey, amplitudeUserId, false, false, null, null, null);
            //var am = new SAE.Amadeus("WSGTFSTA", "UGJtc1I9eDdZSGtL", "ALAKZ28HZ", "xml", amplitudeApiKey, amplitudeUserId, false, false, null, null, null);
            //am.GetDirectFlightsOnDate("SDU", "CGH", new DateTime(2024, 1, 10));
            //am.GetAirportsForChanges("MOW", "OVB", new DateTime(2019, 12, 8), null, 0);

            var searchdt = DateTime.Today.AddDays(10);

            Console.Write("1. GetDateTimeInAirport(CDG)" + Environment.NewLine + Environment.NewLine);
            var t = AlexAm.GetDateTimeInAirport("CDG");
            Console.Write(Newtonsoft.Json.JsonConvert.SerializeObject(t));

            Console.Write(Environment.NewLine + Environment.NewLine + "2. GetAirportsForChanges(NYC, LAX, " + searchdt.ToString() + ", null, 100)" + Environment.NewLine + Environment.NewLine);
            var p = AlexAm.GetAirportsForChanges("NYC", "LAX", searchdt, null, 100);
            Console.Write(Newtonsoft.Json.JsonConvert.SerializeObject(p));

            Console.Write(Environment.NewLine + Environment.NewLine + "3. GetDirectFlightsOnDate(BER, PAR, " + searchdt.ToString() + ")" + Environment.NewLine + Environment.NewLine);
            var f = AlexAm.GetDirectFlightsOnDate("BER", "PAR", DateTime.Today.AddDays(10));
            Console.Write(Newtonsoft.Json.JsonConvert.SerializeObject(f));

            //var f0 = AlexAm.GetDateTimeInAirport("CDG");
            //var f1 = AlexAm.GetAirportsForChanges("BER", "PAR", new DateTime(2024, 3, 10), null, 100);
            //var f = am.GetDirectFlightsCryptic("BER", "PAR", new DateTime(2024, 3, 10));
            //Console.WriteLine(f0);
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
