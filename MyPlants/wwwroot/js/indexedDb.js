window.myPlantsIndexedDb = {
    databaseName: "MyPlantsDb",
    databaseVersion: 2,
    storeName: "plants",

    openDatabase: function () {
        return new Promise((resolve, reject) => {
            const request = indexedDB.open(
                this.databaseName,
                this.databaseVersion
            );

            request.onupgradeneeded = function (event) {
                const db = event.target.result;

                if (!db.objectStoreNames.contains("plants")) {
                    db.createObjectStore("plants", {
                        keyPath: "id"
                    });
                }

                if (!db.objectStoreNames.contains("waterings")) {
                    const store = db.createObjectStore("waterings", {
                        keyPath: "id"
                    });

                    store.createIndex("plantId", "plantId", {
                        unique: false
                    });

                    store.createIndex("date", "date", {
                        unique: false
                    });
                }
            };

            request.onsuccess = event =>
                resolve(event.target.result);

            request.onerror = event =>
                reject(event.target.error);
        });
    },

    getAll: async function () {
        const db = await this.openDatabase();

        return new Promise((resolve, reject) => {
            const request = db
                .transaction(this.storeName, "readonly")
                .objectStore(this.storeName)
                .getAll();

            request.onsuccess = () => resolve(request.result);
            request.onerror = event => reject(event.target.error);
        });
    },

    getById: async function (id) {
        const db = await this.openDatabase();

        return new Promise((resolve, reject) => {
            const request = db
                .transaction(this.storeName, "readonly")
                .objectStore(this.storeName)
                .get(id);

            request.onsuccess = () =>
                resolve(request.result ?? null);

            request.onerror = event =>
                reject(event.target.error);
        });
    },

    add: async function (plant) {
        const db = await this.openDatabase();

        return new Promise((resolve, reject) => {
            const request = db
                .transaction(this.storeName, "readwrite")
                .objectStore(this.storeName)
                .add({
                    ...plant,
                    id: plant.id.toString()
                });

            request.onsuccess = () => resolve();
            request.onerror = event => reject(event.target.error);
        });
    },

    update: async function (plant) {
        const db = await this.openDatabase();

        return new Promise((resolve, reject) => {
            const request = db
                .transaction(this.storeName, "readwrite")
                .objectStore(this.storeName)
                .put({
                    ...plant,
                    id: plant.id.toString()
                });

            request.onsuccess = () => resolve();
            request.onerror = event => reject(event.target.error);
        });
    },

    delete: async function (id) {
        const db = await this.openDatabase();

        return new Promise((resolve, reject) => {
            const request = db
                .transaction(this.storeName, "readwrite")
                .objectStore(this.storeName)
                .delete(id.toString());

            request.onsuccess = () => resolve();
            request.onerror = event => reject(event.target.error);
        });
    },

    addWatering: async function (watering) {
        const db = await this.openDatabase();

        return new Promise((resolve, reject) => {
            const request = db
                .transaction("waterings", "readwrite")
                .objectStore("waterings")
                .add({
                    ...watering,
                    id: watering.id.toString(),
                    plantId: watering.plantId.toString()
                });

            request.onsuccess = () => resolve();

            request.onerror = event =>
                reject(event.target.error);
        });
    },

    getWateringHistory: async function (plantId) {
        const db = await this.openDatabase();

        return new Promise((resolve, reject) => {
            const store = db
                .transaction("waterings", "readonly")
                .objectStore("waterings");

            const index = store.index("plantId");

            const request = index.getAll(plantId.toString());

            request.onsuccess = () => {
                const result = request.result.sort(
                    (a, b) =>
                        new Date(b.date) - new Date(a.date)
                );

                resolve(result);
            };

            request.onerror = event =>
                reject(event.target.error);
        });
    }
};
