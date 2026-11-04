// Convertit une date reçue de l'application en instant UTC non ambigu (ISO 8601).
//
// Une date saisie sans heure arrive sous la forme "2026-10-30T00:00:00", sans fuseau.
// Envoyée telle quelle, Supabase la lirait comme minuit UTC et la date pourrait
// se décaler d'un jour selon le fuseau de l'utilisateur. En la lisant d'abord dans le
// fuseau du navigateur, elle revient à la même heure locale à l'affichage.
export function toIsoOrNull(value) {
    if (!value) {
        return null;
    }

    const date = new Date(value);

    return Number.isNaN(date.getTime()) ? null : date.toISOString();
}