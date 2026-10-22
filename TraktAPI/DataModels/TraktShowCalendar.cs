using System.Runtime.Serialization;

namespace TraktAPI.DataModels
{
  [DataContract]
  public class TraktShowCalendar : TraktEpisodeSummaryEx
  {
    [DataMember( Name = "first_aired" )]
    public string AirsAt { get; set; }
  }
}
