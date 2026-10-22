using System.Collections.Generic;

namespace TraktAPI.DataModels
{
  public class TraktShowsAnticipated : TraktPagination
  {
    public IEnumerable<TraktShowAnticipated> Shows { get; set; }
  }
}
