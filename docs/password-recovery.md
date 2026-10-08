# Password recovery for the private beta

The sign-in page links to `/forgot-password`. Users enter their existing account email, receive a Resend email, open the link, and choose a matching 12–128 character password. They then sign in normally. A reset preserves the account ID, profile, jobs, and applications. Registration can stay closed.

## Configure after review and a private backup/restore test

Recovery is **disabled by default**. Do not enable it or run the migration on production until the owner authorizes deployment. **Merging to main triggers the existing CI → migration → Render deployment workflow when repository secrets are configured, even if recovery remains disabled. Complete the backup/restore test before merging this PR.** Before deployment, follow [backup and recovery](backup-recovery.md) using a private archive and disposable database. Never put database credentials, archives, reset links, or the Resend key in GitHub, chat, or build logs.

After that test and deployment approval, configure these on the Render **backend service**, using encrypted environment settings:

| Variable | Value |
| --- | --- |
| `PasswordRecovery__Enabled` | `true` |
| `PasswordRecovery__PublicBaseUrl` | The actual application's HTTPS origin, e.g. `https://your-app.onrender.com`, with no path, query, or fragment. This is not the sending domain. |
| `PasswordRecovery__From` | `Agentic Job Search <accounts@notify.rrkaze.space>` |
| `PasswordRecovery__ResendApiKey` | The privately saved Resend key restricted to sending for `notify.rrkaze.space`. |

Resend must show the sending domain verified. Leave click/open tracking off so recovery links are not rewritten or tracked. Keep the Cloudflare sending CNAME records DNS-only. Existing data-protection certificate settings must remain intact: the same protection system encrypts queued messages. Invalid enabled settings stop application startup without echoing secret values.

Apply the additive migration using the existing approved deployment workflow before serving the new version. It adds two tables and a session version/password-change timestamp to accounts; it does not replace accounts or job data. **Previously issued login cookies are signed out once on this deployment.** New cookies survive ordinary redeployments; any subsequent password reset invalidates all that account's cookies.

## Test before recovering the personal account

1. Keep registration closed. With the owner's approval for real email delivery, request a reset for the saved test account whose mailbox you control.
2. Confirm the generic request message, sender, correct HTTPS application address, and email delivery (including spam). Opening the link alone must not change the password.
3. Set a new test password. Confirm the old password fails, the new one works, existing sessions are signed out, and the link cannot be reused. Confirm the password-change notification arrives.
4. Repeat for the existing personal-data account. Check the same profile and saved jobs after signing in. Do not create a replacement account or assign a different owner ID.
5. If delivery fails, inspect sanitized application delivery IDs/attempt counts and the private Resend dashboard. Never paste reset links or email content into a public issue. Request a fresh link after fixing configuration.

A cold/sleeping Render service may delay delivery. Requests remain queued in PostgreSQL across restarts, but links expire 30 minutes after the request; an expired queued request is discarded. A refreshed reset page has no token because the page removes it from the URL immediately; reopen the original email link if it is still unused and unexpired.

## Security and delivery behavior

- Requests for known/unknown addresses return the same 202 message. Account lookup and delivery happen in a background worker; malformed addresses also return the generic message.
- Links contain a random 256-bit credential in the URL fragment, which browsers do not send in HTTP requests. The page immediately removes it from current browser history and keeps it only in memory. No token/password goes into local storage. Recovery pages and account responses use `no-store` and `no-referrer`; recovery routes bypass the PWA navigation cache.
- Only a SHA-256 hash is stored in the token table. The durable email queue temporarily contains the complete message encrypted with ASP.NET Data Protection. Prepared messages are committed before sending; retries reuse the same delivery ID and body with Resend's idempotency header. Success or terminal failure erases encrypted content.
- Tokens last 30 minutes, are single-use, and all outstanding links are invalidated atomically with the password change. An account row lock prevents two concurrent valid links from both succeeding. Queued requests made before the password change cannot generate another usable link afterward.
- The API limits recovery to 10 attempts per IP per 15 minutes, including resets. The database limits email requests to one per address per minute, three per address per hour, and 40 total requests per rolling day, including unknown addresses. Each accepted request can produce at most one reset email and one notification. This leaves headroom for private-beta use but does not guarantee a provider quota: delayed deliveries and other sends from the Resend account still count toward its allowance. Per-IP limits assume the hosting proxy replaces forwarded IP headers.
- Email retries back off for at most five attempts, bounded by link expiry (notifications: 12 hours). Worker processing is paced every two seconds while the service is running; this database polling can keep Neon compute awake. Cleanup runs hourly, removing token records a day after expiry and queue records after seven days. A public launch needs a larger abuse-control and delivery-observability design.
- Restored databases must keep recovery disabled to avoid sending copied queued mail. Losing the data-protection certificate prevents queued content decryption; request fresh links after restoring protection configuration.

If the test domain is allowed to expire, first disable recovery, revoke its Resend key, and remove the sender configuration. Do not leave account recovery pointing at a domain somebody else can register.

## Verification

Backend integration tests use disposable PostgreSQL, fictional accounts, an adjustable clock, and a fake sender. They cover preserved account/job data, old-session revocation, unknown addresses, expiry/reuse, concurrent resets, encrypted retries, throttling, disabled recovery, and request-header protection. Provider tests verify Resend request shape, stable idempotency, and sanitized errors. Chrome component tests check token removal, submitted payloads, missing links, feedback, and cached-account cleanup. No automated test sends real email or accesses Neon.
