using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktAPI.DataModels
{
  [DataContract]
  public class TraktSyncMovies
  {
    [DataMember( Name = "movies" )]
    public List<TraktMovie> Movies { get; set; }
  }
}
