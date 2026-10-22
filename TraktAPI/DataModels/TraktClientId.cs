using System.Runtime.Serialization;

namespace TraktAPI.DataModels
{
  [DataContract]
  public class TraktClientId
  {
    [DataMember( Name = "client_id" )]
    public string ClientId { get; set; }
  }
}
