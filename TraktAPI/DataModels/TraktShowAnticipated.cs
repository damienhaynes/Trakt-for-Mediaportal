using System.Runtime.Serialization;

namespace TraktAPI.DataModels
{
  [DataContract]
  public class TraktShowAnticipated
  {
    [DataMember( Name = "list_count" )]
    public int ListCount { get; set; }

    [DataMember( Name = "show" )]
    public TraktShowSummary Show { get; set; }
  }
}