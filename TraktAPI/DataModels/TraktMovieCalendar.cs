using System.Runtime.Serialization;

namespace TraktAPI.DataModels
{
  [DataContract]
  public class TraktMovieCalendar
  {
    [DataMember( Name = "released" )]
    public string Released { get; set; }

    [DataMember( Name = "movie" )]
    public TraktMovieSummary Movie { get; set; }
  }
}
