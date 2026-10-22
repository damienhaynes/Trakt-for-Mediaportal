using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktAPI.DataModels
{
  public class TraktSeasonWatchList : TraktPagination
  {
    public IEnumerable<TraktSeasonWatchListItem> Items { get; set; }
  }

  [DataContract]
  public class TraktSeasonWatchListItem
  {
    [DataMember( Name = "listed_at" )]
    public string ListedAt { get; set; }

    [DataMember( Name = "show" )]
    public TraktShowSummary Show { get; set; }

    [DataMember( Name = "season" )]
    public TraktSeasonSummary Season { get; set; }
  }
}
