# Accounts and first-login onboarding

Registration (`/register`) accepts a name, email, and 12–128 character password. Email is trimmed and normalized; the database enforces uniqueness. ASP.NET Core PasswordHasher stores a salted password hash. Successful registration signs in immediately. `/sign-in` accepts existing accounts. Both lead to `/onboarding` until a career stage is saved, then to the dashboard.

The two explicit career stages are `new_graduate` and `experienced_worker`. Neither is preselected. The choice is stored in PostgreSQL and can be changed, with the display name, from **Personal profile** (`/profile`). Email is read-only. The existing fictional candidate evidence is never copied to a real account. Resume upload and evaluation remain future work.

Sessions use an eight-hour, HttpOnly, SameSite=Strict cookie; production requires HTTPS. No credentials or session tokens are stored in browser local storage. Browser mutations require `X-Agentic-Request: 1` and cross-origin requests are restricted to the configured local frontend origins. Account endpoints have a per-IP limit of 30 requests per minute. Authentication failures use the same error for unknown emails and incorrect passwords. Sign-out clears the cookie and the local job list. Email verification, password recovery, MFA, and server-side session revocation are not implemented in this milestone; do not treat this as complete public-launch account management.

## Run locally

Start the API as before, then `npm start` in `apps/web`. The Angular development proxy sends relative `/api` requests to `127.0.0.1:5156`, keeping cookies on the frontend origin. If Angular runs in Docker, add `--proxy-config proxy.docker.conf.json` to use `host.docker.internal:5156`. Production hosting must proxy `/api` to the API on the same HTTPS origin and persist ASP.NET Core data-protection keys across restarts/replicas.

## Existing imported workspace

Jobs created by an account are assigned its ID. Jobs and candidate profiles can only be read by their owner. Import endpoints retain their separate machine-token authentication and do not use browser cookies.

The migration preserves existing jobs with null owner IDs. They are hidden from all accounts by default. A trusted local administrator may associate the legacy imported workspace with one existing account by setting `Accounts__LegacyWorkspaceOwnerId` to that account's UUID (available from authenticated `/api/account/me`) and restarting the API. Never infer ownership from first registration, an unverified email, or user-supplied request parameters. Future imports with null owners stay visible only to the explicitly configured owner. This bridge supports one legacy import owner; per-user import configuration is deferred.

No real account, password, email, or legacy owner ID is committed. A new account will initially see an empty dashboard until it adds jobs or an administrator assigns the legacy workspace.

## Checks

`dotnet test` covers registration, validation, email normalization/duplicates, cookie attributes, both career stages, persistence across sign-out and sign-in, unauthorized reads, CSRF-header rejection, separate user job and candidate-profile access, and existing import/scoring regressions. `npm run build` validates frontend templates and route wiring. The import smoke script checks database counts directly because its machine token must not authorize user job-list access.
