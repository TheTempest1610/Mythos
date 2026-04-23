using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.DebugMon;

public sealed class ServerTickTimePanel : PanelContainer
{
    private readonly ServerTickTimeManager _manager;
    private readonly IGameTiming _timing;
    private readonly Label _contents;

    public ServerTickTimePanel(ServerTickTimeManager manager, IGameTiming timing)
    {
        _manager = manager;
        _timing = timing;

        _contents = new Label { FontColorShadowOverride = Color.Black };
        AddChild(_contents);

        PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = new Color(35, 134, 37, 138),
            ContentMarginLeftOverride = 5,
            ContentMarginTopOverride = 5,
        };

        MouseFilter = _contents.MouseFilter = MouseFilterMode.Ignore;
        HorizontalAlignment = HAlignment.Left;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!VisibleInTree)
            return;

        if (!_manager.HasData)
        {
            _contents.Text = "Server MSPT: (no data)";
            return;
        }

        var budgetMs = 1000.0 / _timing.TickRate;
        _contents.Text =
            $"Server MSPT: {_manager.AverageTickMs:F2} ms (σ {_manager.StdDevMs:F2} ms, budget {budgetMs:F2} ms)";
    }
}
