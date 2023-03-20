using System.ComponentModel.DataAnnotations.Schema;

namespace Flights_Project.Models;

public class Route
{
    [Column("route_id")]
    public int RouteId { get; set; }
    
    [Column("origin_city_id")]
    public int OriginCityId { get; set; }
    
    [Column("destination_city_id")]
    public int DestinationCityId { get; set; }
    
    [Column("departure_date")]
    public DateTime DepartureDate { get; set; }
    
    [Column("segment_id")]
    public int SegmentId { get; set; }
    
    public Segment Segment { get; set; }
    public List<Flight> Flights { get; set; }
}
