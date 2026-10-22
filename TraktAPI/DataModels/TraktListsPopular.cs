using System.Collections.Generic;

namespace TraktAPI.DataModels
{
  public class TraktListsPopular : TraktPagination
  {
    public IEnumerable<TraktListPopular> Lists { get; set; }
  }
}
