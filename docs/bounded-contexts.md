# Bounded contexts og ansvar

Denne oversigt beskriver, hvilke domæneområder (bounded contexts) vi har identificeret, samt deres ansvar, data ownership og integration.

## CatalogueService (Catalogue context)
- **Ansvar**: Source of truth for programme metadata (titel, år, genre, etc.).
- **Data**: Programme-tabeller i egen Postgres.
- **Integration**: Publicerer `ProgrammeUpserted`, som andre kan reagere på.

## RatingsService (Ratings context)
- **Ansvar**: Oprette rating/review og udstille rating summary pr. programme.
- **Data**: Ratings i egen Postgres.
- **Integration**: Publicerer `RatingSubmitted`.

## SearchService (Search/Discovery context)
- **Ansvar**: Read model til søgning/filtrering på tværs af programme-data og rating-summeringer.
- **Data**: Egen søge-optimeret model i egen Postgres.
- **Integration**: Konsumerer `ProgrammeUpserted` og `RatingSubmitted` og materialiserer read modellen.
- **Konsekvens**: Eventual consistency (read modellen kan være kortvarigt bagud).

## IdentityService (Identity context)
- **Ansvar**: Registrering, login og JWT-udstedelse.
- **Data**: Users i egen Postgres (PII: e-mail + password-hash).
- **Integration**: JWT bruges af andre services til at beskytte brugerhandlinger.

## RecommendationService (Recommendation context)
- **Ansvar**: Returnere simple anbefalinger (“more programmes you may like”).
- **Integration**: Kalder Search synkront i prototypen for at holde demoen enkel.

## Contracts (Shared contracts)
- **Formål**: Delt assembly til event-kontrakter i prototypen.
- **Trade-off**: Øger build-time coupling; i en moden løsning ville vi versionere events via separate packages/schemas.
