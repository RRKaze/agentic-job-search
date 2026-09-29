# Installable mobile application

The production Angular build is a Progressive Web App (PWA). It can be installed from the hosted HTTPS address and opened from the phone's Home Screen without an app-store release.

## Install on iPhone or iPad

1. Open the hosted application in Safari and sign in.
2. Tap **Share**.
3. Select **Add to Home Screen**.
4. Confirm the name and tap **Add**.
5. Open **Job Search** from the Home Screen.

Apple does not currently show the same automatic install prompt as Android. Installation must be started from Safari's Share menu.

## Install on Android

1. Open the hosted application in Chrome and sign in.
2. Open Chrome's menu and select **Install app** or **Add to Home screen**.
3. Confirm the installation.
4. Open **Job Search** from the launcher.

## Offline and update behavior

The service worker saves only the versioned application shell: HTML, JavaScript, styles, the manifest, and icons. It does not cache `/api` requests or authenticated job-search data.

When the device is offline, the application displays a connection banner. Previously loaded app screens can open, but signing in, loading private records, and saving changes require a connection. The application does not imply that a change was saved while offline.

When a new deployment has finished downloading, the application shows an update banner. Selecting **Reload** starts the latest complete version. An unrecoverable cached version also displays a recovery message with a reload action.

The service worker is enabled only in production builds. Local `npm start` development continues without caching so code changes appear immediately.

## Release verification

After the hosted deployment is available, verify these items on current iOS Safari and Android Chrome:

1. The install action is available and uses the Agentic Job Search icon and name.
2. The application opens without browser chrome in standalone mode.
3. Dashboard, applications, opportunities, profile, and sign-out navigation remain reachable from the bottom navigation bar.
4. Forms do not zoom unexpectedly and their primary actions remain visible above the navigation bar.
5. Turning off connectivity displays the offline banner and failed API actions do not claim success.
6. Deploying a new version displays the update banner and reloads into the new version.
7. Browser storage inspection contains application assets but no authenticated `/api` response bodies.
