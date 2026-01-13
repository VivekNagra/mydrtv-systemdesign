# DDD-artefakter

## Bounded contexts 
- CatalogueService: kilde til programmer; emitterer ProgrammeUpserted
- RatingsService: kilde til ratings; emitterer RatingSubmitted
- SearchService: læsemodel; konsumerer ProgrammeUpserted + RatingSubmitted
- IdentityService: brugere og JWT
- RecommendationService: læse fra Search og levere simple anbefalinger
- Contracts: delte event-kontrakter

## Ubiquitous language (centrale termer)
- Programme (Id, Title, Year, Genre, Synopsis)
- Rating (ProgrammeId, UserId, Stars, Review, CreatedAt)
- User (Email, DisplayName, PasswordHash), Token/JWT
- Recommendation (genre, minRating) som simpel læseforespørgsel
- Events: ProgrammeUpserted, RatingSubmitted

## Context map (relationer)
- Catalogue -> Search via ProgrammeUpserted (event)
- Ratings -> Search via RatingSubmitted (event)
- Recommendation -> Search via HTTP (query)
- Identity er sidevogn til auth (ingen events ud herfra)
- Contracts er et shared library for eventskemaer

Kommunikation:
- Event-bus (RabbitMQ/MassTransit) for domænehændelser
- HTTP for queries/commands fra klient (demo-script eller curl)
