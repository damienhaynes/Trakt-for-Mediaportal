using System.Runtime.Serialization;

namespace TraktAPI.DataModels
{
  [DataContract]
  public class TraktUserToken : TraktStatus
  {
    [DataMember( Name = "token" )]
    public string Token { get; set; }
  }
}
