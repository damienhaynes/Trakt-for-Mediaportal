using System.Collections.Generic;

namespace TraktAPI.DataModels
{
  public class TraktLikes : TraktPagination
  {
    public IEnumerable<TraktLike> Likes { get; set; }
  }
}
