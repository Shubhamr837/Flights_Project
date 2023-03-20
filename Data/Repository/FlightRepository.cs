using Flights_Project.Models;
using Microsoft.EntityFrameworkCore;

namespace Flights_Project.Data.Repository;

public class FlightRepository : IFlightRepository
{
    private readonly FlightHistoryDBContext _context;
    
    public FlightRepository(FlightHistoryDBContext context)
    {
        _context = context;
    }
    
    public IEnumerable<Flight> GetAllFlights()
    {
        return _context.Set<Flight>().ToList();
    }
    
    public Flight? GetFlightById(int flightId)
    {
        return _context.Set<Flight>().Find(flightId);
    }
    /*
     * Get the list of flights between provide dates.
     * Filter out flights that dont belong to the specified segments
     * Also filter out the flights that belong to the specified agency
     */
    public List<Flight> GetAllFlightsBetweenDates(DateTime startDate, DateTime endDate, int agencyId, List<int> segmentIdList)
    {
        var flights = _context.flights
            .Include(f => f.Route)
            .Where(f => f.DepartureTime >= startDate && f.DepartureTime <= endDate
                                                     && f.AirlineId != agencyId && segmentIdList.Contains(f.Route.SegmentId))
            .OrderBy(f => f.DepartureTime)
            .ToList();
        
        return flights;
    }
    
    public void AddFlight(Flight flight)
    {
        _context.Set<Flight>().Add(flight);
        _context.SaveChanges();
    }
    
    public void UpdateFlight(Flight flight)
    {
        _context.Entry(flight).State = EntityState.Modified;
        _context.SaveChanges();
    }
    
    public void DeleteFlight(int flightId)
    {
        var flight = _context.Set<Flight>().Find(flightId);
        if (flight != null)
        {
            _context.Set<Flight>().Remove(flight);
            _context.SaveChanges();
        }
    }
}