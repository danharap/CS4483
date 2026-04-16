using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Shared palette and UI helpers for The Pit main menu, account gate, and related overlays.
/// </summary>
public static class PitMenuBranding
{
    public const string GameTitle = "THE PIT";
    public const string GameSubtitle = "Underground arena survival";
    public const string VersionFooter = "CS4483 · Group 21 · Karmali · Harapiak · Yuan · Goodman";
}

public static class PitMenuUiTheme
{
    public static readonly Color VoidBlack = new Color(0.03f, 0.032f, 0.04f, 1f);
    public static readonly Color OverlayDim = new Color(0f, 0f, 0f, 0.78f);
    public static readonly Color PanelBase = new Color(0.07f, 0.075f, 0.09f, 0.97f);
    public static readonly Color PanelDeep = new Color(0.055f, 0.058f, 0.07f, 0.98f);
    public static readonly Color PanelRim = new Color(0.55f, 0.42f, 0.18f, 0.45f);
    public static readonly Color GoldAccent = new Color(0.9f, 0.72f, 0.32f, 1f);
    public static readonly Color GoldSoft = new Color(0.75f, 0.62f, 0.38f, 1f);
    public static readonly Color Steel = new Color(0.32f, 0.45f, 0.58f, 1f);
    public static readonly Color SteelDeep = new Color(0.18f, 0.26f, 0.36f, 1f);
    public static readonly Color MutedGreen = new Color(0.22f, 0.48f, 0.34f, 1f);
    public static readonly Color MutedGreenHi = new Color(0.28f, 0.58f, 0.4f, 1f);
    public static readonly Color TextPrimary = new Color(0.93f, 0.94f, 0.96f, 1f);
    public static readonly Color TextMuted = new Color(0.58f, 0.62f, 0.7f, 1f);
    public static readonly Color TextError = new Color(0.95f, 0.4f, 0.36f, 1f);
    public static readonly Color TextSuccess = new Color(0.42f, 0.78f, 0.55f, 1f);
    public static readonly Color Danger = new Color(0.55f, 0.16f, 0.18f, 1f);
    public static readonly Color DangerHi = new Color(0.72f, 0.22f, 0.24f, 1f);
    public static readonly Color SlotRow = new Color(0.1f, 0.11f, 0.14f, 0.95f);
    public static readonly Color SlotRowHover = new Color(0.14f, 0.16f, 0.2f, 0.98f);
    public static readonly Color InputBg = new Color(0.09f, 0.1f, 0.13f, 1f);
    public static readonly Color InputBorder = new Color(0.25f, 0.3f, 0.38f, 0.85f);

    public const float ButtonFadeDuration = 0.12f;

    public static void WireMenuButton(Button b, UnityEngine.Events.UnityAction action)
    {
        if (b == null || action == null) return;
        b.onClick.AddListener(GameAudio.PlayButtonClick);
        b.onClick.AddListener(action);
        var et = b.gameObject.GetComponent<EventTrigger>();
        if (et == null) et = b.gameObject.AddComponent<EventTrigger>();
        var hover = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        hover.callback.AddListener(_ => GameAudio.PlayButtonHover());
        et.triggers.Add(hover);
    }

    public static ColorBlock ColorBlockFor(Color normal, Color highlighted, Color pressed, Color disabled)
    {
        var cb = ColorBlock.defaultColorBlock;
        cb.normalColor = normal;
        cb.highlightedColor = highlighted;
        cb.pressedColor = pressed;
        cb.disabledColor = disabled;
        cb.colorMultiplier = 1f;
        cb.fadeDuration = ButtonFadeDuration;
        return cb;
    }

    /// <summary>Muted green — primary run / enter actions.</summary>
    public static void ApplyPrimaryRunButton(Button b, Image target)
    {
        if (b == null || target == null) return;
        b.transition = Selectable.Transition.ColorTint;
        target.color = MutedGreen;
        b.colors = ColorBlockFor(
            MutedGreen,
            MutedGreenHi,
            new Color(MutedGreen.r * 0.75f, MutedGreen.g * 0.75f, MutedGreen.b * 0.75f),
            new Color(MutedGreen.r, MutedGreen.g, MutedGreen.b, 0.35f));
        b.navigation = new Navigation { mode = Navigation.Mode.Automatic };
    }

    /// <summary>Steel — secondary / load.</summary>
    public static void ApplySecondarySteelButton(Button b, Image target)
    {
        if (b == null || target == null) return;
        b.transition = Selectable.Transition.ColorTint;
        target.color = SteelDeep;
        b.colors = ColorBlockFor(
            SteelDeep,
            Steel,
            new Color(SteelDeep.r * 0.7f, SteelDeep.g * 0.7f, SteelDeep.b * 0.7f),
            new Color(SteelDeep.r, SteelDeep.g, SteelDeep.b, 0.35f));
        b.navigation = new Navigation { mode = Navigation.Mode.Automatic };
    }

    public static void ApplyNeutralPanelButton(Button b, Image target)
    {
        if (b == null || target == null) return;
        b.transition = Selectable.Transition.ColorTint;
        var n = new Color(0.16f, 0.17f, 0.22f, 1f);
        var h = new Color(0.22f, 0.24f, 0.3f, 1f);
        target.color = n;
        b.colors = ColorBlockFor(n, h, new Color(n.r * 0.85f, n.g * 0.85f, n.b * 0.85f), new Color(n.r, n.g, n.b, 0.35f));
    }

    public static void ApplyGoldGhostButton(Button b, Image target)
    {
        if (b == null || target == null) return;
        b.transition = Selectable.Transition.ColorTint;
        var n = new Color(GoldAccent.r, GoldAccent.g, GoldAccent.b, 0.12f);
        var h = new Color(GoldAccent.r, GoldAccent.g, GoldAccent.b, 0.22f);
        target.color = n;
        b.colors = ColorBlockFor(n, h, new Color(GoldAccent.r * 0.5f, GoldAccent.g * 0.5f, GoldAccent.b * 0.5f, 0.2f), new Color(1f, 1f, 1f, 0.08f));
    }

    public static void ApplyDangerButton(Button b, Image target)
    {
        if (b == null || target == null) return;
        b.transition = Selectable.Transition.ColorTint;
        target.color = Danger;
        b.colors = ColorBlockFor(Danger, DangerHi, new Color(0.4f, 0.1f, 0.12f), new Color(Danger.r, Danger.g, Danger.b, 0.3f));
    }

    public static void StyleSectionHeader(TMP_Text t)
    {
        if (t == null) return;
        t.fontStyle = FontStyles.Bold;
        t.fontSize = 15f;
        t.color = GoldSoft;
        t.alignment = TextAlignmentOptions.Left;
        t.characterSpacing = 1.2f;
    }

    public static void StyleBody(TMP_Text t, float size = 14f)
    {
        if (t == null) return;
        t.fontSize = size;
        t.color = TextPrimary;
        t.alignment = TextAlignmentOptions.Left;
    }

    public static void StyleInputField(TMP_InputField field)
    {
        if (field == null) return;
        var g = field.gameObject.GetComponent<Image>();
        if (g != null)
        {
            g.color = InputBg;
            g.raycastTarget = true;
        }
        if (field.textComponent != null)
        {
            field.textComponent.color = TextPrimary;
            field.textComponent.fontSize = 15f;
        }
        if (field.placeholder is Graphic ph) ph.color = new Color(TextMuted.r, TextMuted.g, TextMuted.b, 0.45f);
    }
}
