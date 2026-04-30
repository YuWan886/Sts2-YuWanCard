using YuWanCard.Core.Interop;

namespace YuWanCard.Config.Interop;

[ModInterop("BaseLib")]
public static class BaseLibConfigInterop
{
    [InteropTarget("BaseLib.Config.ModConfigRegistry", "Register")]
    public static void Register(string modId, object config)
    {
        // Fallback: BaseLib not loaded
    }
}
