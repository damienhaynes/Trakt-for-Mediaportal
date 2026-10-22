using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktAPI.DataModels
{
  [DataContract]
  public class TraktMovieSummary : TraktMovie
  {
    [DataMember( Name = "tagline" )]
    public string Tagline { get; set; }

    [DataMember( Name = "overview" )]
    public string Overview { get; set; }

    [DataMember( Name = "released" )]
    public string Released { get; set; }

    [DataMember( Name = "runtime" )]
    public int? Runtime { get; set; }

    [DataMember( Name = "trailer" )]
    public string Trailer { get; set; }

    [DataMember( Name = "updated_at" )]
    public string UpdatedAt { get; set; }

    [DataMember( Name = "homepage" )]
    public string Homepage { get; set; }

    [DataMember( Name = "certification" )]
    public string Certification { get; set; }

    [DataMember( Name = "rating" )]
    public double? Rating { get; set; }

    [DataMember( Name = "votes" )]
    public int Votes { get; set; }

    [DataMember( Name = "country" )]
    public string Country { get; set; }

    [DataMember( Name = "language" )]
    public string Language { get; set; }

    [DataMember( Name = "languages" )]
    public List<string> Languages { get; set; }

    [DataMember( Name = "comment_count" )]
    public int CommentCount { get; set; }

    [DataMember( Name = "available_translations" )]
    public List<string> AvailableTranslations { get; set; }

    [DataMember( Name = "genres" )]
    public List<string> Genres { get; set; }

    [DataMember( Name = "subgenres" )]
    public List<string> SubGenres { get; set; }

    [DataMember( Name = "after_credits" )]
    public bool AfterCredits { get; set; }

    [DataMember( Name = "during_credits" )]
    public bool DuringCredits { get; set; }

    [DataMember( Name = "original_title" )]
    public string OriginalTitle { get; set; }
  }
}
