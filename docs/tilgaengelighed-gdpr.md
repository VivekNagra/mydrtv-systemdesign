# Tilgængelighed og GDPR (kort)

## Tilgængelighed / high availability (perspektiv)
Prototypen kører lokalt i Docker Compose og er ikke en egentlig HA-opstilling. Vi har dog valgt en arkitektur, der understøtter høj tilgængelighed i et produktionsmiljø:

- **Stateless services**: Services kan køre i flere instanser og skaleres horisontalt.
- **Event-driven integration**: RabbitMQ kan fungere som buffer ved spikes og midlertidige problemer.
- **Fault isolation**: Fejl i én bounded context behøver ikke slå hele systemet ud.
- **Health endpoints**: Alle services udstiller `/health`, som i produktion kan bruges til readiness/liveness checks.

I en fuld løsning ville vi tilføje:
- Replikerede databaser og failover-strategi
- Flere instanser pr. service bag load balancer
- Bedre observability (metrics/tracing) og automatiseret alerting

## GDPR (perspektiv)
Casen kræver håndtering af persondata (PII) på en compliant måde. I prototypen har vi valgt dataminimering:

- **PII isoleres i Identity**: Vi gemmer kun e-mail og password-hash.
- **Password i hash**: Password lagres ikke i klartekst.
- **Andre services bruger kun UserId**: Ratings og øvrige contexts har ikke behov for e-mail eller andre PII.

I en udvidet løsning ville vi tilføje:
- Bruger-sletning/anonymisering og retention-politikker
- Stærkere secret management (keys/secrets uden for kode)
- Audit-logging (hvad er ændret, hvornår og af hvem) i overensstemmelse med dataminimering
