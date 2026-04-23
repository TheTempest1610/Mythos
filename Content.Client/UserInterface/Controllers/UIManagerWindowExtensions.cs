using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.UserInterface.Controllers;

public static class UIManagerWindowExtensions
{
    /// <summary>
    /// Returns <paramref name="window"/> if it is still live, otherwise creates a new one and runs <paramref name="init"/>.
    /// </summary>
    public static T EnsureWindow<T>(this IUserInterfaceManager uiManager, ref T? window, Action<T>? init = null)
        where T : BaseWindow, new()
    {
        if (window is { Disposed: false })
            return window;

        window = uiManager.CreateWindow<T>();
        init?.Invoke(window);
        return window;
    }
}
