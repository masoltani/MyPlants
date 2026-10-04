window.myPlantsStorage = {
    getLanguage: function () {
        return localStorage.getItem("myplants-language");
    },

    setLanguage: function (language) {
        localStorage.setItem("myplants-language", language);
    }
};
