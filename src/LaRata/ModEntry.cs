using System.Reflection;
using BaseLib.Patches.Content;
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

    internal static void ApplyPatches(Harmony harmony)
    {
        harmony.PatchAllUncategorized(Assembly.GetExecutingAssembly());
        harmony.PatchCategory(Assembly.GetExecutingAssembly(), ResetEnergyLedger.PatchCategory);
        // RelicMetrics.PatchCategory deliberately remains inactive, including UI
        // and deferred/payment observers. Gameplay identities are uncategorized.
    }

    public static void Initialize()
    {
        // Early Access changes private hooks frequently. Never silently run an unvalidated ruleset.
        if (ReleaseInfoManager.Instance.ReleaseInfo?.Version != "v0.107.1")
            throw new NotSupportedException("La Rata Remix 0.9.4 requires Slay the Spire 2 v0.107.1.");

        var harmony = new Harmony(Id);
        AuditChecksums = CommandLineHelper.HasArg("larata-audit");
        try
        {
            UiText.Register();
            Endgame.Register();
            CombatJournal.Register();
            RelicMetrics.Register();
            ComboTurnPower.Register();
            CustomContentDictionary.RegisterType(typeof(RoyalSecondShadowPower));
            CustomContentDictionary.RegisterType(typeof(RoyalSmugglersDividendPower));
            CustomContentDictionary.RegisterType(typeof(RoyalCourtOfCindersPower));
            CustomContentDictionary.RegisterType(typeof(SturdyTwistPower));
            CustomContentDictionary.RegisterType(typeof(KeenTwistPower));
            CustomContentDictionary.RegisterType(typeof(EmpoweredTwistPower));
            CustomContentDictionary.RegisterType(typeof(ReinforcementTwistPower));
            CustomContentDictionary.RegisterType(typeof(HexTwistPower));
            ApplyPatches(harmony);
        }
        catch
        {
            harmony.UnpatchAll(Id);
            throw;
        }
        Log.Info("Loaded La Rata Remix 0.9.4.");
    }
}
