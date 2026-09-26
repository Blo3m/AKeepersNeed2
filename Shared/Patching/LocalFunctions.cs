using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace AKeepersNeed2.Shared.Patching;

/// <summary>
/// Locates compiler-generated C# local functions so they can be patched. The compiler names
/// them <c>&lt;Outer&gt;g__Local|N_M</c> and, when the local function is captured as a delegate,
/// emits it on a nested display class rather than the declaring type, so both are searched.
/// </summary>
internal static class LocalFunctions
{
    public static MethodInfo Find(Type declaringType, string outerMethod, string localFunction)
    {
        string prefix = $"<{outerMethod}>g__{localFunction}|";
        return Candidates(declaringType).FirstOrDefault(m => m.Name.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static MethodInfo[] Candidates(Type type)
    {
        return AccessTools.GetDeclaredMethods(type)
            .Concat(type.GetNestedTypes(AccessTools.all).SelectMany(Candidates))
            .ToArray();
    }
}
