import {
    initialize,
    getSupabase
} from "./supabaseClient.js";

export { initialize };

let supabase;

export async function signIn(email, password) {
    const supabase = getSupabase();
    const { data, error } =
        await supabase.auth.signInWithPassword({
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

export async function getSession() {
    const supabase = getSupabase();
    const { data, error } = await supabase.auth.getSession();

    if (error) {
        return {
            isLoggedIn: false,
            userId: null,
            email: null
        };
    }

    return {
        isLoggedIn: !!data.session,
        userId: data.session?.user?.id ?? null,
        email: data.session?.user?.email ?? null
    };
}

export async function signOut() {
    const supabase = getSupabase();
    const { error } = await supabase.auth.signOut();

    if (error) {
        throw new Error(error.message);
    }
}
