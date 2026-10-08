# Database backup and recovery

Run these commands on a trusted computer with Python 3.11+ and PostgreSQL client tools of the same major version as the Neon database (currently PostgreSQL 18), using the matching major version for restoration too. A PostgreSQL 17 pg_dump cannot back up a PostgreSQL 18 server.

## Take a backup

Use the direct Neon endpoint (no `-pooler` in its host). Supply individual connection settings, rather than the .NET connection string or a password-bearing URI:

```sh
export PGHOST='<direct Neon host>'
export PGDATABASE='neondb'
export PGUSER='neondb_owner'
export PGSSLMODE='verify-full'
export PGCHANNELBINDING='require'
python3 scripts/database-backup.py backup --directory "$HOME/PrivateBackups/agentic-job-search"
```

The script prompts for the password without echoing it. Alternatively, use `PGPASSFILE` pointing to a private PostgreSQL password file with mode `0600`. Avoid putting passwords into shell commands or checked-in environment files. The script never prints PostgreSQL stderr because it can contain private values.

The timestamped `.dump` is a consistent, custom-format logical snapshot. Its matching `.sha256` detects file corruption. Both files are created with owner-only permissions. The archive contains private profile data, password hashes, and session keys; keep it in encrypted storage outside the repository, Render, and Neon. Copy both files to a second secure location. This script does not encrypt the archive itself.

Take a backup at least weekly and before each deployment that changes the database schema. Keep the latest four weekly backups and a pre-migration backup until the release and restore check succeed. The deployment workflow does not yet automate off-site backups: before merging a migration, complete and record the backup manually. Do not rely on Neon's limited restore window alone.

## Restore into a disposable database

Create a fresh, isolated PostgreSQL database of the same major version as the source. Use a separate development project or local container; a Neon child branch containing production tables is not empty. Keep the app disconnected from the target during restoration.

Set `PGHOST`, `PGDATABASE`, `PGUSER`, and TLS settings to the disposable target. Then run:

```sh
python3 scripts/database-backup.py restore /absolute/path/backup.dump --confirm-target '<target database name>'
```

The target name must exactly match `PGDATABASE`. Restoration rejects a damaged archive or a target containing user tables, views, or sequences. It never drops existing objects. The restore runs in one transaction and fails on the first error. Only restore archives you trust: PostgreSQL archives can execute SQL.

The printed JSON report includes the checksum, migration history, row counts for all public tables, and check time, with no row contents. PostgreSQL enforces foreign keys while restoring; verification also rejects unvalidated constraints and missing core tables. Counts are structural evidence, not a guarantee that every product workflow is correct.

Connect a local copy of the same application commit to the restored database and verify sign-in, profile, saved job evaluations, evidence, and application history. Disable registration and imports. Do not expose the recovery instance publicly. To preserve encrypted session keys, separately retain the original data-protection PFX and password; a database archive alone cannot replace them.

Record backup UTC time, application commit, checksum, latest migration, operator, restore UTC time, and pass/fail in a private operations log. Keep candidate data and the backup itself out of GitHub. If verification fails, preserve the source and archive and investigate before switching the live application.

## Real incident

Pause writes, recover to a new database, verify it, and only then change Render's connection string. Preserve the previous database for investigation. Coordinate migrations with the restored application's version. Do not restore over the live database or run a destructive reset.

## Automated recovery exercise

`python3 scripts/test-database-recovery.py` creates a temporary PostgreSQL container, migrates a fresh source database, seeds fictional related account/profile/job/application data, backs it up, restores it into another database, and checks contents and safety refusals. It removes only the container it creates. CI runs this exercise independently of production credentials. A passing exercise demonstrates tooling recovery; complete a separate private restore exercise with a hosted backup before treating recovery as verified for your live workspace.

References: [PostgreSQL pg_dump](https://www.postgresql.org/docs/current/app-pgdump.html) and [pg_restore](https://www.postgresql.org/docs/current/app-pgrestore.html). The local recovery exercise selects a container major version matching the installed pg_dump client; set `RECOVERY_POSTGRES_IMAGE` only when its version matches.

## Restoring password-recovery data

Keep `PasswordRecovery__Enabled=false` on any disposable restore or recovery instance. Backups may include pending encrypted reset emails and valid reset-token hashes; enabling delivery could send real account emails. The data-protection certificate is also needed to decrypt pending messages. Never expose the disposable instance publicly or reuse its reset links against production.
