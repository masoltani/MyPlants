
import { createClient } from
    "https://cdn.jsdelivr.net/npm/@supabase/supabase-js@2/+esm";

let supabase = null;

export function initialize(url, publishableKey) {
    if (supabase) {
        return;
    }

    supabase = createClient(url, publishableKey, {
        auth: {
            persistSession: true,
            autoRefreshToken: true,
            detectSessionInUrl: true
        }
    });
}

export function getSupabase() {
    if (!supabase) {
        throw new Error("Le client Supabase n'est pas initialisé.");
    }

    return supabase;
}
