using System.Collections.Generic;

namespace AKeepersNeed2.Modules.StackSizes;

/// <summary>
/// The owners of the containers whose add/can-add methods are running, innermost last. A stack's
/// own <c>CanAddItemCount</c> doesn't know its container, so it reads the innermost owner here;
/// with nothing running (e.g. merging ground piles) it's <see cref="StackOwner.Other"/>. Only
/// touched from the main thread.
/// </summary>
internal static class StackContext
{
    private static readonly List<StackOwner> Owners = new List<StackOwner>();

    public static StackOwner Current => Owners.Count > 0
        ? Owners[Owners.Count - 1]
        : StackOwner.Other;

    public static void Push(StackOwner owner)
    {
        Owners.Add(owner);
    }

    public static void Pop()
    {
        if (Owners.Count > 0)
        {
            Owners.RemoveAt(Owners.Count - 1);
        }
    }
}
