using System.Collections.Generic;

namespace TraktAPI.DataModels
{
  public class TraktHiddenItems : TraktPagination
  {
    public IEnumerable<TraktHiddenItem> HiddenItems { get; set; }
  }
}
