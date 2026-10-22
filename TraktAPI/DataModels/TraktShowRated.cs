using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktAPI.DataModels
{
  public class TraktShowsRated : TraktPagination
  {
    public IEnumerable<TraktShowRatedItem> Items { get; set; }
  }

  [DataContract]
  public class TraktShowRatedItem
  {
    [DataMember( Name = "rating" )]
    public int Rating { get; set; }

    [DataMember( Name = "rated_at" )]
    public string RatedAt { get; set; }

    [DataMember( Name = "show" )]
    public TraktShow Show { get; set; }
  }
}