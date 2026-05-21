using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace YuWanCard.Core.Utils;

internal static class CardPlayUiFocus
{
    internal static void AfterCardPlayFinished()
    {
        NCombatRoom.Instance?.Ui.Hand.DefaultFocusedControl.TryGrabFocus();
    }
}
