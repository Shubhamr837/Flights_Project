using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Flights_Project.Models;

public class Segment {
    [Key]
    [Column("segment_id")]
    public int SegmentId { get; set; }

    [Column("origin_city_id")]
    public int OriginCityId { get; set; }

    [Column("destination_city_id")]
    public int DestinationCityId { get; set; }
}