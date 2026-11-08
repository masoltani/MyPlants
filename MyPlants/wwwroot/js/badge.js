// Badge sur l'icône de l'application installée (nombre de plantes à arroser).
// Pris en charge par Chrome/Edge (ordinateur, Android) et Safari (application installée) ;
// ailleurs, il ne se passe simplement rien.
window.myPlantsBadge = {
    set: function (count) {
        try {
            if (!("setAppBadge" in navigator)) return;

            const result = count > 0 ? navigator.setAppBadge(count) : navigator.clearAppBadge();

            // Refus de l'autorisation, page non installée… : jamais une erreur pour l'application.
            if (result && result.catch) result.catch(() => { });
        } catch {
            // Rien à faire : le badge est facultatif.
        }
    }
};