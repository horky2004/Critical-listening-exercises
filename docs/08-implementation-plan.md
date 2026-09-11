# 08 - Plan implementacije

Phase 1 je zatvorena: sve 20 odluka u [07-decisions.md](07-decisions.md) su potvrđene i specifikacija je fiksirana. Ovaj dokument je plan izvedbe.

Pravilo rada ostaje: **faze se ne preskaču i ne implementiraju se unaprijed.** Svaka faza završava buildom i testovima, pa se čeka potvrda.

---

## 1. Stanje razvojne okoline

Provjereno na ovom računalu:

| Alat | Stanje | Potrebno za |
| --- | --- | --- |
| .NET SDK | 10.0.204 | Phase 2+ |
| .NET runtime | 8.0.27 i 10.0.8, uz ASP.NET Core oba | Phase 2+ |
| nuget.org | konfiguriran | Phase 2+ |
| Git | **2.55.0** - instaliran, repozitorij inicijaliziran | odmah |
| Node.js / npm | **nije instaliran** | Phase 6+ |
| PostgreSQL | **nije instaliran** (ni Docker) | Phase 2 (migracija) |

### 1.1 Verzija .NET-a - odlučeno: .NET 10

`TargetFramework` je `net10.0`, EF Core 10. Specifikacija je prvotno tražila .NET 8, ali je **.NET 8 izvan podrške od 10. studenog 2026.** i već je u maintenance fazi. .NET 10 je LTS do 11/2028. Puno obrazloženje u [07-decisions.md](07-decisions.md#odluka-21---net-10-umjesto-net-8).

Promjena ne dira nijedan entitet, endpoint ni drugu odluku.

### 1.2 Git - odlučeno: uveden odmah

Git 2.55 je instaliran, repozitorij je inicijaliziran na branchu `main`, `.gitignore` je postavljen. Jedan commit po završenoj fazi. Audio se ne verzionira. Obrazloženje u [07-decisions.md](07-decisions.md#odluka-22---git-od-početka-commit-po-fazi).

### 1.3 Node.js - potreban od Phase 6

Za frontend treba Node.js LTS (v22 ili v24) s npm-om. Nije blokada za Phase 2-5, ali treba ga instalirati prije Phase 6. Bolje ranije, da instalacija ne prekida rad na frontendu.

### 1.4 Baza podataka - odlučeno: lokalni PostgreSQL

Odabran je lokalni PostgreSQL, uz prelazak na Neon.tech kasnije. Docker nije instaliran, pa se lokalni server postavlja jednim od dva načina:

- **PostgreSQL kao Windows servis** (`winget install PostgreSQL.PostgreSQL.18`) - lakše, bez WSL2 i bez Docker Desktop licence, startuje automatski
- **Docker Desktop** - teže (WSL2, ~2 GB, moguć restart), ali korisno ako se kasnije žele Testcontainers za integracijske testove

Preporuka je nativna instalacija za sada. Testna baza za integracijske testove može biti druga baza na istom serveru, što uklanja potrebu za Dockerom i u Phase 5.

Connection string ide u `dotnet user-secrets`, nikad u `appsettings.json`. Potreban je prije zadnjeg koraka Phase 2; sve do tada (projekt, entiteti, `DbContext`, auth) radi bez baze.

Produkcijski plan ostaje Neon.tech - prelazak je promjena connection stringa, jer je shema identična.

---

## 2. PHASE 2 - Backend skeleton

Cilj: aplikacija koja se builda, startuje, ima kompletnu shemu baze u migraciji, i radeću autentikaciju kroz dev bypass.

**Nije u dosegu Phase 2:** seed podaci, progression logika, test engine, studentski i admin endpointi, frontend. To su Phase 3-6.

### 2.1 Struktura i projekti

```
App/
  CriticalListeningLab.sln
  .gitignore
  backend/
    src/CriticalListeningLab.Api/CriticalListeningLab.Api.csproj
    tests/CriticalListeningLab.Tests/CriticalListeningLab.Tests.csproj
```

`TargetFramework` je `net10.0` za oba projekta, `Nullable` i `TreatWarningsAsErrors` uključeni, `ImplicitUsings` uključen.

NuGet paketi za API (verzije 10.x gdje postoje):

- `Npgsql.EntityFrameworkCore.PostgreSQL`
- `Microsoft.EntityFrameworkCore.Design`
- `EFCore.NamingConventions` - `snake_case` mapiranje
- `Microsoft.Identity.Web` - validacija Entra tokena
- `Swashbuckle.AspNetCore` - OpenAPI za ručnu provjeru u razvoju

Za testni projekt: `xunit`, `xunit.runner.visualstudio`, `FluentAssertions`, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.NET.Test.Sdk`.

### 2.2 Entiteti

Dvanaest klasa u `Domain/Entities/`, točno prema [02-data-model.md](02-data-model.md):

`Cohort`, `User`, `Module`, `CohortModuleAvailability`, `AudioSource`, `AudioAsset`, `ExerciseSegment`, `ExerciseLevel`, `LevelUnlockRequirement`, `StudentProgress`, `TestSession`, `TestSessionQuestion`

Plus enumi: `UserRole`, `ExerciseType`, `TestSessionStatus`, `UnlockRequirementType`, `LevelStatus`.

Enumi imaju eksplicitne cjelobrojne vrijednosti i mapiraju se kao `int`.

### 2.3 DbContext i konfiguracije

`Data/AppDbContext.cs` s `DbSet`-ovima, plus jedan `IEntityTypeConfiguration<T>` po entitetu u `Data/Configurations/`. Konfiguracije nose ono što se ne smije izgubiti:

- unique indeksi: `User.EntraObjectId`, `User.Email`, `Cohort.Name`, `Module.Slug`, `(AudioSource.ModuleId, Slug)`, `(AudioAsset.AudioSourceId, VariantSlug)`, `(ExerciseSegment.ModuleId, Key)`, `(ExerciseLevel.SegmentId, LevelNumber)`, `(CohortModuleAvailability.CohortId, ModuleId)`, `(LevelUnlockRequirement.ExerciseLevelId, RequiredExerciseLevelId)`, `(StudentProgress.UserId, AudioSourceId, ExerciseLevelId)`, `(TestSessionQuestion.TestSessionId, QuestionIndex)`, `TestSessionQuestion.AudioToken`
- filtrirani unique indeks `Cohort.IsActive` s filtrom `is_active` - najviše jedan aktivan cohort
- pomoćni indeksi: `(StudentProgress.UserId, AudioSourceId)`, `(TestSession.UserId, ExerciseLevelId, AudioSourceId, Status)`
- `jsonb` za `ExerciseLevel.ConfigJson` i `TestSessionQuestion.PromptJson`
- `timestamptz` za sve `DateTimeOffset`
- `DeleteBehavior.Restrict` svugdje osim `TestSession → TestSessionQuestion` (`Cascade`)
- dvije FK veze `LevelUnlockRequirement → ExerciseLevel` (`ExerciseLevelId` i `RequiredExerciseLevelId`) s eksplicitnim `WithMany`, inače EF ne može razlučiti relacije

### 2.4 Autentikacija

Prema [05-auth.md](05-auth.md), u `Auth/`:

- `AuthOptions` - `AllowedEmailDomains`, `AdminEmails`, `UseDevBypass`, `DevUser`
- JWT bearer registracija preko `Microsoft.Identity.Web` (`AzureAd` sekcija)
- `DevAuthenticationHandler` - aktivan samo kad `UseDevBypass`
- `UserProvisioningClaimsTransformation` - domenska provjera, JIT upsert korisnika, dodavanje `app:userId` / `app:role` / `app:cohortId` claimova
- policyji `RequireStudent` i `RequireAdmin`, plus fallback `RequireAuthenticatedUser()` da je svaki endpoint zaštićen po defaultu
- **guard u `Program.cs`**: ako je okolina Production i `UseDevBypass == true`, aplikacija baca izuzetak pri startu

Obje grane (Entra i bypass) prolaze istu domensku provjeru i isti provisioning. Entra grana ostaje neprovjerena do dobivanja app registracija (odluka 12).

### 2.5 Infrastruktura zahtjeva

- CORS iz `Cors:AllowedOrigins`, bez `AllowCredentials`
- `AddProblemDetails()` i globalni exception handler koji vraća RFC 7807 bez internih detalja
- JSON: `camelCase`, enumi kao stringovi u odgovorima
- `camelCase` rute u malim slovima
- `GET /health` - javni
- `GET /api/me` - prvi pravi endpoint, dokazuje da provisioning radi

### 2.6 Migracija

```
dotnet ef migrations add InitialCreate -p backend/src/CriticalListeningLab.Api
dotnet ef database update -p backend/src/CriticalListeningLab.Api
```

Migracija se **ne** primjenjuje automatski pri startu aplikacije - to je u produkciji rizik. Primjenjuje se eksplicitno.

### 2.7 Testovi Phase 2

- model baze se gradi bez greške (`Database.GenerateCreateScript()` nad `AppDbContext`) - hvata pogreške u konfiguracijama bez prave baze
- svaki unique indeks iz 2.3 postoji u modelu
- zahtjev bez tokena na `/api/me` → `401`
- bypass s dopuštenom domenom → korisnik kreiran, rola `Student`
- bypass s nedopuštenom domenom → `403`, korisnik **nije** kreiran
- e-mail na `AdminEmails` → rola `Admin`
- drugi zahtjev istog `oid` s promijenjenim e-mailom → isti `User` red, osvježen e-mail
- `UseDevBypass = true` uz Production okolinu → start pada

### 2.8 Kriteriji završetka

1. `dotnet build` bez greške i bez upozorenja u našem kodu
2. `dotnet test` - svi testovi prolaze
3. `dotnet ef migrations add InitialCreate` generira migraciju koja sadrži svih 12 tablica
4. `dotnet ef database update` uspješno kreira shemu na Neonu
5. Aplikacija startuje, `GET /health` vraća `200`
6. `GET /api/me` uz bypass vraća profil i kreira `User` red u bazi
7. Aplikacija odbija startati s bypassom u Production okolini

---

## 3. PHASE 3 - Baza i progression

Cilj: sadržaj u bazi i dokazano ispravna unlock logika.

- Seed (`Data/Seed/`): cohort `2025/26`, dva modula, četiri EQ i dva Compression izvora, 4 + 12 audio assetova, četiri segmenta, 13 EQ i 3 Compression levela, 14 unlock zahtjeva (12 EQ + 2 Compression). Deterministički `Guid`-ovi iz stabilnih ključeva, idempotentno.
- Typed config recordi (`EqLevelConfig`, `CompressionLevelConfig`) i deserializacija po `ExerciseType`
- `UnlockEvaluator` i `ScoreRules` - čiste statičke klase
- `ProgressionService` - `GetTreeStateAsync`, `IsUnlockedAsync`, `ApplyTestResultAsync`
- `ModuleAccessService` - globalna i cohort dostupnost

Obavezni testovi su popisani u [03-progression.md](03-progression.md#7-obavezni-testovi-phase-3): scoring (uključujući 11/14 fail, 12/14 pass, `BestScore` se ne smanjuje), unlock (BOOST L3 → CUT L1 **i** BOOST L4, CUT L3 → COMBINED L1), izolacija (student A vs B, Drums vs Vocal, EQ vs Compression), pristup, i seed validacija.

Testovi koji traže pravu bazu (seed validacija, zbog `jsonb`) koriste **zasebnu bazu na lokalnom PostgreSQL serveru** (`cll_test`), koja se prije svakog runa presoča migracijom. Testcontainers se ne uvode jer Docker nije instaliran, a nativni server pokriva istu potrebu. Čistim unit testovima - a to je većina obaveznih iz Phase 3 - baza ne treba uopće.

---

## 4. PHASE 4 - Test engine

- `IQuestionGenerator` registriran po `ExerciseType`; `EqFrequencyGenerator`, `EqFrequencyAndDirectionGenerator`, `CompressionChoiceGenerator`
- `BalancedAnswerSequence` - multiset odgovora kroz 14 pitanja uz pravilo "ne više od dva ista u nizu" (odluka 17)
- `TestSessionService` - kreiranje sesije sa svih 14 pitanja, zaprimanje odgovora, automatska finalizacija na zadnjem odgovoru u jednoj transakciji (odluka 7), idempotentnost
- `AudioToken` generiranje po pitanju

Testovi: balansiranost raspodjele za sve tipove levela, nema tri ista odgovora u nizu, `CorrectAnswerKey` se ne serializira prije odgovora, dvostruka finalizacija ne mijenja `AttemptCount`, odgovor van redoslijeda odbijen.

---

## 5. PHASE 5 - REST API

Svi endpointi iz [04-api.md](04-api.md), plus `IAudioStorage` i `LocalFileAudioStorage` (audio endpointi su dio API-ja).

Integracijski testovi preko `WebApplicationFactory` - popis u [04-api.md](04-api.md#8-integracijski-testovi-phase-5), uključujući provjere nad raw JSON-om da `correctAnswerKey` i `storageKey` ne izlaze kad ne smiju.

---

## 6. PHASE 6 - React frontend

Vite + React + TS + Tailwind, React Router, TanStack Query, MSAL (s dev bypassom). Login, dashboard, modul, izvor s progression treeom, test UI, ekran rezultata.

Progression tree se crta iz `requiredLevelIds` u tree odgovoru, bez EQ-specifične logike. Stanja `Locked`/`Unlocked`/`InProgress`/`Completed` razlikuju se bojom **i** oblikom/ikonom.

Zahtijeva Node.js (1.2).

---

## 7. PHASE 7 - Web Audio EQ

**Prvi korak: provjera FLAC dekodiranja** kroz `decodeAudioData` na Chrome, Firefox, Edge i Safari (odluka 11). Ako padne, fallback je WAV kroz promjenu seeda.

Zatim `audioContext` singleton, `AudioBufferCache`, `EqPlaybackEngine` s wet/dry granama i konstantnom master atenuacijom, rampe protiv klikova prema tablici u [06-audio.md](06-audio.md#prevencija-klikova-i-pops), EQ practice ekran.

Zahtijeva pripremljene EQ izvore (FLAC, peak -18 dBFS).

---

## 8. PHASE 8 - Compression

`ClipPlayer` za test, `CompressionAbPlayer` s paralelnim varijantama za practice (pozicijski usklađeno prebacivanje), UI vježbe, Compression practice ekran.

Zahtijeva pripremljene i **gain-matchane** varijante (±0.5 LU), sve iste dužine i istog isječka. Ovdje se provjerava i je li MP3 320 dovoljan za `light` varijantu bubnjeva (odluka 11).

---

## 9. PHASE 9 - Admin

Cohorti, dostupnost modula globalno i po cohortu, pregled studenata i njihovog napretka.

---

## 10. PHASE 10 - Testiranje i polish

Pristupačnost (tipkovnica, kontrast, stanja koja nisu samo boja), responsive za desktop i tablet, audio edge caseovi iz [06-audio.md](06-audio.md#8-edge-caseovi-i-testiranje-phase-7-8-10), error i loading stanja, produkcijska konfiguracija, deployment dokumentacija.

---

## 11. Što treba od tebe i kada

| Kada | Što |
| --- | --- |
| **Do kraja Phase 2** | Instaliran lokalni PostgreSQL i poznata `postgres` lozinka (1.4) |
| Prije Phase 6 | Instaliran Node.js (1.3) |
| Prije Phase 7 | EQ izvori: FLAC, 4 fajla, peak -18 dBFS, 8-15 s, loopabilni |
| Prije Phase 8 | Compression varijante: 12 fajlova, MP3 320, ~10 s, gain-matchane ±0.5 LU |
| Paralelno | Zahtjev za Entra app registracije ([05-auth.md](05-auth.md#što-zatražiti-od-tenant-administratora)) |
