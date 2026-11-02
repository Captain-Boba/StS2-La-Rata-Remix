using System.Runtime.CompilerServices;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Sync;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace LaRata;

// A receipt carried by the ORIGINAL native purchased-card packet. No new message,
// purchase flow, shop RNG, payment formula or local-only relic counter is used.
internal static class SootboundMerchantPurchase
{
    private sealed record Purchase(Player Owner, MerchantCardEntry Entry, CardModel Card,
        SootboundTicket Ticket, string Merchant, string PreviousReceipt)
    { internal bool Settled; }
    private static readonly ConditionalWeakTable<Player, Purchase> Pending = new();
    private static readonly SavedSpireField<CardModel, string> Receipt = new(() => "", "LaRataSootboundMerchantV1");

    internal static string MerchantKey(Player player) =>
        // Unlike transient room IDs, native history is persisted and includes
        // every merchant subroom at this map point, not only the floor number.
        $"{player.RunState.RunLocation.mapLocation}|shop:{player.RunState.CurrentMapPointHistoryEntry?.Rooms.Count(r => r.RoomType == RoomType.Shop) ?? 0}";

    internal static bool IsFree(SootboundTicket ticket, MerchantEntry entry)
    {
        if (ticket.IsMelted || ticket.HasBeenRemovedFromState || entry is not MerchantCardEntry ||
            ticket.Owner.RunState.CurrentRoom is not MerchantRoom) return false;
        if (Pending.TryGetValue(ticket.Owner, out var pending))
            return !pending.Settled && ReferenceEquals(pending.Entry, entry);
        return ticket.SpentMerchant != MerchantKey(ticket.Owner);
    }

    [HarmonyPatch(typeof(MerchantEntry), nameof(MerchantEntry.OnTryPurchaseWrapper))]
    private static class NativePurchase
    {
        // Register the existing BaseLib saved-card field during mod patch setup,
        // before BaseLib finalizes its save/packet property cache.
        private static bool Prepare() { _ = Receipt; return true; }
        private static bool Prefix(MerchantEntry __instance, Player ____player,
            ref Task<bool> __result, out Purchase? __state)
        {
            __state = null;
            if (__instance is not MerchantCardEntry entry) return true;
            if (Pending.TryGetValue(____player, out _))
            { __result = Task.FromResult(false); return false; }
            var ticket = ____player.Relics.OfType<SootboundTicket>().FirstOrDefault(r => IsFree(r, entry));
            if (ticket == null || !entry.IsStocked || entry.CreationResult?.Card is not { } card) return true;
            __state = new(____player, entry, card, ticket, MerchantKey(____player), Receipt.Get(card) ?? "");
            Pending.Add(____player, __state);
            Receipt.Set(card, __state.Merchant);
            return true;
        }
        private static void Postfix(ref Task<bool> __result, Purchase? __state)
        { if (__state != null) __result = Complete(__result, __state); }
        private static async Task<bool> Complete(Task<bool> purchase, Purchase pending)
        {
            try
            {
                return await purchase;
            }
            finally
            {
                if (!pending.Settled) Receipt.Set(pending.Card, pending.PreviousReceipt);
                Pending.Remove(pending.Owner);
            }
        }
        private static Exception? Finalizer(Exception? __exception, Purchase? __state)
        {
            if (__exception != null && __state != null && !__state.Settled)
            { Receipt.Set(__state.Card, __state.PreviousReceipt); Pending.Remove(__state.Owner); }
            return __exception;
        }
    }

    // Commit only after the native purchase has actually succeeded, including
    // its zero debit and native card packet, BEFORE Courier restocks/price UI.
    [HarmonyPatch(typeof(MerchantCardEntry), "OnTryPurchase")]
    private static class NativeSuccess
    {
        private static void Postfix(MerchantCardEntry __instance, Player ____player, ref Task<(bool, int)> __result)
        {
            if (Pending.TryGetValue(____player, out var pending) && ReferenceEquals(pending.Entry, __instance))
                __result = Commit(__result, pending);
        }
        private static async Task<(bool, int)> Commit(Task<(bool, int)> purchase, Purchase pending)
        {
            var result = await purchase;
            if (result.Item1)
            {
                pending.Settled = true;
                pending.Ticket.SpentMerchant = pending.Merchant;
                pending.Ticket.Flash();
            }
            return result;
        }
    }

    [HarmonyPatch(typeof(RewardSynchronizer), "HandleRewardObtainedMessage")]
    private static class NativeReceipt
    {
        private static void Postfix(RewardObtainedMessage message, ulong senderId,
            MegaCrit.Sts2.Core.Runs.IPlayerCollection ____playerCollection)
        {
            if (message.rewardType != RewardType.Card || message.wasSkipped || message.cardModel == null) return;
            var owner = ____playerCollection.GetPlayer(senderId);
            if (owner?.RunState.CurrentRoom is not MerchantRoom || message.location != owner.RunState.RunLocation) return;
            string receipt = Receipt.Get(message.cardModel) ?? "";
            if (receipt.Length == 0 || receipt != MerchantKey(owner)) return;
            foreach (var ticket in owner.Relics.OfType<SootboundTicket>().Where(r => !r.IsMelted))
                ticket.SpentMerchant = receipt;
        }
    }
}
