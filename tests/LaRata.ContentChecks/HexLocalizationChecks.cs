using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

// Called with the existing fixture's real LocManager and native language tables.
// Explicit expected arguments are independent of HexTwistPower.Describe.
internal static class HexLocalizationChecks
{
    private static void Require(bool value, string why) { if (!value) throw new Exception(why); }
    internal static int Run(string language)
    {
        int formatted = NativeVarietyDescriptions(language);
        string Render(LocString text)
        {
            string value = text.GetFormattedText(); formatted++;
            Require(!string.IsNullOrWhiteSpace(value) && !value.Contains('{') && !value.Contains('}'), "Unresolved hex text: " + language + "/" + value);
            return value;
        }
        var withering = ModelDb.Power<WitheringPower>();
        Require(withering.Id.Entry == "LARATA-WITHERING_POWER", "Withering native model ID differs");
        Require(Render(withering.Title) == Render(new LocString("powers", "LARATA-WITHERING_POWER.title")) &&
            Render(withering.Description) == Render(new LocString("powers", "LARATA-WITHERING_POWER.description")),
            "Withering status text binding differs: " + language);
        Require(withering.CustomPackedIconPath == ModelDb.Power<MindRotPower>().PackedIconPath &&
            withering.CustomBigIconPath == withering.CustomPackedIconPath, "Withering native icon binding differs");
        Console.WriteLine($"WITHERING TEXT {language}: native ID, title/description and existing MindRot icon paths resolve. No UI rendering claim.");
        foreach (int amount in new[] { 10, 18, 26 })
        {
            var state = HexPresentationChecks.Setup(new(TwistKind.Sturdy, 0, amount), 1);
            var marker = ModelDb.Power<SturdyTwistPower>().ToMutable();
            marker.ApplyInternal(state.Enemy, amount, true);
            var text = marker.Description; text.Add("Amount", marker.Amount);
            string rendered = Render(text);
            Require(System.Text.RegularExpressions.Regex.IsMatch(rendered, @"\+\s*" + amount + @"\s*%"), "Sturdy actual percentage absent: " + rendered);
        }
        foreach (var kind in EncounterPlan.ActiveKinds.Where(EncounterPlan.IsHex).Append(TwistKind.Doomweaver))
        foreach (bool lesser in new[] { false, true })
        foreach (int variant in kind is TwistKind.Echoing or TwistKind.Hexed ? new[] { 1, 2, 3 } : new[] { 1 })
        {
            var twist = new EnemyTwist(kind, 0, variant, Lesser: lesser);
            var state = HexPresentationChecks.Setup(twist, 1);
            var marker = ModelDb.Power<HexTwistPower>().ToMutable();
            marker.ApplyInternal(state.Enemy, (int)kind + 1, true);
            string prefix = "LARATA-HEX_TWIST_POWER.";
            string key = prefix + kind.ToString().ToLowerInvariant();
            string unbound = Render(new LocString("powers", prefix + (kind == TwistKind.Echoing ? "echoing_unbound" : "unbound") + ".description"));
            Require(Render(marker.Description) == unbound, "Unbound marker claims an active move: " + kind);
            Require(Render(marker.Title) == Render(new LocString("powers", key + ".title")), "Wrong bound family title");

            state.Enemy.Monster!.SetUpForCombat();
            AccessTools.Method(typeof(HexTelegraphPatch), "Postfix").Invoke(null, [state.Enemy.Monster, state.Enemy]);
            var move = state.Enemy.Monster.MoveStateMachine!.States.Values.OfType<MoveState>()
                .Single(m => HexTelegraphPatch.Hexes.TryGetValue(m, out _));
            int dose = kind == TwistKind.Ominous ? (lesser ? 2 : 4) : lesser ? 1 : 2;
            PowerModel debuff = variant switch { 2 => ModelDb.Power<FrailPower>(), 3 => ModelDb.Power<VulnerablePower>(), _ => ModelDb.Power<WeakPower>() };
            var expected = new LocString("powers", key + ".description");
            expected.Add("Dose", dose); expected.Add("Debuff", Render(debuff.Title));
            string wanted = Render(expected);
            int hp = state.Enemy.MaxHp;
            Require(Render(marker.Description) == wanted && Render(marker.Description) == wanted, "Rule variant/Lesser text differs: " + kind);
            var intent = move.Intents.Single(i => i is HexDebuffIntent or HexStatusIntent or HexBuffIntent);
            var intentText = (LocString)AccessTools.Method(intent.GetType(), "GetIntentDescription").Invoke(intent, [state.Targets, state.Enemy])!;
            Require(Render(intentText) == wanted, "Native intent and rule description disagree: " + kind);
            marker.RemoveInternal();
            var copy = ModelDb.Power<HexTwistPower>().ToMutable();
            copy.ApplyInternal(state.Enemy, (int)kind + 1, true);
            Require(Render(copy.Description) == wanted && state.Enemy.MaxHp == hp && state.Enemy.Powers.OfType<HexTwistPower>().Count() == 1,
                "Read/reconstructed marker changed body or description");
            Require(copy.DisplayAmount == 0 && !copy.ShouldScaleInMultiplayer, "Hex identity displayed as dose/duration");
        }
        Console.WriteLine($"HEX TEXT {language}: {EncounterPlan.ActiveKinds.Count(EncounterPlan.IsHex)} active families + legacy Doomweaver, native debuff variants, Lesser, bound/unbound/Echoing, native intent descriptions, reconstructed marker, Sturdy 10/18/26 percent. Formatter only; no NPower label/layout or SVG import.");
        return formatted;
    }

    private static int NativeVarietyDescriptions(string language)
    {
        var harmony = new Harmony("LaRata.ContentChecks.VarietyDescriptions");
        var getters = new[] { nameof(PowerModel.Description), nameof(PowerModel.SmartDescription), nameof(PowerModel.RemoteDescription) }
            .Select(name => AccessTools.PropertyGetter(typeof(PowerModel), name)).ToArray();
        int formatted = 0;
        try
        {
            harmony.CreateClassProcessor(typeof(VarietyPowerDescriptionPatch)).Patch();
            foreach (var getter in getters)
                Require(Harmony.GetPatchInfo(getter)?.Postfixes.Any(p => p.owner == harmony.Id && p.PatchMethod.DeclaringType == typeof(VarietyPowerDescriptionPatch)) == true,
                    "Variety description getter not patched: " + getter.Name);
            foreach (var (kind, canonical, amount) in new (TwistKind, PowerModel, int)[] {
                (TwistKind.Sheltered, ModelDb.Power<HardToKillPower>(), 27),
                (TwistKind.Reactive, ModelDb.Power<SneakyPower>(), 5),
                (TwistKind.Drifting, ModelDb.Power<SoarPower>(), 1) })
            {
                var state = HexPresentationChecks.Setup(new(kind, 0, 1), 1);
                var control = HexPresentationChecks.Setup(new(TwistKind.Sturdy, 0, 10), 1);
                var power = canonical.ToMutable(); power.ApplyInternal(state.Enemy, amount, true);
                var unrelated = canonical.ToMutable(); unrelated.ApplyInternal(control.Enemy, amount, true);
                string key = "LARATA-TWIST_VARIETY." + kind.ToString().ToLowerInvariant() + ".description";
                foreach (var getter in getters)
                {
                    var text = (LocString)getter.Invoke(power, null)!;
                    var expected = new LocString("powers", key); expected.Add("Amount", amount);
                    string rendered = text.GetFormattedText(); formatted++;
                    Require(text.LocEntryKey == key && rendered == expected.GetFormattedText() && !rendered.Contains('{'),
                        "Contextual native description differs: " + language + "/" + kind + "/" + getter.Name);
                    foreach (var native in new[] { canonical, unrelated })
                        Require(!((LocString)getter.Invoke(native, null)!).LocEntryKey.StartsWith("LARATA-TWIST_VARIETY."),
                            "Variety description leaked outside its twist context");
                }
                Require(power.Amount == amount && unrelated.Amount == amount, "Description getter mutated native power");
            }
            Console.WriteLine($"VARIETY TEXT {language}: 3 native description getter bindings; 9 contextual formats, canonical/unrelated controls, retained amounts 27/5/1. No rendered tooltip claim.");
        }
        finally { harmony.UnpatchAll(harmony.Id); }
        return formatted;
    }
}
