namespace Content.Client.UserInterface.Screens;

public enum ScreenType
{
    /// <summary>
    ///     The modern SS14 user interface.
    /// </summary>
    Default,
    /// <summary>
    ///     The classic SS13 user interface.
    /// </summary>
    Separated,
    // Mythos: V2 HUD reskin entry; routed in GameplayState.LoadMainScreen.
    Mythos
}
