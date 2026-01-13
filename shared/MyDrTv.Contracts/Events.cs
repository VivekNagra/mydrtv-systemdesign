namespace MyDrTv.Contracts;

public record ProgrammeUpserted(
    Guid ProgrammeId,
    string Title,
    int Year,
    string Genre,
    string Synopsis
);

public record RatingSubmitted(
    Guid ProgrammeId,
    Guid UserId,
    int Stars,
    string? Review,
    DateTimeOffset CreatedAt
);
