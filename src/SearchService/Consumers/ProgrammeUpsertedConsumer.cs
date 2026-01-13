using MassTransit;
using Microsoft.EntityFrameworkCore;
using MyDrTv.Contracts;
using SearchService.Data;

namespace SearchService.Consumers;

public class ProgrammeUpsertedConsumer : IConsumer<ProgrammeUpserted>
{
    private readonly SearchDbContext _db;

    public ProgrammeUpsertedConsumer(SearchDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<ProgrammeUpserted> context)
    {
        var e = context.Message;

        var existing = await _db.Programmes.SingleOrDefaultAsync(p => p.ProgrammeId == e.ProgrammeId);
        if (existing is null)
        {
            _db.Programmes.Add(new ProgrammeSearch
            {
                ProgrammeId = e.ProgrammeId,
                Title = e.Title,
                Year = e.Year,
                Genre = e.Genre,
                Synopsis = e.Synopsis
            });
        }
        else
        {
            existing.Title = e.Title;
            existing.Year = e.Year;
            existing.Genre = e.Genre;
            existing.Synopsis = e.Synopsis;
        }

        await _db.SaveChangesAsync();
    }
}
