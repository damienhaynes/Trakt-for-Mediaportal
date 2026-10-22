using System.Collections.Generic;

namespace TraktAPI.DataModels
{
  public class TraktMoviesPopular : TraktPagination
  {
    public IEnumerable<TraktMovieSummary> Movies { get; set; }
  }
}
