using MassTransit;
using Microsoft.EntityFrameworkCore;
using MyDrTv.Contracts;
using RecommendationService.Data;

namespace RecommendationService.Consumers;

public class RatingSubmittedConsumer : IConsumer<RatingSubmitted>
{
    private readonly RecsDbContext _db;

    public RatingSubmittedConsumer(RecsDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<RatingSubmitted> context)
    {
        var e = context.Message;

        // only treat 4-5 stars as positive signal
        if (e.Stars < 4) return;

    }
}
