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
        code: isNetworkError ? "network" : (error.code || error.name || "unknown"),
        error: error.message,
        userId: null,
        email: null
    };
}

// Un compte doit être validé manuellement avant de pouvoir se connecter.
// La base de données l'impose déjà (voir lot10-comptes.sql) ; ici on refuse aussi la
// connexion côté application, avec un message clair.
async function signOutLocally(supabase) {
    await supabase.auth.signOut({ scope: "local" });
}

export async function signIn(email, password) {
    const supabase = getSupabase();

    const { data, error } = await supabase.auth.signInWithPassword({
        email,
        password
    });

    if (error) {
        return failure(error);
    }

    const { data: profile, error: profileError } = await supabase
        .from("profiles")
        .select("is_approved")
        .eq("id", data.user.id)
        .maybeSingle();

    if (profileError) {
        // Dans le doute (réseau, table absente), on refuse plutôt que d'ouvrir l'accès.
        await signOutLocally(supabase);
        return failure(profileError);
    }

    if (!profile?.is_approved) {
        await signOutLocally(supabase);
        return { success: false, code: "pending_approval", error: null, userId: null, email: null };
    }

    return success(data.user);
}

// Crée un compte. Il ne permet pas de se connecter tant qu'il n'est pas validé :
// on ferme donc immédiatement la session que Supabase peut ouvrir à la création.
// needsEmailConfirmation : vrai si Supabase attend d'abord la confirmation de l'adresse e-mail.
export async function signUp(email, password) {
    const supabase = getSupabase();

    const { data, error } = await supabase.auth.signUp({
        email,
        password,
        options: { emailRedirectTo: document.baseURI }
    });

    if (error) {
        return failure(error);
    }

    const needsEmailConfirmation = !data.session;

    if (data.session) {
        await signOutLocally(supabase);
    }

    return { ...success(), needsEmailConfirmation };
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