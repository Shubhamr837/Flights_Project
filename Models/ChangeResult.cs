namespace Flights_Project.Models;

public class ChangeResult
{
    public ChangeResult(int flightId, int originCityId, int destinationCityId, DateTime departureTime, DateTime arrivalTime, int airlineId, Status flightStatus)
    {
        FlightId = flightId;
        OriginCityId = originCityId;
        DestinationCityId = destinationCityId;
        DepartureTime = departureTime;
        ArrivalTime = arrivalTime;
        AirlineId = airlineId;
        FlightStatus = flightStatus;
    }

    public int FlightId { get; set; }
    public int OriginCityId { get; set; }
    public int DestinationCityId { get; set; }
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public int AirlineId { get; set; }
    public Status FlightStatus { get; set; }

    public enum Status
    {
        NEW,
        DISCONTINUED
    }
}