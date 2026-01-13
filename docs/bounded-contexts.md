# Bounded contexts og ansvar
- CatalogueService: kilde til sandhed for programmer; publicerer ProgrammeUpserted.
- RatingsService: kilde til ratings; publicerer RatingSubmitted.
- SearchService: læsemodel til søgning; konsumerer ProgrammeUpserted og RatingSubmitted.
- IdentityService: registrering, login, JWT.
- RecommendationService: kalder Search for at levere simple anbefalinger.
- Contracts: deler event-kontrakter mellem services.
