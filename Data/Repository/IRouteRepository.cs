using Flights_Project.Models;

namespace Flights_Project.Data.Repository;

public interface IRouteRepository
{
    // Returns a list with all routes in the repository
    List<Route> GetAllRoutes();
    
    // Returns the route with the specified ID, or null if not found
    Route GetRouteById(int routeId);
    
    // Adds a new route to the repository and returns its ID
    int AddRoute(Route route);
    
    // Updates an existing route in the repository and returns true if successful
    bool UpdateRoute(Route route);
    
    // Deletes the route with the specified ID and returns true if successful
    bool DeleteRoute(int routeId);
}