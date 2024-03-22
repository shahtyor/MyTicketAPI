using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace SAEKZ {
  [DataContract]
  class AmplitudeEvent {
    [DataMember]
    public string user_id;
    [DataMember]
    protected string event_type;
  }

  [DataContract]
  class GetAirportsForChangesEvent : AmplitudeEvent {
    public GetAirportsForChangesEvent() {
      event_type = "GetAirportsForChanges";
    }
    [DataMember]
    public GetAirportsForChanges_EP event_properties;
  }

  [DataContract]
  class GetDirectFlightsOnDateEvent : AmplitudeEvent {
    public GetDirectFlightsOnDateEvent() {
      event_type = "GetDirectFlightsOnDate";
    }
    [DataMember]
    public GetDirectFlightsOnDate_EP event_properties;
  }
  
  [DataContract]
  class GetDateTimeInAirportEvent : AmplitudeEvent {
    public GetDateTimeInAirportEvent() {
      event_type = "GetDateTimeInAirport";
    }
    [DataMember]
    public GetDateTimeInAirport_EP event_properties;
  }

  [DataContract]
  class EventProperties {
    [DataMember]
    public string origin;
    [DataMember]
    public string destination;
    [DataMember]
    public int time_execute;
  }

  [DataContract]
  class GetAirportsForChanges_EP : EventProperties {
    [DataMember]
    public string[] aircompanies;
    [DataMember]
    public int total_transfer_location;
    [DataMember]
    public int radius;
    [DataMember]
    public int total_from;
    [DataMember]
    public int total_to;
    [DataMember]
    public string gate;
    }

    [DataContract]
  class GetDirectFlightsOnDate_EP : EventProperties {
    [DataMember]
    public int total_variants;
    [DataMember]
    public int air_multiavail_req_count;
    [DataMember]
    public string gate;
  }

  [DataContract]
  class GetDateTimeInAirport_EP {
    [DataMember]
    public string airport;
  }
}
