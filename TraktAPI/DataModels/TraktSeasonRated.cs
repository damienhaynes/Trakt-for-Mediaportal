using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktAPI.DataModels
{
  public class TraktSeasonsRated : TraktPagination
  {
    public IEnumerable<TraktSeasonRatedItem> Items { get; set; }
  }

  [DataContract]
  public class TraktSeasonRatedItem
  {
    [DataMember( Name = "rating" )]
    public int Rating { get; set; }

    [DataMember( Name = "rated_at" )]
    public string RatedAt { get; set; }

    [DataMember( Name = "show" )]
    public TraktShow Show { get; set; }

    [DataMember( Name = "season" )]
    public TraktSeason Season { get; set; }
  }
}