using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Tubeshade.Data.Generators;

/// <summary>
/// <c>Media/Videos/VideoRepository.Find.sql</c> becomes <c>VideoRepository.Queries.Find</c>, where <c>VideoRepository</c>
/// is a partial class in the namespace of the file's folder below the project directory, so that a misspelt query is
/// a compile error instead of a runtime exception.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class QueryGenerator : IIncrementalGenerator
{
    private const string Category = "Queries";
    private const string NestedClassName = "Queries";
    private const string RegistryClassName = "QueryRegistry";

    public static readonly DiagnosticDescriptor InvalidName = new(
        "TSSQL001",
        "Query name is not valid",
        "'{0}' cannot become a name because it is not a valid C# identifier, files must be named <Class>.<Query>.sql",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateName = new(
        "TSSQL002",
        "Query name is used more than once",
        "'{0}' clashes with another name generated in the same class",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnreadableFile = new(
        "TSSQL003",
        "Query file could not be read",
        "Could not read the query file '{0}'",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ClassNotFound = new(
        "TSSQL004",
        "Class for the query was not found",
        "There is no class '{0}' in the namespace '{1}', which is the namespace of the folder of the query file",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ClassNotPartial = new(
        "TSSQL005",
        "Class for the query is not partial",
        "The class '{0}' must be declared partial to contain its queries",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var files = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .Select(static (file, cancellationToken) => new QueryFile(file.Path, file.GetText(cancellationToken)?.ToString()))
            .Collect();

        var options = context.AnalyzerConfigOptionsProvider
            .Select(static (options, _) => new ProjectOptions(
                options.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace) ? rootNamespace : null,
                options.GlobalOptions.TryGetValue("build_property.MSBuildProjectDirectory", out var directory) ? directory : null));

        // Only the assembly name and the declared classes are taken from the compilation, as equatable values,
        // so that edits which do not change them do not regenerate the queries.
        var assemblyName = context.CompilationProvider.Select(static (compilation, _) => compilation.AssemblyName);

        var classes = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax { Parent: BaseNamespaceDeclarationSyntax or CompilationUnitSyntax },
                static (syntax, _) => ClassDeclaration.From((ClassDeclarationSyntax)syntax.Node))
            .Collect();

        var model = files
            .Combine(options)
            .Combine(assemblyName)
            .Combine(classes)
            .Select(static (input, _) => CreateModel(input.Left.Left.Left, input.Left.Left.Right, input.Left.Right, input.Right));

        context.RegisterSourceOutput(model, static (output, model) => Generate(output, model));
    }

    private static GenerationModel CreateModel(
        ImmutableArray<QueryFile> files,
        ProjectOptions options,
        string? assemblyName,
        ImmutableArray<ClassDeclaration> declarations)
    {
        var rootNamespace = string.IsNullOrWhiteSpace(options.RootNamespace) ? assemblyName : options.RootNamespace;
        var partialClasses = declarations
            .GroupBy(declaration => declaration.FullName, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.All(declaration => declaration.IsPartial), StringComparer.Ordinal);

        var classes = new SortedDictionary<string, QueryClass>(StringComparer.Ordinal);
        var diagnostics = new List<DiagnosticInfo>();

        foreach (var file in files.OrderBy(file => file.Path, StringComparer.Ordinal))
        {
            Add(classes, diagnostics, partialClasses, rootNamespace, options.ProjectDirectory, file);
        }

        return new GenerationModel(
            rootNamespace,
            new EquatableArray<QueryClassModel>(classes.Values.Select(queryClass => queryClass.ToModel())),
            new EquatableArray<DiagnosticInfo>(diagnostics));
    }

    private static void Generate(SourceProductionContext context, GenerationModel model)
    {
        foreach (var diagnostic in model.Diagnostics)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                diagnostic.Descriptor,
                Location.Create(diagnostic.Path, default, default),
                diagnostic.Arguments.Cast<object>().ToArray()));
        }

        foreach (var queryClass in model.Classes)
        {
            context.AddSource($"{queryClass.FullName}.Queries.g.cs", WriteClass(queryClass));
        }

        if (model.Classes.Count > 0)
        {
            context.AddSource($"{RegistryClassName}.g.cs", WriteRegistry(model.RootNamespace, model.Classes));
        }
    }

    private static void Add(
        SortedDictionary<string, QueryClass> classes,
        List<DiagnosticInfo> diagnostics,
        Dictionary<string, bool> partialClasses,
        string? rootNamespace,
        string? projectDirectory,
        QueryFile file)
    {
        if (file.Text is null)
        {
            diagnostics.Add(new DiagnosticInfo(UnreadableFile, file.Path, file.Path));
            return;
        }

        var (folders, fileName) = SplitPath(file.Path, projectDirectory);
        var nameParts = fileName.Split('.');
        if (nameParts.Length != 2)
        {
            diagnostics.Add(new DiagnosticInfo(InvalidName, file.Path, fileName));
            return;
        }

        var className = nameParts[0];
        var queryName = nameParts[1];
        foreach (var segment in folders.Append(className).Append(queryName))
        {
            if (!IsValidIdentifier(segment))
            {
                diagnostics.Add(new DiagnosticInfo(InvalidName, file.Path, segment));
                return;
            }
        }

        var classNamespace = string.Join(".", new[] { rootNamespace }.Concat(folders).Where(segment => !string.IsNullOrEmpty(segment)));
        var fullName = classNamespace.Length == 0 ? className : $"{classNamespace}.{className}";
        if (!partialClasses.TryGetValue(fullName, out var isPartial))
        {
            diagnostics.Add(new DiagnosticInfo(ClassNotFound, file.Path, className, classNamespace));
            return;
        }

        if (!isPartial)
        {
            diagnostics.Add(new DiagnosticInfo(ClassNotPartial, file.Path, fullName));
            return;
        }

        if (queryName == NestedClassName ||
            (classes.TryGetValue(fullName, out var existing) && existing.Queries.ContainsKey(queryName)))
        {
            diagnostics.Add(new DiagnosticInfo(DuplicateName, file.Path, queryName));
            return;
        }

        if (!classes.TryGetValue(fullName, out var queryClass))
        {
            queryClass = new QueryClass(classNamespace, className, fullName);
            classes.Add(fullName, queryClass);
        }

        queryClass.Queries.Add(queryName, file.Text.Replace("\r\n", "\n"));
    }

    private static (string[] Folders, string FileName) SplitPath(string path, string? projectDirectory)
    {
        var segments = Normalize(path).Split(['/'], StringSplitOptions.RemoveEmptyEntries);
        var directory = Normalize(projectDirectory ?? string.Empty).Split(['/'], StringSplitOptions.RemoveEmptyEntries);

        var isBelowProject = directory.Length > 0 &&
                             segments.Length > directory.Length &&
                             directory.Zip(segments, (left, right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase)).All(equal => equal);

        var folders = isBelowProject
            ? segments.Skip(directory.Length).Take(segments.Length - directory.Length - 1).ToArray()
            : [];

        var fileName = segments[segments.Length - 1];
        return (folders, fileName.Substring(0, fileName.LastIndexOf('.')));
    }

    private static string Normalize(string path) => path.Replace('\\', '/');

    private static bool IsValidIdentifier(string name) =>
        SyntaxFacts.IsValidIdentifier(name) && SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None;

    private static string WriteClass(QueryClassModel queryClass)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated/>");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        if (queryClass.Namespace.Length > 0)
        {
            builder.AppendLine($"namespace {queryClass.Namespace};");
            builder.AppendLine();
        }

        builder.AppendLine($"partial class {queryClass.Name}");
        builder.AppendLine("{");
        builder.AppendLine($"\tinternal static class {NestedClassName}");
        builder.AppendLine("\t{");
        foreach (var query in queryClass.Queries)
        {
            builder.AppendLine($"\t\tinternal const string {query.Name} = @\"{query.Text.Replace("\"", "\"\"")}\";");
        }

        builder.AppendLine("\t}");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string WriteRegistry(string? rootNamespace, IEnumerable<QueryClassModel> classes)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated/>");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        if (!string.IsNullOrWhiteSpace(rootNamespace))
        {
            builder.AppendLine($"namespace {rootNamespace};");
            builder.AppendLine();
        }

        builder.AppendLine($"internal static class {RegistryClassName}");
        builder.AppendLine("{");
        builder.AppendLine("\tinternal static readonly global::System.Collections.Generic.IReadOnlyDictionary<string, string> All =");
        builder.AppendLine("\t\tnew global::System.Collections.Generic.Dictionary<string, string>");
        builder.AppendLine("\t\t{");
        foreach (var queryClass in classes)
        {
            var keyPrefix = string.IsNullOrEmpty(rootNamespace) || !queryClass.FullName.StartsWith(rootNamespace + ".", StringComparison.Ordinal)
                ? queryClass.FullName
                : queryClass.FullName.Substring(rootNamespace!.Length + 1);

            foreach (var query in queryClass.Queries)
            {
                builder.AppendLine($"\t\t\t[\"{keyPrefix}.{query.Name}\"] = global::{queryClass.FullName}.{NestedClassName}.{query.Name},");
            }
        }

        builder.AppendLine("\t\t};");
        builder.AppendLine("}");
        return builder.ToString();
    }

    /// <summary>A <c>.sql</c> file, as an equatable value so that unchanged files are not regenerated.</summary>
    private readonly struct QueryFile(string path, string? text) : IEquatable<QueryFile>
    {
        public string Path { get; } = path;

        public string? Text { get; } = text;

        public bool Equals(QueryFile other) => Path == other.Path && Text == other.Text;

        public override bool Equals(object? obj) => obj is QueryFile other && Equals(other);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Path);
    }

    private readonly struct ProjectOptions(string? rootNamespace, string? projectDirectory) : IEquatable<ProjectOptions>
    {
        public string? RootNamespace { get; } = rootNamespace;

        public string? ProjectDirectory { get; } = projectDirectory;

        public bool Equals(ProjectOptions other) => RootNamespace == other.RootNamespace && ProjectDirectory == other.ProjectDirectory;

        public override bool Equals(object? obj) => obj is ProjectOptions other && Equals(other);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(RootNamespace ?? string.Empty);
    }

    /// <summary>A top-level class declaration; a class is partial only if all of its declarations are.</summary>
    private readonly struct ClassDeclaration(string fullName, bool isPartial) : IEquatable<ClassDeclaration>
    {
        public string FullName { get; } = fullName;

        public bool IsPartial { get; } = isPartial;

        public static ClassDeclaration From(ClassDeclarationSyntax syntax)
        {
            // Generic classes get their metadata name, so that a query file never matches them
            var name = syntax.TypeParameterList is { } typeParameters
                ? $"{syntax.Identifier.ValueText}`{typeParameters.Parameters.Count}"
                : syntax.Identifier.ValueText;

            var namespaces = syntax.Ancestors()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .Select(declaration => declaration.Name.ToString())
                .Reverse();

            return new ClassDeclaration(
                string.Join(".", namespaces.Append(name)),
                syntax.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword)));
        }

        public bool Equals(ClassDeclaration other) => FullName == other.FullName && IsPartial == other.IsPartial;

        public override bool Equals(object? obj) => obj is ClassDeclaration other && Equals(other);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(FullName);
    }

    private sealed class QueryClass(string classNamespace, string name, string fullName)
    {
        public string Namespace { get; } = classNamespace;

        public string Name { get; } = name;

        public string FullName { get; } = fullName;

        public SortedDictionary<string, string> Queries { get; } = new(StringComparer.Ordinal);

        public QueryClassModel ToModel() => new(
            Namespace,
            Name,
            FullName,
            new EquatableArray<Query>(Queries.Select(query => new Query(query.Key, query.Value))));
    }

    private sealed class GenerationModel(
        string? rootNamespace,
        EquatableArray<QueryClassModel> classes,
        EquatableArray<DiagnosticInfo> diagnostics) : IEquatable<GenerationModel>
    {
        public string? RootNamespace { get; } = rootNamespace;

        public EquatableArray<QueryClassModel> Classes { get; } = classes;

        public EquatableArray<DiagnosticInfo> Diagnostics { get; } = diagnostics;

        public bool Equals(GenerationModel? other) =>
            other is not null &&
            RootNamespace == other.RootNamespace &&
            Classes.Equals(other.Classes) &&
            Diagnostics.Equals(other.Diagnostics);

        public override bool Equals(object? obj) => Equals(obj as GenerationModel);

        public override int GetHashCode() => Classes.GetHashCode();
    }

    private sealed class QueryClassModel(string classNamespace, string name, string fullName, EquatableArray<Query> queries)
        : IEquatable<QueryClassModel>
    {
        public string Namespace { get; } = classNamespace;

        public string Name { get; } = name;

        public string FullName { get; } = fullName;

        public EquatableArray<Query> Queries { get; } = queries;

        public bool Equals(QueryClassModel? other) =>
            other is not null &&
            FullName == other.FullName &&
            Name == other.Name &&
            Namespace == other.Namespace &&
            Queries.Equals(other.Queries);

        public override bool Equals(object? obj) => Equals(obj as QueryClassModel);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(FullName);
    }

    private readonly struct Query(string name, string text) : IEquatable<Query>
    {
        public string Name { get; } = name;

        public string Text { get; } = text;

        public bool Equals(Query other) => Name == other.Name && Text == other.Text;

        public override bool Equals(object? obj) => obj is Query other && Equals(other);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Name);
    }

    /// <summary>A diagnostic without its <see cref="Location"/>, which is not equatable by value.</summary>
    private sealed class DiagnosticInfo(DiagnosticDescriptor descriptor, string path, params string[] arguments)
        : IEquatable<DiagnosticInfo>
    {
        public DiagnosticDescriptor Descriptor { get; } = descriptor;

        public string Path { get; } = path;

        public EquatableArray<string> Arguments { get; } = new(arguments);

        public bool Equals(DiagnosticInfo? other) =>
            other is not null &&
            Descriptor.Equals(other.Descriptor) &&
            Path == other.Path &&
            Arguments.Equals(other.Arguments);

        public override bool Equals(object? obj) => Equals(obj as DiagnosticInfo);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Path);
    }

    private readonly struct EquatableArray<T>(IEnumerable<T> items) : IEquatable<EquatableArray<T>>, IEnumerable<T>
        where T : IEquatable<T>
    {
        private readonly ImmutableArray<T> _items = items.ToImmutableArray();

        public int Count => _items.IsDefault ? 0 : _items.Length;

        public bool Equals(EquatableArray<T> other) => this.SequenceEqual(other);

        public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

        public override int GetHashCode() => Count;

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)(_items.IsDefault ? ImmutableArray<T>.Empty : _items)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
