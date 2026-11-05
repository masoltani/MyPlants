window.myPlantsStorage = {
    getLanguage: function () {
        return localStorage.getItem("myplants-language");
    },

    setLanguage: function (language, direction) {
        localStorage.setItem("myplants-language", language);
        localStorage.setItem("myplants-direction", direction);
    },

    getProfile: function () {
        return localStorage.getItem("myplants-profile");
    },

    setProfile: function (profileId) {
        localStorage.setItem("myplants-profile", profileId);
    },

    // Préférences d'affichage : le stockage peut être indisponible (navigation privée,
    // données bloquées), ce qui ne doit jamais provoquer d'erreur.
    getPreference: function (key) {
        try {
            return localStorage.getItem("myplants-pref-" + key);
        } catch {
            return null;
        }
    },

    setPreference: function (key, value) {
        try {
            localStorage.setItem("myplants-pref-" + key, value);
        } catch {
            // Préférence non conservée.
        }
    }
};