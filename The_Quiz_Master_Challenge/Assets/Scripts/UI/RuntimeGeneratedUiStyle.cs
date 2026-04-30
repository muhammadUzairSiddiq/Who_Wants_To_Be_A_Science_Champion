using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Premium panel/button sprites for runtime-built overlays. Loads from Resources/QuizMasterRuntimeUi (copied from Assets/UI).
/// </summary>
public static class RuntimeGeneratedUiStyle
{
    const string ResourcesPanelPath = "QuizMasterRuntimeUi/Panel 1";
    const string ResourcesButtonPath = "QuizMasterRuntimeUi/BUTTON";

    static Sprite _panel;
    static Sprite _button;
    static Sprite _white;

    public static bool TryResolvePremiumSprites()
    {
        if (_panel == null)
            _panel = Resources.Load<Sprite>(ResourcesPanelPath);
        if (_button == null)
            _button = Resources.Load<Sprite>(ResourcesButtonPath);
        return _panel != null && _button != null;
    }

    public static bool UsePremiumChrome() => TryResolvePremiumSprites();

    public static Sprite WhiteFallbackSprite()
    {
        if (_white == null)
            _white = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
        return _white;
    }

    static bool SpriteHasNineSlice(Sprite sp)
    {
        if (sp == null) return false;
        var b = sp.border;
        return (b.x + b.y + b.z + b.w) > 0.25f;
    }

    public static void ApplyPanel(Image img)
    {
        TryResolvePremiumSprites();
        if (_panel != null)
        {
            img.sprite = _panel;
            img.color = Color.white;
            img.preserveAspect = false;
            img.type = SpriteHasNineSlice(_panel) ? Image.Type.Sliced : Image.Type.Simple;
        }
        else
        {
            img.sprite = WhiteFallbackSprite();
            img.color = new Color(0.14f, 0.08f, 0.24f, 1f);
            img.type = Image.Type.Simple;
        }
    }

    public static void ApplyButton(Image img)
    {
        TryResolvePremiumSprites();
        if (_button != null)
        {
            img.sprite = _button;
            img.color = Color.white;
            img.preserveAspect = false;
            img.type = SpriteHasNineSlice(_button) ? Image.Type.Sliced : Image.Type.Simple;
        }
        else
        {
            img.sprite = WhiteFallbackSprite();
            img.color = new Color(0.42f, 0.28f, 0.62f, 1f);
            img.type = Image.Type.Simple;
        }
    }
}
