using System.Runtime.Serialization;

namespace TraktAPI.DataModels
{
  [DataContract]
  public class TraktUserId
  {
    [DataMember( Name = "slug" )]
    public string Slug { get; set; }
  }
}
