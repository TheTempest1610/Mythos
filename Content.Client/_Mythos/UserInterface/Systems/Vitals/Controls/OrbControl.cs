using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Maths;

namespace Content.Client._Mythos.UserInterface.Systems.Vitals.Controls;

// Mythos: Circular orb meter for the V2 HUD vitals strip. "Health-meter geometry":
// a thick dark outer frame, a dark inner well showing the empty portion at top, and
// liquid-style fill rising from the bottom bounded by a horizontal surface line. All
// primitives, no textures, so it scales cleanly without placeholder PNGs.
public sealed class OrbControl : Control
{
    private float _value = 1f;
    private float _maxValue = 1f;
    private Color _frameColor = Color.FromHex("#040608");
    private Color _backplateColor = Color.FromHex("#0D131C");
    private Color _fillColor = Color.FromHex("#5AD8FF");
    private Color _ringColor = Color.FromHex("#67CFEF");
    private Color _surfaceColor = Color.FromHex("#8CE7FF");

    public float Value
    {
        get => _value;
        set { _value = value; }
    }

    public float MaxValue
    {
        get => _maxValue;
        set { _maxValue = value; }
    }

    public Color FrameColor
    {
        get => _frameColor;
        set => _frameColor = value;
    }

    public Color BackplateColor
    {
        get => _backplateColor;
        set => _backplateColor = value;
    }

    public Color FillColor
    {
        get => _fillColor;
        set => _fillColor = value;
    }

    public Color RingColor
    {
        get => _ringColor;
        set => _ringColor = value;
    }

    public Color SurfaceColor
    {
        get => _surfaceColor;
        set => _surfaceColor = value;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var sizef = (Vector2)PixelSize;
        if (sizef.X <= 0f || sizef.Y <= 0f)
            return;

        var outerRadius = MathF.Min(sizef.X, sizef.Y) * 0.5f;
        var frameThickness = MathF.Max(3f, outerRadius * 0.15f);
        var innerRadius = MathF.Max(1f, outerRadius - frameThickness);
        var center = sizef * 0.5f;

        // Thick dark outer frame: filled disc at the outer radius.
        handle.DrawCircle(center, outerRadius, _frameColor, filled: true);

        // Subtle dark outer ring at the very edge of the frame so the silhouette
        // reads cleanly against any background.
        handle.DrawCircle(center, outerRadius, Color.Black, filled: false);

        // Inner well: dark interior showing through where the orb is empty.
        handle.DrawCircle(center, innerRadius, _backplateColor, filled: true);

        // Liquid fill: lower segment of the inner disc, bounded by a horizontal chord
        // at the surface line. The fan apex sits AT the right end of the chord and
        // walks the bottom arc to the left end. The implicit closing edge (last
        // vertex -> apex) is then the chord itself, giving a true horizontal surface
        // instead of two diagonal lines meeting at the center.
        var fillFrac = _maxValue > 0f ? Math.Clamp(_value / _maxValue, 0f, 1f) : 0f;
        if (fillFrac > 0f)
        {
            // y(theta) = cy - r * cos(theta); fill region is y >= cy + r * (1 - 2 * fillFrac).
            var k = 1f - 2f * fillFrac;
            var thetaFill = MathF.Acos(Math.Clamp(-k, -1f, 1f));

            // ILVerify (SS14 sandbox) rejects stackalloc Span<T> in content assemblies;
            // this is a heap array. 64+1 vectors per draw is negligible.
            const int segments = 64;
            var pts = new Vector2[segments + 1];
            for (var i = 0; i <= segments; i++)
            {
                var t = (float)i / segments;
                var theta = thetaFill + t * (MathHelper.TwoPi - 2f * thetaFill);
                pts[i] = new Vector2(
                    center.X + innerRadius * MathF.Sin(theta),
                    center.Y - innerRadius * MathF.Cos(theta));
            }

            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, pts, _fillColor);

            // Bright horizontal surface line where the liquid meets air. Skip when
            // the fill is at the very top or bottom (chord collapses to a point).
            if (fillFrac > 0.001f && fillFrac < 0.999f)
            {
                var surfaceY = center.Y + innerRadius * (1f - 2f * fillFrac);
                var halfChord = innerRadius * MathF.Sin(thetaFill);
                handle.DrawLine(
                    new Vector2(center.X - halfChord, surfaceY),
                    new Vector2(center.X + halfChord, surfaceY),
                    _surfaceColor);
            }
        }

        // Bright inner ring at the frame / well boundary, outlining the orb interior.
        handle.DrawCircle(center, innerRadius, _ringColor, filled: false);
    }
}
