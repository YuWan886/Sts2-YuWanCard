using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace YuWanCard.Core.HealthBar;

[HarmonyPatch]
public static class HealthBarOverlayPatch
{
    private static readonly SpireField<NHealthBar, OverlayUiState> UiStates = new(() => null);

    // ── cached field accessors ──────────────────────────────────────
    private static readonly AccessTools.FieldRef<NHealthBar, Creature> CreatureRef =
        AccessTools.FieldRefAccess<NHealthBar, Creature>("_creature");

    private static readonly AccessTools.FieldRef<NHealthBar, Control> HpForegroundRef =
        AccessTools.FieldRefAccess<NHealthBar, Control>("_hpForeground");

    private static readonly AccessTools.FieldRef<NHealthBar, Control> HpForegroundContainerRef =
        AccessTools.FieldRefAccess<NHealthBar, Control>("_hpForegroundContainer");

    private static readonly AccessTools.FieldRef<NHealthBar, Control> PoisonForegroundRef =
        AccessTools.FieldRefAccess<NHealthBar, Control>("_poisonForeground");

    private static readonly AccessTools.FieldRef<NHealthBar, Control> DoomForegroundRef =
        AccessTools.FieldRefAccess<NHealthBar, Control>("_doomForeground");

    private static readonly AccessTools.FieldRef<NHealthBar, Control> HpMiddlegroundRef =
        AccessTools.FieldRefAccess<NHealthBar, Control>("_hpMiddleground");

    private static readonly AccessTools.FieldRef<NHealthBar, MegaLabel> HpLabelRef =
        AccessTools.FieldRefAccess<NHealthBar, MegaLabel>("_hpLabel");

    private static readonly AccessTools.FieldRef<NHealthBar, float> ExpectedMaxFgWidthRef =
        AccessTools.FieldRefAccess<NHealthBar, float>("_expectedMaxFgWidth");

    private static readonly AccessTools.FieldRef<NHealthBar, Tween?> MiddlegroundTweenRef =
        AccessTools.FieldRefAccess<NHealthBar, Tween?>("_middlegroundTween");

    // ── foreground ──────────────────────────────────────────────────

    [HarmonyPatch(typeof(NHealthBar), "RefreshForeground")]
    [HarmonyPostfix]
    private static void RefreshForegroundPostfix(NHealthBar __instance)
    {
        var creature = CreatureRef(__instance);
        if (creature.CurrentHp <= 0)
        {
            HideAllOverlays(__instance);
            return;
        }

        var segments = CollectSegments(creature);
        if (segments.Count == 0)
        {
            HideAllOverlays(__instance);
            return;
        }

        if (!EnsureUiState(__instance))
            return;

        var state = UiStates[__instance];
        if (state == null)
            return;

        RenderOverlays(__instance, creature, segments, state);
    }

    private static void RenderOverlays(
        NHealthBar bar, Creature creature,
        List<HealthBarOverlaySegment> allSegments, OverlayUiState state)
    {
        var maxWidth = GetMaxFgWidth(bar);
        var hpForeground = HpForegroundRef(bar);
        var baseHp = HpFromOffsetRight(bar, hpForeground.OffsetRight, creature);

        // ── FromRight (poison-style) ──────────────────────────────
        var rightSegments = allSegments
            .Where(s => s.Direction == HealthBarOverlayDirection.FromRight)
            .OrderBy(s => s.Order)
            .ToList();

        var remainingHp = Math.Max(0, baseHp);
        float? lethalEdge = null;
        Color? lethalRightColor = null;

        for (int i = 0; i < rightSegments.Count; i++)
        {
            if (remainingHp <= 0) break;

            var segment = rightSegments[i];
            var visibleAmount = Math.Min(segment.Amount, remainingHp);
            if (visibleAmount <= 0) continue;

            EnsureNodeCount(state.RightNodes, state.RightContainer, i + 1, state.RightTemplate);
            var node = state.RightNodes[i];
            var previousHp = remainingHp;
            remainingHp -= visibleAmount;

            var leftWidth = GetFgWidth(bar, remainingHp, creature);
            var rightWidth = GetFgWidth(bar, previousHp, creature);
            node.Visible = true;
            node.SelfModulate = segment.Color;
            node.OffsetLeft = remainingHp > 0 ? Math.Max(0f, leftWidth - node.PatchMarginLeft) : 0f;
            node.OffsetRight = rightWidth - maxWidth;

            if (remainingHp <= 0)
            {
                lethalEdge = node.OffsetRight;
                lethalRightColor = segment.Color;
            }
        }

        HideNodes(state.RightNodes, rightSegments.Count);

        if (rightSegments.Count > 0)
        {
            if (remainingHp > 0)
            {
                hpForeground.Visible = true;
                hpForeground.OffsetRight = GetFgWidth(bar, remainingHp, creature) - maxWidth;
            }
            else
            {
                hpForeground.Visible = false;
            }
        }

        // ── FromLeft (doom-style) ─────────────────────────────────
        var leftSegments = allSegments
            .Where(s => s.Direction == HealthBarOverlayDirection.FromLeft)
            .OrderBy(s => s.Order)
            .ToList();

        var leftAccumulated = 0;

        for (int i = 0; i < leftSegments.Count; i++)
        {
            if (leftAccumulated >= remainingHp) break;

            var segment = leftSegments[i];
            var segmentStart = leftAccumulated;
            leftAccumulated = Math.Min(remainingHp, leftAccumulated + segment.Amount);
            if (leftAccumulated <= segmentStart) continue;

            EnsureNodeCount(state.LeftNodes, state.LeftContainer, i + 1, state.LeftTemplate);
            var node = state.LeftNodes[i];
            var startWidth = GetFgWidth(bar, segmentStart, creature);
            var endWidth = GetFgWidth(bar, leftAccumulated, creature);

            node.Visible = true;
            node.SelfModulate = segment.Color;
            node.OffsetLeft = segmentStart > 0 ? Math.Max(0f, startWidth - node.PatchMarginLeft) : 0f;
            var offsetRight = Math.Min(0f, endWidth - maxWidth + node.PatchMarginRight);
            if (lethalEdge.HasValue)
                offsetRight = Math.Min(offsetRight, lethalEdge.Value);
            node.OffsetRight = offsetRight;
        }

        HideNodes(state.LeftNodes, leftSegments.Count);

        // ── lethal color resolution ───────────────────────────────
        var lethalLeftColor = ResolveLeftLethalColor(remainingHp, leftSegments);

        state.LastLethalRightColor = lethalRightColor;
        state.LastLethalLeftColor = lethalLeftColor;
        state.LastRightOverlayEdge = lethalEdge;
        state.HasRightOverlay = rightSegments.Count > 0;
    }

    // ── middleground ───────────────────────────────────────────────

    [HarmonyPatch(typeof(NHealthBar), "RefreshMiddleground")]
    [HarmonyPostfix]
    private static void RefreshMiddlegroundPostfix(NHealthBar __instance)
    {
        var state = UiStates[__instance];
        if (state == null || !state.HasRightOverlay)
            return;

        var creature = CreatureRef(__instance);
        if (creature.CurrentHp <= 0)
            return;

        var hpMiddleground = HpMiddlegroundRef(__instance);
        var targetOffsetRight = state.LastRightOverlayEdge
                                ?? HpForegroundRef(__instance).OffsetRight;

        var shouldAnimateImmediately = targetOffsetRight >= hpMiddleground.OffsetRight;
        hpMiddleground.OffsetRight += 1f;

        var oldTween = MiddlegroundTweenRef(__instance);
        oldTween?.Kill();

        var tween = __instance.CreateTween();
        tween.TweenProperty(hpMiddleground, "offset_right", targetOffsetRight - 2f, 1.0)
            .SetDelay(shouldAnimateImmediately ? 0.0 : 1.0)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
        MiddlegroundTweenRef(__instance) = tween;
    }

    // ── text ───────────────────────────────────────────────────────

    [HarmonyPatch(typeof(NHealthBar), "RefreshText")]
    [HarmonyPostfix]
    private static void RefreshTextPostfix(NHealthBar __instance)
    {
        var state = UiStates[__instance];
        if (state == null)
            return;

        var creature = CreatureRef(__instance);
        if (creature.CurrentHp <= 0)
            return;

        var lethalColor = state.LastLethalRightColor ?? state.LastLethalLeftColor;
        if (!lethalColor.HasValue)
            return;

        var hpLabel = HpLabelRef(__instance);
        hpLabel.AddThemeColorOverride("font_color", lethalColor.Value);
        hpLabel.AddThemeColorOverride("font_outline_color", DarkenForOutline(lethalColor.Value));
    }

    // ── helpers ────────────────────────────────────────────────────

    private static List<HealthBarOverlaySegment> CollectSegments(Creature creature)
    {
        var segments = new List<HealthBarOverlaySegment>();
        var context = new HealthBarOverlayContext(creature);

        foreach (var source in creature.Powers.OfType<IHealthBarOverlaySource>())
        {
            try
            {
                foreach (var seg in source.GetHealthBarOverlaySegments(context))
                {
                    if (seg.Amount > 0)
                        segments.Add(seg);
                }
            }
            catch
            {
                // Silently skip broken sources
            }
        }

        return segments;
    }

    private static bool EnsureUiState(NHealthBar bar)
    {
        if (UiStates[bar] != null)
            return true;

        var poisonForeground = PoisonForegroundRef(bar);
        var doomForeground = DoomForegroundRef(bar);

        if (poisonForeground is not NinePatchRect poisonTemplate)
            return false;
        if (doomForeground is not NinePatchRect doomTemplate)
            return false;
        if (poisonForeground.GetParent() is not Control mask)
            return false;

        var rightContainer = new Control
        {
            Name = "YuWanOverlayRightContainer",
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        rightContainer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var leftContainer = new Control
        {
            Name = "YuWanOverlayLeftContainer",
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        leftContainer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        mask.AddChild(rightContainer);
        mask.AddChild(leftContainer);

        var rightTemplate = (NinePatchRect)poisonTemplate.Duplicate();
        rightTemplate.Name = "YuWanOverlayRightTemplate";
        rightTemplate.Visible = false;
        rightTemplate.SelfModulate = Colors.White;
        rightTemplate.Material = null;

        var leftTemplate = (NinePatchRect)doomTemplate.Duplicate();
        leftTemplate.Name = "YuWanOverlayLeftTemplate";
        leftTemplate.Visible = false;
        leftTemplate.SelfModulate = Colors.White;
        leftTemplate.Material = null;

        UiStates[bar] = new OverlayUiState(rightContainer, leftContainer, rightTemplate, leftTemplate);
        return true;
    }

    private static void EnsureNodeCount(
        List<NinePatchRect> nodes, Control container, int needed, NinePatchRect template)
    {
        while (nodes.Count < needed)
        {
            var node = (NinePatchRect)template.Duplicate();
            node.Name = $"YuWanOverlaySeg{nodes.Count}";
            node.Visible = false;
            container.AddChild(node);
            nodes.Add(node);
        }
    }

    private static void HideNodes(List<NinePatchRect> nodes, int startIndex = 0)
    {
        for (int i = startIndex; i < nodes.Count; i++)
        {
            nodes[i].Visible = false;
            nodes[i].Material = null;
            nodes[i].SelfModulate = Colors.White;
        }
    }

    private static void HideAllOverlays(NHealthBar bar)
    {
        var state = UiStates[bar];
        if (state == null) return;
        HideNodes(state.RightNodes);
        HideNodes(state.LeftNodes);
        state.LastLethalRightColor = null;
        state.LastLethalLeftColor = null;
        state.HasRightOverlay = false;
    }

    private static float GetMaxFgWidth(NHealthBar bar)
    {
        var expected = ExpectedMaxFgWidthRef(bar);
        return expected > 0f ? expected : HpForegroundContainerRef(bar).Size.X;
    }

    private static float GetFgWidth(NHealthBar bar, int amount, Creature creature)
    {
        if (creature.MaxHp <= 0 || amount <= 0) return 0f;
        var width = (float)amount / creature.MaxHp * GetMaxFgWidth(bar);
        return Math.Max(width, creature.CurrentHp > 0 ? 12f : 0f);
    }

    private static int HpFromOffsetRight(NHealthBar bar, float offsetRight, Creature creature)
    {
        var maxWidth = GetMaxFgWidth(bar);
        if (maxWidth <= 0f || creature.MaxHp <= 0) return 0;
        var width = Math.Clamp(offsetRight + maxWidth, 0f, maxWidth);
        return (int)Math.Round(width / maxWidth * creature.MaxHp);
    }

    private static Color DarkenForOutline(Color color)
    {
        return new Color(
            Math.Clamp(color.R * 0.3f, 0f, 1f),
            Math.Clamp(color.G * 0.3f, 0f, 1f),
            Math.Clamp(color.B * 0.3f, 0f, 1f));
    }

    private static Color? ResolveLeftLethalColor(
        int remainingHp, List<HealthBarOverlaySegment> leftSegments)
    {
        if (remainingHp <= 0) return null;

        var accumulated = 0;
        foreach (var seg in leftSegments.OrderBy(s => s.Order))
        {
            accumulated = Math.Min(remainingHp, accumulated + seg.Amount);
            if (accumulated >= remainingHp)
                return seg.Color;
        }

        return null;
    }

    // ── UI state ───────────────────────────────────────────────────

    private sealed class OverlayUiState(
        Control rightContainer,
        Control leftContainer,
        NinePatchRect rightTemplate,
        NinePatchRect leftTemplate)
    {
        public Control RightContainer = rightContainer;
        public Control LeftContainer = leftContainer;
        public NinePatchRect RightTemplate = rightTemplate;
        public NinePatchRect LeftTemplate = leftTemplate;
        public List<NinePatchRect> RightNodes = [];
        public List<NinePatchRect> LeftNodes = [];
        public Color? LastLethalRightColor;
        public Color? LastLethalLeftColor;
        public float? LastRightOverlayEdge;
        public bool HasRightOverlay;
    }
}
