import { getSupabase } from "./supabaseClient.js";
import { toIsoOrNull } from "./dates.js";

// PostgREST renvoie au plus 1000 lignes par requête : on lit par pages.
const PAGE_SIZE = 1000;
const UPSERT_CHUNK_SIZE = 500;

// Lit l'identifiant de l'utilisateur dans la session locale (sans appel réseau).
// La sécurité reste assurée côté serveur par les politiques RLS.
async function requireUserId(supabase) {
    const { data, error } = await supabase.auth.getSession();

    if (error || !data.session) {
        throw new Error("Une connexion est nécessaire.");
    }

    return data.session.user.id;
}

function toRow(watering, userId) {
    return {
        id: watering.id,
        user_id: userId,
        plant_id: watering.plantId,
        date: toIsoOrNull(watering.date),
        note: watering.note ?? null
    };
}

// `since` (texte ISO, optionnel) : ne renvoie que les arrosages modifiés après cet instant (heure du serveur).
export async function getAll(since) {
    const supabase = getSupabase();
    await requireUserId(supabase);

    const rows = [];

    // Tri par id : indispensable pour que la pagination soit stable.
    for (let from = 0; ; from += PAGE_SIZE) {
        let query = supabase
            .from("waterings")
            .select("id, plant_id, date, note, updated_at")
            .is("deleted_at", null);

        if (since) {
            query = query.gt("updated_at", since);
        }

        const { data, error } = await query
            .order("id")
            .range(from, from + PAGE_SIZE - 1);

        if (error) {
            throw new Error(error.message);
        }

        rows.push(...(data ?? []));

        if (!data || data.length < PAGE_SIZE) {
            break;
        }
    }

    return rows.map(row => ({
        id: row.id,
        plantId: row.plant_id,
        date: row.date,
        note: row.note,
        updatedAt: row.updated_at,
        isSynced: true
    }));
}

export async function upsert(watering) {
    const supabase = getSupabase();
    const userId = await requireUserId(supabase);

    const { error } = await supabase
        .from("waterings")
        .upsert(toRow(watering, userId), { onConflict: "id" });

    if (error) {
        throw new Error(error.message);
    }
}

// Envoi groupé : une requête pour plusieurs arrosages au lieu d'une par arrosage.
export async function upsertMany(waterings) {
    const supabase = getSupabase();
    const userId = await requireUserId(supabase);

    for (let start = 0; start < waterings.length; start += UPSERT_CHUNK_SIZE) {
        const rows = waterings
            .slice(start, start + UPSERT_CHUNK_SIZE)
            .map(watering => toRow(watering, userId));

        const { error } = await supabase
            .from("waterings")
            .upsert(rows, { onConflict: "id" });

        if (error) {
            throw new Error(error.message);
        }
    }
}