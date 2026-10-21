window.myPlantsStorage = {
    getLanguage: function () {
        return localStorage.getItem("myplants-language");
    },

    setLanguage: function (language, direction) {
        localStorage.setItem("myplants-language", language);
        localStorage.setItem("myplants-direction", direction);
    }
};
