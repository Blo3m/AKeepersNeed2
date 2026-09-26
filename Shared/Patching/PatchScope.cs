using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using HarmonyLib;

namespace AKeepersNeed2.Shared.Patching;

/// <summary>
/// "Only while execution is inside method X": <see cref="Wrap"/> patches methods with a
/// prefix/finalizer pair that counts entries, and <see cref="Active"/> is true while any of
/// them is running (nested calls included). Use it to scope a shared-method patch to one
/// caller, e.g. faking inventory checks only inside a craft check.
/// </summary>
internal sealed class PatchScope
{
    // One scope per wrapped method, so the static prefix can find its counter.
    private static readonly Dictionary<MethodBase, PatchScope> ByMethod = new Dictionary<MethodBase, PatchScope>();

    private readonly string _owner;
    private readonly ThreadLocal<int> _depth = new ThreadLocal<int>();

    /// <param name="owner">Module name for log messages.</param>
    public PatchScope(string owner)
    {
        _owner = owner;
    }

    public bool Active => _depth.Value > 0;

    /// <summary>
    /// Makes <see cref="Active"/> true while <paramref name="method"/> runs. Logs and skips a
    /// null method (renamed by a game update) or one already wrapped by another scope.
    /// </summary>
    public void Wrap(Harmony harmony, MethodBase method)
    {
        if (method == null)
        {
            Plugin.Logger.LogWarning($"[{_owner}] scope target not found; the game may have renamed it.");
            return;
        }
        if (ByMethod.TryGetValue(method, out PatchScope existing) && existing != this)
        {
            Plugin.Logger.LogError($"[{_owner}] {method.Name} is already wrapped by {existing._owner}'s scope.");
            return;
        }
        ByMethod[method] = this;
        harmony.Patch(
            method,
            prefix: new HarmonyMethod(typeof(PatchScope), nameof(Enter)),
            finalizer: new HarmonyMethod(typeof(PatchScope), nameof(Exit))
        );
    }

    // __state carries the scope so Exit decrements exactly what Enter incremented, even if
    // another mod's prefix skips ours.
    private static void Enter(MethodBase __originalMethod, out PatchScope __state)
    {
        if (ByMethod.TryGetValue(__originalMethod, out __state))
        {
            __state._depth.Value++;
        }
    }

    private static void Exit(PatchScope __state)
    {
        if (__state != null)
        {
            __state._depth.Value--;
        }
    }
}
