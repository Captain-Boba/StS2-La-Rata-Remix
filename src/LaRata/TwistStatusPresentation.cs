using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.addons.mega_text;

namespace LaRata;

// Presentation only: preserve the real Amount, native counter, color and layout.
[HarmonyPatch(typeof(NPower), "RefreshAmount")]
internal static class SturdyTwistAmountPresentation
{
    [HarmonyPostfix]
    private static void Postfix(PowerModel? ____model, MegaLabel ____amountLabel)
    {
        if (____model is SturdyTwistPower sturdy)
            ____amountLabel.SetTextAutoSize($"+{sturdy.Amount}%");
    }
}
