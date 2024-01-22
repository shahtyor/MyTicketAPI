using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.IO;



namespace SAE.AmadeusPRD {

  public partial class AmadeusWebServices {

    public System.Web.Services.Protocols.SoapHeader[] mySoapHeaders;

    protected override System.Xml.XmlWriter GetWriterForMessage(System.Web.Services.Protocols.SoapClientMessage message, int bufferSize) {

      if (mySoapHeaders != null) {
        for (int i = 0; i < mySoapHeaders.Length; ++i) {
          if (mySoapHeaders[i] == null) {
            continue;
          }
          if (mySoapHeaders[i] is SAE.MySoapHeaders.Action) {
            ((SAE.MySoapHeaders.Action)mySoapHeaders[i]).Value = message.Action;
          }
          message.Headers.Add(mySoapHeaders[i]);
        }
        mySoapHeaders = null;
      }
      return base.GetWriterForMessage(message, bufferSize);
    }
    
    protected override System.Net.WebRequest GetWebRequest(Uri uri) {
      var webRequest = (System.Net.HttpWebRequest)base.GetWebRequest(uri);
      webRequest.ServicePoint.Expect100Continue = false;
      webRequest.KeepAlive = false;
      return webRequest;
    }
    
  }
}
