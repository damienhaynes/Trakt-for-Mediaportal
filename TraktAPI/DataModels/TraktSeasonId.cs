using System.Runtime.Serialization;

namespace TraktAPI.DataModels
{
  [DataContract]
  public class TraktSeasonId
  {
    [DataMember( Name = "trakt" )]
    public int? Trakt { get; set; }

    [DataMember( Name = "tmdb" )]
    public int? Tmdb { get; set; }

    [DataMember( Name = "tvdb" )]
    public int? Tvdb { get; set; }

    [DataMember( Name = "plex" )]
    public TraktPlexId Plex { get; set; }
  }
}
