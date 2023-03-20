using Flights_Project.Models;
using Microsoft.EntityFrameworkCore;

namespace Flights_Project.Data.Repository;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly FlightHistoryDBContext _dbContext;
    
    public SubscriptionRepository(FlightHistoryDBContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public Subscription GetById(int id)
    {
        return _dbContext.subscriptions.Find(id);
    }
    
    public IEnumerable<Subscription> GetAll()
    {
        return _dbContext.subscriptions.ToList();
    }
    
    public void Add(Subscription subscription)
    {
        _dbContext.subscriptions.Add(subscription);
        _dbContext.SaveChanges();
    }
    
    public void Update(Subscription subscription)
    {
        _dbContext.subscriptions.Update(subscription);
        _dbContext.SaveChanges();
    }
    
    public void Delete(int id)
    {
        var subscription = _dbContext.subscriptions.Find(id);
        _dbContext.subscriptions.Remove(subscription);
        _dbContext.SaveChanges();
    }
    
    /*
     * Gets the segments that an agency is interested in.
     * Finds the segments in which the agency has a subscription.
     */
    public IEnumerable<int> GetSegmentsForAgency(int agencyId)
    {
        var subscriptions = _dbContext.subscriptions
            .Where(s => s.AgencyId == agencyId)
            .Select(s => s.SegmentId)
            .ToList();
        return subscriptions;
    }
}
