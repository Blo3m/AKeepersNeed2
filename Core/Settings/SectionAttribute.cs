using System;

namespace AKeepersNeed2.Core.Settings;

/// <summary>
/// Places a <see cref="MenuSection"/> member: which tab it's on, its header title, and its sort
/// order within the tab. Read at runtime by <see cref="SettingsRegistry"/> and at compile time
/// by the AKeepersNeed2.Analyzers project, so the values must stay constants.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
internal sealed class SectionAttribute : Attribute
{
    public SectionAttribute(MenuTab tab, string title, int order)
    {
        Tab = tab;
        Title = title;
        Order = order;
    }

    public MenuTab Tab { get; }

    public string Title { get; }

    public int Order { get; }
}
