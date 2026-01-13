# Tilgængelighed og GDPR (kort)
- Tilgængelighed: Lokalt kører alt i Docker; ingen HA i dette setup. I produktion ville vi have replikerede databaser og flere instanser.
- GDPR: Vi har kun e-mail + password-hash. I et rigtigt setup skulle vi håndtere sletning/anonymisering ved bruger-sletning, sikre secrets/keys, og have databehandleraftaler.
