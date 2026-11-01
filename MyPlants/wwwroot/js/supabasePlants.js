
import { getSupabase } from "./supabaseClient.js";

export async function getAll() {
    const supabase = getSupabase();

    const { data: userData, error: userError } =
        await supabase.auth.getUser();

    if (userError || !userData.user) {
        throw new Error("Une connexion est nécessaire.");
    }

    const { data, error } = await supabase
        .from("plants")
        .select(`
            id,
            name,
            species,
            date_planted,
            last_watered,
            is_archived,
            watering_interval,
            updated_at,
            deleted_at
        `)
        .is("deleted_at", null)
        .order("updated_at", { ascending: false });

    if (error) {
        throw new Error(error.message);
    }

    return data ?? [];
}

export async function save(plant) {
    const supabase = getSupabase();

    const { data: userData, error: userError } =
        await supabase.auth.getUser();

    if (userError || !userData.user) {
        throw new Error("Une connexion est nécessaire.");
    }

    const payload = {
        id: plant.id,
        user_id: userData.user.id,
        name: plant.name ?? null,
        species: plant.species ?? null,
        date_planted: plant.datePlanted ?? null,
        last_watered: plant.lastWatered ?? null,
        is_archived: plant.isArchived ?? false,
        watering_interval: plant.wateringInterval ?? null
    };

    const { data, error } = await supabase
        .from("plants")
        .upsert(payload, { onConflict: "id" })
        .select()
        .single();

    if (error) {
        throw new Error(error.message);
    }

    return data;
}

export async function remove(id) {
    const supabase = getSupabase();
    await requireAuthenticatedUser(supabase);

    const { error } = await supabase
        .from("plants")
        .update({
            deleted_at: new Date().toISOString()
        })
        .eq("id", id);

    if (error) {
        throw new Error(error.message);
    }
}

export async function upsert(plant) {
    const supabase = getSupabase();

    const {
        data: { user },
        error: userError
    } = await supabase.auth.getUser();

    if (userError) {
        throw new Error(userError.message);
    }

    if (!user) {
        throw new Error("Aucun utilisateur Supabase connecté.");
    }

    const row = {
        id: plant.id,
        user_id: user.id,
        name: plant.name,
        species: plant.species,
        date_planted: plant.datePlanted,
        last_watered: plant.lastWatered,
        is_archived: plant.isArchived,
        watering_interval: plant.wateringInterval,
        updated_at: plant.updatedAt,
        deleted_at: plant.deletedAt
    };

    const { error } = await supabase
        .from("plants")
        .upsert(row, { onConflict: "id" });

    if (error) {
        throw new Error(error.message);
    }
}
