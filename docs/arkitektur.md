# Arkitektur og valg

## Valgt arkitekturstil
Vi har valgt en “light” microservice/SOA-inspireret stil med event-driven integration og en separat read model til søgning:

- Flere små services (bounded contexts), hver med **egen database**
- **Asynkron** synkronisering via events (RabbitMQ/MassTransit)
- Search fungerer som **read model** (CQRS-inspireret), bygget ud fra events

Det er en bevidst balance: vi vil demonstrere moderne arkitekturprincipper (DDD, bounded contexts, events, read model) uden at gøre løsningen unødigt stor.

## Alternativer vi overvejede
### 1) Klassisk monolit (layered)
**Fordele**
- Simpelt at udvikle og debugge
- Let drift lokalt og i små miljøer

**Ulemper**
- Tæt kobling mellem domæner (ratings/search/identity/catalogue)
- Sværere at isolere reputationskritiske dele (fx ratings) ved fejl eller load
- Skalerer ofte “hele systemet” fremfor målrettet

### 2) Modulær monolit
**Fordele**
- Klarere grænser end en traditionel monolit
- Mindre distribueret kompleksitet end microservices

**Ulemper**
- Ingen uafhængig deploy/skalering pr. modul
- Risiko for gradvis “grænse-erosion” uden tydelig data ownership

### 3) Microservices + event-driven (valgt)
**Fordele**
- Klar domæneafgrænsning og data ownership (DDD/bounded contexts)
- Mulighed for uafhængig deploy og horisontal skalering
- Event-driven integration reducerer runtime coupling og kan fungere som buffer ved spikes

**Ulemper**
- Distribueret kompleksitet (fejlsøgning, retries, idempotency)
- Eventual consistency (read model kan være “stale” i korte perioder)
- Kræver bedre observability i en rigtig produktion (traces/metrics)

## Systematisk tilgang (Richards-inspirerede kriterier)
Vi vurderede alternativerne ud fra følgende kriterier (inspireret af Mark Richards’ sammenligningskriterier):

1) **Evolvability / changeability**
- Vi forventer, at features som search og recommendations ændrer sig over tid.
- Separate contexts gør det muligt at ændre én del uden at påvirke alt andet.

2) **Deployability**
- Uafhængige services kan deployes separat (relevant ift. digital transformation og hyppige ændringer).
- I prototypen viser vi princippet, selvom alt kører lokalt i Docker.

3) **Scalability**
- Search og ratings kan få høj trafik. Med separate services kan de skaleres målrettet.

4) **Availability / fault tolerance**
- Event-driven integration gør, at Search kan “indhente” ændringer senere.
- Fejl i én service behøver ikke vælte hele systemet.

5) **Data integrity og konsistens**
- Vi accepterer eventual consistency for Search-read modellen.
- Write-siderne (Catalogue/Ratings) er “source of truth” for deres data.

6) **Simplicity (time-to-market)**
- Vi har holdt antallet af services lavt og flows simple, så løsningen er realistisk at demonstrere.

## Modulær struktur og coupling
- Services er adskilt pr. domæne og har egen database (reducerer runtime coupling).
- Vi bruger et delt `Contracts`-projekt til event-kontrakter for at gøre prototypen enkel.
  - Trade-off: det øger build-time coupling.
  - I en mere moden løsning ville vi versionere events og styre kontrakter via separate packages eller schemas.

## Teknologistak (kort)
- .NET services med Swagger og health endpoints
- PostgreSQL (database pr. service)
- RabbitMQ + MassTransit (events)
- Docker Compose (lokal drift/demonstration)
- JWT (Identity) til at beskytte brugerhandlinger (Ratings)
