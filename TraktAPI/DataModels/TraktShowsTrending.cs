using System.Collections.Generic;

namespace TraktAPI.DataModels
{
  public class TraktShowsTrending : TraktPagination
  {
    public int TotalWatchers { get; set; }
    public IEnumerable<TraktShowTrending> Shows { get; set; }
  }
}
