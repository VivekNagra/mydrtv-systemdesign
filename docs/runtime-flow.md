# Runtime flow (kommandoer og events)
1) Admin seed opretter programmer i Catalogue (kommando)
2) Catalogue publicerer ProgrammeUpserted (event)
3) Search konsumerer ProgrammeUpserted og opdaterer søgedata
4) Bruger registrerer + logger ind (Identity) og får JWT (kommando)
5) Bruger poster rating i Ratings (kommando)
6) Ratings publicerer RatingSubmitted (event)
7) Search konsumerer RatingSubmitted og opdaterer gennemsnit/antal
8) Bruger søger i Search (query)
9) Bruger kalder Recommendations (query), som kalder Search bagved
