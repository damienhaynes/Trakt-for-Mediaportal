using System.Runtime.Serialization;

namespace TraktAPI.DataModels
{
  [DataContract]
  public class TraktSharingText
  {
    [DataMember( Name = "watching" )]
    public string Watching { get; set; }

    [DataMember( Name = "watched" )]
    public string Watched { get; set; }
  }
}
