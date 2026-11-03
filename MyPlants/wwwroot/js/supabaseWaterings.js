import { getSupabase } from "./supabaseClient.js";

export async function getAll() {
    const supabase = getSupabase();

    const { data: { user }, error: userError } =
        await supabase.auth.getUser();

    if (userError || !user) {
        throw new Error("Une connexion est nécessaire.");
    }

    const { data, error } = await supabase
        .from("waterings")
        .select(`
            id,
            user_id,
            plant_id,
            date,
            note,
            updated_at
        `)
        .order("updated_at", { ascending: false });

    if (error) {
        throw new Error(error.message);
    }

    return (data ?? []).map(watering => ({
        id: watering.id,
        profileId: watering.user_id,
        plantId: watering.plant_id,
        date: watering.date,
        note: watering.note,
        updatedAt: watering.updated_at,
        isSynced: true
    }));
}

export async function upsert(watering) {
    const supabase = getSupabase();

    const { data: { user }, error: userError } =
        await supabase.auth.getUser();

    if (userError || !user) {
        throw new Error("Une connexion est nécessaire.");
    }

    const payload = {
        id: watering.id,
        user_id: user.id,
        plant_id: watering.plantId,
        date: watering.date,
        note: watering.note ?? null,
        updated_at: watering.updatedAt
    };

    const { error } = await supabase
        .from("waterings")
        .upsert(payload, { onConflict: "id" });

    if (error) {
        throw new Error(error.message);
    }
}
