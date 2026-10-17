using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace LaRata;

[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
    public const string Id = "LaRata";
    public static readonly Logger Log = new(Id, LogType.Generic);
    internal static bool AuditChecksums { get; private set; }

    public static void Initialize()
    {
        // Early Access changes private hooks frequently. Never silently run an unvalidated ruleset.
        if (ReleaseInfoManager.Instance.ReleaseInfo?.Version != "v0.107.1")
            throw new NotSupportedException("La Rata Remix 0.6.0 requires Slay the Spire 2 v0.107.1.");

        var harmony = new Harmony(Id);
        AuditChecksums = CommandLineHelper.HasArg("larata-audit");
        try
        {
            Endgame.Register();
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
        catch
        {
            harmony.UnpatchAll(Id);
            throw;
        }
        Log.Info("Loaded 0.6.0: party-sized chaos; 31 twist families; distributed effects and up to two reinforcements; animated goblin and Cleanse.");
    }
}
