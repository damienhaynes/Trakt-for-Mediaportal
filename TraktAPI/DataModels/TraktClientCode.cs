using System.Runtime.Serialization;

namespace TraktAPI.DataModels
{
  [DataContract]
  public class TraktClientCode : TraktClientId
  {
    [DataMember( Name = "code" )]
    public string Code { get; set; }

    [DataMember( Name = "client_secret" )]
    public string ClientSecret { get; set; }
  }
}
