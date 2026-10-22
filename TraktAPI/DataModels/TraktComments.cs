using System.Collections.Generic;

namespace TraktAPI.DataModels
{
  public class TraktComments : TraktPagination
  {
    public IEnumerable<TraktCommentItem> Comments { get; set; }
  }
}
