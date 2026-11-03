window.myPlantsIndexedDb = {
    databaseName: "MyPlantsDb",
    databaseVersion: 3,
    storeName: "plants",

    openDatabase: function () {
        return new Promise((resolve, reject) => {
            const request = indexedDB.open(
                this.databaseName,
                3
            );

            request.onupgradeneeded = function (event) {
                const db = event.target.result;
                const tx = event.target.transaction;
                const oldVersion = event.oldVersion;

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

                if (oldVersion < 3) {
                    const plantStore = tx.objectStore("plants");
                    const plantCursor = plantStore.openCursor();

                    plantCursor.onsuccess = function (event) {
                        const cursor = event.target.result;
                        if (!cursor)
                            return;
                        cursor.update({
                            ...cursor.value,
                            profileId: "guest"
                        });
                        cursor.continue();
                    };
                }
            };

            request.onsuccess = event =>
                resolve(event.target.result);

            request.onerror = event =>
                reject(event.target.error);

            request.onblocked = () =>
                reject(new Error(
                    "La migration IndexedDB est bloquée par un autre onglet. Ferme les autres onglets MyPlants puis réessaie."
                ));
        });
    },

    getAll: async function (profileId) {
        const db = await this.openDatabase();

        return new Promise((resolve, reject) => {
            const request = db
                .transaction(this.storeName, "readonly")
                .objectStore(this.storeName)
                .getAll();

            request.onsuccess = () =>
                resolve(request.result.filter(
                    (plant) =>
                        plant.profileId === profileId
                        && !plant.deletedAt
                ));
            request.onerror = event => reject(event.target.error);
        });
    },

    getById: async function (id, profileId) {
        const db = await this.openDatabase();

        return new Promise((resolve, reject) => {
            const request = db
                .transaction(this.storeName, "readonly")
                .objectStore(this.storeName)
                .get(id);

            request.onsuccess = () => {
                const plant = request.result;
                if (!plant || plant.profileId !== profileId || plant.deletedAt) {
                    resolve(null);
                    return;
                }
                resolve(plant);
            };

            request.onerror = event =>
                reject(event.target.error);
        });
    },

    add: async function (plant, profileId) {
        const db = await this.openDatabase();

        return new Promise((resolve, reject) => {
            const request = db
                .transaction(this.storeName, "readwrite")
                .objectStore(this.storeName)
                .add({
                    ...plant,
                    id: plant.id.toString(),
                    profileId: profileId,
                    updatedAt: new Date().toISOString(),
                    deletedAt: null,
                    isSynced: false
                });

            request.onsuccess = () => resolve();
            request.onerror = event => reject(event.target.error);
        });
    },

    update: async function (plant, profileId) {
        const db = await this.openDatabase();

        return new Promise((resolve, reject) => {
            const request = db
                .transaction(this.storeName, "readwrite")
                .objectStore(this.storeName)
                .put({
                    ...plant,
                    id: plant.id.toString(),
                    profileId: profileId,
                    updatedAt: new Date().toISOString(),
                    isSynced: false
                });

            request.onsuccess = () => resolve();
            request.onerror = event => reject(event.target.error);
        });
    },

    delete: async function (id, profileId) {
        const db = await this.openDatabase();

        return new Promise((resolve, reject) => {
            const tx = db.transaction(this.storeName, "readwrite");
            const store = tx.objectStore(this.storeName);
            const request = store.get(id);

            request.onsuccess = () => {
                const plant = request.result;
                if (!plant || plant.profileId !== profileId) {
                    return;
                }
                store.put({
                    ...plant,
                    updatedAt: new Date().toISOString(),
                    deletedAt: new Date().toISOString(),
                    isSynced: false
                });
            };

            tx.oncomplete = () => resolve();
            tx.onerror = event => reject(event.error);
            tx.onabort = event => reject(
                event.error ?? new Error("Suppression annulée.")
            );
        });
    },

    addWatering: async function (watering, profileId) {
        const db = await this.openDatabase();

        return new Promise((resolve, reject) => {
            const tx = db.transaction(["plants", "waterings"], "readwrite");
            const plants = tx.objectStore("plants");
            const waterings = tx.objectStore("waterings");
            const request = plants.get(watering.plantId.toString());

            request.onsuccess = () => {
                const plant = request.result;
                if (!plant || plant.profileId !== profileId) {
                    tx.abort();
                    return;
                }
                waterings.add({
                    ...watering,
                    id: watering.id.toString(),
                    plantId: watering.plantId.toString(),
                    profileId
                });
            };

            tx.oncomplete = () => resolve();
            tx.onerror = event => reject(event.error);
            tx.onabort = event => reject(event.error);
        });
    },

    getWateringHistory: async function (plantId, profileId) {
        const db = await this.openDatabase();

        return new Promise((resolve, reject) => {
            const tx = db.transaction(
                ["plants", "waterings"],
                "readonly"
            );

            const plantRequest = tx
                .objectStore("plants")
                .get(plantId.toString());

            const wateringRequest = tx
                .objectStore("waterings")
                .index("plantId")
                .getAll(plantId.toString());

            let plant;
            let history;

            plantRequest.onsuccess = () => {
                plant = plantRequest.result;
            };

            wateringRequest.onsuccess = () => {
                history = wateringRequest.result;
            };

            tx.oncomplete = () => {
                if (!plant || plant.profileId !== profileId) {
                    resolve([]);
                    return;
                }

                resolve(
                    history
                        .filter(w => w.profileId === profileId)
                        .sort((a, b) =>
                            new Date(b.date) - new Date(a.date)
                        )
                );
            };

            tx.onerror = () => reject(tx.error);
        });
    },

    importGuestPlants: async function (targetProfileId) {
        if (!targetProfileId || targetProfileId === "guest") {
            throw new Error("Profil cible invalide.");
        }

        const db = await this.openDatabase();

        const [allPlants, allWaterings] = await Promise.all([
            new Promise((resolve, reject) => {
                const request = db
                    .transaction("plants", "readonly")
                    .objectStore("plants")
                    .getAll();

                request.onsuccess = () => resolve(request.result);
                request.onerror = e => reject(e.target.error);
            }),

            new Promise((resolve, reject) => {
                const request = db
                    .transaction("waterings", "readonly")
                    .objectStore("waterings")
                    .getAll();

                request.onsuccess = () => resolve(request.result);
                request.onerror = e => reject(e.target.error);
            })
        ]);

        const guestPlants = allPlants.filter(
            p => p.profileId === "guest"
        );

        const targetPlants = allPlants.filter(
            p => p.profileId === targetProfileId
        );

        const alreadyImported = new Set(
            targetPlants
                .map(p => p.importedFromGuestId)
                .filter(Boolean)
        );

        const guestWateringsByPlant = new Map();

        for (const watering of allWaterings) {
            if (watering.profileId !== "guest") continue;

            const list = guestWateringsByPlant.get(watering.plantId) ?? [];
            list.push(watering);
            guestWateringsByPlant.set(watering.plantId, list);
        }

        const plantsToAdd = [];
        const wateringsToAdd = [];

        for (const plant of guestPlants) {
            const originalId = plant.id.toString();

            if (alreadyImported.has(originalId)) continue;

            const newPlantId = crypto.randomUUID();

            plantsToAdd.push({
                ...plant,
                id: newPlantId,
                profileId: targetProfileId,
                importedFromGuestId: originalId
            });

            const history = guestWateringsByPlant.get(originalId) ?? [];

            for (const watering of history) {
                wateringsToAdd.push({
                    ...watering,
                    id: crypto.randomUUID(),
                    plantId: newPlantId,
                    profileId: targetProfileId
                });
            }
        }

        if (plantsToAdd.length === 0)
            return 0;

        return await new Promise((resolve, reject) => {
            const tx = db.transaction(
                ["plants", "waterings"],
                "readwrite"
            );

            const plantsStore = tx.objectStore("plants");
            const wateringsStore = tx.objectStore("waterings");

            for (const plant of plantsToAdd) {
                plantsStore.add(plant);
            }

            for (const watering of wateringsToAdd) {
                wateringsStore.add(watering);
            }

            tx.oncomplete = () => resolve(plantsToAdd.length);
            tx.onerror = () => reject(tx.error);
            tx.onabort = () => reject(
                tx.error ?? new Error("Import annulé.")
            );
        });
    },

    async hasGuestPlants() {
        const db = await this.openDatabase();

        return new Promise((resolve, reject) => {
            const tx = db.transaction(["plants"], "readonly");
            const request = tx.objectStore("plants").getAll();

            request.onsuccess = () => {
                const plants = request.result ?? [];
                resolve(plants.some(p => p.profileId === "guest"));
            };

            request.onerror = () => reject(request.error);
        });
    },

    getUnsynced: async function (profileId) {
        const db = await this.openDatabase();

        return new Promise((resolve, reject) => {
            const transaction = db.transaction(
                this.storeName,
                "readonly"
            );

            const request = transaction
                .objectStore(this.storeName)
                .getAll();

            request.onsuccess = () => {
                const plants = request.result.filter(
                    plant =>
                        plant.profileId === profileId &&
                        plant.isSynced === false
                );

                resolve(plants);
            };

            request.onerror = event => reject(event.target.error);
        });
    },

    markAsSynced: async function (id, profileId) {
        const db = await this.openDatabase();

        return new Promise((resolve, reject) => {
            const transaction = db.transaction(
                this.storeName,
                "readwrite"
            );

            const store = transaction.objectStore(this.storeName);
            const request = store.get(id);

            request.onsuccess = () => {
                const plant = request.result;

                if (!plant || plant.profileId !== profileId) {
                    return;
                }

                store.put({
                    ...plant,
                    isSynced: true
                });
            };

            transaction.oncomplete = () => resolve();
            transaction.onerror = event => reject(event.target.error);
            transaction.onabort = event => reject(event.target.error);
        });
    },

    upsertFromCloud: async function (plant, profileId) {
        const db = await this.openDatabase();

        return new Promise((resolve, reject) => {
            const transaction = db.transaction(
                this.storeName,
                "readwrite"
            );

            const store = transaction.objectStore(this.storeName);

            store.put({
                ...plant,
                id: plant.id.toString(),
                profileId: profileId,
                isSynced: true
            });

            transaction.oncomplete = () => resolve();
            transaction.onerror = event => reject(event.target.error);
            transaction.onabort = event => reject(event.target.error);
        });
    },
};
