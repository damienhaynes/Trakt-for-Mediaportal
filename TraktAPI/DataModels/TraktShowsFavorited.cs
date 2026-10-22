using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktAPI.DataModels
{
  public class TraktShowsFavorited : TraktPagination
  {
    public IEnumerable<TraktShowFavorited> Shows { get; set; }
  }

  [DataContract]
  public class TraktShowFavorited
  {
    [DataMember( Name = "user_count" )]
    public int UserCount { get; set; }

    [DataMember( Name = "show" )]
    public TraktShowSummary Show { get; set; }
  }
}
