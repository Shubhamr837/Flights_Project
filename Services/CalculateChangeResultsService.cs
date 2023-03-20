using Flights_Project.Data.Repository;
using Flights_Project.Models;

using CsvHelper;

namespace Flights_Project.Services;

public class CalculateChangeResultsService : IGenerateChangeResultsService
{
    private readonly IFlightRepository _flightRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;

    public CalculateChangeResultsService(IFlightRepository flightRepository,ISubscriptionRepository subscriptionRepository)
    {
        _flightRepository = flightRepository;
        _subscriptionRepository = subscriptionRepository;
    }

    private List<int> GetSegmentsForAnAgency(int agencyId)
    {
         var subscriptions = _subscriptionRepository.GetSegmentsForAgency(agencyId);
         return subscriptions.ToList();
    }
    public void GenerateResultsCsvForDates(DateTime startDate, DateTime endDate, int agencyId)
    {
        TimeSpan daysSpan = endDate - startDate;
        
        // This variable stores the total number of days between between the given dates. 
        int numDays = (int)daysSpan.TotalDays + 1;
        List<ChangeResult> results = new List<ChangeResult>();
        
        Dictionary<int, List<Flight>>[] arrayOfDictionaries = new Dictionary<int, List<Flight>>[numDays];
        
        for (int i = 0; i < numDays; i++)
        {
            arrayOfDictionaries[i] = new Dictionary<int, List<Flight>>();
        }
        /*
         * This is the segments in which the given agency is subscriber to.
         * A segment is a unique pair of origin and destination.
         */
        var segments = GetSegmentsForAnAgency(agencyId);
        
        /*
         * This is the flights between the dates that belong to the specified segments.
         * We also filter out the flights that belong to the same airline agency
         */
        var flights = _flightRepository.GetAllFlightsBetweenDates(startDate, endDate, agencyId, segments);
        for (int i = 0; i < flights.Count; i++)
        {
            Flight flight = flights[i];
            TimeSpan span = flight.DepartureTime - startDate;
            int day = (int)span.TotalDays;
            
            if(!arrayOfDictionaries[day].ContainsKey(flight.DepartureTime.Hour))
            {
                List<Flight> flightsInDay = new List<Flight>();
                arrayOfDictionaries[day].Add(flight.DepartureTime.Hour,flightsInDay);
            }
            arrayOfDictionaries[day][flight.DepartureTime.Hour].Add(flight);
        }

        /*
         * 
         */
        for (int i = 0; i < flights.Count; i++)
        {
            Flight flight = flights[i];
            TimeSpan span = flight.DepartureTime - startDate;
            int day = (int)span.TotalDays;
            int offset = 7;
            
            /*
             * New flights
             */
            if (day - offset >= 0)
            {
                int offsetDay = day - 7;
                List<Flight>? offsetNextHour = null;
                List<Flight>? offsetPreviousHour =  null;
                if (flight.DepartureTime.Hour == 23 && arrayOfDictionaries[offsetDay+1].ContainsKey(0))
                {
                    offsetNextHour = arrayOfDictionaries[offsetDay + 1][0];
                }
                if (flight.DepartureTime.Hour == 0 && offsetDay-1 >= 0 && arrayOfDictionaries[offsetDay-1].ContainsKey(23))
                {
                    offsetPreviousHour = arrayOfDictionaries[offsetDay - 1][23];
                }

                ChangeResult? changeResult = calculateChangeStatus(flight,arrayOfDictionaries[offsetDay][flight.DepartureTime.Hour],
                    offsetNextHour, offsetPreviousHour, -offset);
                if (changeResult != null)
                {
                    results.Add(changeResult);
                }
            }
            
            /*
             * Discontinued Flights
             */
            if (day + offset < arrayOfDictionaries.Length)
            {
                int offsetDay = day + 7;
                List<Flight>? offsetNextHour = null;
                List<Flight>? offsetPreviousHour =  null;
                List<Flight>? offsetHour = null;
                if (flight.DepartureTime.Hour == 23 && offsetDay+1<numDays && arrayOfDictionaries[offsetDay+1].ContainsKey(0))
                {
                    offsetNextHour = arrayOfDictionaries[offsetDay + 1][0];
                }
                if (flight.DepartureTime.Hour == 0 && arrayOfDictionaries[offsetDay-1].ContainsKey(23))
                {
                    offsetPreviousHour = arrayOfDictionaries[offsetDay - 1][23];
                }

                if (arrayOfDictionaries[offsetDay].ContainsKey(flight.DepartureTime.Hour))
                {
                    offsetHour = arrayOfDictionaries[offsetDay][flight.DepartureTime.Hour];
                }
                ChangeResult? changeResult = calculateChangeStatus(flight,offsetNextHour,
                    offsetNextHour, offsetPreviousHour, offset);
                if (changeResult != null)
                {
                    results.Add(changeResult);
                }
            }
        }
        
        WriteResultsToCsv(results);
    }

    public ChangeResult? calculateChangeStatus(Flight flight, List<Flight>? offsetHour,List<Flight>? offsetNextHour,List<Flight>? offsetPreviousHour,int offset)
    {
        /*
         * If the departure time is in the second half of the hour then for 30 minute
         * tolerance we need to look at 7 days back data for that hour and the next hour
         * Else we need to look at 7 days back data for that hour and the previous hour
         *
         * Both cases we have to look at data for the same hour.
         * So we first loop through the same hour and then for the corresponding
         * next or previous hour 7 days back.
         */

        if (offsetHour != null)
        {
            foreach (var offsetFlight in offsetHour)
            {
                TimeSpan timeDiff = (flight.DepartureTime - offsetFlight.DepartureTime).Duration();
                
                // Adding (offset*24*60) to remove the time difference of 7 days and check if time difference is less than 30 minutes
                if (Math.Abs(timeDiff.TotalMinutes+(offset*24*60)) < 30 && flight.AirlineId == offsetFlight.AirlineId &&
                    flight.Route.SegmentId == offsetFlight.Route.SegmentId)
                {
                    /*
                     * As the corresponding flight is found we invalidate the data by setting it to DateTime.MinValue
                     * so that it doesn't become the corresponding flight from some other flight in same time with same segment
                     * As we have found the corresponding flight we return null as there is no change
                    */
                    offsetFlight.DepartureTime = DateTime.MinValue;
                    return null;
                }
            }
        }


        if ( flight.DepartureTime.Minute>=30 && offsetNextHour != null)
        {
                foreach( var offsetFlight in offsetNextHour)
                {
                    TimeSpan timeDiff = (flight.DepartureTime - offsetFlight.DepartureTime).Duration();
                    if (Math.Abs(timeDiff.TotalMinutes+(offset*24*60)) < 30 && flight.AirlineId == offsetFlight.AirlineId && flight.Route.SegmentId == offsetFlight.Route.SegmentId)
                    {
                        /*
                         * As the corresponding flight is found we invalidate the data by setting it to DateTime.MinValue
                         * so that it doesn't become the corresponding flight from some other flight in same time with same segment
                         * As we have found the corresponding flight we return null as there is no change
                        */
                        offsetFlight.DepartureTime = DateTime.MinValue;
                        return null;
                    }
                }
        }
        else if ( offsetPreviousHour != null )
        {
            foreach( var offsetFlight in offsetPreviousHour)
            {
                TimeSpan timeDiff = (flight.DepartureTime - offsetFlight.DepartureTime).Duration();
                if (Math.Abs(timeDiff.TotalMinutes+(offset*24*60)) < 30 && flight.AirlineId == offsetFlight.AirlineId && flight.Route.SegmentId == offsetFlight.Route.SegmentId)
                {
                    /*
                     * As the corresponding flight is found we invalidate the data by setting it to DateTime.MinValue
                     * so that it doesn't become the corresponding flight from some other flight in same time with same segment
                     * As we have found the corresponding flight we return null as there is no change
                    */
                    offsetFlight.DepartureTime = DateTime.MinValue;
                    return null;
                }
            }
        }
        // Change result is one row of the results.csv file
        return new ChangeResult(flight.FlightId,
            flight.Route.OriginCityId,
            flight.Route.DestinationCityId,
            flight.DepartureTime,
            flight.ArrivalTime,
            flight.AirlineId,
            offset>0?ChangeResult.Status.DISCONTINUED:ChangeResult.Status.NEW);
            
    }
    
    /*
     * This function takes the list of rows to be added to the results.csv file and adds them accordingly
     */
    public void WriteResultsToCsv(List<ChangeResult> results)
    {
        try
        {
            using var writer = new StreamWriter("results.csv");
            using var csv = new CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture);
            csv.WriteRecords(results);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing results to CSV file: {ex.Message}");
        }
    }

}