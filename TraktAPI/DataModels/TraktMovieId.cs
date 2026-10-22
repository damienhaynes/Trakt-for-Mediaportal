using System.Runtime.Serialization;

namespace TraktAPI.DataModels
{
  [DataContract]
  public class TraktMovieId : TraktId
  {
    [DataMember( Name = "imdb" )]
    public string Imdb { get; set; }

    [DataMember( Name = "tmdb" )]
    public int? Tmdb { get; set; }

    [DataMember( Name = "plex" )]
    public TraktPlexId Plex { get; set; }
  }
}
