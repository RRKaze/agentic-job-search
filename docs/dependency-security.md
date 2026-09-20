# Dependency security

## 2026-09-20 frontend review

The Angular 19 dependency tree reported 31 advisories, including seven production advisories and one critical development-tooling advisory. Angular 19 had no patched release for the runtime findings, so the application was migrated through each supported major version to Angular 22 using the official Angular CLI migrations.

After the migration:

- `npm audit --omit=dev` reports zero vulnerabilities.
- `npm audit` reports zero vulnerabilities across the complete dependency tree.
- The production build completes successfully.
- The application uses Angular's current esbuild/Vite build package instead of the deprecated webpack compatibility package.

No dependency-security exception is currently required. Continue to avoid exposing the local development server to untrusted networks.

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
