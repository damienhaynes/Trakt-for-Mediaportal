using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktAPI.DataModels
{
  public class TraktEpisodesRated : TraktPagination
  {
    public IEnumerable<TraktEpisodeRatedItem> Items { get; set; }
  }

  [DataContract]
  public class TraktEpisodeRatedItem
  {
    [DataMember( Name = "rating" )]
    public int Rating { get; set; }

    [DataMember( Name = "rated_at" )]
    public string RatedAt { get; set; }

    [DataMember( Name = "episode" )]
    public TraktEpisode Episode { get; set; }

    [DataMember( Name = "show" )]
    public TraktShow Show { get; set; }
  }
}
