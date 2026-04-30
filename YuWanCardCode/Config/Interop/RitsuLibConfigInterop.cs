using YuWanCard.Core.Interop;

namespace YuWanCard.Config.Interop;

[ModInterop("STS2RitsuLib")]
public static class RitsuLibConfigInterop
{
    [InteropTarget("STS2RitsuLib.RitsuLibFramework", "RegisterModSettingsReflectionProviderAndTryRegister")]
    public static int? RegisterModSettings(Type type)
    {
        return null;
    }

    [InteropTarget("STS2RitsuLib.RitsuLibFramework", "GetDataStore")]
    public static object? GetDataStore(string modId)
    {
        return null;
    }
}
