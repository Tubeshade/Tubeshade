using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;

namespace Tubeshade.Data.Generators.Tests;

public sealed class QueryGeneratorTests
{
    private const string Project = "C:/project";

    private const string Classes =
        """
        namespace Test
        {
            public partial class RootRepository;
        }

        namespace Test.Media
        {
            public sealed partial class LibraryRepository;
        }

        namespace Test.Media.Videos
        {
            public sealed partial class VideoRepository;

            public sealed partial class VideoFileRepository;

            public sealed class SealedRepository;

            public interface IRepository;
        }

        namespace Test.Media.Videos.Details
        {
            public partial class DetailRepository;
        }

        namespace Test.Split
        {
            public partial class SplitRepository;

            public partial class SplitRepository;
        }
        """;

    [Test]
    public void FileBecomesConstantInNestedClass()
    {
        var result = Run(Sql("Media/Videos/VideoRepository.Find.sql", "SELECT 1;"));

        result.Diagnostics.Should().BeEmpty();
        result.Constant("Test.Media.Videos.VideoRepository+Queries", "Find").Should().Be("SELECT 1;");
    }

    [Test]
    public void FileInProjectFolderUsesRootNamespace()
    {
        var result = Run(Sql("RootRepository.Version.sql", "SELECT version();"));

        result.Diagnostics.Should().BeEmpty();
        result.Constant("Test.RootRepository+Queries", "Version").Should().Be("SELECT version();");
    }

    [Test]
    public void DeeplyNestedFoldersAreNamespaces()
    {
        var result = Run(Sql("Media/Videos/Details/DetailRepository.Get.sql", "get"));

        result.Diagnostics.Should().BeEmpty();
        result.Constant("Test.Media.Videos.Details.DetailRepository+Queries", "Get").Should().Be("get");
    }

    [Test]
    public void QueriesOfSeveralClassesAreSeparate()
    {
        var result = Run(
            Sql("Media/Videos/VideoRepository.Find.sql", "video"),
            Sql("Media/Videos/VideoRepository.Get.sql", "videos"),
            Sql("Media/Videos/VideoFileRepository.Find.sql", "file"),
            Sql("Media/LibraryRepository.Find.sql", "library"));

        result.Diagnostics.Should().BeEmpty();
        result.Constant("Test.Media.Videos.VideoRepository+Queries", "Find").Should().Be("video");
        result.Constant("Test.Media.Videos.VideoRepository+Queries", "Get").Should().Be("videos");
        result.Constant("Test.Media.Videos.VideoFileRepository+Queries", "Find").Should().Be("file");
        result.Constant("Test.Media.LibraryRepository+Queries", "Find").Should().Be("library");
    }

    [Test]
    public void WindowsPathsAreSupported()
    {
        var result = Run(@"C:\project", new SqlFile(@"C:\project\Media\Videos\VideoRepository.Find.sql", "SELECT 1;"));

        result.Diagnostics.Should().BeEmpty();
        result.Constant("Test.Media.Videos.VideoRepository+Queries", "Find").Should().Be("SELECT 1;");
    }

    [Test]
    public void ProjectDirectoryIsMatchedIgnoringCase()
    {
        var result = Run("c:/PROJECT", new SqlFile("C:/project/Media/Videos/VideoRepository.Find.sql", "SELECT 1;"));

        result.Diagnostics.Should().BeEmpty();
        result.Constant("Test.Media.Videos.VideoRepository+Queries", "Find").Should().Be("SELECT 1;");
    }

    [Test]
    public void FilesWithOtherExtensionsAreIgnored()
    {
        var result = Run(
            Sql("Media/Videos/VideoRepository.Find.sql", "SELECT 1;"),
            Sql("Media/Videos/Readme.md", "not a query"),
            Sql("Media/Videos/Not a valid name.txt", "not a query"));

        result.Diagnostics.Should().BeEmpty();
        result.Constant("Test.Media.Videos.VideoRepository+Queries", "Find").Should().Be("SELECT 1;");
    }

    [Test]
    public void ExtensionIsMatchedIgnoringCase()
    {
        var result = Run(Sql("Media/Videos/VideoRepository.Find.SQL", "SELECT 1;"));

        result.Diagnostics.Should().BeEmpty();
        result.Constant("Test.Media.Videos.VideoRepository+Queries", "Find").Should().Be("SELECT 1;");
    }

    [Test]
    public void QuotesAreEscaped()
    {
        const string sql = "SELECT 'it''s' AS \"Name\", \"\"\"quoted\"\"\" FROM \"media\".\"videos\" WHERE x = \"\";";
        var result = Run(Sql("Media/Videos/VideoRepository.Find.sql", sql));

        result.Diagnostics.Should().BeEmpty();
        result.Constant("Test.Media.Videos.VideoRepository+Queries", "Find").Should().Be(sql);
    }

    [Test]
    public void BackslashesAndBracesAreKept()
    {
        const string sql = "SELECT '\\n', '{0}', '{{', E'\\\\' /* @\" */;";
        var result = Run(Sql("Media/Videos/VideoRepository.Find.sql", sql));

        result.Diagnostics.Should().BeEmpty();
        result.Constant("Test.Media.Videos.VideoRepository+Queries", "Find").Should().Be(sql);
    }

    [Test]
    public void MultilineTextIsKeptWithLineFeeds()
    {
        var result = Run(Sql("Media/Videos/VideoRepository.Find.sql", "SELECT *\r\nFROM media.videos\r\nWHERE id = @Id;\r\n"));

        result.Diagnostics.Should().BeEmpty();
        result.Constant("Test.Media.Videos.VideoRepository+Queries", "Find").Should().Be("SELECT *\nFROM media.videos\nWHERE id = @Id;\n");
    }

    [Test]
    public void OutputIsDeterministic()
    {
        var files = new[]
        {
            Sql("Media/Videos/VideoRepository.Find.sql", "find"),
            Sql("Media/LibraryRepository.Get.sql", "get"),
            Sql("RootRepository.Version.sql", "version"),
            Sql("Media/Videos/VideoRepository.Delete.sql", "delete"),
        };

        var first = Run(files).Sources;
        var second = Run(files.Reverse().ToArray()).Sources;
        var third = Run(files).Sources;

        first.Should().NotBeEmpty();
        second.Should().Equal(first);
        third.Should().Equal(first);
    }

    [Test]
    public void OutputDoesNotDependOnLineEndings()
    {
        var windows = Run(Sql("Media/Videos/VideoRepository.Find.sql", "a\r\nb\r\n")).Sources;
        var unix = Run(Sql("Media/Videos/VideoRepository.Find.sql", "a\nb\n")).Sources;

        windows.Should().NotBeEmpty();
        windows.Should().Equal(unix);
    }

    [Test]
    public void GeneratedSourceCompilesWithoutWarnings()
    {
        var result = Run(
            Sql("Media/Videos/VideoRepository.Find.sql", "find"),
            Sql("Media/Videos/VideoRepository.Get.sql", "get"),
            Sql("Media/Videos/Details/DetailRepository.Get.sql", "details"),
            Sql("RootRepository.Get.sql", "root"));

        result.CompilationDiagnostics.Should().BeEmpty();
    }

    [Test]
    public void RegistryContainsEveryQueryByName()
    {
        var result = Run(
            Sql("RootRepository.Version.sql", "version"),
            Sql("Media/Videos/VideoRepository.Find.sql", "find"),
            Sql("Media/Videos/Details/DetailRepository.Get.sql", "get \"quoted\""));

        result.Diagnostics.Should().BeEmpty();
        result.All("Test").Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["RootRepository.Version"] = "version",
            ["Media.Videos.VideoRepository.Find"] = "find",
            ["Media.Videos.Details.DetailRepository.Get"] = "get \"quoted\"",
        });
    }

    [Test]
    public void RegistryIsOrderedByName()
    {
        var result = Run(
            Sql("Media/Videos/VideoRepository.Find.sql", "find"),
            Sql("Media/LibraryRepository.Get.sql", "get"),
            Sql("Media/Videos/VideoRepository.Delete.sql", "delete"));

        result.All("Test").Keys.Should().Equal(
            "Media.LibraryRepository.Get",
            "Media.Videos.VideoRepository.Delete",
            "Media.Videos.VideoRepository.Find");
    }

    [Test]
    public void RegistryDoesNotContainInvalidQueries()
    {
        var result = Run(
            Sql("Media/Videos/VideoRepository.Find.sql", "find"),
            Sql("Media/Videos/Missing.Find.sql", "missing"));

        result.All("Test").Keys.Should().Equal("Media.Videos.VideoRepository.Find");
    }

    [Test]
    public void ConstantsAreGeneratedInTheRootNamespace()
    {
        var result = Run(
            "Tubeshade.Data",
            "Other.Assembly",
            Project,
            Classes.Replace("namespace Test", "namespace Tubeshade.Data"),
            Sql("Media/Videos/VideoRepository.Find.sql", "find"));

        result.Diagnostics.Should().BeEmpty();
        result.Constant("Tubeshade.Data.Media.Videos.VideoRepository+Queries", "Find").Should().Be("find");
        result.All("Tubeshade.Data").Should().ContainKey("Media.Videos.VideoRepository.Find");
    }

    [Test]
    public void AssemblyNameIsUsedWhenThereIsNoRootNamespace()
    {
        var result = Run(
            null,
            "Some.Assembly",
            Project,
            Classes.Replace("namespace Test", "namespace Some.Assembly"),
            Sql("Media/Videos/VideoRepository.Find.sql", "find"));

        result.Diagnostics.Should().BeEmpty();
        result.Constant("Some.Assembly.Media.Videos.VideoRepository+Queries", "Find").Should().Be("find");
    }

    [Test]
    public void NothingIsGeneratedWithoutQueries()
    {
        var result = Run();

        result.Diagnostics.Should().BeEmpty();
        result.Sources.Should().BeEmpty();
    }

    [TestCase("Not Valid")]
    [TestCase("my-query")]
    [TestCase("1Query")]
    [TestCase("class")]
    [TestCase("namespace")]
    public void InvalidQueryNameIsReported(string name)
    {
        var path = $"{Project}/Media/Videos/VideoRepository.{name}.sql";
        var result = Run(new SqlFile(path, "SELECT 1;"));

        var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be("TSSQL001");
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Error);
        diagnostic.GetMessage().Should().Contain($"'{name}'");
        diagnostic.Location.GetLineSpan().Path.Should().Be(path);
    }

    [TestCase("Not Valid")]
    [TestCase("my-class")]
    [TestCase("class")]
    public void InvalidClassNameIsReported(string name)
    {
        var result = Run(Sql($"Media/Videos/{name}.Find.sql", "SELECT 1;"));

        var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be("TSSQL001");
        diagnostic.GetMessage().Should().Contain($"'{name}'");
    }

    [TestCase("Not Valid")]
    [TestCase("my-folder")]
    [TestCase("1Folder")]
    [TestCase("class")]
    public void InvalidFolderNameIsReported(string folder)
    {
        var result = Run(Sql($"{folder}/VideoRepository.Find.sql", "SELECT 1;"));

        var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be("TSSQL001");
        diagnostic.GetMessage().Should().Contain($"'{folder}'");
    }

    [TestCase("Find.sql", "Find")]
    [TestCase("VideoRepository.Find.More.sql", "VideoRepository.Find.More")]
    [TestCase("VideoRepository..sql", "")]
    public void FileNameWithoutClassAndQueryIsReported(string fileName, string reported)
    {
        var result = Run(Sql($"Media/Videos/{fileName}", "SELECT 1;"));

        var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be("TSSQL001");
        diagnostic.GetMessage().Should().Contain($"'{reported}'");
    }

    [Test]
    public void InvalidQueryDoesNotPreventOthersFromBeingGenerated()
    {
        var result = Run(
            Sql("Media/Videos/VideoRepository.Find.sql", "find"),
            Sql("Media/Videos/VideoRepository.not valid.sql", "invalid"));

        result.Diagnostics.Should().ContainSingle().Which.Id.Should().Be("TSSQL001");
        result.Constant("Test.Media.Videos.VideoRepository+Queries", "Find").Should().Be("find");
    }

    [Test]
    public void FilesWithTheSameNameAreReported()
    {
        var result = Run(
            Sql("Media/Videos/VideoRepository.Find.sql", "first"),
            Sql("Media/Videos/VideoRepository.Find.SQL", "second"));

        var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be("TSSQL002");
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Error);
        diagnostic.GetMessage().Should().Contain("'Find'");
    }

    [Test]
    public void FilesWithTheSameNameInDifferentClassesAreAllowed()
    {
        var result = Run(
            Sql("Media/Videos/VideoRepository.Find.sql", "video"),
            Sql("Media/Videos/VideoFileRepository.Find.sql", "file"));

        result.Diagnostics.Should().BeEmpty();
        result.Constant("Test.Media.Videos.VideoRepository+Queries", "Find").Should().Be("video");
        result.Constant("Test.Media.Videos.VideoFileRepository+Queries", "Find").Should().Be("file");
    }

    [Test]
    public void QueryNamedLikeTheQueriesClassIsReported()
    {
        var result = Run(Sql("Media/Videos/VideoRepository.Queries.sql", "find"));

        var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be("TSSQL002");
        diagnostic.GetMessage().Should().Contain("'Queries'");
    }

    [Test]
    public void MissingClassIsReported()
    {
        var path = $"{Project}/Media/Videos/MissingRepository.Find.sql";
        var result = Run(new SqlFile(path, "find"));

        var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be("TSSQL004");
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Error);
        diagnostic.GetMessage().Should().Contain("'MissingRepository'").And.Contain("'Test.Media.Videos'");
        diagnostic.Location.GetLineSpan().Path.Should().Be(path);
    }

    [Test]
    public void ClassInAnotherNamespaceIsReported()
    {
        var result = Run(Sql("Media/VideoRepository.Find.sql", "find"));

        result.Diagnostics.Should().ContainSingle().Which.Id.Should().Be("TSSQL004");
    }

    [Test]
    public void InterfaceIsReportedAsMissingClass()
    {
        var result = Run(Sql("Media/Videos/IRepository.Find.sql", "find"));

        result.Diagnostics.Should().ContainSingle().Which.Id.Should().Be("TSSQL004");
    }

    [Test]
    public void FileOutsideTheProjectDirectoryIsReported()
    {
        var result = Run(new SqlFile("D:/other/Media/Videos/VideoRepository.Find.sql", "find"));

        result.Diagnostics.Should().ContainSingle().Which.Id.Should().Be("TSSQL004");
    }

    [Test]
    public void ClassThatIsNotPartialIsReported()
    {
        var result = Run(Sql("Media/Videos/SealedRepository.Find.sql", "find"));

        var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be("TSSQL005");
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Error);
        diagnostic.GetMessage().Should().Contain("Test.Media.Videos.SealedRepository");
    }

    [Test]
    public void ClassWithSomeDeclarationsNotPartialIsReported()
    {
        const string source =
            """
            namespace Test.Split2
            {
                public partial class MixedRepository;

                public class MixedRepository;
            }
            """;

        var result = Run("Test", "Test.Assembly", Project, source, Sql("Split2/MixedRepository.Find.sql", "find"));

        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Id == "TSSQL005");
    }

    [Test]
    public void ClassDeclaredInSeveralPartsIsSupported()
    {
        var result = Run(Sql("Split/SplitRepository.Find.sql", "find"));

        result.Diagnostics.Should().BeEmpty();
        result.Constant("Test.Split.SplitRepository+Queries", "Find").Should().Be("find");
    }

    [Test]
    public void UnreadableFileIsReported()
    {
        var path = $"{Project}/Media/Videos/VideoRepository.Find.sql";
        var result = Run(new SqlFile(path, null), Sql("Media/Videos/VideoRepository.Get.sql", "get"));

        var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be("TSSQL003");
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Error);
        diagnostic.GetMessage().Should().Contain(path);
        diagnostic.Location.GetLineSpan().Path.Should().Be(path);
        result.Constant("Test.Media.Videos.VideoRepository+Queries", "Get").Should().Be("get");
        result.Constant("Test.Media.Videos.VideoRepository+Queries", "Find").Should().BeNull();
    }

    [Test]
    public void OutputIsCachedWhenAnUnrelatedClassIsAdded()
    {
        var reasons = OutputReasonsAfterEdit(compilation => compilation.AddSyntaxTrees(
            CSharpSyntaxTree.ParseText("namespace Test.Other { public class Unrelated { public int Value => 1; } }")));

        reasons.Should().NotBeEmpty().And.OnlyContain(reason => reason == IncrementalStepRunReason.Cached);
    }

    [Test]
    public void OutputIsCachedWhenARepositoryMemberChanges()
    {
        var reasons = OutputReasonsAfterEdit(compilation =>
        {
            var tree = compilation.SyntaxTrees.Single();
            var edited = tree.WithChangedText(SourceText.From(Classes.Replace(
                "public partial class RootRepository;",
                "public partial class RootRepository { public int Count => 2; }")));

            return compilation.ReplaceSyntaxTree(tree, edited);
        });

        reasons.Should().NotBeEmpty().And.OnlyContain(reason => reason == IncrementalStepRunReason.Cached);
    }

    [Test]
    public void OutputIsRegeneratedWhenTheRepositoryIsRemoved()
    {
        var reasons = OutputReasonsAfterEdit(compilation =>
        {
            var tree = compilation.SyntaxTrees.Single();
            var edited = tree.WithChangedText(SourceText.From(Classes.Replace("public partial class RootRepository;", string.Empty)));
            return compilation.ReplaceSyntaxTree(tree, edited);
        });

        reasons.Should().Contain(reason => reason != IncrementalStepRunReason.Cached);
    }

    private static IncrementalStepRunReason[] OutputReasonsAfterEdit(Func<Compilation, Compilation> edit)
    {
        var compilation = CSharpCompilation.Create(
            "Test.Assembly",
            [CSharpSyntaxTree.ParseText(Classes)],
            GetReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var globalOptions = ImmutableDictionary<string, string>.Empty
            .Add("build_property.MSBuildProjectDirectory", Project)
            .Add("build_property.RootNamespace", "Test");

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new QueryGenerator().AsSourceGenerator()],
            additionalTexts: [new TestText($"{Project}/RootRepository.Version.sql", "SELECT version();")],
            optionsProvider: new TestOptions(globalOptions),
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

        driver = driver.RunGenerators(compilation);
        driver = driver.RunGenerators(edit(compilation));

        return driver.GetRunResult().Results
            .SelectMany(result => result.TrackedOutputSteps.Values)
            .SelectMany(steps => steps)
            .SelectMany(step => step.Outputs)
            .Select(output => output.Reason)
            .ToArray();
    }

    private static SqlFile Sql(string relativePath, string text) => new($"{Project}/{relativePath}", text);

    private static GeneratorResult Run(params SqlFile[] files) => Run(Project, files);

    private static GeneratorResult Run(string projectDirectory, params SqlFile[] files) =>
        Run("Test", "Test.Assembly", projectDirectory, Classes, files);

    private static GeneratorResult Run(
        string? rootNamespace,
        string assemblyName,
        string projectDirectory,
        string source,
        params SqlFile[] files)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(source)],
            GetReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var globalOptions = ImmutableDictionary<string, string>.Empty.Add("build_property.MSBuildProjectDirectory", projectDirectory);
        if (rootNamespace is not null)
        {
            globalOptions = globalOptions.Add("build_property.RootNamespace", rootNamespace);
        }

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new QueryGenerator().AsSourceGenerator()],
            additionalTexts: files.Select(file => (AdditionalText)new TestText(file.Path, file.Text)).ToArray(),
            optionsProvider: new TestOptions(globalOptions));

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
        var runResult = driver.GetRunResult();

        return new GeneratorResult(
            output,
            runResult.Diagnostics,
            runResult.Results.SelectMany(result => result.GeneratedSources).Select(source => source.SourceText.ToString()).ToArray());
    }

    private static IEnumerable<MetadataReference> GetReferences()
    {
        var assemblies = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        return assemblies
            .Split(Path.PathSeparator)
            .Where(path => Path.GetFileName(path) is "System.Private.CoreLib.dll" or "System.Runtime.dll" or "System.Collections.dll")
            .Select(path => MetadataReference.CreateFromFile(path));
    }

    private sealed record SqlFile(string Path, string? Text);

    private sealed class GeneratorResult(Compilation compilation, ImmutableArray<Diagnostic> diagnostics, string[] sources)
    {
        private readonly Lazy<Assembly?> _assembly = new(() => Load(compilation));

        public ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;

        public string[] Sources { get; } = sources;

        public IEnumerable<Diagnostic> CompilationDiagnostics =>
            compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning);

        public string? Constant(string typeName, string constant)
        {
            var field = _assembly.Value?.GetType(typeName)?.GetField(constant, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            return (string?)field?.GetRawConstantValue();
        }

        public IReadOnlyDictionary<string, string> All(string rootNamespace)
        {
            var registry = _assembly.Value?.GetType($"{rootNamespace}.QueryRegistry");
            if (registry is null)
            {
                return new Dictionary<string, string>();
            }

            return (IReadOnlyDictionary<string, string>)registry.GetField("All", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        }

        private static Assembly? Load(Compilation compilation)
        {
            using var stream = new MemoryStream();
            var result = compilation.Emit(stream);
            return result.Success ? Assembly.Load(stream.ToArray()) : null;
        }
    }

    private sealed class TestText(string path, string? text) : AdditionalText
    {
        public override string Path { get; } = path;

        public override SourceText? GetText(CancellationToken cancellationToken = default) =>
            text is null ? null : SourceText.From(text);
    }

    private sealed class TestOptions(ImmutableDictionary<string, string> globalOptions) : AnalyzerConfigOptionsProvider
    {
        private readonly TestConfig _global = new(globalOptions);

        public override AnalyzerConfigOptions GlobalOptions => _global;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _global;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _global;
    }

    private sealed class TestConfig(ImmutableDictionary<string, string> options) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, out string value) => options.TryGetValue(key, out value!);
    }
}
