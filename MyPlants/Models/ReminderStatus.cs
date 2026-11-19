namespace MyPlants.Models;

/// <summary>Ce que le navigateur permet pour les rappels locaux, tel que renvoyé par reminders.js.</summary>
public sealed class ReminderStatus
{
    /// <summary>Notifications et Periodic Background Sync disponibles (Chrome/Edge, service worker actif).</summary>
    public bool Supported { get; set; }

    /// <summary>"default" (pas encore demandée), "granted" ou "denied".</summary>
    public string Permission { get; set; } = "default";

    /// <summary>L'application est installée (fenêtre dédiée) : condition de Chrome pour les réveils en arrière-plan.</summary>
    public bool Installed { get; set; }
}

public enum ReminderEnableResult
{
    Ok,
    Unsupported,
    Denied,
    Failed
}