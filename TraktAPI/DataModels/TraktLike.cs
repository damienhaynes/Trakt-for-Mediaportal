using System;
using System.Runtime.Serialization;

namespace TraktAPI.DataModels
{
  [DataContract]
  public class TraktLike : IEquatable<TraktLike>
  {
    [DataMember( Name = "liked_at" )]
    public string LikedAt { get; set; }

    [DataMember( Name = "type" )]
    public string Type { get; set; }

    [DataMember( Name = "list" )]
    public TraktListDetail List { get; set; }

    [DataMember( Name = "comment" )]
    public TraktComment Comment { get; set; }

    #region IEquatable
    public bool Equals( TraktLike other )
    {
      if ( other == null || ( other.Comment == null && other.Type == "comment" ) || ( other.List == null && other.Type == "list" ) )
        return false;

      if ( this.Type == "list" )
      {
        if ( this.List.Id == null || other.List.Id == null )
          return false;

        return ( this.Type == other.Type && this.List.Id == other.List.Id );
      }
      else
      {
        return ( this.Type == other.Type && this.Comment.Id == other.Comment.Id );
      }
    }

    public override int GetHashCode()
    {
      if ( this.Type == "list" )
      {
        return ( this.List.Id.ToString() + "_" + this.Type ).GetHashCode();
      }
      else
      {
        return ( this.Comment.Id.ToString() + "_" + this.Type ).GetHashCode();
      }
    }
    #endregion
  }
}
