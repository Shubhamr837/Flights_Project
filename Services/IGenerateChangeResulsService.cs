namespace Flights_Project.Services;

public interface IGenerateChangeResultsService
{
    void GenerateResultsCsvForDates(DateTime startDate, DateTime endDate, int agencyId);
}
