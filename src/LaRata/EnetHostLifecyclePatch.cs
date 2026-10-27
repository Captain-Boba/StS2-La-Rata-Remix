using System.Reflection;
using System.Reflection.Emit;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Transport.ENet;

namespace LaRata;

// v0.107.1 ENetHost.Update polls raw Disconnect events but skips their handler.
// Godot has already invalidated that peer: keeping it in the host's list causes
// later heartbeat/input sends and StopHost to operate on a dead connection.
// Restore the existing native lifecycle notification, not a send/error filter.
[HarmonyPatch(typeof(ENetHost), nameof(ENetHost.Update))]
internal static class EnetHostLifecyclePatch
{
    private static readonly MethodInfo NativePoll = AccessTools.Method(typeof(ENetConnectionExtension),
        nameof(ENetConnectionExtension.TryService), [typeof(ENetConnection), typeof(ENetServiceData?).MakeByRefType()])
        ?? throw new MissingMethodException("ENetConnectionExtension.TryService");
    private static readonly MethodInfo FindConnection = AccessTools.Method(typeof(ENetHost),
        "GetConnectionByPeer", [typeof(ENetPacketPeer)])
        ?? throw new MissingMethodException("ENetHost.GetConnectionByPeer");
    private static readonly MethodInfo HandleDisconnection = AccessTools.Method(typeof(ENetHost),
        "HandleClientDisconnection")
        ?? throw new MissingMethodException("ENetHost.HandleClientDisconnection");

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var code = instructions.ToList();
        int calls = code.Count(i => i.Calls(NativePoll));
        if (calls != 1)
            throw new InvalidOperationException($"ENetHost.Update expected exactly one native TryService call; found {calls}.");
        foreach (var instruction in code)
        {
            if (instruction.Calls(NativePoll))
            {
                // Keep the original connection/out arguments; append this host.
                var host = new CodeInstruction(OpCodes.Ldarg_0);
                host.labels.AddRange(instruction.labels);
                host.blocks.AddRange(instruction.blocks);
                instruction.labels.Clear();
                instruction.blocks.Clear();
                yield return host;
                instruction.opcode = OpCodes.Call;
                instruction.operand = AccessTools.Method(typeof(EnetHostLifecyclePatch), nameof(Poll));
            }
            yield return instruction;
        }
    }

    private static bool Poll(ENetConnection connection, out ENetServiceData? output, ENetHost host)
    {
        // A native disconnect callback may stop/destroy the host reentrantly.
        // End its polling loop before the next call on that destroyed connection.
        if (!host.IsConnected) { output = null; return false; }
        bool serviced = ENetConnectionExtension.TryService(connection, out output);
        if (serviced && output is { type: ENetConnection.EventType.Disconnect } data)
        {
            object? client = FindConnection.Invoke(host, BindingFlags.DoNotWrapExceptions, null, [data.peer], null);
            if (client != null)
                // An application disconnect packet may already have removed it.
                // A raw event supplies no application-level reason; use the same
                // fallback as the native ENetClient, retaining native callbacks.
                HandleDisconnection.Invoke(host, BindingFlags.DoNotWrapExceptions, null,
                    [client, NetError.UnknownNetworkError, true], null);
        }
        // Active polling preserves every native event and the original result,
        // including errors, connect/receive events and the normal empty poll.
        return serviced;
    }
}
