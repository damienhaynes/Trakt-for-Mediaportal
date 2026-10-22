using System.Runtime.Serialization;

namespace TraktAPI.DataModels
{
  [DataContract]
  public class TraktLastSyncActivities
  {
    [DataContract]
    public class Activities
    {
      [DataMember( Name = "updated_at" )]
      public string UpdatedAt { get; set; }
    }

    [DataMember( Name = "all" )]
    public string All { get; set; }

    [DataMember( Name = "movies" )]
    public MovieActivities Movies { get; set; }

    [DataContract]
    public class MovieActivities
    {
      [DataMember( Name = "watched_at" )]
      public string Watched { get; set; }

      [DataMember( Name = "collected_at" )]
      public string Collection { get; set; }

      [DataMember( Name = "rated_at" )]
      public string Rating { get; set; }

      [DataMember( Name = "watchlisted_at" )]
      public string Watchlist { get; set; }

      [DataMember( Name = "commented_at" )]
      public string Comment { get; set; }

      [DataMember( Name = "paused_at" )]
      public string PausedAt { get; set; }

      [DataMember( Name = "recommendations_at" )]
      public string RecommendationsAt { get; set; }

      [DataMember( Name = "favorited_at" )]
      public string FavoritedAt { get; set; }

      [DataMember( Name = "hidden_at" )]
      public string HiddenAt { get; set; }
    }

    [DataMember( Name = "episodes" )]
    public EpisodeActivities Episodes { get; set; }

    [DataContract]
    public class EpisodeActivities
    {
      [DataMember( Name = "watched_at" )]
      public string Watched { get; set; }

      [DataMember( Name = "collected_at" )]
      public string Collection { get; set; }

      [DataMember( Name = "rated_at" )]
      public string Rating { get; set; }

      [DataMember( Name = "watchlisted_at" )]
      public string Watchlist { get; set; }

      [DataMember( Name = "commented_at" )]
      public string Comment { get; set; }

      [DataMember( Name = "paused_at" )]
      public string PausedAt { get; set; }

      [DataMember( Name = "favorited_at" )]
      public string FavoritedAt { get; set; }
    }

    [DataMember( Name = "shows" )]
    public ShowActivities Shows { get; set; }

    [DataContract]
    public class ShowActivities
    {
      [DataMember( Name = "rated_at" )]
      public string Rating { get; set; }

      [DataMember( Name = "watchlisted_at" )]
      public string Watchlist { get; set; }

      [DataMember( Name = "commented_at" )]
      public string Comment { get; set; }

      [DataMember( Name = "hidden_at" )]
      public string HiddenAt { get; set; }

      [DataMember( Name = "favorited_at" )]
      public string FavoritedAt { get; set; }

      [DataMember( Name = "dropped_at" )]
      public string DroppedAt { get; set; }

      [DataMember( Name = "recommendations_at" )]
      public string RecommendationsAt { get; set; }
    }

    [DataMember( Name = "seasons" )]
    public SeasonActivities Seasons { get; set; }

    [DataContract]
    public class SeasonActivities
    {
      [DataMember( Name = "rated_at" )]
      public string Rating { get; set; }

      [DataMember( Name = "watchlisted_at" )]
      public string Watchlist { get; set; }

      [DataMember( Name = "commented_at" )]
      public string Comment { get; set; }

      [DataMember( Name = "hidden_at" )]
      public string HiddenAt { get; set; }

      [DataMember( Name = "favorited_at" )]
      public string FavoritedAt { get; set; }
    }

    [DataMember( Name = "comments" )]
    public CommentActivities Comments { get; set; }

    [DataContract]
    public class CommentActivities
    {
      [DataMember( Name = "liked_at" )]
      public string LikedAt { get; set; }

      [DataMember( Name = "reacted_at" )]
      public string ReactedAt { get; set; }

      [DataMember( Name = "blocked_at" )]
      public string BlockedAt { get; set; }
    }

    [DataMember( Name = "lists" )]
    public ListActivities Lists { get; set; }

    [DataContract]
    public class ListActivities : Activities
    {
      [DataMember( Name = "liked_at" )]
      public string LikedAt { get; set; }

      [DataMember( Name = "commented_at" )]
      public string Comment { get; set; }

      [DataMember( Name = "reacted_at" )]
      public string ReactedAt { get; set; }
    }

    [DataMember( Name = "watchlist" )]
    public Activities Watchlist { get; set; }

    [DataMember( Name = "favorites" )]
    public Activities Favorites { get; set; }

    [DataMember( Name = "recommendations" )]
    public Activities Recommendations { get; set; }

    [DataMember( Name = "collaborations" )]
    public Activities Collaborations { get; set; }

    [DataMember( Name = "saved_filters" )]
    public Activities SavedFilters { get; set; }

    [DataMember( Name = "notes" )]
    public Activities Notes { get; set; }

    [DataMember( Name = "account" )]
    public AccountActivities Accounts { get; set; }

    [DataContract]
    public class AccountActivities
    {
      [DataMember( Name = "settings_at" )]
      public string SettingsAt { get; set; }

      [DataMember( Name = "followed_at" )]
      public string FollowedAt { get; set; }

      [DataMember( Name = "following_at" )]
      public string FollowingAt { get; set; }

      [DataMember( Name = "pending_at" )]
      public string PendingAt { get; set; }

      [DataMember( Name = "requested_at" )]
      public string RequestedAt { get; set; }
    }
  }
}
