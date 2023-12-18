// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.BlazorIdentity
{
    internal static class BlazorIdentityHelper
    {
        internal static string GetFormattedRelativeIdentityFile(string fullFileName)
        {
            string identifier = "BlazorIdentity\\";
            int index = fullFileName.IndexOf(identifier);
            if (index != -1)
            {
                string pathAfterIdentifier = fullFileName.Substring(index + identifier.Length);
                string extension = pathAfterIdentifier.StartsWith("Identity") ? ".cs" : ".razor";
                string pathWithoutExtension = pathAfterIdentifier.Replace(".tt", extension);
                return pathWithoutExtension;
            }

            return string.Empty;
        }

        internal static string NavMenuEndAddition =
            @"@code {
    private string? currentUrl;

    protected override void OnInitialized()
    {
        currentUrl = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        NavigationManager.LocationChanged += OnLocationChanged;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        currentUrl = NavigationManager.ToBaseRelativePath(e.Location);
        StateHasChanged();
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
    }
}";
        internal static string NavMenuStartAddition =
            @"@implements IDisposable

@inject NavigationManager NavigationManager";

        internal static string NavMenuReplacementText =
            @"
        <div class=""nav-item px-3"">
            <NavLink class=""nav-link"" href=""weather"">
                <span class=""bi bi-list-nested-nav-menu"" aria-hidden=""true""></span> Weather
            </NavLink>
        </div>

        <div class=""nav-item px-3"">
            <NavLink class=""nav-link"" href=""auth"">
                <span class=""bi bi-lock-nav-menu"" aria-hidden=""true""></span> Auth Required
            </NavLink>
        </div>

        <AuthorizeView>
            <Authorized>
                <div class=""nav-item px-3"">
                    <NavLink class=""nav-link"" href=""Account/Manage"">
                        <span class=""bi bi-person-fill-nav-menu"" aria-hidden=""true""></span> @context.User.Identity?.Name
                    </NavLink>
                </div>
                <div class=""nav-item px-3"">
                    <form action=""Account/Logout"" method=""post"">
                        <AntiforgeryToken />
                        <input type=""hidden"" name=""ReturnUrl"" value=""@currentUrl"" />
                        <button type=""submit"" class=""nav-link"">
                            <span class=""bi bi-arrow-bar-left-nav-menu"" aria-hidden=""true""></span> Logout
                        </button>
                    </form>
                </div>
            </Authorized>
            <NotAuthorized>
                <div class=""nav-item px-3"">
                    <NavLink class=""nav-link"" href=""Account/Register"">
                        <span class=""bi bi-person-nav-menu"" aria-hidden=""true""></span> Register
                    </NavLink>
                </div>
                <div class=""nav-item px-3"">
                    <NavLink class=""nav-link"" href=""Account/Login"">
                        <span class=""bi bi-person-badge-nav-menu"" aria-hidden=""true""></span> Login
                    </NavLink>
                </div>
            </NotAuthorized>
        </AuthorizeView>";

        internal static string NavMenuReplacementTextOriginal =
            @"<div class=""nav-item px-3"">
    <NavLink class=""nav-link"" href=""weather"">
        <span class=""bi bi-list-nested-nav-menu"" aria-hidden=""true""></span> Weather
    </NavLink>
</div>";

        internal static string NavMenuCssEndAddition =
            @"
.bi-lock-nav-menu {
    background-image: url(""data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='16' height='16' fill='white' class='bi bi-list-nested' viewBox='0 0 16 16'%3E%3Cpath d='M8 1a2 2 0 0 1 2 2v4H6V3a2 2 0 0 1 2-2zm3 6V3a3 3 0 0 0-6 0v4a2 2 0 0 0-2 2v5a2 2 0 0 0 2 2h6a2 2 0 0 0 2-2V9a2 2 0 0 0-2-2zM5 8h6a1 1 0 0 1 1 1v5a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V9a1 1 0 0 1 1-1z'/%3E%3C/svg%3E"");
}

.bi-person-nav-menu {
    background-image: url(""data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='16' height='16' fill='white' class='bi bi-person' viewBox='0 0 16 16'%3E%3Cpath d='M8 8a3 3 0 1 0 0-6 3 3 0 0 0 0 6Zm2-3a2 2 0 1 1-4 0 2 2 0 0 1 4 0Zm4 8c0 1-1 1-1 1H3s-1 0-1-1 1-4 6-4 6 3 6 4Zm-1-.004c-.001-.246-.154-.986-.832-1.664C11.516 10.68 10.289 10 8 10c-2.29 0-3.516.68-4.168 1.332-.678.678-.83 1.418-.832 1.664h10Z'/%3E%3C/svg%3E"");
}

.bi-person-badge-nav-menu {
    background-image: url(""data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='16' height='16' fill='white' class='bi bi-person-badge' viewBox='0 0 16 16'%3E%3Cpath d='M6.5 2a.5.5 0 0 0 0 1h3a.5.5 0 0 0 0-1h-3zM11 8a3 3 0 1 1-6 0 3 3 0 0 1 6 0z'/%3E%3Cpath d='M4.5 0A2.5 2.5 0 0 0 2 2.5V14a2 2 0 0 0 2 2h8a2 2 0 0 0 2-2V2.5A2.5 2.5 0 0 0 11.5 0h-7zM3 2.5A1.5 1.5 0 0 1 4.5 1h7A1.5 1.5 0 0 1 13 2.5v10.795a4.2 4.2 0 0 0-.776-.492C11.392 12.387 10.063 12 8 12s-3.392.387-4.224.803a4.2 4.2 0 0 0-.776.492V2.5z'/%3E%3C/svg%3E"");
}

.bi-person-fill-nav-menu {
    background-image: url(""data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='16' height='16' fill='white' class='bi bi-person-fill' viewBox='0 0 16 16'%3E%3Cpath d='M3 14s-1 0-1-1 1-4 6-4 6 3 6 4-1 1-1 1H3Zm5-6a3 3 0 1 0 0-6 3 3 0 0 0 0 6Z'/%3E%3C/svg%3E"");
}

.bi-arrow-bar-left-nav-menu {
    background-image: url(""data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='16' height='16' fill='white' class='bi bi-arrow-bar-left' viewBox='0 0 16 16'%3E%3Cpath d='M12.5 15a.5.5 0 0 1-.5-.5v-13a.5.5 0 0 1 1 0v13a.5.5 0 0 1-.5.5ZM10 8a.5.5 0 0 1-.5.5H3.707l2.147 2.146a.5.5 0 0 1-.708.708l-3-3a.5.5 0 0 1 0-.708l3-3a.5.5 0 1 1 .708.708L3.707 7.5H9.5a.5.5 0 0 1 .5.5Z'/%3E%3C/svg%3E"");
}";

    }
}
