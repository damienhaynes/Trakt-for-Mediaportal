using System.Runtime.Serialization;

namespace TraktAPI.DataModels
{
  [DataContract]
  public class TraktPlexId
  {
    [DataMember( Name = "guid" )]
    public string Guid { get; set; }

    [DataMember( Name = "slug" )]
    public string Slug { get; set; }
  }
}
