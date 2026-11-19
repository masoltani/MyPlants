// Petit stockage partagé entre la page et le service worker pour les rappels d'arrosage locaux.
// Base IndexedDB séparée de celle des plantes (MyPlantsDb) : on ne touche pas à son numéro de version.
//
// Contenu :
//   "settings" : { enabled, hour, title, bodyOne, bodyMany, testBody, lastNotifiedOn }
//   "due"      : { profileId, dates: ["2026-11-17", ...] }  prochaines dates d'arrosage (sans nom de plante)
//
// Chargé par la page (<script>) et par le service worker (importScripts) : pas de window, que globalThis.
(function (root) {
    const DATABASE_NAME = "MyPlantsReminders";
    const STORE = "kv";

    function openDatabase() {
        return new Promise((resolve, reject) => {
            const request = indexedDB.open(DATABASE_NAME, 1);

            request.onupgradeneeded = () => request.result.createObjectStore(STORE);
            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
        });
    }

    async function get(key) {
        const database = await openDatabase();

        try {
            return await new Promise((resolve, reject) => {
                const request = database.transaction(STORE, "readonly").objectStore(STORE).get(key);

                request.onsuccess = () => resolve(request.result ?? null);
                request.onerror = () => reject(request.error);
            });
        } finally {
            database.close();
        }
    }

    async function set(key, value) {
        const database = await openDatabase();

        try {
            await new Promise((resolve, reject) => {
                const transaction = database.transaction(STORE, "readwrite");

                transaction.objectStore(STORE).put(value, key);
                transaction.oncomplete = () => resolve();
                transaction.onerror = () => reject(transaction.error);
                transaction.onabort = () => reject(transaction.error);
            });
        } finally {
            database.close();
        }
    }

    // Date locale de l'appareil au format aaaa-mm-jj (comparable en texte).
    function toLocalDateString(date) {
        const pad = number => String(number).padStart(2, "0");

        return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
    }

    // Combien de plantes sont à arroser, si une notification doit partir maintenant ; 0 sinon.
    // Fonction pure : toutes les conditions d'envoi sont ici, simples à lire et à tester.
    function countToNotify(settings, due, now, permission) {
        if (!settings || !settings.enabled) return 0;
        if (permission !== "granted") return 0;

        // Le navigateur choisit le moment du réveil : on n'envoie qu'à partir de l'heure choisie…
        if (now.getHours() < settings.hour) return 0;

        // …et une seule fois par jour.
        const today = toLocalDateString(now);
        if (settings.lastNotifiedOn === today) return 0;

        if (!due || !Array.isArray(due.dates)) return 0;

        return due.dates.filter(date => date <= today).length;
    }

    root.myPlantsRemindersStore = { get, set, toLocalDateString, countToNotify };

    if (typeof module !== "undefined" && module.exports) {
        module.exports = root.myPlantsRemindersStore;
    }
})(globalThis);