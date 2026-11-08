// Déclencheurs de la synchronisation automatique : retour du réseau, retour de l'application
// au premier plan, et intervalle régulier tant que l'application est visible.
// Ce fichier ne décide de rien : il prévient C# (AutoSyncService.OnTriggerAsync), qui décide.
window.myPlantsAutoSync = (() => {
    const INTERVAL_MS = 5 * 60 * 1000;

    let listeners = null;

    function stop() {
        if (!listeners) return;

        window.removeEventListener("online", listeners.onOnline);
        document.removeEventListener("visibilitychange", listeners.onVisibility);
        clearInterval(listeners.timer);

        listeners = null;
    }

    function start(dotNetRef) {
        stop();

        const notify = reason => {
            // Une erreur ici (référence .NET libérée, par exemple) ne doit jamais remonter à la page.
            dotNetRef.invokeMethodAsync("OnTriggerAsync", reason).catch(() => { });
        };

        const onOnline = () => notify("online");

        const onVisibility = () => {
            if (document.visibilityState === "visible") notify("visible");
        };

        const timer = setInterval(() => {
            if (document.visibilityState === "visible" && navigator.onLine) notify("interval");
        }, INTERVAL_MS);

        window.addEventListener("online", onOnline);
        document.addEventListener("visibilitychange", onVisibility);

        listeners = { onOnline, onVisibility, timer };
    }

    return { start, stop };
})();