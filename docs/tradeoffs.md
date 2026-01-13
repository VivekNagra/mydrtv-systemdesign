# Trade-offs
- Eventual consistency: Search kan være et par sekunder bagud ift. Catalogue/Ratings.
- Debugging på tværs: Fejl kan ligge i flere services; vi bruger request-id + key event logs.
- Observability light: Logs i stedet for fuld tracing/metrics (bevidst for skoleprojekt).
- Flere services = lidt mere drift/kompleksitet, men klarere domænegrænser.
