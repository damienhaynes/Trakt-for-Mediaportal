using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktAPI.DataModels
{
  public class TraktMoviesFavorited : TraktPagination
  {
    public IEnumerable<TraktMovieFavorited> Movies { get; set; }
  }

  [DataContract]
  public class TraktMovieFavorited
  {
    [DataMember( Name = "user_count" )]
    public int UserCount { get; set; }

    [DataMember( Name = "movie" )]
    public TraktMovieSummary Movie { get; set; }
  }
}
