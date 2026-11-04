import { getSupabase } from "./supabaseClient.js";
import { toIsoOrNull } from "./dates.js";

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
export async function getAll() {
    const supabase = getSupabase();
    await requireUserId(supabase);

    const { data, error } = await supabase
        .from("plants")
        .select(COLUMNS)
        .order("updated_at", { ascending: false });

    if (error) {
        throw new Error(error.message);
    }

    return data ?? [];
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