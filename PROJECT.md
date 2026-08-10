# Project: ReactiveForm ASP.NET Core 8.0 MVC Conversion (`landingmvc`)

## Architecture
- Framework: ASP.NET Core 8.0 MVC
- Containerization: Multi-stage Dockerfile (SDK build step + ASP.NET Core 8.0 runtime step)
- Views & Assets: Razor view `Views/Home/Index.cshtml`, `wwwroot/css/site.css`, `wwwroot/art.jpg`
- Controllers & Services:
  - `Controllers/FormController.cs` (Handles AJAX POST `/api/form/submit` and `/api/form/connect`)
  - `Services/IEmailValidationService.cs` & `EmailValidationService.cs` (RFC 5322 regex validation, 44+ domain blocklist, async MX verification via api.mailcheck.ai with timeout)
  - `Services/IGoogleSheetsService.cs` & `GoogleSheetsService.cs` (Appends rows to Google Sheet tabs `waitinglist` or `connectwithteam` via Google Sheets API v4 using API key / HttpClient)

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| 1 | ASP.NET Core 8.0 MVC Setup & Docker Conversion | Scaffolding `landingmvc.csproj`, `Program.cs`, `Dockerfile`, porting `Index.cshtml`, `site.css`, `art.jpg` | None | DONE |
| 2 | Backend Email Validation & AJAX Submission | Backend `EmailValidationService`, `FormController`, frontend Fetch/AJAX form handlers & error UI | M1 | DONE |
| 3 | Google Sheets Integration | `GoogleSheetsService` appending to `waitinglist` & `connectwithteam` tabs via Google Sheets API v4 | M2 | DONE |
| 4 | Verification & Docker Container Validation | Build verification, Docker test (`docker build`), E2E testing & project memory update | M1, M2, M3 | DONE |

## Interface Contracts
### Frontend AJAX ↔ `FormController`
- `POST /Form/SubmitJoinBeta`: `{ email: string }` -> `{ success: bool, message: string, errors: string[] }`
- `POST /Form/ConnectTeam`: `{ email: string, name?: string, message?: string }` -> `{ success: bool, message: string, errors: string[] }`

### `FormController` ↔ `GoogleSheetsService`
- `AppendRowAsync(string tabName, IList<object> values)` -> `Task<bool>`

## Code Layout
- `e:/landingmvc/landingmvc.csproj`
- `e:/landingmvc/Program.cs`
- `e:/landingmvc/Dockerfile`
- `e:/landingmvc/Controllers/HomeController.cs`
- `e:/landingmvc/Controllers/FormController.cs`
- `e:/landingmvc/Services/EmailValidationService.cs`
- `e:/landingmvc/Services/GoogleSheetsService.cs`
- `e:/landingmvc/Views/Home/Index.cshtml`
- `e:/landingmvc/Views/Shared/_Layout.cshtml`
- `e:/landingmvc/wwwroot/css/site.css`
- `e:/landingmvc/wwwroot/art.jpg`
