using Flights_Project.Models;

namespace Flights_Project.Data.Repository;

public class RouteRepository : IRouteRepository
{
    private List<Route> _routes = new List<Route>();
    
    public List<Route> GetAllRoutes()
    {
        return _routes;
    }
    
    public Route GetRouteById(int routeId)
    {
        return _routes.FirstOrDefault(r => r.RouteId == routeId);
    }
    
    public int AddRoute(Route route)
    {
        int newId = _routes.Count > 0 ? _routes.Max(r => r.RouteId) + 1 : 1;
        route.RouteId = newId;
        _routes.Add(route);
        return newId;
    }
    
    public bool UpdateRoute(Route route)
    {
        Route existingRoute = _routes.FirstOrDefault(r => r.RouteId == route.RouteId);
        if (existingRoute != null)
        {
            _routes.Remove(existingRoute);
            _routes.Add(route);
            return true;
        }
        else
        {
            return false;
        }
    }
    
    public bool DeleteRoute(int routeId)
    {
        Route routeToDelete = _routes.FirstOrDefault(r => r.RouteId == routeId);
        if (routeToDelete != null)
        {
            _routes.Remove(routeToDelete);
            return true;
        }
        else
        {
            return false;
        }
    }
}
