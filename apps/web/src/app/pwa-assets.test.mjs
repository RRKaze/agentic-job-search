import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

const readJson = async path => JSON.parse(await readFile(path, 'utf8'));

test('manifest installs Agentic Job Search as a standalone application', async () => {
  const manifest = await readJson('public/manifest.webmanifest');

  assert.equal(manifest.name, 'Agentic Job Search');
  assert.equal(manifest.short_name, 'Job Search');
  assert.equal(manifest.display, 'standalone');
  assert.equal(manifest.start_url, '/');
  assert.ok(manifest.icons.some(icon => icon.sizes === '192x192'));
  assert.ok(manifest.icons.some(icon => icon.sizes === '512x512'));
  assert.ok(manifest.icons.some(icon => icon.purpose?.includes('maskable')));
});

test('service worker caches the application shell without caching private API data', async () => {
  const config = await readJson('ngsw-config.json');
  const cachedAssets = JSON.stringify(config.assetGroups);

  assert.ok(config.assetGroups.some(group => group.installMode === 'prefetch'));
  assert.ok(!cachedAssets.includes('/api'));
  assert.deepEqual(config.dataGroups ?? [], []);
  assert.ok(config.navigationUrls.includes('!/api/**'));
});

test('document metadata advertises the manifest and mobile standalone mode', async () => {
  const index = await readFile('src/index.html', 'utf8');

  assert.match(index, /rel="manifest" href="manifest\.webmanifest"/);
  assert.match(index, /name="theme-color"/);
  assert.match(index, /name="apple-mobile-web-app-capable" content="yes"/);
});
