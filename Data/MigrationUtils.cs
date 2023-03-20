using System.Diagnostics;
using Flights_Project.Models;
using Npgsql;

namespace Flights_Project.Data;

public class MigrationUtils
{
    public static string UPDATE_SEGMENT_ID =
        "UPDATE Flights SET segment_id = routes.segment_id,  origin_city_id = routes.origin_city_id, destination_city_id = routes.destination_city_id FROM routes WHERE flights.route_id = routes.route_id;";
    
    private static Dictionary<KeyValuePair<int, int>, int> segments = new();
    private static int _segmentCount = 0;
 
    /*
     * Function that populates the data from the csv files path.
     * It expects the csv files path to be a folder containing all the required CSV files.
     * It calls the populate functions in the required order to maintain all foreign key constrains
     */
    public static void ImportCSVToPostgreSQL(string csvFilesPath, string connectionString)
    {
        // Check if the CSV file exists
        if (!File.Exists(csvFilesPath+ "routes.csv")||!File.Exists(csvFilesPath+ "flights.csv")||!File.Exists(csvFilesPath+ "subscriptions.csv"))
        {
            throw new ArgumentException("CSV file does not exist");
        }
        
        var connection = new NpgsqlConnection(connectionString);
        CreateTables(connection);

        
        var stopwatch = new Stopwatch();

        stopwatch.Start();
        PopulateRouteTable(csvFilesPath+"routes.csv",connectionString);
        stopwatch.Stop();
        Console.WriteLine("PopulateRouteTable Task completed successfully in time :" + (double)stopwatch.ElapsedTicks / Stopwatch.Frequency +" Seconds");

        stopwatch.Reset();

        var segmentsList = ConvertToSegments(segments);
        stopwatch.Start();
        PopulateSegmentTable(segmentsList,connectionString);
        stopwatch.Stop();
        Console.WriteLine("PopulateSegmentTable Task completed successfully in time :" + (double)stopwatch.ElapsedTicks / Stopwatch.Frequency +" Seconds");

        stopwatch.Reset();

        stopwatch.Start();
        PopulateSubscriptionTable(csvFilesPath+"subscriptions.csv",connectionString);
        stopwatch.Stop();
        Console.WriteLine("PopulateSubscriptionTable Task completed successfully in time :" + (double)stopwatch.ElapsedTicks / Stopwatch.Frequency +" Seconds");

        stopwatch.Reset();

        stopwatch.Start();
        PopulateFlightTable(csvFilesPath+"flights.csv",connectionString);
        stopwatch.Stop();
        Console.WriteLine("PopulateFlightTable Task completed successfully in time :" + (double)stopwatch.ElapsedTicks / Stopwatch.Frequency +" Seconds");

        AddForeignKeyConstraintToRoutesTable(connectionString);
    }
    
    /*
     * Function to create the table if tables doesn't exists
     */
    private static async void CreateTables(NpgsqlConnection connection)
    {
        connection.Open();
        await using NpgsqlCommand createRoutesTableCommand = new NpgsqlCommand("CREATE TABLE IF NOT EXISTS Routes (route_id INTEGER PRIMARY KEY,origin_city_id INTEGER,destination_city_id INTEGER,departure_date TIMESTAMP, segment_id INTEGER);", connection);
        await createRoutesTableCommand.ExecuteNonQueryAsync();
                
        await using NpgsqlCommand createFlightsCommand = new NpgsqlCommand("CREATE TABLE IF NOT EXISTS Flights (flight_id INTEGER PRIMARY KEY,route_id INTEGER,departure_time TIMESTAMP,arrival_time TIMESTAMP,airline_id INTEGER, FOREIGN KEY (route_id) REFERENCES Routes(route_id));", connection);
        await createFlightsCommand.ExecuteNonQueryAsync();

        await using NpgsqlCommand createSegmentTableCommand = new NpgsqlCommand("CREATE TABLE IF NOT EXISTS Segments (segment_id INTEGER PRIMARY KEY ,origin_city_id INTEGER,destination_city_id INTEGER);", connection);
        await createSegmentTableCommand.ExecuteNonQueryAsync();
        
        await using NpgsqlCommand createSubscriptionTableCommand = new NpgsqlCommand("CREATE TABLE IF NOT EXISTS Subscriptions (agency_id INTEGER ,origin_city_id INTEGER,destination_city_id INTEGER, segment_id INTEGER, FOREIGN KEY (segment_id) REFERENCES Segments(segment_id));", connection);
        await createSubscriptionTableCommand.ExecuteNonQueryAsync();
    }

    private static int GetSegmentIdForOriginDestination(int origin_city_id, int destination_city_id)
    {
        KeyValuePair<int, int> originDestinationPair = new KeyValuePair<int, int>(origin_city_id,destination_city_id);
        if (!segments.ContainsKey(originDestinationPair))
        { 
            segments.Add(originDestinationPair,_segmentCount++);
        }
        return segments[originDestinationPair]; 
    }
    
    public static List<Segment> ConvertToSegments(Dictionary<KeyValuePair<int, int>, int> segments) {
        List<Segment> result = new List<Segment>();
        foreach (var entry in segments) {
            KeyValuePair<int, int> key = entry.Key;
            int value = entry.Value;
            Segment segment = new Segment {
                SegmentId = value,
                OriginCityId = key.Key,
                DestinationCityId = key.Value
            };
            result.Add(segment);
        }
        return result;
    }
    private static async void PopulateFlightTable(string pathToCSV,string connectionString)
    {
        try
        {
            /* Set up SQL statement which we will modify dynamically to insert a batch of data at once.
             * This is done to avoid large number of database call for.
             */
            string sql = "INSERT INTO flights (flight_id, route_id, departure_time, arrival_time, airline_id) VALUES ";

            // Set up list of parameter values that we add at once
            List<string> values = new List<string>();

            using (StreamReader reader = new StreamReader(pathToCSV))
            {
                // Skip header row as it contains the column names
                reader.ReadLine();

                while (!reader.EndOfStream)
                {
                    string[] fields = reader.ReadLine().Split(',');

                    values.Add($"({fields[0]}, {fields[1]}, '{fields[2]}', '{fields[3]}', {fields[4]})");

                    // When 1000 rows have been added, execute SQL statement and clear parameter values
                    if (values.Count == 1000)
                    {
                        string valueString = string.Join(",", values);

                        string fullSql = sql + valueString;

                        // Execute SQL statement
                        await using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
                        {
                            connection.Open();
                            await using (NpgsqlCommand command = new NpgsqlCommand(fullSql, connection))
                            {
                                command.ExecuteNonQuery();
                            }
                        }

                        // Clear parameter values to create a new batch of data
                        values.Clear();
                    }
                }

                // If there are any remaining parameter values, execute final SQL statement
                if (values.Count > 0)
                {
                    // Join parameter values into a single string
                    string valueString = string.Join(",", values);

                    // Concatenate SQL statement with parameter values
                    string fullSql = sql + valueString;

                    // Execute SQL statement
                    await using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
                    connection.Open();
                    await using NpgsqlCommand command = new NpgsqlCommand(fullSql, connection);
                    command.ExecuteNonQuery();
                }
            }

            Console.WriteLine("Flights Data has been inserted into the database successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred while inserting data into the database: " + ex.Message);
        }
    }
    
    private static async void PopulateRouteTable(String pathToCSV,String connectionString)
    {
        try
        {

            // Set up SQL statement
            string sql = "INSERT INTO Routes (route_id, origin_city_id, destination_city_id, departure_date, segment_id) VALUES ";

            List<string> values = new List<string>();

            using (StreamReader reader = new StreamReader(pathToCSV))
            {
                // Skip header row
                reader.ReadLine();

                while (!reader.EndOfStream)
                {
                    string[] fields = reader.ReadLine().Split(',');
                    int segmentId = GetSegmentIdForOriginDestination(int.Parse(fields[1]), int.Parse(fields[2]));
                    values.Add($"({fields[0]}, {fields[1]}, {fields[2]}, '{fields[3]}', {segmentId})");

                    if (values.Count == 1000)
                    {
                        string valueString = string.Join(",", values);

                        string fullSql = sql + valueString;

                        using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
                        {
                            connection.Open();
                            using (NpgsqlCommand command = new NpgsqlCommand(fullSql, connection))
                            {
                                command.ExecuteNonQuery();
                            }
                        }

                        values.Clear();
                    }
                }

                if (values.Count > 0)
                {
                    string valueString = string.Join(",", values);

                    string fullSql = sql + valueString;

                    await using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
                    connection.Open();
                    await using NpgsqlCommand command = new NpgsqlCommand(fullSql, connection);
                    command.ExecuteNonQuery();
                }
            }

            Console.WriteLine("Routes Data has been inserted into the database successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred while inserting data into the database: " + ex.Message);
        }
    }
    
    private static async void PopulateSubscriptionTable(String pathToCSV,String connectionString)
    {
        try
        {

            string sql = "INSERT INTO Subscriptions (agency_id,origin_city_id,destination_city_id,segment_id) VALUES ";

            List<string> values = new List<string>();

            using (StreamReader reader = new StreamReader(pathToCSV))
            {
                // Skip header row
                reader.ReadLine();

                while (!reader.EndOfStream)
                {
                    string[] fields = reader.ReadLine().Split(',');
                    int segmentId = GetSegmentIdForOriginDestination(int.Parse(fields[1]), int.Parse(fields[2]));
                    values.Add($"({fields[0]}, {fields[1]}, {fields[2]}, {segmentId})");

                    if (values.Count == 1000)
                    {
                        string valueString = string.Join(",", values);

                        string fullSql = sql + valueString;

                        using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
                        {
                            connection.Open();
                            using (NpgsqlCommand command = new NpgsqlCommand(fullSql, connection))
                            {
                                command.ExecuteNonQuery();
                            }
                        }

                        values.Clear();
                    }
                }

                if (values.Count > 0)
                {
                    string valueString = string.Join(",", values);

                    string fullSql = sql + valueString;

                    await using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
                    connection.Open();
                    await using NpgsqlCommand command = new NpgsqlCommand(fullSql, connection);
                    command.ExecuteNonQuery();
                }
            }

            Console.WriteLine("Subscription Data has been inserted into the database successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred while inserting data into the database: " + ex.Message);
        }
    }

    private static void PopulateSegmentTable(List<Segment> segments, string connectionString)
    {
        using (var connection = new NpgsqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    using (var command = new NpgsqlCommand())
                    {
                        command.Connection = connection;
                        command.Transaction = transaction;
                        command.CommandText =
                            "INSERT INTO Segments (segment_id, origin_city_id, destination_city_id) VALUES (@segment_id, @origin_city_id, @destination_city_id)";
                        foreach (Segment segment in segments)
                        {
                            command.Parameters.Clear();
                            command.Parameters.AddWithValue("@segment_id", segment.SegmentId);
                            command.Parameters.AddWithValue("@origin_city_id", segment.OriginCityId);
                            command.Parameters.AddWithValue("@destination_city_id", segment.DestinationCityId);
                            command.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine("An error occurred while inserting segments into the database:");
                Console.WriteLine(ex.ToString());
                throw; // rethrow the exception to the caller
            }
            finally
            {
                connection.Close();
            }
        }
    }
    
    private static void AddForeignKeyConstraintToRoutesTable(string connectionString) {
        using (var connection = new NpgsqlConnection(connectionString)) {
            connection.Open();
            using (var command = new NpgsqlCommand()) {
                command.Connection = connection;
                command.CommandText = "ALTER TABLE routes ADD CONSTRAINT fk_routes_segment_id FOREIGN KEY (segment_id) REFERENCES Segments (segment_id)";
                command.ExecuteNonQuery();
            }
            connection.Close();
        }
    }
}