using System.Collections.Generic;

namespace TraktAPI.DataModels
{
  public class TraktListsTrending : TraktPagination
  {
    public IEnumerable<TraktListTrending> Lists { get; set; }
  }
}
