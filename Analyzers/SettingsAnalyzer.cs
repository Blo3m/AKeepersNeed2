using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace AKeepersNeed2.Analyzers;

/// <summary>
/// Fails the build on mistakes in <c>SettingsBuilder</c> calls, which a runtime registry would
/// only notice once the game runs:
/// AKN001 two rows share a section and order number, AKN002 two config entries in the same
/// scope share a section and key, AKN003 two rows in one tab share a label, and AKN000 a value
/// those checks need isn't a compile-time constant (so they can't be bypassed).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SettingsAnalyzer : DiagnosticAnalyzer
{
    private const string BuilderTypeName = "AKeepersNeed2.Core.Settings.SettingsBuilder";
    private const string SectionAttributeName = "AKeepersNeed2.Core.Settings.SectionAttribute";
    private const string Category = "AKeepersNeed2.Settings";

    private static readonly DiagnosticDescriptor NotConstant = new DiagnosticDescriptor(
        "AKN000",
        "Settings value must be a compile-time constant",
        "'{0}' must be a compile-time constant{1} so settings collisions can be checked at build time",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor OrderCollision = new DiagnosticDescriptor(
        "AKN001",
        "Two settings rows share a section and order",
        "Row '{0}' uses order {1} in section {2}, already used by row '{3}' ({4})",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: WellKnownDiagnosticTags.CompilationEnd
    );

    private static readonly DiagnosticDescriptor KeyCollision = new DiagnosticDescriptor(
        "AKN002",
        "Two config entries share a section and key",
        "{0} config entry [{1}] {2} is already bound at {3}",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: WellKnownDiagnosticTags.CompilationEnd
    );

    private static readonly DiagnosticDescriptor LabelCollision = new DiagnosticDescriptor(
        "AKN003",
        "Two settings rows in one tab share a label",
        "Row label '{0}' is already used on the {1} tab at {2}",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: WellKnownDiagnosticTags.CompilationEnd
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(NotConstant, OrderCollision, KeyCollision, LabelCollision);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            INamedTypeSymbol builder = start.Compilation.GetTypeByMetadataName(BuilderTypeName);
            if (builder == null)
            {
                return;
            }
            var rows = new ConcurrentBag<RowCall>();
            var entries = new ConcurrentBag<EntryCall>();
            start.RegisterOperationAction(
                op => Collect(op, builder, rows, entries),
                OperationKind.Invocation
            );
            start.RegisterCompilationEndAction(end => ReportCollisions(end, rows, entries));
        });
    }

    private static void Collect(
        OperationAnalysisContext context,
        INamedTypeSymbol builder,
        ConcurrentBag<RowCall> rows,
        ConcurrentBag<EntryCall> entries
    )
    {
        var invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol method = invocation.TargetMethod;
        if (!SymbolEqualityComparer.Default.Equals(method.ContainingType, builder))
        {
            return;
        }

        switch (method.Name)
        {
            case "Profile":
            case "Global":
                if (TryConstantString(context, invocation, "section", out string configSection)
                    & TryConstantString(context, invocation, "key", out string key))
                {
                    entries.Add(new EntryCall(method.Name, configSection, key, invocation.Syntax.GetLocation()));
                }
                break;
            case "Toggle":
            case "Key":
            case "Slider":
                bool hasSection = TrySection(
                    context,
                    invocation,
                    out string sectionName,
                    out int tab,
                    out string tabName
                );
                bool hasOrder = TryConstantInt(context, invocation, "order", out int order);
                string label = string.Empty;
                bool hasLabel = method.Name == "Slider"
                    || TryConstantString(context, invocation, "label", out label);
                if (hasSection && hasOrder && hasLabel)
                {
                    rows.Add(new RowCall(sectionName, tab, tabName, order, label, invocation.Syntax.GetLocation()));
                }
                break;
        }
    }

    private static void ReportCollisions(
        CompilationAnalysisContext context,
        ConcurrentBag<RowCall> rows,
        ConcurrentBag<EntryCall> entries
    )
    {
        // Sorted by source position so the first declaration "wins" and reports are stable.
        List<RowCall> sortedRows = rows
            .OrderBy(r => r.Location.SourceTree?.FilePath)
            .ThenBy(r => r.Location.SourceSpan.Start)
            .ToList();

        foreach (IGrouping<(string, int), RowCall> group in sortedRows.GroupBy(r => (r.Section, r.Order)))
        {
            RowCall first = group.First();
            foreach (RowCall duplicate in group.Skip(1))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    OrderCollision,
                    duplicate.Location,
                    Describe(duplicate),
                    duplicate.Order,
                    duplicate.Section,
                    Describe(first),
                    Where(first.Location)
                ));
            }
        }

        IEnumerable<RowCall> labelled = sortedRows.Where(r => r.Label.Trim().Length > 0);
        foreach (IGrouping<(int, string), RowCall> group in labelled.GroupBy(r => (r.Tab, r.Label.Trim())))
        {
            RowCall first = group.First();
            foreach (RowCall duplicate in group.Skip(1))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    LabelCollision,
                    duplicate.Location,
                    duplicate.Label.Trim(),
                    duplicate.TabName,
                    Where(first.Location)
                ));
            }
        }

        List<EntryCall> sortedEntries = entries
            .OrderBy(e => e.Location.SourceTree?.FilePath)
            .ThenBy(e => e.Location.SourceSpan.Start)
            .ToList();
        IEnumerable<IGrouping<(string, string, string), EntryCall>> entryGroups =
            sortedEntries.GroupBy(e => (e.Scope, e.Section, e.Key));
        foreach (IGrouping<(string, string, string), EntryCall> group in entryGroups)
        {
            EntryCall first = group.First();
            foreach (EntryCall duplicate in group.Skip(1))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    KeyCollision,
                    duplicate.Location,
                    duplicate.Scope,
                    duplicate.Section,
                    duplicate.Key,
                    Where(first.Location)
                ));
            }
        }
    }

    private static string Describe(RowCall row)
    {
        return row.Label.Trim().Length > 0 ? row.Label.Trim() : "(slider)";
    }

    private static string Where(Location location)
    {
        FileLinePositionSpan span = location.GetLineSpan();
        return $"{System.IO.Path.GetFileName(span.Path)}:{span.StartLinePosition.Line + 1}";
    }

    private static IArgumentOperation Argument(IInvocationOperation invocation, string parameter)
    {
        return invocation.Arguments.FirstOrDefault(a => a.Parameter?.Name == parameter);
    }

    private static bool TryConstantString(
        OperationAnalysisContext context,
        IInvocationOperation invocation,
        string parameter,
        out string value
    )
    {
        value = null;
        IArgumentOperation argument = Argument(invocation, parameter);
        if (argument == null)
        {
            return false;
        }
        if (argument.Value.ConstantValue.HasValue && argument.Value.ConstantValue.Value is string text)
        {
            value = text;
            return true;
        }
        context.ReportDiagnostic(Diagnostic.Create(NotConstant, argument.Syntax.GetLocation(), parameter, ""));
        return false;
    }

    private static bool TryConstantInt(
        OperationAnalysisContext context,
        IInvocationOperation invocation,
        string parameter,
        out int value
    )
    {
        value = 0;
        IArgumentOperation argument = Argument(invocation, parameter);
        if (argument == null)
        {
            return false;
        }
        if (argument.Value.ConstantValue.HasValue && argument.Value.ConstantValue.Value is int number)
        {
            value = number;
            return true;
        }
        context.ReportDiagnostic(Diagnostic.Create(NotConstant, argument.Syntax.GetLocation(), parameter, ""));
        return false;
    }

    /// <summary>Reads a <c>MenuSection.X</c> argument and its tab from X's [Section] attribute.</summary>
    private static bool TrySection(
        OperationAnalysisContext context,
        IInvocationOperation invocation,
        out string name,
        out int tab,
        out string tabName
    )
    {
        name = null;
        tab = 0;
        tabName = null;
        IArgumentOperation argument = Argument(invocation, "section");
        if (argument == null)
        {
            return false;
        }

        IOperation value = argument.Value;
        while (value is IConversionOperation conversion)
        {
            value = conversion.Operand;
        }
        if (value is IFieldReferenceOperation fieldReference && fieldReference.Field.HasConstantValue)
        {
            IFieldSymbol field = fieldReference.Field;
            AttributeData section = field.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == SectionAttributeName);
            if (section != null && section.ConstructorArguments.Length > 0)
            {
                TypedConstant tabArgument = section.ConstructorArguments[0];
                name = field.Name;
                tab = tabArgument.Value is int tabValue ? tabValue : 0;
                tabName = tabArgument.Type is INamedTypeSymbol tabType
                    ? tabType.GetMembers().OfType<IFieldSymbol>()
                        .FirstOrDefault(f => f.HasConstantValue && Equals(f.ConstantValue, tabArgument.Value))?.Name
                    : null;
                tabName = tabName ?? tab.ToString();
                return true;
            }
        }
        context.ReportDiagnostic(Diagnostic.Create(
            NotConstant,
            argument.Syntax.GetLocation(),
            "section",
            " (a MenuSection member with a [Section] attribute)"
        ));
        return false;
    }

    private sealed class RowCall
    {
        public RowCall(string section, int tab, string tabName, int order, string label, Location location)
        {
            Section = section;
            Tab = tab;
            TabName = tabName;
            Order = order;
            Label = label;
            Location = location;
        }

        public string Section { get; }

        public int Tab { get; }

        public string TabName { get; }

        public int Order { get; }

        public string Label { get; }

        public Location Location { get; }
    }

    private sealed class EntryCall
    {
        public EntryCall(string scope, string section, string key, Location location)
        {
            Scope = scope;
            Section = section;
            Key = key;
            Location = location;
        }

        public string Scope { get; }

        public string Section { get; }

        public string Key { get; }

        public Location Location { get; }
    }
}
