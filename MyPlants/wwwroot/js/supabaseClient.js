// supabase-js est hébergé localement (wwwroot/lib/supabase/supabase.js) et chargé
// par index.html : il est ainsi mis en cache par le service worker et reste
// disponible hors ligne. Il expose l'objet global window.supabase.

let supabase = null;

export function initialize(url, publishableKey) {
    if (supabase) {
        return;
    }

    const library = window.supabase;

    if (!library) {
        throw new Error("La bibliothèque supabase-js n'est pas chargée.");
    }

    supabase = library.createClient(url, publishableKey, {
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