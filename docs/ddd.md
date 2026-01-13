# Domain-Driven Design (DDD)

Formålet med DDD i dette projekt er at starte i domænet (MyDRTV) og strukturere systemet efter ansvar og sprog, før vi taler teknologi. Det gør grænserne tydelige og reducerer coupling, samtidig med at det bliver lettere at forklare arkitekturvalget.

## Ubiquitous language (fælles begreber)
Vi bruger disse nøglebegreber konsekvent på tværs af kode, API’er og dokumentation:

- **Programme**: Et stykke indhold i kataloget (TV-program/film/episode i denne prototype). Ejes af Catalogue.
- **User**: En bruger med login-identitet. Persondata (PII) ejes af Identity.
- **Rating**: En brugerhandling der vurderer et programme (stjerner + evt. tekst). Ejes af Ratings.
- **Search / Discovery**: Læsemodel optimeret til filtrering/søgning på tværs af programme-data og rating-summeringer. Ejes af Search.
- **Recommendation**: “More programmes you may like” baseret på discovery-data (i prototypen en simpel heuristik). Ejes af Recommendations.
- **Domain event**: En forretningssignifikant hændelse, som andre contexts må reagere på uden at kende intern logik.

## Bounded contexts (afgrænsning)
I prototypen mapper vi hver bounded context til en selvstændig service med egen database. Det giver tydeligt data-ejerskab og gør det muligt at udvikle og deploye uafhængigt.

- **Catalogue (CatalogueService)**
  - Ansvar: Programme-CRUD/seed, “source of truth” for programme metadata.
  - Publicerer events når programme-data ændres.

- **Identity (IdentityService)**
  - Ansvar: Registrering, login, JWT-udstedelse.
  - Ejer PII (e-mail) og password-hash.

- **Ratings (RatingsService)**
  - Ansvar: Oprette rating/review, vise rating summary pr. programme.
  - Publicerer event når der indsendes rating.

- **Search (SearchService)**
  - Ansvar: Read model til søgning/filtrering (titel, år, genre, minRating).
  - Konsumerer events fra Catalogue og Ratings og materialiserer en søge-optimeret model.

- **Recommendations (RecommendationService)**
  - Ansvar: Returnere simple anbefalinger.
  - I prototypen kaldes Search synkront for at holde løsningen enkel at demonstrere.

## Context map (relationer og integration)
Vi beskriver relationerne som upstream/downstream og hvilken integrationsform vi bruger:

- **Catalogue → Search**
  - Relation: Catalogue er **Upstream**, Search er **Downstream**.
  - Integration: **Event-driven** via `ProgrammeUpserted`.
  - Konsekvens: Search er eventual consistent ift. Catalogue.

- **Ratings → Search**
  - Relation: Ratings er **Upstream**, Search er **Downstream**.
  - Integration: **Event-driven** via `RatingSubmitted`.
  - Konsekvens: Søgning kan i korte perioder vise gamle rating-summeringer.

- **Identity → Ratings**
  - Relation: Identity leverer autentifikation (JWT). Ratings validerer token.
  - Integration: **Synkron** via JWT (ikke direkte servicekald i prototypen).

- **Search → Recommendations**
  - Relation: Search fungerer som datakilde for anbefalinger i prototypen.
  - Integration: **Synkron** HTTP-kald (bevidst valg for demo).

## Domain events
Vi bruger domain events for at undgå runtime coupling og for at gøre det muligt at bygge en separat read model:

- **ProgrammeUpserted**
  - Betydning: Programme-data er oprettet/ændret.
  - Forbruges af: Search (opdaterer read model).

- **RatingSubmitted**
  - Betydning: En bruger har indsendt en rating.
  - Forbruges af: Search (opdaterer average/antal i read model).

## Data ownership og GDPR-perspektiv
- PII (e-mail) ligger i **Identity** og deles ikke ud til andre contexts.
- Andre contexts bruger kun `UserId` som reference.
- Password gemmes som hash, ikke i klartekst.
- I en udvidet løsning ville vi tilføje sletning/anonymisering af brugerdata samt dataminimering og retention-politikker.
