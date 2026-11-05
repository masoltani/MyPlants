import {
    initialize,
    getSupabase
} from "./supabaseClient.js";

export { initialize };

// Résultat commun aux opérations d'authentification.
// code : identifiant d'erreur de Supabase, ou "network" / "no_session" (voir ci-dessous).
function success(user = null) {
    return {
        success: true,
        code: null,
        error: null,
        userId: user?.id ?? null,
        email: user?.email ?? null
    };
}

function failure(error) {
    const isNetworkError =
        error.name === "AuthRetryableFetchError" || error.status === 0;

    return {
        success: false,
        code: isNetworkError ? "network" : (error.code ?? error.name ?? "unknown"),
        error: error.message,
        userId: null,
        email: null
    };
}

export async function signIn(email, password) {
    const { data, error } =
        await getSupabase().auth.signInWithPassword({
            email,
            password
        });

    return error ? failure(error) : success(data.user);
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

// Envoie un e-mail contenant un lien de réinitialisation.
// Supabase répond de la même façon que l'adresse existe ou non : on ne révèle rien.
// Le lien ramène sur la racine de l'application (document.baseURI), qui doit figurer
// dans les « Redirect URLs » de Supabase.
export async function requestPasswordReset(email) {
    const { error } = await getSupabase().auth.resetPasswordForEmail(email, {
        redirectTo: document.baseURI
    });

    return error ? failure(error) : success();
}

// Change le mot de passe de l'utilisateur connecté (ou arrivé par un lien de réinitialisation).
export async function updatePassword(newPassword) {
    const supabase = getSupabase();

    const { data: sessionData } = await supabase.auth.getSession();

    if (!sessionData.session) {
        return { success: false, code: "no_session", error: null, userId: null, email: null };
    }

    const { data, error } = await supabase.auth.updateUser({ password: newPassword });

    return error ? failure(error) : success(data.user);
}