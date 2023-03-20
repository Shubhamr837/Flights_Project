
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
                MigrationUtils.ImportCSVToPostgreSQL(args[1],connectionString);
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
                    .AddSingleton<IGenerateChangeResultsService,CalculateChangeResultsService>()
                    .BuildServiceProvider();

                using var scope = serviceProvider.CreateScope();
                var generateChangeResultsService = scope.ServiceProvider.GetService<IGenerateChangeResultsService>();
                generateChangeResultsService.GenerateResultsCsvForDates(startDate,endDate,agencyId);
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


