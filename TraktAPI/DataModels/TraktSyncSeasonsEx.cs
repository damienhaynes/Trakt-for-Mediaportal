using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktAPI.DataModels
{
  [DataContract]
  public class TraktSyncSeasonsEx
  {
    [DataMember( Name = "shows" )]
    public List<TraktSyncSeasonEx> Shows { get; set; }
  }
}
