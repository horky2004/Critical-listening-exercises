# 07 - Arhitektonske odluke

Zapis odluka donesenih u Phase 1, s obrazloženjem i razmatranim alternativama. Svrha je da se za šest mjeseci zna **zašto** je nešto tako, a ne samo kako.

Status:

- **Potvrđeno** - odlučeno i ugrađeno u specifikaciju
- **Otvoreno** - ugrađeno kao predložak, ali traži potvrdu ili informaciju prije Phase 2

Sve odluke su potvrđene. Phase 1 je zatvorena i specifikacija je spremna za implementaciju.

| # | Odluka | Status |
| --- | --- | --- |
| 1 | ExerciseLevel je template po modulu | Potvrđeno |
| 2 | Audio kroz backend endpoint s opaque tokenom | Potvrđeno |
| 3 | Jedan backend projekt bez slojevite podjele | Potvrđeno |
| 4 | `ModuleConfig` tablica ukinuta | Potvrđeno |
| 5 | `IsUnlocked` se ne sprema | Potvrđeno |
| 6 | `LevelUnlockRequirement` kao zasebna tablica | Potvrđeno |
| 7 | Automatska finalizacija testa, bez `POST /complete` | Potvrđeno |
| 8 | Odgovaranje po `questionIndex` | Potvrđeno |
| 9 | Slug rute za module i izvore | Potvrđeno |
| 10 | EQ odgovor je vidljiv na klijentu | Potvrđeno - rizik prihvaćen |
| 11 | Formati: EQ FLAC, Compression MP3 320 | Potvrđeno |
| 12 | Dev auth bypass | Potvrđeno - app registracija se traži paralelno |
| 13 | Rola u bazi, ne u Entra App Roles | Potvrđeno |
| 14 | Cohort se dodjeljuje automatski | Potvrđeno |
| 15 | `ConfigJson` kao `jsonb` | Potvrđeno |
| 16 | Segment je tablica, ne enum | Potvrđeno |
| 17 | Balansirana randomizacija pitanja | Potvrđeno |
| 18 | UI samo na hrvatskom, bez i18n biblioteke | Potvrđeno |
| 19 | Practice mode za EQ i za Compression | Potvrđeno |
| 20 | Practice mode otkriva mapiranje varijanti | Potvrđeno - posljedica odluke 19, rizik prihvaćen |
| 21 | .NET 10 umjesto .NET 8 | Potvrđeno |
| 22 | Git od početka, commit po fazi | Potvrđeno |

---

## Odluka 1 - ExerciseLevel je template po modulu

**Potvrđeno.**

`ExerciseLevel` je definiran po `(Module, Segment, LevelNumber)`, a napredak je ključan po `(User, AudioSource, ExerciseLevel)`.

Početni prijedlog imao je `AudioSource` direktno na `ExerciseLevel`. To bi značilo 4 × 13 = 52 reda za EQ i 2 × 3 = 6 za Compression, i svaku izmjenu konfiguracije levela (npr. promjena `Q` ili praga) trebalo bi raditi na četiri mjesta, s rizikom da se razlikuju.

Template model daje istu funkcionalnost - napredak je i dalje potpuno neovisan po izvoru, jer izolaciju daje ključ `StudentProgress`, a ne duplicirana konfiguracija. Vidi [03-progression.md](03-progression.md#1-neovisnost-po-audio-izvoru).

Ako se kasnije pojavi potreba da neki izvor ima podskup levela (npr. Pink Noise bez COMBINED segmenta), dodaje se mala tablica isključenja `SourceLevelExclusion`. Ne uvodimo je unaprijed jer trenutno svi izvori unutar modula imaju sve levele.

---

## Odluka 2 - Audio kroz backend endpoint s opaque tokenom

**Potvrđeno.**

Audio se poslužuje preko `/api/audio/assets/{assetId}` i `/api/audio/questions/{audioToken}`, nikad direktnim linkom na fajl.

Razlozi: naziv `drums-ratio-12.wav` odaje odgovor; audio treba autentikaciju; i prelazak na R2 je time promjena jedne implementacije umjesto promjene svih URL-ova u aplikaciji.

Alternativa s hashiranim statičnim nazivima je odbijena jer je naziv stabilan kroz pitanja - detaljno obrazloženje u [06-audio.md](06-audio.md#4-zašto-token-po-pitanju-i-bez-cachea).

---

## Odluka 3 - Jedan backend projekt bez slojevite podjele

**Potvrđeno.**

`CriticalListeningLab.Api` + `CriticalListeningLab.Tests`. Bez `Application`/`Domain`/`Infrastructure` assembly-a, bez repository layera nad EF Coreom, bez MediatR-a, CQRS-a ni AutoMappera.

Glavni argument za slojeve je testabilnost poslovne logike, a ona je ostvarena time što su `UnlockEvaluator`, `ScoreRules` i generatori pitanja čiste klase bez EF i ASP.NET ovisnosti. `DbContext` je već Unit of Work i repozitorij; dodatni sloj nad njim bio bi prepisivanje LINQ-a u druge metode.

Izdvajanje `Domain/` u zasebni projekt kasnije je mehaničko - nema kružnih ovisnosti jer domenske klase ne referenciraju ništa iz `Features/` ni `Data/`.

---

## Odluka 4 - `ModuleConfig` tablica ukinuta

**Potvrđeno.**

`IsEnabledGlobally` je kolona na `Module`. Zasebna 1:1 tablica s jednim boolean stupcem donosi join na svaki upit i nijednu prednost. Kad globalna konfiguracija naraste, dodaje se `ConfigJson jsonb` kolona.

`CohortModuleAvailability` **ostaje** kao zasebna tablica jer je stvarna 1:N relacija i nosi podatak koji na `Module` ne može stati.

---

## Odluka 5 - `IsUnlocked` se ne sprema

**Potvrđeno.**

Otključanost se izračunava iz `LevelUnlockRequirement` + `StudentProgress`, a ne čuva u koloni.

Spremljeno stanje bi zahtijevalo backfill migraciju pri svakoj promjeni unlock pravila, i moglo bi se razići s pravilima (npr. ako se red u `StudentProgress` promijeni izvan servisa). Izračun je trivijalan: dva upita i petlja kroz najviše 13 levela.

Posljedica: `StudentProgress` red postoji samo za levele koje je student **pokušao**. Nepostojanje reda znači nula pokušaja, što je ispravno i za zaključane i za otključane nedirnute levele.

---

## Odluka 6 - `LevelUnlockRequirement` kao zasebna tablica

**Potvrđeno.**

Zahtjevi su redovi u tablici, s AND semantikom i konvencijom "nula redova = otključan od početka".

Razmatrana jednostavnija alternativa je jedna self-FK kolona `ExerciseLevel.RequiresLevelId`, koja bi **pokrila sve trenutne slučajeve** - svaki level u sustavu ima točno nula ili jedan prerequisite, uključujući branching (branching nastaje jer dva levela pokazuju na isti prerequisite, a ne jer jedan level ima dva).

Odabrana je tablica jer:

- omogućuje "zahtijeva više levela" bez migracije (npr. budući "master" level koji zahtijeva prolaz svih segmenata)
- nosi `MinScore` za buduće "zahtijeva 13/14 za pristup"
- `RequirementType` ostavlja prostor za druge vrste uvjeta bez mijenjanja sheme

Cijena je jedna tablica i jedan `Include` u upitu. Kod evaluacije je praktično isti (petlja kroz listu zahtjeva umjesto provjere jednog polja), pa ovo nije "za svaki slučaj" apstrakcija nego konkretan i vjerojatan put razvoja.

---

## Odluka 7 - Automatska finalizacija testa, bez `POST /complete`

**Potvrđeno.**

Sesija se boduje i napredak se ažurira u istoj transakciji kao zapis zadnjeg odgovora.

Zasebni `complete` poziv stvara stanje "svi odgovori zapisani, rezultat neobračunat". Ako klijent tada izgubi mrežu ili korisnik zatvori tab, test je odrađen a napredak izgubljen, i ne postoji ispravan način da se to kasnije sanira - server ne može znati je li student odustao ili mu je pao browser.

Automatska finalizacija to stanje ne može imati. Rezultat se dohvaća preko `GET /api/test-sessions/{id}` koliko puta treba. Prekid je pokriven eksplicitnim `POST /abandon`.

---

## Odluka 8 - Odgovaranje po `questionIndex`

**Potvrđeno.**

`POST /api/test-sessions/{id}/questions/{index}/answer` s `index` od 1 do 14.

Index je pozicija u testu koju UI ionako prikazuje, i omogućuje serveru da odbije odgovor koji preskače redoslijed (`index != answeredCount + 1` → `409`). S `questionId` klijent bi mogao odgovarati u proizvoljnom redoslijedu, a nastavak nakon refresha bi trebao dodatni poziv da dozna id sljedećeg pitanja.

---

## Odluka 9 - Slug rute za module i izvore

**Potvrđeno.**

`/api/modules/eq/sources/drums/tree` umjesto Guid ruta. Čitljivo u logovima i DevToolsima, stabilno, bookmarkable, i URL frontenda može biti isti oblik. Guid ostaje gdje je identitet tehnički: sesije, leveli, assetovi.

`levelId` je Guid iako bi `{segmentKey}/{levelNumber}` bilo čitljivije - level se referencira samo iz tree odgovora, nikad se ne upisuje ručno, i Guid je jedan lookup umjesto tri.

---

## Odluka 10 - EQ odgovor je vidljiv na klijentu

**Potvrđeno - rizik je prihvaćen.**

EQ pitanje mora klijentu poslati `frequencyHz`, `gainDb` i `q`, jer bez njih nije moguće konfigurirati `BiquadFilterNode`. Znači: student koji otvori DevTools može pročitati točan odgovor prije nego odgovori.

To nije propust implementacije nego posljedica zahtjeva da EQ obrada bude **realtime na klijentu**. Jedini način da odgovor ne bude na klijentu je server-side renderiranje EQ-a, što bi značilo 224 unaprijed renderirana fajla (ili renderiranje na zahtjev), gubitak practice modea sa slobodnim parametrima, i suprotno je eksplicitnom zahtjevu iz specifikacije.

Što **ostaje** zaštićeno:

- rezultat se računa isključivo na serveru, iz baze
- ne postoji način da se level označi prođenim bez odgovaranja na svih 14 pitanja
- zaključan level je nedostupan
- Compression nema ovaj problem - tamo je odgovor u audiju, a ne u parametrima

Praktična procjena: rizik je relevantan za samoprocjenu studenta, a ne za ocjenjivanje. Ako se aplikacija ikad koristi za ocjenu, EQ testovi bi se trebali provoditi u nadziranim uvjetima, isto kao svaki drugi ispit na računalu.

Ublažavanja koja su moguća bez arhitektonske promjene, ako se pokaže potreba: minifikacija i izbjegavanje očitih naziva u produkcijskom buildu (podiže prag, ne rješava problem), te bilježenje vremena odgovora (nerealno kratko vrijeme uz 14/14 je signal). Nijedno se ne uvodi sada.

Posljedica za daljnji razvoj: aplikacija je alat za trening i samoprocjenu. Ako se ikad koristi za ocjenu, EQ dio mora biti u nadziranim uvjetima.

---

## Odluka 11 - Formati: EQ FLAC, Compression MP3 320

**Potvrđeno.**

- **EQ izvori: FLAC** (`audio/flac`), 44.1 kHz, 24 bit, stereo
- **Compression varijante: MP3 320 kbps** (`audio/mpeg`), 44.1 kHz, stereo, ~10 s

FLAC za EQ je bolji odabir od prvotno predloženog WAV-a: bez ikakvog gubitka kvalitete, a fajl je oko 50-60% veličine WAV-a. Kako se EQ izvor skine **jednom** po izvoru i cachira dugotrajno (odluka 2), to je čista ušteda bez kompromisa. Za EQ je lossless bitan jer se na signal primjenjuje filtar u realnom vremenu, pa kodek artefakti ulaze u obradu.

MP3 320 za Compression je posljedica toga što se klip pitanja ne cachira i skida se do 14 puta po testu (~5.6 MB umjesto ~24 MB za lossless). Ovdje se na signal **ne** primjenjuje nikakva obrada - klip se samo reproducira - pa je rizik kodeka manji nego kod EQ-a.

Preostala provjera u Phase 8: potvrditi slušanjem da 320 kbps ne maskira razliku između `uncompressed` i `light` na bubnjevima, gdje je razlika najmanja. Ako je maskira, prelazak na FLAC i za Compression je zamjena fajlova te `MimeType` i `StorageKey` vrijednosti u seedu, bez promjene koda - uz prihvaćanje ~4x većeg prometa po testu.

**Rizik koji treba provjeriti u Phase 7:** FLAC dekodiranje kroz `decodeAudioData` podržavaju svi ciljani preglednici (Chrome/Edge od v56, Firefox od v51, Safari od v11), ali podrška za FLAC u Web Audio kontekstu povijesno je bila neujednačenija od MP3-a. Prva stvar u Phase 7 je provjera dekodiranja na Chrome, Firefox, Edge i Safari s pravim fajlom. Ako neki ciljani preglednik padne, fallback je WAV za taj izvor - `MimeType` i `StorageKey` su podaci, pa promjena ne dira kod.

---

## Odluka 12 - Dev auth bypass

**Potvrđeno. App registracija se traži paralelno; razvoj ide s bypassom.**

App registracije u Algebrinom tenantu još ne postoje i tek se traže. Zato:

- **Phase 2 implementira i bypass i pravi Entra tok.** Bypass je funkcionalan odmah; Entra grana se piše do kraja (validacija tokena, claims transformacija, JIT provisioning), ali ostaje **neprovjerena** dok registracija ne postoji.
- Bypass prolazi kroz **istu** domensku provjeru i isti JIT provisioning kao pravi tok, pa se testira sve osim same validacije tokena. Time se izbjegava da se pojavi klasa grešaka koja čeka prvi pravi login.
- `Auth:UseDevBypass` se čita samo iz konfiguracije, a aplikacija **odbija startati** ako je bypass uključen u Production okolini. Detalji u [05-auth.md](05-auth.md#6-dev-bypass).

Kad registracije budu dostupne, potrebno je: upisati `TenantId`/`ClientId` u konfiguraciju, isključiti bypass, i odraditi prijavu jednim pravim računom s `@algebra.hr` i jednim s `@student.algebra.hr`. To je konfiguracijski, a ne kodni korak.

Podaci koje treba zatražiti od tenant administratora navedeni su u [05-auth.md](05-auth.md#2-entra-id-konfiguracija) - dvije registracije (API i SPA), izložen scope `access_as_user`, i redirect URI za SPA.
---

## Odluka 13 - Rola u bazi, ne u Entra App Roles

**Potvrđeno.**

`User.Role` kolona, bootstrap prvog admina preko `Auth:AdminEmails` konfiguracije.

Entra App Roles su "ispravniji" pristup u velikim organizacijama, ali zahtijevaju pravo konfiguriranja app registracije i dodjeljivanja rola u tenantu - što je često pravo koje ima samo tenant administrator, a ne vlasnik aplikacije. Rola u bazi znači da promjena admina ne zahtijeva ničiju intervenciju izvan aplikacije, i vidljiva je odmah bez čekanja novog tokena.

Prelazak na App Roles kasnije je promjena jednog koraka u claims transformaciji (čitanje `roles` claima umjesto kolone), bez promjene policyja, endpointa ni frontenda.

---

## Odluka 14 - Cohort se dodjeljuje automatski

**Potvrđeno.**

Novi korisnik pri prvoj prijavi dobiva cohort s `IsActive = true`; admin ga može promijeniti.

Ako aktivnog cohorta nema, korisnik se kreira s `CohortId = null` i vidi module po globalnoj dostupnosti. Blokiranje do ručne dodjele bi značilo da na početku semestra prvi studenti ne mogu ući u aplikaciju dok ih admin ne obradi.

Promjena cohorta mijenja dostupnost modula, ali nikad ne dira `StudentProgress`.

---

## Odluka 15 - `ConfigJson` kao `jsonb`

**Potvrđeno.**

Parametri vježbe (`frequenciesHz`, `gainsDb`, `q`, `options`) su `jsonb`, a ne zasebne kolone ili normalizirane tablice.

Shema configa ovisi o `ExerciseType`, i to je upravo mjesto gdje se dodaju novi tipovi vježbi. Normalizacija bi značila tablicu po tipu vježbe i migraciju za svaki novi tip - točno onaj refaktoring koji treba izbjeći.

Rizik `jsonb`-a je da neispravan config puca u runtimeu. Zato Phase 3 obavezno uključuje seed validacijske testove koji deserializiraju **svaki** config i provjeravaju dopuštene vrijednosti, pa neispravan seed pada u CI-u a ne pri prvom studentskom testu. Popis u [02-data-model.md](02-data-model.md#seed-validacijski-testovi).

Ostali podaci nisu u JSON-u - sve po čemu se filtrira, sortira ili joina je normalna kolona.

---

## Odluka 16 - Segment je tablica, ne enum

**Potvrđeno.**

`ExerciseSegment` tablica (4 reda: `eq/boost`, `eq/cut`, `eq/combined`, `compression/detection`).

Progression tree u UI-u iz nje vuče labele i poredak, pa frontend ne mora znati da EQ ima tri segmenta. Compression s jednim segmentom renderira ista komponenta bez posebnog koda. Enum bi značio mapiranje enum → prikazni naziv u frontendu, tj. znanje o strukturi modula na dva mjesta.

---

## Odluka 17 - Balansirana randomizacija pitanja

**Potvrđeno.**

Umjesto neovisnog slučajnog odabira po pitanju, gradi se multiset odgovora raspoređen kroz 14 pitanja, pa se promiješa uz pravilo "ne više od dva identična odgovora u nizu".

- EQ sa 7 frekvencija → svaka točno 2 puta
- EQ BOOST L1 sa 4 frekvencije → 4/4/3/3
- Compression L3 s 3 opcije → 5/5/4
- Compression L1 s 2 opcije → 7/7

Čisti random po pitanju bi uz 14 pitanja redovito davao testove u kojima se neka frekvencija ne pojavi nijednom, a druga pet puta. To mijenja težinu između pokušaja i čini rezultate neusporedivima, a studenta uči da pogađa najčešći odgovor. `RandomSeed` se sprema u sesiju radi reproducibilnosti pri debugiranju.

Za `EqFrequencyAndDirection` balansira se i smjer (7 boost / 7 cut), pa smjer nema pristranost.

---

## Odluka 18 - UI samo na hrvatskom, bez i18n biblioteke

**Potvrđeno.**

Stringovi su centralizirani u `src/lib/strings.ts`, bez `i18next` ili sličnog.

Korisnici su studenti Algebre; višejezičnost nije zahtjev. Centralizacija stringova znači da bi uvođenje i18n biblioteke kasnije bilo zamjena jednog modula, a ne pretraživanje teksta po komponentama.

Tekstovi pitanja (`prompt`) dolaze **sa servera** jer su dio konfiguracije vježbe, a ne UI teksta. Nazivi modula, segmenata i izvora također.

---

## Odluka 19 - Practice mode za EQ i za Compression

**Potvrđeno. Oba modula imaju practice mode.**

Endpoint je zato generaliziran na `GET /api/modules/{moduleSlug}/sources/{sourceSlug}/practice`, a oblik odgovora ovisi o modulu (polje `mode`):

- **EQ** (`mode: "eqBand"`) - jedan asset, lista frekvencija, lista gainova, `q`; student sam bira parametre i sluša rezultat u realnom vremenu, s A/B prema ravnom signalu
- **Compression** (`mode: "compressionVariants"`) - lista svih šest varijanti izvora s **vidljivim** labelama, za slobodno A/B prebacivanje

U practice modeu labele **smiju** biti vidljive - to je cijela svrha: student treba naučiti kako 4:1 zvuči prije nego to mora prepoznati. Test i practice su strogo odvojeni: practice ne kreira `TestSession`, ne daje score, ne otključava levele i ne piše ništa u `StudentProgress`.

Practice zahtijeva samo dostupnost modula, ne otključan level - student mora moći slušati prije prvog testa.

Oba practice ekrana idu u svoje faze: EQ practice u Phase 7 (isti engine kao test), Compression practice u Phase 8.

---

## Odluka 20 - Practice mode otkriva mapiranje varijanti

**Potvrđeno - posljedica odluke 19, rizik prihvaćen.**

Compression practice mora studentu pokazati koja je varijanta koja, pa vraća `assetId` uz čitljivu labelu (`ratio-12` → "12:1"). Test, s druge strane, audio poslužuje preko tokena pitanja bez naziva varijante (odluka 2).

Otvara se suptilan kanal: bajtovi koje vraća `/api/audio/assets/{id}` u practice modeu i `/api/audio/questions/{token}` u testu su **identični** za istu varijantu. Student koji bi skriptirao DevTools mogao bi hashirati odgovore i time razriješiti koja varijanta se svira u testu, bez slušanja.

Rizik se prihvaća jer:

- zahtijeva pisanje skripte koja hashira mrežne odgovore i usporeduje ih s prethodno prikupljenom tablicom - nesrazmjerno naporu potrebnom da se vježba stvarno riješi slušanjem
- ista klasa rizika već postoji i priznata je za EQ (odluka 10), pa je model povjerenja prema klijentu konzistentan: aplikacija je alat za trening, ne za ocjenjivanje
- pedagoška vrijednost compression practicea je veća od ovog rizika - bez practicea student nema način naučiti zvuk pojedinog omjera

Ublažavanja koja **nisu** uvedena jer su nesrazmjerna: posluživanje practice audija kao zasebnih, drukčije enkodiranih fajlova (dvostruki set assetova i dvostruka DAW priprema), ili ubacivanje slučajnog offseta/tišine u stream pitanja (mijenja zvuk i troši procesorsko vrijeme na svaki zahtjev).

Ako se aplikacija ikad bude koristila za ocjenjivanje, ovo je - zajedno s odlukom 10 - razlog zašto testovi moraju biti u nadziranim uvjetima.

Odgovor: Potvrđujem, želim practice mod i za kompresiju.
---

## Odluka 21 - .NET 10 umjesto .NET 8

**Potvrđeno.**

`TargetFramework` je `net10.0`, EF Core 10, i sve prateće biblioteke u odgovarajućim verzijama.

Početna specifikacija je tražila .NET 8. Promijenjeno je jer:

- **.NET 8 izlazi iz podrške 10. studenog 2026.** Već je u maintenance fazi, u kojoj dobiva samo sigurnosne zakrpe, bez ispravaka grešaka. Projekt koji danas počinje bio bi izvan podrške praktički odmah.
- **.NET 10 je LTS do 14. studenog 2028.** Sljedeći LTS nakon 8 je upravo 10; .NET 9 nije opcija jer mu podrška istječe istog dana kao i .NET 8.
- Na razvojnom računalu je instaliran **samo SDK 10**.
- Nema nijedne prednosti .NET-a 8 za ovu aplikaciju: nema postojećeg koda, nema ovisnosti koje bi bile problem, i sve biblioteke iz plana (EF Core, Npgsql, Microsoft.Identity.Web) podržavaju 10.

Promjena ne dira nijedan entitet, endpoint ni drugu odluku - samo verzije u `.csproj`.

---

## Odluka 22 - Git od početka, commit po fazi

**Potvrđeno.**

Repozitorij se inicijalizira prije Phase 2, s jednim commitom po završenoj fazi.

Razvoj kroz deset faza s međukoracima potvrde bez verzioniranja znači da nema povratka na prethodno stanje, nema pregleda što je koja faza promijenila, i nema zaštite od slučajnog brisanja.

`.gitignore` isključuje `bin/`, `obj/`, `node_modules/`, `.env*`, `appsettings.Development.json`, `*.user` i `backend/src/CriticalListeningLab.Api/wwwroot/audio/`. Audio se **ne** verzionira - to su veliki binarni fajlovi koji bi napuhali repozitorij, a nastaju u DAW-u i čuvaju se odvojeno (kasnije u Cloudflare R2).

---

## Odbijene alternative - kratki zapis

Stvari koje su razmatrane i **nisu** ušle, da se ne otvaraju ponovno:

- **Slojevita arhitektura, repository, MediatR, CQRS** - vidi odluku 3
- **Renderiranje EQ-a na serveru** - suprotno zahtjevu, 224 fajla, gubitak practice modea (odluka 10)
- **Loudness normalizacija u pregledniku** - uništila bi razliku koju Compression vježba mjeri, i uklonila legitimnu informaciju iz EQ vježbe ([06-audio.md](06-audio.md#gain-i-headroom))
- **Per-question gain kompenzacija u EQ-u** - različita razina među pitanjima sama postaje trag
- **`DynamicsCompressorNode` kao limiter u lancu** - dinamički mijenja zvuk, u Compression modulu bio bi fatalan; headroom se rješava računom
- **Spremanje `ScorePercentage`** - izvedena vrijednost, spremanjem se omogućuje nekonzistentnost
- **Usporedba postotaka za prolaz** - `correctAnswers >= PassThreshold` je cjelobrojno i nedvosmisleno
- **Zaseban `/api/progress` resurs** - napredak nema smisla bez konteksta modula i izvora; tri endpointa s istim podatkom su tri mjesta za neusklađenost (vidi [04-api.md](04-api.md#razlike-od-početnog-prijedloga))
- **Redirect na stabilan obfusciran audio URL** - URL stabilan kroz pitanja postaje trag nakon prvog feedbacka (odluka 2)
- **WAV za EQ izvore** - FLAC je lossless uz ~50% manji fajl, a EQ izvor se cachira jednom (odluka 11)
- **Dvostruki set audio assetova (jedan za practice, jedan za test)** - dvostruka DAW priprema i dvostruko održavanje radi ublažavanja rizika koji zahtijeva skriptiranje DevToolsa (odluka 20)
- **Slučajni offset ili tišina u streamu pitanja** - mijenja zvuk koji vježba mjeri i troši procesorsko vrijeme na svaki zahtjev (odluka 20)
- **Guid rute** - vidi odluku 9
- **PostgreSQL enum tipovi** - dodavanje člana zahtijeva migraciju tipa; `int` s eksplicitnim vrijednostima ne
