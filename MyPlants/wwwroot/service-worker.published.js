// Caution! Be sure you understand the caveats before publishing an application with
// offline support. See https://aka.ms/blazor-offline-considerations

self.importScripts('./service-worker-assets.js');
self.importScripts('./reminders-sw.js');
self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));
// Permet à la page de demander l'activation immédiate de la nouvelle version (lot 18).
self.addEventListener('message', event => {
    if (event.data && event.data.type === 'SKIP_WAITING') self.skipWaiting();
});

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [ /\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.webmanifest$/ ];
const offlineAssetsExclude = [ /^service-worker\.js$/ ];

// Replace with your base path if you are hosting on a subfolder. Ensure there is a trailing '/'.
const base = "/";
const baseUrl = new URL(base, self.origin);
const manifestUrlList = self.assetsManifest.assets.map(asset => new URL(asset.url, baseUrl).href);

async function onInstall(event) {
    console.info('Service worker: Install');

    // Fetch and cache all matching items from the assets manifest
    // Essayer avec l'intégrité SRI d'abord, puis sans si cela échoue
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)));

    const cache = await caches.open(cacheName);

    for (const asset of assetsRequests) {
        try {
            // Essayer d'abord avec l'intégrité SRI
            const request = new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' });
            const response = await fetch(request);
            if (response.ok) {
                await cache.put(asset.url, response);
            } else {
                console.warn(`Failed to fetch ${asset.url} with integrity check, retrying without...`);
                // Fallback : essayer sans intégrité
                const fallbackRequest = new Request(asset.url, { cache: 'no-cache' });
                const fallbackResponse = await fetch(fallbackRequest);
                if (fallbackResponse.ok) {
                    await cache.put(asset.url, fallbackResponse);
                } else {
                    console.error(`Failed to cache ${asset.url}`);
                }
            }
        } catch (error) {
            console.warn(`Error caching ${asset.url}: ${error.message}. Will try without integrity check...`);
            try {
                // Fallback final : sans intégrité
                const fallbackRequest = new Request(asset.url, { cache: 'no-cache' });
                const fallbackResponse = await fetch(fallbackRequest);
                if (fallbackResponse.ok) {
                    await cache.put(asset.url, fallbackResponse);
                } else {
                    console.error(`Failed to cache ${asset.url} on fallback`);
                }
            } catch (fallbackError) {
                console.error(`Complete failure to cache ${asset.url}: ${fallbackError.message}`);
            }
        }
    }
}

async function onActivate(event) {
    console.info('Service worker: Activate');

    // Delete unused caches
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
}

async function onFetch(event) {
    let cachedResponse = null;
    if (event.request.method === 'GET') {
        // For all navigation requests, try to serve index.html from cache,
        // unless that request is for an offline resource.
        // If you need some URLs to be server-rendered, edit the following check to exclude those URLs
        const shouldServeIndexHtml = event.request.mode === 'navigate'
            && !manifestUrlList.some(url => url === event.request.url);

        const request = shouldServeIndexHtml ? 'index.html' : event.request;
        const cache = await caches.open(cacheName);
        cachedResponse = await cache.match(request);
    }

    return cachedResponse || fetch(event.request).catch(err => {
        // Si la requête échoue (pas de réseau), retourner une réponse contrôlée
        // au lieu de laisser l'erreur brute qui casse l'application
        console.error('Service Worker: Erreur fetch', event.request.url, err);

        // Pour les requêtes de navigation, retourner index.html en cache
        if (event.request.mode === 'navigate') {
            return caches.match('index.html');
        }

        // Pour les autres requêtes (API, assets), retourner un signal d'erreur gracieux
        return new Response(
            JSON.stringify({ error: 'offline', message: 'Application en mode hors ligne' }),
            { status: 503, statusText: 'Service Unavailable', headers: { 'Content-Type': 'application/json' } }
        );
    });
}

