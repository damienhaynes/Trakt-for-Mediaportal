using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktAPI.DataModels
{
  public class TraktShowWatchlist : TraktPagination
  {
    public IEnumerable<TraktShowWatchListItem> Items { get; set; }
  }

  [DataContract]
  public class TraktShowWatchListItem
  {
    [DataMember( Name = "listed_at" )]
    public string ListedAt { get; set; }

    [DataMember( Name = "show" )]
    public TraktShowSummary Show { get; set; }

    [DataMember( Name = "notes" )]
    public string Notes { get; set; }
  }
}
