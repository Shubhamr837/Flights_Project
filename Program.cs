
using System.Diagnostics;
using System.Globalization;
using Flights_Project.Data;
using Flights_Project.Data.Repository;
using Flights_Project.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

internal class Program
{
    static void Main(string[] args)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        if (args.Length >= 2)
        {
            DateTime startDate;
            DateTime endDate;
            int agencyId;

            var configuration = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appconfig.json"))
                .Build();

            string connectionString = configuration.GetConnectionString("PostgresConnectionString");
            
            
            
            if (args[0].ToLower().Equals("migrate"))
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                MigrationUtils.ImportCSVToPostgreSQL(args[1],connectionString);
                stopwatch.Stop();
                Console.WriteLine("Migration Task completed successfully in time :" + (double)stopwatch.ElapsedTicks / Stopwatch.Frequency +" Seconds");
            }
            else if (DateTime.TryParseExact(args[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate) 
                     && DateTime.TryParseExact(args[1], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate)
                     && int.TryParse(args[2], out agencyId))
            {
                /*
                 * Adding all the dependencies to the DI container
                 * As this is a console application we add the dependencies as singleton
                 */
                var serviceProvider = new ServiceCollection()
                    .AddDbContext<FlightHistoryDBContext>(options =>
                        options.UseNpgsql(connectionString, options => options.SetPostgresVersion(new Version(15, 4))))
                    .AddSingleton<IRouteRepository,RouteRepository>()
                    .AddSingleton<IFlightRepository, FlightRepository>()
                    .AddSingleton<ISubscriptionRepository,SubscriptionRepository>()
                    .AddSingleton<ICalculateChangeResultsService,CalculateChangeResultsService>()
                    .BuildServiceProvider();

                using var scope = serviceProvider.CreateScope();
                var calculateChangeResultsService = scope.ServiceProvider.GetService<ICalculateChangeResultsService>();
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                calculateChangeResultsService.GenerateResultsCsvForDates(startDate,endDate,agencyId);
                stopwatch.Stop();
                Console.WriteLine("Calculation of changed flights Task completed successfully in time :" + (double)stopwatch.ElapsedTicks / Stopwatch.Frequency +" Seconds");
            }
            else
            {
                // Date string is in invalid format
                Console.WriteLine($"Input is invalid");
            }
        }
        else
        {
            Console.WriteLine("Error: Invalid input parameters.");
        }
    }


}


