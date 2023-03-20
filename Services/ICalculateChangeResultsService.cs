namespace Flights_Project.Services;

public interface ICalculateChangeResultsService
{
    void GenerateResultsCsvForDates(DateTime startDate, DateTime endDate, int agencyId);
}
