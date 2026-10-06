# Dependency security

## 2026-09-20 frontend review

The Angular 19 dependency tree reported 31 advisories, including seven production advisories and one critical development-tooling advisory. Angular 19 had no patched release for the runtime findings, so the application was migrated through each supported major version to Angular 22 using the official Angular CLI migrations.

After the migration:

- `npm audit --omit=dev` reports zero vulnerabilities.
- `npm audit` reports zero vulnerabilities across the complete dependency tree.
- The production build completes successfully.
- The application uses Angular's current esbuild/Vite build package instead of the deprecated webpack compatibility package.

No dependency-security exception is currently required. Continue to avoid exposing the local development server to untrusted networks.

## 2026-10-05 router security update

The production audit failed on `@angular/router` 22.1.7 for [GHSA-ff3f-86qr-9cv3](https://github.com/advisories/GHSA-ff3f-86qr-9cv3). Pin the Angular framework packages, service worker, and compiler CLI together at 22.2.1 to include the fix and satisfy their exact peer dependencies. The existing build tools and unrelated locked packages remain unchanged.

Validation with CI's Node.js 22.22.3: clean `npm ci`, all nine model/PWA tests, production build, and `npm audit --omit=dev --audit-level=high` pass. The production audit reports zero vulnerabilities. The existing audit gate remains enabled; no advisory suppression was added.

The full development dependency audit currently reports eight findings (six high and two critical, including propagated findings) involving `braces`/Karma, `piscina`, and `source-map-js`. These are outside the production dependency audit and need a separate development-tooling update; the September full-audit result above is historical, not a current clean bill of health.

## Controls

- Node.js is pinned through `.nvmrc` and `package.json` engine metadata.
- CI installs dependencies with `npm ci`.
- CI fails on high or critical production dependency advisories.
- Major framework upgrades must use Angular's migration tooling and pass the production build.

Run the audits locally from `apps/web`:

```bash
npm audit --omit=dev
npm audit
```
