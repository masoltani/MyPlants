// Rappels d'arrosage locaux, côté page : disponibilité, activation, test.
// Les données (réglages, prochaines dates) sont écrites dans remindersStore.js, lues par le service worker.
window.myPlantsReminders = (() => {
    const PERIODIC_TAG = "myplants-due-check";
    const MIN_INTERVAL_MS = 12 * 60 * 60 * 1000;
    const READY_TIMEOUT_MS = 2000;

    const store = globalThis.myPlantsRemindersStore;

    // « ready » ne se résout jamais sans service worker (mode développement) : on n'attend pas indéfiniment.
    async function getRegistration() {
        if (!("serviceWorker" in navigator)) return null;

        return Promise.race([
            navigator.serviceWorker.ready,
            new Promise(resolve => setTimeout(() => resolve(null), READY_TIMEOUT_MS))
        ]);
    }

    function isInstalled() {
        const modes = ["standalone", "fullscreen", "minimal-ui", "window-controls-overlay"];

        return modes.some(mode => window.matchMedia(`(display-mode: ${mode})`).matches)
            || navigator.standalone === true;
    }

    async function getStatus() {
        const registration = await getRegistration();

        return {
            supported: "Notification" in window && registration !== null && "periodicSync" in registration,
            permission: "Notification" in window ? Notification.permission : "denied",
            installed: isInstalled()
        };
    }

    // À appeler directement depuis un clic : la demande d'autorisation exige un geste de l'utilisateur.
    // Renvoie "ok", "unsupported", "denied" ou "failed".
    async function enable() {
        if (!("Notification" in window)) return "unsupported";

        let permission = Notification.permission;

        if (permission === "default") {
            permission = await Notification.requestPermission();
        }

        if (permission !== "granted") return "denied";

        const registration = await getRegistration();

        if (!registration || !("periodicSync" in registration)) return "unsupported";

        try {
            await registration.periodicSync.register(PERIODIC_TAG, { minInterval: MIN_INTERVAL_MS });
        } catch {
            // Application non installée, ou autorisation refusée par le navigateur.
            return "failed";
        }

        return "ok";
    }

    async function disable() {
        try {
            const registration = await getRegistration();

            if (registration && "periodicSync" in registration) {
                await registration.periodicSync.unregister(PERIODIC_TAG);
            }
        } catch {
            // Rien à désenregistrer : le réglage « désactivé » suffit, le service worker n'enverra plus rien.
        }
    }

    // Fusionne avec l'existant : le service worker y garde lastNotifiedOn.
    async function saveSettings(settings) {
        const existing = (await store.get("settings")) ?? {};

        await store.set("settings", { ...existing, ...settings });
    }

    async function saveDue(profileId, dates) {
        await store.set("due", { profileId, dates });
    }

    // Notification immédiate, pour vérifier que tout fonctionne. Renvoie "ok", "denied" ou "failed".
    async function test() {
        if (!("Notification" in window) || Notification.permission !== "granted") return "denied";

        const registration = await getRegistration();
        const settings = await store.get("settings");

        if (!registration || !settings) return "failed";

        await registration.showNotification(settings.title, {
            body: settings.testBody,
            tag: "myplants-test",
            icon: "icon-192.png",
            badge: "icon-192.png",
            data: { url: registration.scope }
        });

        return "ok";
    }

    return { getStatus, enable, disable, saveSettings, saveDue, test };
})();