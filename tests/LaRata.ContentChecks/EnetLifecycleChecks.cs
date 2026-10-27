using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using Godot;
using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Multiplayer.Transport.ENet;

// Controlled poll boundary, real native host list/packet parser/callback methods.
// Null peer identity deliberately avoids fabricating a live Godot object. This
// proves lifecycle branching, not sockets, engine handles or actual multiplayer.
internal static class EnetLifecycleChecks
{
    private static ENetServiceData? next;
    private static bool result;
    private static int polls, checks;
    private static Exception? pollFailure;
    private static readonly Type Patch = typeof(ModEntry).Assembly.GetType("LaRata.EnetHostLifecyclePatch")!;
    private static readonly MethodInfo Poll = AccessTools.Method(Patch, "Poll");
    private static readonly MethodInfo NativePoll = AccessTools.Method(typeof(ENetConnectionExtension), "TryService");
    private static void Check(bool condition, string why)
    { checks++; if (!condition) throw new Exception("ENet lifecycle: " + why); }
    private static bool Supply(out ENetServiceData? output, ref bool __result)
    {
        polls++;
        if (pollFailure != null) throw pollFailure;
        output = next; __result = result; return false;
    }
    private sealed class Handler : INetHostHandler
    {
        internal readonly List<(ulong, NetError)> Removed = [];
        internal Action? After;
        public void OnPeerConnected(ulong peerId) { }
        public void OnPeerDisconnected(ulong peerId, NetErrorInfo info)
        { Removed.Add((peerId, info.GetReason())); After?.Invoke(); }
        public void OnDisconnected(NetErrorInfo info) { }
        public void OnPacketReceived(ulong senderId, byte[] bytes, NetTransferMode mode, int channel) { }
    }
    private static (ENetHost host, Handler handler) NewHost()
    {
        var handler = new Handler(); var host = new ENetHost(handler);
        AccessTools.Field(typeof(ENetHost), "_isConnected").SetValue(host, true);
        var type = typeof(ENetHost).GetNestedType("ClientConnection", BindingFlags.NonPublic)!;
        var conn = Activator.CreateInstance(type)!;
        AccessTools.Field(type, "netId").SetValue(conn, 2101UL);
        AccessTools.Field(type, "peer").SetValue(conn, null);
        ((IList)AccessTools.Field(typeof(ENetHost), "_connectedPeers").GetValue(host)!).Add(conn);
        return (host, handler);
    }
    private static (bool serviced, ENetServiceData? data) Invoke(ENetHost host)
    {
        object?[] args = [null, null, host];
        bool serviced = (bool)Poll.Invoke(null, BindingFlags.DoNotWrapExceptions, null, args, null)!;
        return (serviced, (ENetServiceData?)args[1]);
    }
    internal static void Run(Harmony smoke)
    {
        Check(Harmony.GetPatchInfo(AccessTools.Method(typeof(ENetHost), "Update"))?.Transpilers
            .Any(p => p.owner == smoke.Id && p.PatchMethod.DeclaringType == Patch) == true, "host Update patch not active");
        var transpile = AccessTools.Method(Patch, "Transpiler");
        foreach (int count in new[] { 0, 2 })
        {
            bool rejected = false;
            try { ((IEnumerable<CodeInstruction>)transpile.Invoke(null,
                [Enumerable.Range(0, count).Select(_ => new CodeInstruction(OpCodes.Call, NativePoll)).ToArray()])!).ToArray(); }
            catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "callsite drift was not rejected: " + count);
        }
        var fixture = new Harmony("LaRata.ContentChecks.EnetPollBoundary");
        fixture.Patch(NativePoll, prefix: new(typeof(EnetLifecycleChecks), nameof(Supply)));
        try
        {
            foreach (var kind in new[] { ENetConnection.EventType.Connect, ENetConnection.EventType.Receive, ENetConnection.EventType.Error })
            {
                var state = NewHost(); next = new() { type = kind, channel = 7, error = Error.Failed, packetData = [42] }; result = true;
                var actual = Invoke(state.host);
                Check(actual.serviced && actual.data?.type == kind && actual.data?.channel == 7 && actual.data?.error == Error.Failed
                    && ReferenceEquals(actual.data?.packetData, next.Value.packetData), "non-disconnect event modified");
                Check(state.handler.Removed.Count == 0 && state.host.ConnectedPeerIds.Single() == 2101, "non-disconnect removed peer");
            }
            var empty = NewHost(); next = null; result = false;
            Check(Invoke(empty.host) == (false, null), "empty poll changed");
            var raw = NewHost(); next = new() { type = ENetConnection.EventType.Disconnect }; result = true;
            Check(Invoke(raw.host).serviced, "raw event lost");
            Check(!raw.host.ConnectedPeerIds.Any() && raw.handler.Removed.SequenceEqual(new[] { (2101UL, NetError.UnknownNetworkError) }), "native removal/callback mismatch");
            Invoke(raw.host);
            Check(raw.handler.Removed.Count == 1, "duplicate/unknown peer notified twice");
            var app = NewHost();
            AccessTools.Method(typeof(ENetHost), "HandlePacketReceived").Invoke(app.host, [new ENetServiceData
            { peer = null!, packetData = ENetPacket.FromDisconnection(new ENetDisconnection { reason = NetError.Quit }).AllBytes }]);
            Invoke(app.host);
            Check(app.handler.Removed.SequenceEqual(new[] { (2101UL, NetError.Quit) }) && !app.host.ConnectedPeerIds.Any(), "app reason lost or raw event notified twice");
            var reentrant = NewHost();
            reentrant.handler.After = () => AccessTools.Field(typeof(ENetHost), "_isConnected").SetValue(reentrant.host, false);
            Check(Invoke(reentrant.host).serviced, "current event changed on callback stop");
            int before = polls;
            Check(Invoke(reentrant.host) == (false, null) && polls == before, "poll continued after callback stopped host");
            var failing = NewHost(); pollFailure = new InvalidOperationException("controlled native poll failure");
            bool propagated = false;
            try { Invoke(failing.host); } catch (InvalidOperationException e) { propagated = ReferenceEquals(e, pollFailure); }
            Check(propagated, "native poll exception hidden or replaced");
            Console.WriteLine($"PASS: {checks} ENet lifecycle checks: actual host patch activation, callsite drift, unchanged non-disconnect/empty/error events, native raw/app removal and reason/idempotence, reentrant stop and exception propagation. Poll boundary substituted; no engine/socket/live claim.");
        }
        finally { pollFailure = null; fixture.UnpatchAll(fixture.Id); }
    }
}
