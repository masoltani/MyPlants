// Persistance locale des plantes et des arrosages (IndexedDB).
// Chaque enregistrement appartient à un profil : "guest" ou "account:<id>".
//
// La version de la base reste à 3 volontairement : l'augmenter empêcherait de
// revenir à une version précédente de l'application (erreur VersionError).
// Les anciens enregistrements sont donc lus de façon tolérante (voir getUnsynced
// et getWateringHistory) plutôt que migrés.

window.myPlantsIndexedDb = (function () {
    const DATABASE_NAME = "MyPlantsDb";
    const DATABASE_VERSION = 3;
    const PLANTS = "plants";
    const WATERINGS = "waterings";
    const GUEST = "guest";

    let databasePromise = null;

    // ---------------------------------------------------------------- Outils

    const now = () => new Date().toISOString();

    function promisify(request) {
        return new Promise((resolve, reject) => {
            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
        });
    }

    function transactionCompleted(transaction) {
        return new Promise((resolve, reject) => {
            transaction.oncomplete = () => resolve();
            transaction.onerror = () => reject(transaction.error);
            transaction.onabort = () => reject(
                transaction.error ?? new Error("Transaction IndexedDB annulée."));
        });
    }

    // Exécute `work` dans une transaction et ne rend la main qu'une fois les
    // données réellement écrites. Toute erreur annule la transaction.
    async function inTransaction(storeNames, mode, work) {
        const database = await openDatabase();
        const transaction = database.transaction(storeNames, mode);
        const completed = transactionCompleted(transaction);

        try {
            const result = await work(transaction);
            await completed;
            return result;
        } catch (error) {
            completed.catch(() => { });

            try {
                transaction.abort();
            } catch {
                // La transaction est déjà terminée.
            }

            throw error;
        }
    }

    // ------------------------------------------------------------ Connexion

    function upgradeDatabase(event) {
        const database = event.target.result;
        const transaction = event.target.transaction;

        if (!database.objectStoreNames.contains(PLANTS)) {
            database.createObjectStore(PLANTS, { keyPath: "id" });
        }

        if (!database.objectStoreNames.contains(WATERINGS)) {
            const store = database.createObjectStore(WATERINGS, { keyPath: "id" });
            store.createIndex("plantId", "plantId", { unique: false });
            store.createIndex("date", "date", { unique: false });
        }

        // Avant la version 3, il n'y avait pas de profils : tout est "guest".
        if (event.oldVersion < 3) {
            const cursorRequest = transaction.objectStore(PLANTS).openCursor();

            cursorRequest.onsuccess = function (cursorEvent) {
                const cursor = cursorEvent.target.result;

                if (!cursor) {
                    return;
                }

                cursor.update({ ...cursor.value, profileId: GUEST });
                cursor.continue();
            };
        }
    }

    // Une seule connexion partagée, rouverte automatiquement si besoin.
    function openDatabase() {
        databasePromise ??= new Promise((resolve, reject) => {
            const request = indexedDB.open(DATABASE_NAME, DATABASE_VERSION);

            request.onupgradeneeded = upgradeDatabase;

            request.onsuccess = () => {
                const database = request.result;

                // Un autre onglet demande une migration : on libère la base.
                database.onversionchange = () => {
                    database.close();
                    databasePromise = null;
                };

                resolve(database);
            };

            request.onerror = () => reject(request.error);

            request.onblocked = () => reject(new Error(
                "La migration IndexedDB est bloquée par un autre onglet. " +
                "Ferme les autres onglets MyPlants puis réessaie."));
        });

        // Une ouverture échouée ne doit pas rester en cache.
        databasePromise.catch(() => { databasePromise = null; });

        return databasePromise;
    }

    // --------------------------------------------------------------- Plantes

    async function getAll(profileId) {
        const plants = await inTransaction(PLANTS, "readonly", transaction =>
            promisify(transaction.objectStore(PLANTS).getAll()));

        return plants.filter(plant =>
            plant.profileId === profileId && !plant.deletedAt);
    }

    async function getById(id, profileId) {
        const plant = await inTransaction(PLANTS, "readonly", transaction =>
            promisify(transaction.objectStore(PLANTS).get(id)));

        const isVisible = plant
            && plant.profileId === profileId
            && !plant.deletedAt;

        return isVisible ? plant : null;
    }

    async function add(plant, profileId) {
        await inTransaction(PLANTS, "readwrite", transaction =>
            promisify(transaction.objectStore(PLANTS).add({
                ...plant,
                id: plant.id.toString(),
                profileId,
                updatedAt: now(),
                deletedAt: null,
                isSynced: false
            })));
    }

    async function update(plant, profileId) {
        await inTransaction(PLANTS, "readwrite", async transaction => {
            const store = transaction.objectStore(PLANTS);
            const id = plant.id.toString();
            const existing = await promisify(store.get(id));

            if (existing && existing.profileId !== profileId) {
                throw new Error("Plante introuvable pour ce profil.");
            }

            await promisify(store.put({
                ...existing, // conserve les champs inconnus du modèle C# (ex. importedFromGuestId)
                ...plant,
                id,
                profileId,
                updatedAt: now(),
                isSynced: false
            }));
        });
    }

    // Suppression logique : l'enregistrement reste pour pouvoir être synchronisé.
    async function remove(id, profileId) {
        await inTransaction(PLANTS, "readwrite", async transaction => {
            const store = transaction.objectStore(PLANTS);
            const plant = await promisify(store.get(id));

            if (!plant || plant.profileId !== profileId) {
                return;
            }

            const timestamp = now();

            await promisify(store.put({
                ...plant,
                updatedAt: timestamp,
                deletedAt: timestamp,
                isSynced: false
            }));
        });
    }

    // -------------------------------------------------------------- Arrosages

    async function addWatering(watering, profileId) {
        await inTransaction([PLANTS, WATERINGS], "readwrite", async transaction => {
            const plantId = watering.plantId.toString();
            const plant = await promisify(
                transaction.objectStore(PLANTS).get(plantId));

            if (!plant || plant.profileId !== profileId) {
                throw new Error("Plante introuvable pour ce profil.");
            }

            await promisify(transaction.objectStore(WATERINGS).add({
                ...watering,
                id: watering.id.toString(),
                plantId,
                profileId
            }));
        });
    }

    async function getWateringHistory(plantId, profileId) {
        return await inTransaction([PLANTS, WATERINGS], "readonly", async transaction => {
            const id = plantId.toString();
            const plant = await promisify(transaction.objectStore(PLANTS).get(id));

            if (!plant || plant.profileId !== profileId) {
                return [];
            }

            const history = await promisify(
                transaction.objectStore(WATERINGS).index("plantId").getAll(id));

            // Les arrosages d'avant les profils n'ont pas de profileId : ils sont "guest".
            return history
                .filter(watering => (watering.profileId ?? GUEST) === profileId)
                .sort((a, b) => new Date(b.date) - new Date(a.date));
        });
    }

    // ------------------------------------------------------ Invité -> compte

    // Plantes invitées qui n'ont pas encore été copiées dans le profil cible.
    // Une plante déjà importée garde la trace de son origine (importedFromGuestId),
    // même si elle a ensuite été modifiée ou supprimée dans le compte.
    function findPendingGuestPlants(allPlants, targetProfileId) {
        const alreadyImported = new Set(
            allPlants
                .filter(plant => plant.profileId === targetProfileId)
                .map(plant => plant.importedFromGuestId)
                .filter(Boolean));

        return allPlants.filter(plant =>
            plant.profileId === GUEST
            && !plant.deletedAt
            && !alreadyImported.has(plant.id.toString()));
    }

    // Vrai seulement s'il reste des plantes invitées à importer dans ce compte.
    async function hasGuestPlants(targetProfileId) {
        if (!targetProfileId || targetProfileId === GUEST) {
            return false;
        }

        const plants = await inTransaction(PLANTS, "readonly", transaction =>
            promisify(transaction.objectStore(PLANTS).getAll()));

        return findPendingGuestPlants(plants, targetProfileId).length > 0;
    }

    // Copie les plantes invitées (et leur historique) vers le compte.
    // Tout se fait dans une seule transaction : pas de doublon si on clique deux fois.
    async function importGuestPlants(targetProfileId) {
        if (!targetProfileId || targetProfileId === GUEST) {
            throw new Error("Profil cible invalide.");
        }

        return await inTransaction([PLANTS, WATERINGS], "readwrite", async transaction => {
            const plantsStore = transaction.objectStore(PLANTS);
            const wateringsStore = transaction.objectStore(WATERINGS);

            const [allPlants, allWaterings] = await Promise.all([
                promisify(plantsStore.getAll()),
                promisify(wateringsStore.getAll())
            ]);

            const guestPlants = findPendingGuestPlants(allPlants, targetProfileId);

            const guestWateringsByPlant = new Map();

            for (const watering of allWaterings) {
                if ((watering.profileId ?? GUEST) !== GUEST) {
                    continue;
                }

                const list = guestWateringsByPlant.get(watering.plantId) ?? [];
                list.push(watering);
                guestWateringsByPlant.set(watering.plantId, list);
            }

            for (const plant of guestPlants) {
                const originalId = plant.id.toString();
                const newPlantId = crypto.randomUUID();

                plantsStore.add({
                    ...plant,
                    id: newPlantId,
                    profileId: targetProfileId,
                    importedFromGuestId: originalId,
                    isSynced: false
                });

                for (const watering of guestWateringsByPlant.get(originalId) ?? []) {
                    wateringsStore.add({
                        ...watering,
                        id: crypto.randomUUID(),
                        plantId: newPlantId,
                        profileId: targetProfileId,
                        isSynced: false
                    });
                }
            }

            return guestPlants.length;
        });
    }

    // --------------------------------------------------------- Synchronisation

    // Les plantes d'avant la synchronisation n'ont pas de champ isSynced :
    // tout ce qui n'est pas explicitement synchronisé reste à envoyer.
    async function getUnsynced(profileId) {
        const plants = await inTransaction(PLANTS, "readonly", transaction =>
            promisify(transaction.objectStore(PLANTS).getAll()));

        return plants.filter(plant =>
            plant.profileId === profileId && plant.isSynced !== true);
    }

    async function markAsSynced(id, profileId) {
        await inTransaction(PLANTS, "readwrite", async transaction => {
            const store = transaction.objectStore(PLANTS);
            const plant = await promisify(store.get(id));

            if (!plant || plant.profileId !== profileId) {
                return;
            }

            await promisify(store.put({ ...plant, isSynced: true }));
        });
    }

    async function upsertFromCloud(plant, profileId) {
        await inTransaction(PLANTS, "readwrite", transaction =>
            promisify(transaction.objectStore(PLANTS).put({
                ...plant,
                id: plant.id.toString(),
                profileId,
                isSynced: true
            })));
    }

    // ------------------------------------------- Synchronisation des arrosages

    // Tous les arrosages du profil, avec leur état de synchronisation.
    // Un arrosage sans champ isSynced est considéré comme à envoyer.
    async function getAllWaterings(profileId) {
        const waterings = await inTransaction(WATERINGS, "readonly", transaction =>
            promisify(transaction.objectStore(WATERINGS).getAll()));

        return waterings.filter(watering =>
            (watering.profileId ?? GUEST) === profileId);
    }

    async function markWateringsAsSynced(ids, profileId) {
        await inTransaction(WATERINGS, "readwrite", async transaction => {
            const store = transaction.objectStore(WATERINGS);

            for (const id of ids) {
                const watering = await promisify(store.get(id));

                if (watering && (watering.profileId ?? GUEST) === profileId) {
                    await promisify(store.put({ ...watering, isSynced: true }));
                }
            }
        });
    }

    // Une seule transaction : tous les arrosages arrivent, ou aucun.
    async function upsertWateringsFromCloud(waterings, profileId) {
        await inTransaction(WATERINGS, "readwrite", async transaction => {
            const store = transaction.objectStore(WATERINGS);

            for (const watering of waterings) {
                await promisify(store.put({
                    ...watering,
                    id: watering.id.toString(),
                    plantId: watering.plantId.toString(),
                    profileId,
                    isSynced: true
                }));
            }
        });
    }

    return {
        getAll,
        getById,
        add,
        update,
        delete: remove,
        addWatering,
        getWateringHistory,
        hasGuestPlants,
        importGuestPlants,
        getUnsynced,
        markAsSynced,
        upsertFromCloud,
        getAllWaterings,
        markWateringsAsSynced,
        upsertWateringsFromCloud
    };
})();