using MassTransit;
using Microsoft.EntityFrameworkCore;
using MyDrTv.Contracts;
using SearchService.Data;

namespace SearchService.Consumers;

public class RatingSubmittedConsumer : IConsumer<RatingSubmitted>
{
    private readonly SearchDbContext _db;

    public RatingSubmittedConsumer(SearchDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<RatingSubmitted> context)
    {
        var e = context.Message;

        var p = await _db.Programmes.SingleOrDefaultAsync(x => x.ProgrammeId == e.ProgrammeId);
        if (p is null) return;

        // Incremental average update
        var newCount = p.RatingsCount + 1;
        var newAvg = ((p.RatingsAverage * p.RatingsCount) + e.Stars) / newCount;

        p.RatingsCount = newCount;
        p.RatingsAverage = Math.Round(newAvg, 4);

        await _db.SaveChangesAsync();
    }
}
