// Rappels d'arrosage locaux, côté service worker (lot 23).
// Chargé par service-worker.published.js : self.importScripts('./reminders-sw.js');
//
// Le navigateur réveille ce code de temps en temps (Periodic Background Sync, Chrome/Edge, application installée).
// Il ne décide pas du moment : on vérifie donc à chaque réveil s'il y a lieu de notifier.

self.importScripts("./js/remindersStore.js");

(function () {
    const PERIODIC_TAG = "myplants-due-check";
    const store = self.myPlantsRemindersStore;

    async function notifyIfNeeded() {
        const [settings, due] = await Promise.all([store.get("settings"), store.get("due")]);
        const now = new Date();

        const count = store.countToNotify(settings, due, now, Notification.permission);

        if (count === 0) return;

        const today = store.toLocalDateString(now);
        const body = count === 1 ? settings.bodyOne : settings.bodyMany.replace("{0}", String(count));

        // Même identifiant pour la journée : un doublon remplace la notification au lieu de s'empiler.
        await self.registration.showNotification(settings.title, {
            body,
            tag: "due-" + today,
            icon: "icon-192.png",
            badge: "icon-192.png",
            data: { url: self.registration.scope }
        });

        await store.set("settings", { ...settings, lastNotifiedOn: today });
    }

    self.addEventListener("periodicsync", event => {
        if (event.tag === PERIODIC_TAG) {
            event.waitUntil(notifyIfNeeded().catch(() => { }));
        }
    });

    // Un clic ramène l'application au premier plan, ou l'ouvre.
    self.addEventListener("notificationclick", event => {
        event.notification.close();

        const url = (event.notification.data && event.notification.data.url) || self.registration.scope;

        event.waitUntil((async () => {
            const windows = await self.clients.matchAll({ type: "window", includeUncontrolled: true });
            const existing = windows.find(client => client.url.startsWith(self.registration.scope));

            if (existing) {
                await existing.focus();
            } else {
                await self.clients.openWindow(url);
            }
        })());
    });
})();