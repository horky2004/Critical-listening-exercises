# 09 - Deployment

Backend i frontend se deployaju **odvojeno**. API ne servira SPA. Migracije se **ne** primjenjuju pri startu.

---

## 1. Što treba prije produkcije

- PostgreSQL (produkcijski plan: Neon.tech). Connection string **nije** u git-u.
- Entra ID registracije iz [05-auth.md](05-auth.md#što-zatražiti-od-tenant-administratora): API i SPA, scope `access_as_user`, produkcijski redirect URI.
- Audio fajlovi na API hostu (`wwwroot/audio` dok je `AudioStorage:Provider` = `Local`). Fajlovi se ne verzioniraju.
- CORS origin = točan origin SPA-a (`https://…`, bez trailing slasha osim ako SPA stvarno tako radi).

---

## 2. API

Primjer konfiguracije: `backend/src/CriticalListeningLab.Api/appsettings.Production.json.example`. Kopija `appsettings.Production.json` je u `.gitignore`.

Obavezno:

| Ključ | Vrijednost |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings:Database` | PostgreSQL (env, key vault ili user-secrets na serveru) |
| `Auth:UseDevBypass` | `false` |
| `AzureAd:TenantId` / `ClientId` / `Audience` | API app registracija |
| `Cors:AllowedOrigins` | origin SPA-a |
| `Auth:AdminEmails` | bootstrap admina (samo pri prvom provisioningu) |

Ako je `UseDevBypass` uključen u Production, proces **odbija startati**.

HTTPS je obavezan. Iza reverse proxyja (Azure, nginx) API čita `X-Forwarded-Proto` / `X-Forwarded-For`.

### Migracije

Iz mape API projekta, **prije** prvog starta i prije svakog releasea koji ima novu migraciju:

```bash
dotnet ef database update
```

Seed kataloga (`CatalogSeeder.EnsureAsync`) radi pri startu i idempotentan je. Ne zamjenjuje migracije.

### Health

`GET /health` je javan, ne dira bazu, odgovor `{ "status": "ok" }`. Koristi ga load balancer.

### Audio

Dok je provider `Local`, deployaj FLAC/MP3 uz API, u putanju iz `AudioStorage:LocalRoot`. Promjena na R2 je kasnija zamjena implementacije, ne URL-ova koje vidi klijent.

### Build

```bash
dotnet publish backend/src/CriticalListeningLab.Api/CriticalListeningLab.Api.csproj -c Release
```

---

## 3. Frontend (SPA)

Primjer: `frontend/.env.production.example`. Vite **upisuje** `VITE_*` u bundle pri `npm run build` — nisu runtime env varijable na static hostu.

| Varijabla | Vrijednost |
| --- | --- |
| `VITE_API_BASE_URL` | javni origin API-ja |
| `VITE_USE_DEV_AUTH` | `false` |
| `VITE_ENTRA_CLIENT_ID` | SPA app registracija |
| `VITE_ENTRA_TENANT_ID` | Algebra tenant |
| `VITE_API_SCOPE` | `api://<api-client-id>/access_as_user` |

Ako produkcijski build ima `VITE_USE_DEV_AUTH=true`, aplikacija baca grešku pri startu.

```bash
cd frontend
npm ci
npm run build
```

`dist/` ide na static host (Azure Static Web Apps, Blob+CDN, nginx). SPA fallback: sve rute → `index.html`. Redirect URI u Entri mora biti točan origin SPA-a.

---

## 4. Provjera nakon deploya

1. `GET https://<api>/health` → `ok`
2. Prijava `@algebra.hr` i `@student.algebra.hr` → `User` redovi u bazi, ispravne role
3. Student na `/admin` → početna; admin vidi administraciju
4. EQ i Compression slušanje (FLAC/MP3) u Chrome i Edge
5. CORS: zahtjev s SPA origina prolazi, drugi origin ne

---

## 5. Testovi prije releasea

```bash
dotnet test
cd frontend && npm test && npm run build
```
