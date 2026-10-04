using Microsoft.AspNetCore.Components;

using System.Runtime.CompilerServices;

namespace MyPlants.Extensions;

public static class NavigationManagerExtentions
{
    public static void NavigateToWithBaseUrl(this NavigationManager navigationManager, string uri, bool forceLoad = false, [CallerMemberName] string? callerName = null)
    {
        navigationManager.NavigateTo($"{navigationManager.BaseUri}{uri}", forceLoad);
    }
}
