using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace SAEKZ.MySoapHeaders {
  [Serializable]
  [XmlRoot(Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd", IsNullable = false, ElementName = "Security")]
  public partial class WSSecurity : System.Web.Services.Protocols.SoapHeader {

    public UsernameTokenType UsernameToken;

    [XmlNamespaceDeclarations]
    public XmlSerializerNamespaces namespaces;

    private void Init() {
      MustUnderstand = true;
      namespaces = new XmlSerializerNamespaces();
      namespaces.Add("wsse", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd");
      namespaces.Add("wsu", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");
    }
    public WSSecurity() {
      Init();
    }
        /// <summary>
        /// Создает WSSecurity при этом вычисляя диджест пароля по принципу HashedPassword = Base64 ( SHA-1 ( nonce + created + SHA-1 ( password ) ) )
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static WSSecurity CreateForAmadeus(string username, byte[] password)
        {
            var ws = new WSSecurity();
            var created = DateTime.Now.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'");
            //var created = "2023-01-29T04:48:42:628Z";
            var nonce = Guid.NewGuid().ToByteArray();
            //var nonce = Encoding.UTF8.GetBytes("ABCDEFGHIJKLMNOP");
            var pwdDgst = PwdDigest(nonce, created, password);
            string p = Encoding.UTF8.GetString(password);
            //string p2 = Encoding.UTF8.GetString(Convert.FromBase64String(p));

            ws.UsernameToken = new UsernameTokenType()
            {
                Username = username,
                Password = new PasswordType()
                {
                    Value = pwdDgst,
                },
                Nonce = new NonceType()
                {
                    Value = Convert.ToBase64String(nonce),
                },
                Created = created,
            };
            return ws;
        }
    /*
    public WSSecurity(string username, byte[] password) {
      Init();

      var created = DateTime.Now.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssZ");
      var nonce = Guid.NewGuid().ToByteArray();
      var pwdDgst = PwdDigest(nonce, created, password);

      this.UsernameToken = new UsernameTokenType() {
        Username = username,
        Password = new PasswordType() {
          Value = pwdDgst,
        },
        Nonce = new NonceType() {
          Value = Convert.ToBase64String(nonce),
        },
        Created = created,
      };
    }
    */
    [Serializable]
    public class UsernameTokenType {
      public string Username;
      public PasswordType Password;
      public NonceType Nonce;
      [XmlElementAttribute(Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd")]
      public string Created;
    }

    [Serializable]
    public class NonceType {
      public NonceType() {
        this.EncodingType = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary";
      }
      [XmlAttribute]
      public string EncodingType;
      [XmlText]
      public string Value;
    }

    [Serializable]
    public class PasswordType {
      public PasswordType() {
        this.Type = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordDigest";
      }
      [XmlAttribute]
      public string Type;
      [XmlText]
      public string Value;
    }
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

    /*
    private string PwdDigest(byte[] nonce, string created, string pwd) {
      /////
      // http://stackoverflow.com/questions/19438000/working-algorithm-for-passworddigest-in-ws-security
      /////
      var createdba = Encoding.UTF8.GetBytes(created);
      var pwdba = Encoding.UTF8.GetBytes(pwd);
      var ba = new byte[nonce.Length + createdba.Length + pwdba.Length];


      Array.Copy(nonce, ba, nonce.Length);
      Array.Copy(createdba, 0, ba, nonce.Length, createdba.Length);
      Array.Copy(pwdba, 0, ba, nonce.Length + createdba.Length, pwdba.Length);

      using (var sha1 = new System.Security.Cryptography.SHA1Managed()) {
        return Convert.ToBase64String(sha1.ComputeHash(ba));
      }
    }
     */
  }

  [Serializable]
  [XmlRoot(Namespace = "http://www.w3.org/2005/08/addressing", IsNullable = false)]
  public partial class MessageID : System.Web.Services.Protocols.SoapHeader {
    [XmlText]
    public string Value;
  }

  [Serializable]
  [XmlRoot(Namespace = "http://www.w3.org/2005/08/addressing", IsNullable = false)]
  public partial class To : System.Web.Services.Protocols.SoapHeader {
    [XmlText]
    public string Value;
  }

  [Serializable]
  [XmlRoot(Namespace = "http://www.w3.org/2005/08/addressing", IsNullable = false)]
  public partial class Action : System.Web.Services.Protocols.SoapHeader {
    [XmlText]
    public string Value;
  }
}
