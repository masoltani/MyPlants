// Service pour détecter et signaler les changements d'état réseau (online/offline).
window.myPlantsOnlineOffline = (() => {
    let dotNetRef = null;

    function handleOnline() {
        console.log("🟢 Connexion rétablie");
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync("OnNetworkStatusChanged", true).catch(() => { });
        }
    }

    function handleOffline() {
        console.log("🔴 Pas de connexion (mode offline)");
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync("OnNetworkStatusChanged", false).catch(() => { });
        }
    }

    return {
        initialize: function (ref) {
            dotNetRef = ref;
            window.addEventListener("online", handleOnline);
            window.addEventListener("offline", handleOffline);
        },

        getStatus: function () {
            return navigator.onLine;
        },

        dispose: function () {
            window.removeEventListener("online", handleOnline);
            window.removeEventListener("offline", handleOffline);
            dotNetRef = null;
        }
    };
})();
