using System.Collections.Generic;

namespace TraktAPI.DataModels
{
  public class TraktShowsUpdated : TraktPagination
  {
    public IEnumerable<TraktShowUpdate> Shows { get; set; }
  }
}
