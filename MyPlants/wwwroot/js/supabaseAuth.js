import {
    initialize,
    getSupabase
} from "./supabaseClient.js";

export { initialize };

export async function signIn(email, password) {
    const { data, error } =
        await getSupabase().auth.signInWithPassword({
            email,
            password
        });

    if (error) {
        return {
            success: false,
            error: error.message,
            userId: null,
            email: null
        };
    }

    return {
        success: true,
        error: null,
        userId: data.user?.id ?? null,
        email: data.user?.email ?? null
    };
}

// status : "authenticated" | "anonymous" | "unavailable"
// "unavailable" = impossible de savoir (réseau coupé) : l'appelant garde son état.
export async function getSession() {
    const { data, error } = await getSupabase().auth.getSession();

    if (error) {
        return { status: "unavailable", userId: null, email: null };
    }

    const user = data.session?.user;

    return {
        status: user ? "authenticated" : "anonymous",
        userId: user?.id ?? null,
        email: user?.email ?? null
    };
}

// Portée "local" : déconnexion sans appel réseau, donc possible hors ligne.
export async function signOut() {
    const { error } = await getSupabase().auth.signOut({ scope: "local" });

    if (error) {
        throw new Error(error.message);
    }
}