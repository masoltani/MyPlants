// Lit l'adresse de la page au démarrage pour savoir si l'utilisateur arrive depuis
// un lien reçu par e-mail (réinitialisation de mot de passe).
//
// À appeler avant que le client Supabase ne traite et efface les informations
// contenues dans l'adresse (le fragment après le « # »).
window.myPlantsAuthLink = {
    // "recovery" : lien valide de réinitialisation
    // "expired"  : lien refusé par Supabase (expiré ou déjà utilisé)
    // "none"     : visite normale
    getStatus: function () {
        const params = new URLSearchParams(window.location.hash.replace(/^#/, ""));

        if (params.get("type") === "recovery" && params.has("access_token")) {
            return "recovery";
        }

        if (params.has("error_code") || params.get("error") === "access_denied") {
            return "expired";
        }

        return "none";
    }
};