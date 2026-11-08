// Enregistre le service worker et signale à l'application quand une nouvelle version est prête.
// Remplace la ligne `navigator.serviceWorker.register(...)` de index.html.
//
// Fonctionnement : le navigateur télécharge la nouvelle version en arrière-plan, qui reste « en attente »
// tant que l'ancienne est utilisée. Quand l'utilisateur accepte, on lui demande de prendre la main
// (message SKIP_WAITING, géré dans service-worker.published.js), puis la page se recharge.
window.myPlantsUpdate = (() => {
    const CHECK_INTERVAL_MS = 60 * 60 * 1000;

    let registration = null;
    let waitingWorker = null;
    let listener = null;
    let applying = false;

    function setWaiting(worker) {
        waitingWorker = worker;

        if (listener) {
            listener.invokeMethodAsync("OnUpdateAvailable").catch(() => { });
        }
    }

    function watch(reg) {
        // Une mise à jour déjà téléchargée avant l'ouverture de cette page.
        if (reg.waiting && navigator.serviceWorker.controller) {
            setWaiting(reg.waiting);
        }

        reg.addEventListener("updatefound", () => {
            const worker = reg.installing;
            if (!worker) return;

            worker.addEventListener("statechange", () => {
                // « controller » absent = première installation, pas une mise à jour.
                if (worker.state === "installed" && navigator.serviceWorker.controller) {
                    setWaiting(worker);
                }
            });
        });
    }

    function check() {
        if (registration && navigator.onLine) {
            registration.update().catch(() => { });
        }
    }

    async function register() {
        if (!("serviceWorker" in navigator)) return;

        try {
            registration = await navigator.serviceWorker.register("service-worker.js", { updateViaCache: "none" });
            watch(registration);
        } catch {
            // Sans service worker, l'application fonctionne, simplement sans mode hors ligne ni mise à jour guidée.
            return;
        }

        // Une application laissée ouverte longtemps vérifie de temps en temps, et à chaque retour au premier plan.
        setInterval(check, CHECK_INTERVAL_MS);
        document.addEventListener("visibilitychange", () => {
            if (document.visibilityState === "visible") check();
        });

        // Recharger seulement après un choix de l'utilisateur, jamais de façon inattendue.
        navigator.serviceWorker.addEventListener("controllerchange", () => {
            if (applying) location.reload();
        });
    }

    register();

    return {
        // Appelé par le composant d'affichage ; renvoie vrai si une mise à jour attend déjà.
        subscribe(dotNetRef) {
            listener = dotNetRef;
            return waitingWorker !== null;
        },

        unsubscribe() {
            listener = null;
        },

        apply() {
            if (!waitingWorker) return;

            applying = true;
            waitingWorker.postMessage({ type: "SKIP_WAITING" });
        }
    };
})();