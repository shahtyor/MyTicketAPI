using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using SAE.AmadeusPRD;

namespace SAE
{
    public class AmadeusSOAP
    {
        protected readonly string username, passwordBase64, pcc;
        protected readonly string xmlPath;
        protected readonly bool logXml;
        protected readonly bool useProxy;
        protected readonly string urlProxy;
        protected readonly string logProxy;
        protected readonly string parProxy;
        protected static int fileCounter = 0;

        protected AmadeusSOAP(string username, string passwordBase64, string pcc, string xmlPath, bool logXml, bool useProxy, string urlProxy, string logProxy, string parProxy)
        {
            this.username = username;
            this.passwordBase64 = passwordBase64;
            this.pcc = pcc;

            this.xmlPath = xmlPath;
            this.logXml = logXml;
            this.useProxy = useProxy;
            this.urlProxy = urlProxy;
            this.logProxy = logProxy;
            this.parProxy = parProxy;
        }

        static AmadeusSOAP()
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
        }

        protected void SetSoapHeaders(AmadeusWebServices am, bool sessionStart)
        {
            am.mySoapHeaders = new System.Web.Services.Protocols.SoapHeader[] {
          sessionStart? SAE.MySoapHeaders.WSSecurity.CreateForAmadeus(username, Convert.FromBase64String(passwordBase64)):null,
          new SAE.MySoapHeaders.MessageID(){ Value="urn:uuid:" + Guid.NewGuid().ToString()},
          new SAE.MySoapHeaders.To(){Value=am.Url},
          new SAE.MySoapHeaders.Action()
            };
            if (sessionStart)
            {
                am.AMA_SecurityHostedUserValue = new AMA_SecurityHostedUser()
                {
                    UserID = new AMA_SecurityHostedUserUserID()
                    {
                        POS_Type = "1",
                        PseudoCityCode = pcc,
                        AgentDutyCode = "SU",
                        RequestorType = "U",
                    },
                };
            }
            else
            {
                am.AMA_SecurityHostedUserValue = null;
            }
        }

        protected AmadeusWebServices CreateAWS(Session s, bool stateless = false)
        {
            System.Net.WebRequest.DefaultWebProxy = new _DummyProxy();
            System.Net.ServicePointManager.UseNagleAlgorithm = false;

            var am = new AmadeusWebServices();
            if (username == "WSS7TS7T")
                am.Url = "https://nodeD1.production.webservices.amadeus.com/1ASIWS7TS7T";
            else if (username == "WSRUITPA")
                am.Url = "https://noded2.production.webservices.amadeus.com/1ASIWTPARUI";
            else if (username == "WSGTFSTA")
                am.Url = "https://nodeD2.production.webservices.amadeus.com/1ASIWSTAGTF";

            IWebProxy proxyObject;
            proxyObject = new WebProxy(urlProxy, true);
            proxyObject.Credentials = new NetworkCredential(logProxy, parProxy);

            if (useProxy)
            {
                am.Proxy = proxyObject;
            }
            else
            {
                am.Proxy = null;
            }
            am.EnableDecompression = true;
            am.PreAuthenticate = true;

            SetSoapHeaders(am, s == null || stateless);

            if (stateless)
            {
                am.SessionValue = null;
            }
            else if (s == null)
            {
                am.SessionValue = new Session()
                {
                    TransactionStatusCode = "Start",
                };
            }
            else
            {
                am.SessionValue = s;
            }
            return am;
        }

        protected void IncrementSession(AmadeusWebServices am, bool endSession)
        {
            var s = am.SessionValue;
            s.SequenceNumber = (int.Parse(s.SequenceNumber) + 1).ToString();
            s.TransactionStatusCode = endSession ? "End" : "InSeries";
            SetSoapHeaders(am, false);
        }

        protected void SignOut(AmadeusWebServices am, string subDir, int? fc = null)
        {
            IncrementSession(am, true);
            var soRsp = am.Security_SignOut(new Security_SignOut() { });
            //SerializeXml(soRsp, subDir, "signout", fc);
        }

        protected int SerializeXml(object o, string subDir, string fileName, int? fc = null)
        {
            string[] dirParts;
            if (subDir == ".")
            {
                dirParts = new[] { xmlPath, "Amadeus" };
            }
            else
            {
                dirParts = new[] { xmlPath, "Amadeus", subDir };
            }
            subDir = System.IO.Path.Combine(dirParts);
            if (!System.IO.Directory.Exists(subDir))
            {
                System.IO.Directory.CreateDirectory(subDir);
            }

            int _fc = fc ?? unchecked((int)System.Threading.Interlocked.Increment(ref fileCounter));

            if (logXml)
            {
                var srl = new System.Xml.Serialization.XmlSerializer(o.GetType());
                using (var sw = new System.IO.StreamWriter(System.IO.Path.Combine(subDir, fileName + "_" +
                  DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + "_" + _fc + ".xml")))
                {
                    srl.Serialize(sw, o);
                }
            }
            return _fc;
        }

        protected void DeserializeXml<T>(out T o, string path)
        {
            var srl = new System.Xml.Serialization.XmlSerializer(typeof(T));
            using (var sr = new System.IO.StreamReader(path))
            {
                o = (T)srl.Deserialize(sr);
            }
        }
    }

    class _DummyProxy : System.Net.IWebProxy
    {
        public System.Net.ICredentials Credentials
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public Uri GetProxy(Uri destination)
        {
            throw new NotImplementedException();
        }

        public bool IsBypassed(Uri host)
        {
            return true;
        }
    }
}
