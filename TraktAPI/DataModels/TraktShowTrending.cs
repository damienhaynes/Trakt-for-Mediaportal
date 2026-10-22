using System.Runtime.Serialization;

namespace TraktAPI.DataModels
{
  [DataContract]
  public class TraktShowTrending
  {
    [DataMember( Name = "watchers" )]
    public int Watchers { get; set; }

    [DataMember( Name = "show" )]
    public TraktShowSummary Show { get; set; }
  }
}