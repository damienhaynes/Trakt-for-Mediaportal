using System.Collections.Generic;

namespace TraktAPI.DataModels
{
  public class TraktMoviesAnticipated : TraktPagination
  {
    public IEnumerable<TraktMovieAnticipated> Movies { get; set; }
  }
}
