using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;


namespace SAEKZ {
  [Serializable]
  public class Flight {
    /// <summary>
    /// Код аэропорта вылета
    /// </summary>
    public string Origin;
    /// <summary>
    /// Код аэропорта прилета
    /// </summary>
    public string Destination;
    /// <summary>
    /// Номер рейса
    /// </summary>
    public string FlightNumber;
    /// <summary>
    /// Оперирующая авиакомпания
    /// </summary>
    public string OperatingCarrier;
    /// <summary>
    /// Маркетинговая авиакомпания
    /// </summary>
    public string MarketingCarrier;
    /// <summary>
    /// Терминал вылета
    /// </summary>
    public string DepartureTerminal;
    /// <summary>
    /// Терминал прилета
    /// </summary>
    public string ArrivalTerminal;
    /// <summary>
    /// Дата и время вылета
    /// </summary>
    public DateTime DepartureDateTime;
    /// <summary>
    /// Дата и время прилета
    /// </summary>
    public DateTime ArrivalDateTime;
    /// <summary>
    /// Тип самолета
    /// </summary>
    public string Equipment;
    /// <summary>
    /// Время в пути в минутах
    /// </summary>
    public int Duration;
    /// <summary>
    /// Список классов бронирования с количеством мест (L4, K5, M3, …)
    /// </summary>
    public string[] NumSeatsForBookingClass;
  }

  [Serializable]
  public class GetAirportsForChanges_Result {
    public string[] AirportsForChanges;
    public string[] Origins;
    public string[] Destinations;
    internal static GetAirportsForChanges_Result Empty() {
      return new GetAirportsForChanges_Result() {
        AirportsForChanges = new string[0],
        Origins = new string[0],
        Destinations = new string[0],
      };
    }
  }
}
