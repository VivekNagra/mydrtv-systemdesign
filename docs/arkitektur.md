# Arkitektur og valg
- Stil: Små services (light microservice/SOA) med events, hver med egen DB.
- Alternativer:
  - Monolit: simplere drift/debug, men tæt kobling og sværere at ændre.
  - Modulær monolit: bedre grænser end monolit, men ingen løs kobling og ingen uafhængig skalering.
- Hvorfor dette? Jeg vil vise event-drevet integration og bounded contexts, men holde kompleksiteten lav (få services, simple flows).

## Systematisk tilgang: kriterier og fravalg
**Kriterier for valg**
- Løs kobling og klar domæneafgrænsning (bounded contexts)
- Uafhængig udvikling/deploy per service
- Event-drevet synkronisering accepterer eventual consistency
- Let at demo’e lokalt (Docker + ét script)
- Simpel fejlsøgning via logs og request-id (ingen tung observability-stack)

**Fravalg (bevidst)**
- Ingen fuldtracing/metrics-stack (for at holde skoleprojektet let)
- Ingen HA-setup (én instans pr. service, lokal Postgres/RabbitMQ)
- Ingen stærk konsistens på tværs (accepterer evt. forsinkelse i Search)
- Ingen delt database mellem services (undgår tight coupling)
