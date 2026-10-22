using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktAPI.DataModels
{
  public class TraktMovieCollected : TraktPagination
  {
    public IEnumerable<TraktMovieCollectedItem> Items { get; set; }
  }

  [DataContract]
  public class TraktMovieCollectedItem
  {
    [DataMember( Name = "collected_at" )]
    public string CollectedAt { get; set; }

    [DataMember( Name = "movie" )]
    public TraktMovie Movie { get; set; }
  }
}
