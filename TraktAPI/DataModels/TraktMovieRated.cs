using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktAPI.DataModels
{
  public class TraktMoviesRated : TraktPagination
  {
    public IEnumerable<TraktMovieRatedItem> Items { get; set; }
  }

  [DataContract]
  public class TraktMovieRatedItem
  {
    [DataMember( Name = "rating" )]
    public int Rating { get; set; }

    [DataMember( Name = "rated_at" )]
    public string RatedAt { get; set; }

    [DataMember( Name = "movie" )]
    public TraktMovie Movie { get; set; }
  }
}
