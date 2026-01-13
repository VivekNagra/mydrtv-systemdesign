# Projekt-dokumentation (MyDRTV)

Hej! Her er en kort, uformel gennemgang af systemet. Det er tænkt som eksamens-venlig dokumentation: kort, konkret og på dansk.

## Krav
**Funktionelle**
- Opret/vis programmer (Catalogue)
- Registrér/log ind brugere og udsted JWT (Identity)
- Opret anmeldelser/ratings og se gennemsnit (Ratings)
- Søg i programmer + ratings (Search)
- Få simple anbefalinger (Recommendations)

**Ikke-funktionelle**
- Kørbar med én kommando (docker compose + demo-script)
- Enkel observability: log request-id, migrations, events
- Event-baseret synkronisering mellem services (løst koblet)
- Skal kunne køre lokalt uden ekstra opsætning

## Arkitekturstil (valg og alternativer)
- Valgt: Små services med events (enkel microservice/light SOA). Hver service har eget ansvar og egen DB, synk via events.
- Alternativ 1: Monolit – simplere at debugge, men tæt kobling og tungere at ændre.
- Alternativ 2: Modulær monolit – bedre grænser end ren monolit, men mangler løs kobling og uafhængig skalering.
- Hvorfor valgt? Vi vil demonstrere event-drevet integration og bounded contexts, men stadig holde kompleksiteten nede (få services, simple flows).

## Bounded contexts og ansvar
- **CatalogueService**: Kilde til sandhed for programmer. Publicerer ProgrammeUpserted.
- **RatingsService**: Kilde til sandhed for ratings. Publicerer RatingSubmitted.
- **SearchService**: Læsemodel til søgning. Konsumerer ProgrammeUpserted og RatingSubmitted, gemmer i egen DB.
- **IdentityService**: Brugere, registrering, login, JWT.
- **RecommendationService**: Henter fra Search og returnerer simple anbefalinger baseret på genre/minRating.
- **Contracts**: Deler event-kontrakter mellem services.

## Runtime flow (kommandoer + events)
1) Admin/seed opretter programmer i Catalogue (kommando).
2) Catalogue publicerer ProgrammeUpserted (event).
3) Search konsumerer ProgrammeUpserted og opdaterer sin søgedata.
4) Bruger registrerer + logger ind i Identity (kommando) og får JWT.
5) Bruger poster rating i Ratings (kommando).
6) Ratings publicerer RatingSubmitted (event).
7) Search konsumerer RatingSubmitted og opdaterer gennemsnit/antal.
8) Bruger søger i Search (query) og får resultater med ratings.
9) Bruger kalder Recommendations (query), som kalder Search bagved.

## Trade-offs
- Eventual consistency: Search kan være et par sekunder bagud ift. Catalogue/Ratings.
- Debugging på tværs: Fejl kan ligge i flere services; vi logger request-id og key events for at følge flowet.
- Simpel observability: Logs i stedet for fuld tracing/metrics (bevidst afgrænset for skoleprojekt).
- Flere services øger kompleksitet ift. drift, men giver klarere domænegrænser.

## Tilgængelighed og GDPR (kort)
- Kører som lokale containere; ingen HA-opstilling her. I produktion ville vi have replikerede databaser og flere service-instanser.
- GDPR: Ingen følsomme data ud over e-mail og password-hash. I et rigtigt setup skulle vi have databehandleraftaler, sletning/anonymisering ved bruger-sletning, og sikre secrets/keys uden for kode.

## Hvordan kører man det?
- `pwsh scripts/demo.ps1` (one-command demo) eller `docker compose -f infra/docker-compose.yml up -d --build`
- Health endpoints: /health på alle services (8081-8085)
- Logs: `docker compose -f infra/docker-compose.yml logs --tail=200 <service>`

## Hvis noget fejler
- Search tom? Kør katalog-seed igen (republiserer ProgrammeUpserted).
- Migrations? Tjek service-logs for “DB migrations applied”.
- RabbitMQ/Postgres nede? `docker compose ps` og se rabbitmq/postgres logs.

Det var det! Spørg endelig, hvis noget skal uddybes. :)
