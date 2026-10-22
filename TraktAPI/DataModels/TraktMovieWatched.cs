using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktAPI.DataModels
{
  public class TraktMoviesWatched : TraktPagination
  {
    public IEnumerable<TraktMovieWatchedItem> Items { get; set; }
  }

  [DataContract]
  public class TraktMovieWatchedItem
  {
    [DataMember( Name = "plays" )]
    public int Plays { get; set; }

    [DataMember( Name = "last_watched_at" )]
    public string LastWatchedAt { get; set; }

    [DataMember( Name = "movie" )]
    public TraktMovie Movie { get; set; }
  }
}
