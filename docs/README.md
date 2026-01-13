# Dokumentation (MyDRTV)

Denne mappe indeholder den dokumentation, der understøtter vores 10-minutters præsentation og den kørende prototype.

## Hurtigt overblik
- **Arkitekturvalg og systematisk tilgang:** `arkitektur.md`
- **Domain-Driven Design (DDD):** `ddd.md`
- **Bounded contexts (oversigt):** `bounded-contexts.md`
- **Funktionelle krav:** `krav-funktionelle.md`
- **Ikke-funktionelle krav:** `krav-ikke-funktionelle.md`
- **Runtime-flow (kommandoer/events):** `runtime-flow.md`
- **Tilgængelighed + GDPR (kort):** `tilgaengelighed-gdpr.md`
- **Trade-offs:** `tradeoffs.md`

## Hvordan kører man prototypen (localhost)
- One-command demo: `pwsh scripts/demo.ps1`
- Alternativt: `docker compose -f infra/docker-compose.yml up -d --build`

Services (Swagger + health):
- IdentityService: `http://localhost:8081` (Swagger) og `http://localhost:8081/health`
- CatalogueService: `http://localhost:8082` og `http://localhost:8082/health`
- RatingsService: `http://localhost:8083` og `http://localhost:8083/health`
- SearchService: `http://localhost:8084` og `http://localhost:8084/health`
- RecommendationService: `http://localhost:8085` og `http://localhost:8085/health`

Logs:
- `docker compose -f infra/docker-compose.yml logs --tail=200 <service>`

## Hvis noget fejler under demo
- **Search er tom:** kør catalogue-seed igen (den republiserer ProgrammeUpserted).
- **DB migrations:** tjek service-logs for “DB migrations applied”.
- **RabbitMQ/Postgres:** tjek `docker compose ps` og se logs for rabbitmq/postgres.
