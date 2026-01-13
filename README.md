# MyDRTV System Design (School Project)

This repo contains a small microservice-style demo for a fictional streaming platform. It is designed to run locally with Docker Compose and .NET 10, and to be easy to demo for coursework.

## Services
- **CatalogueService**: stores and serves programmes; publishes ProgrammeUpserted events.
- **RatingsService**: lets users submit ratings and reviews; publishes RatingSubmitted events.
- **SearchService**: stores a search-friendly view of programmes and ratings; consumes ProgrammeUpserted and RatingSubmitted.
- **IdentityService**: minimal auth (register/login/JWT) and issues tokens.
- **RecommendationService**: calls Search to return simple recommendations.
- **shared/MyDrTv.Contracts**: shared contracts/events.

## Prerequisites
- Docker + Docker Compose
- PowerShell 7+
- .NET SDK 10.0 installed (for local builds; containers also carry SDK/runtime)

## One-command demo
From the repo root:
```pwsh
pwsh scripts/demo.ps1
```
What it does:
- `docker compose -f infra/docker-compose.yml up -d --build`
- Health-check each service
- Seed catalogue
- Register/login a demo user
- Submit a rating + fetch the summary
- Run search and recommendations

## Running manually (optional)
```pwsh
docker compose -f infra/docker-compose.yml up -d --build
# health checks
curl http://localhost:8081/health
curl http://localhost:8082/health
curl http://localhost:8083/health
curl http://localhost:8084/health
curl http://localhost:8085/health
```

## Observability / logs
- Each service logs request id, migration applied, and key events (publishes/consumes).
- To inspect logs for a service:
```pwsh
docker compose -f infra/docker-compose.yml logs --tail=200 <service>
# services: identity, catalogue, ratings, search, recs, postgres, rabbitmq
```

## Common troubleshooting
- If Search returns empty results after a restart, rerun catalogue seed (`POST /admin/seed`) to republish programmes.
- If migrations fail, check the relevant service logs for “DB migrations applied” or connection errors.
- RabbitMQ/Postgres come up with Docker Compose; verify via `docker compose ps`.

## Project structure
```
infra/                 # docker-compose.yml and infra config
scripts/demo.ps1       # one-command demo script
shared/MyDrTv.Contracts# shared event contracts
src/CatalogueService   # programmes API + events
src/RatingsService     # ratings API + events
src/SearchService      # search/read model, consumes events
src/IdentityService    # auth/JWT
src/RecommendationService # simple recommendations via search
```

## Notes
- `.gitignore` excludes build artifacts (bin/obj).
- Swagger JSONs are included for reference (swagger-*.json).
- appsettings.json files contain local settings for demo only.
