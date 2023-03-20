using Flights_Project.Models;

namespace Flights_Project.Data.Repository;

public interface IFlightRepository
{
    IEnumerable<Flight> GetAllFlights();
    
    Flight? GetFlightById(int flightId);

    List<Flight> GetAllFlightsBetweenDates(DateTime startDate, DateTime endDate, int agencyId, List<int> segmentId);

    void AddFlight(Flight flight);
    
    void UpdateFlight(Flight flight);
    
    void DeleteFlight(int flightId);
}
