using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client.MainMenu.UI;

/// <summary>
/// Draws a texture with cover scaling and a gentle time-based pan.
/// </summary>
public sealed class AnimatedCoverBackgroundTextureRect : TextureRect
{
    private const float HorizontalPanPeriod = 18f;
    private const float VerticalPanPeriod = 24f;
    private const float HorizontalPanAmplitude = 0.3f;
    private const float VerticalPanAmplitude = 0.22f;

    private float _animationTime;

    public float AnimationTime
    {
        get => _animationTime;
        set
        {
            if (MathHelper.CloseToPercent(_animationTime, value))
                return;

            _animationTime = value;
            InvalidateArrange();
        }
    }

    public AnimatedCoverBackgroundTextureRect()
    {
        RectClipContent = true;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var texture = Texture;
        if (texture is null || PixelSize.X <= 0f || PixelSize.Y <= 0f)
            return;

        var imageSize = TextureSizeTarget;
        if (imageSize.X <= 0f || imageSize.Y <= 0f)
            return;

        var scale = MathF.Max(PixelSize.X / imageSize.X, PixelSize.Y / imageSize.Y);
        var drawSize = imageSize * scale;
        var centeredPosition = (PixelSize - drawSize) / 2f;
        var overflow = Vector2.Max(drawSize - PixelSize, Vector2.Zero);

        var panOffset = new Vector2(
            MathF.Sin(AnimationTime * MathF.Tau / HorizontalPanPeriod) * overflow.X * HorizontalPanAmplitude,
            MathF.Sin(AnimationTime * MathF.Tau / VerticalPanPeriod + 0.9f) * overflow.Y * VerticalPanAmplitude);

        handle.DrawTextureRect(texture, UIBox2.FromDimensions(centeredPosition + panOffset, drawSize));
    }
}
