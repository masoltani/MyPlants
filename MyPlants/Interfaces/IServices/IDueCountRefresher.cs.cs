namespace MyPlants.Interfaces.IServices;

/// <summary>
/// Permet aux services qui modifient les plantes de demander la mise à jour du nombre de plantes à arroser
/// (badge de l'application), sans connaître la façon dont il est calculé ni affiché.
/// </summary>
public interface IDueCountRefresher
{
    void RequestRefresh();
}