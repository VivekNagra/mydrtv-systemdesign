# MyDRTV System Design (School Project)

This repository contains a small, microservice-style prototype for a fictional streaming platform and social layer for DR content. The solution is designed to run locally (Docker Compose) and to support a short architecture presentation for coursework.

The prototype demonstrates:
- Domain-aligned decomposition (DDD-inspired bounded contexts)
- Event-driven integration (RabbitMQ) to reduce runtime coupling
- A separate read model for discovery/search (CQRS-inspired)
- Basic authentication (JWT) with a GDPR-minded separation of PII

## Scope (what is implemented)
Core features covered by the prototype:
- Programme catalogue (title/year/genre) with seeding
- User accounts (register/login) and JWT issuance
- Ratings/reviews and rating summary per programme
- Search/filtering across programme attributes and rating summaries
- Simple “more programmes you may like” recommendations (demo heuristic)

## Architecture overview (DDD + bounded contexts)
Each bounded context is implemented as a separate service with its own database:

- **CatalogueService**: owns programme metadata and publishes `ProgrammeUpserted`
- **RatingsService**: owns ratings/reviews and publishes `RatingSubmitted`
- **SearchService**: owns a search-friendly read model and consumes `ProgrammeUpserted` + `RatingSubmitted`
- **IdentityService**: owns user identities and issues JWT tokens
- **RecommendationService**: returns simple recommendations by calling Search (synchronous in the demo)
- **shared/MyDrTv.Contracts**: shared event contracts (pragmatic for the prototype)

For more detail, see `docs/ddd.md` and `docs/arkitektur.md`.

## Why this architecture (brief trade-offs)
We intentionally chose a microservice-style design with event-driven integration because the case emphasizes:
- **High availability** (fault isolation and scaling critical parts independently)
- **A reputationally sensitive ratings system** (separate ownership and isolation)
- **A discovery/search experience** that benefits from a dedicated read model

Trade-offs (important to acknowledge):
- More distributed complexity (events, retries, debugging)
- Eventual consistency in Search (the read model can be briefly behind)
- Shared contracts increase build-time coupling (acceptable for a school prototype; versioned schemas/packages would be preferred in a mature setup)

## GDPR perspective (prototype-level)
- PII (email + password hash) is isolated in **IdentityService**
- Other services reference users only by `UserId`
- Passwords are stored as hashes, not plaintext

A full production solution would add retention policies, deletion/anonymization flows, stronger secrets management, and audit logging.

## Prerequisites
- Docker + Docker Compose
- PowerShell 7+
- .NET SDK 10.0 (optional for local builds; containers include runtime/SDK as needed)

## One-command demo
From the repo root:

```powershell
pwsh scripts/demo.ps1
```
What it does:
- docker compose -f infra/docker-compose.yml up -d --build
- Health-check each service
- Seed catalogue
- Register/login a demo user
- Submit a rating + fetch the summary
- Run search and recommendations

## Running manually (hvis ovenfor ikek virker)
```
docker compose -f infra/docker-compose.yml up -d --build
``` 
**Health checks**
curl http://localhost:8081/health
curl http://localhost:8082/health
curl http://localhost:8083/health
curl http://localhost:8084/health
curl http://localhost:8085/health

**Swagger UI*
Swagger UIs:
- IdentityService: http://localhost:8081/swagger
- CatalogueService: http://localhost:8082/swagger
- RatingsService: http://localhost:8083/swagger
- SearchService: http://localhost:8084/swagger
- RecommendationService: http://localhost:8085/swagger

## logs

```
docker compose -f infra/docker-compose.yml logs --tail=200 <service>
```
Services: identity, catalogue, ratings, search, recs, postgres, rabbitmq

