using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktAPI.DataModels
{
  public class TraktFavoriteItems : TraktPagination
  {
    public IEnumerable<TraktFavoriteItem> Items { get; set; }
  }

  [DataContract]
  public class TraktFavoriteItem
  {
    [DataMember( Name = "id" )]
    public int? Id { get; set; }

    [DataMember( Name = "rank" )]
    public int? Rank { get; set; }

    [DataMember( Name = "listed_at" )]
    public string ListedAt { get; set; }

    [DataMember( Name = "notes" )]
    public string Notes { get; set; }

    [DataMember( Name = "type" )]
    public string Type { get; set; }

    [DataMember( Name = "movie" )]
    public TraktMovieSummary Movie { get; set; }

    [DataMember( Name = "show" )]
    public TraktShowSummary Show { get; set; }

    //[DataMember( Name = "season" )]
    //public TraktSeasonSummary Season { get; set; }

    //[DataMember( Name = "episode" )]
    //public TraktEpisodeSummary Episode { get; set; }
  }
}
