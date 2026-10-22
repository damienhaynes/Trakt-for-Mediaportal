using System.Collections.Generic;

namespace TraktAPI.DataModels
{
  public class TraktShowsPopular : TraktPagination
  {
    public IEnumerable<TraktShowSummary> Shows { get; set; }
  }
}
