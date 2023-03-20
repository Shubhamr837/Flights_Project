using Flights_Project.Models;

namespace Flights_Project.Data.Repository;

public interface ISubscriptionRepository
{
    Subscription GetById(int id);
    IEnumerable<Subscription> GetAll();
    void Add(Subscription subscription);
    void Update(Subscription subscription);
    void Delete(int id);
    IEnumerable<int> GetSegmentsForAgency(int agencyId);
}
