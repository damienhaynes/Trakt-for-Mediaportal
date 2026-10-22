using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktAPI.DataModels
{
  public class TraktEpisodeWatchList : TraktPagination
  {
    public IEnumerable<TraktEpisodeWatchListItem> Items { get; set; }
  }

  [DataContract]
  public class TraktEpisodeWatchListItem : TraktEpisodeSummaryEx
  {
    [DataMember( Name = "listed_at" )]
    public string ListedAt { get; set; }

    [DataMember( Name = "notes" )]
    public string Notes { get; set; }
  }
}
