using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace AKeepersNeed2.Shared.Patching;

/// <summary>
/// Transpiler helper: redirects every call to one method inside a patched method to a static
/// replacement with the same stack shape (an instance method's <c>this</c> becomes the
/// replacement's first parameter). Used where the value we want to change is read through a
/// trivial property getter, which Mono may inline, so patching the getter itself can silently
/// do nothing.
/// </summary>
internal static class CallSwap
{
    /// <param name="owner">Module name for the warning logged when no call was found.</param>
    public static IEnumerable<CodeInstruction> Replace(
        IEnumerable<CodeInstruction> instructions,
        MethodInfo target,
        MethodInfo replacement,
        string owner
    )
    {
        int swapped = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.Calls(target))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
                swapped++;
            }
            yield return instruction;
        }
        if (swapped == 0)
        {
            Plugin.Logger.LogWarning($"[{owner}] no call to {target?.Name} found; the game may have changed.");
        }
    }
}
