import { getSupabase } from "./supabaseClient.js";
import { toIsoOrNull } from "./dates.js";

// Nombre de lignes lues par requête (limite de PostgREST).
const PAGE_SIZE = 1000;

const COLUMNS = `
    id,
    name,
    species,
    date_planted,
    last_watered,
    is_archived,
    watering_interval,
    client_updated_at,
    updated_at,
    deleted_at
`;

// Lit l'identifiant de l'utilisateur dans la session locale (sans appel réseau).
// La sécurité reste assurée côté serveur par les politiques RLS.
async function requireUserId(supabase) {
    const { data, error } = await supabase.auth.getSession();

    if (error || !data.session) {
        throw new Error("Une connexion est nécessaire.");
    }

    return data.session.user.id;
}

// Renvoie aussi les plantes supprimées : c'est ce qui propage une suppression
// faite sur un appareil vers les autres.
// `since` (texte ISO, optionnel) : ne renvoie que les plantes modifiées après cet instant (heure du serveur).
export async function getAll(since) {
    const supabase = getSupabase();
    await requireUserId(supabase);

    const rows = [];

    // PostgREST renvoie au plus 1000 lignes par requête : on lit par pages, avec un tri stable.
    for (let from = 0; ; from += PAGE_SIZE) {
        let query = supabase.from("plants").select(COLUMNS);

        if (since) {
            query = query.gt("updated_at", since);
        }

        const { data, error } = await query
            .order("updated_at", { ascending: true })
            .order("id", { ascending: true })
            .range(from, from + PAGE_SIZE - 1);

        if (error) {
            throw new Error(error.message);
        }

        rows.push(...(data ?? []));

        if (!data || data.length < PAGE_SIZE) {
            break;
        }
    }

    return rows;
}

export async function upsert(plant) {
    const supabase = getSupabase();
    const userId = await requireUserId(supabase);

    const row = {
        id: plant.id,
        user_id: userId,
        name: plant.name ?? null,
        species: plant.species ?? null,
        date_planted: toIsoOrNull(plant.datePlanted),
        last_watered: toIsoOrNull(plant.lastWatered),
        is_archived: plant.isArchived ?? false,
        watering_interval: plant.wateringInterval ?? null,
        client_updated_at: plant.updatedAt,
        deleted_at: plant.deletedAt ?? null
    };

    const { error } = await supabase
        .from("plants")
        .upsert(row, { onConflict: "id" });

    if (error) {
        throw new Error(error.message);
    }
}