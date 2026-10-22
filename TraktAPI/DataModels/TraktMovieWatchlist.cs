using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktAPI.DataModels
{
  public class TraktMovieWatchlist : TraktPagination
  {
    public IEnumerable<TraktMovieWatchListItem> Items { get; set; }
  }

  [DataContract]
  public class TraktMovieWatchListItem
  {
    [DataMember( Name = "listed_at" )]
    public string ListedAt { get; set; }

    [DataMember( Name = "movie" )]
    public TraktMovieSummary Movie { get; set; }

    [DataMember( Name = "notes" )]
    public string Notes { get; set; }
  }
}
