using System.Xml.Linq;
using TUnit.Assertions.Enums;

namespace ResXLocalize.Avalonia.Tests.Generators;

public sealed class ResourceKeysGeneratorTest
{
    private static readonly TestResourceFile StringsFile = new("/project/Strings.resx", "", ("Title", "Title"));

    private static readonly TestResourceFile[] CultureFiles =
    [
        new("/project/XAML/XAML.resx", "",
            ("Button_Apply", "Apply"),
            ("Greeting", "Hello"),
            ("OnlyNeutral", "Only neutral"),
            ("SelectedCount", "Selected {0} items")),
        new("/project/XAML/XAML.de.resx", "de",
            ("Button_Apply", "Anwenden"),
            ("SelectedCount", "{0} ausgewählt")),
        new("/project/XAML/XAML.zh.resx", "zh",
            ("Greeting", "你好")),
        new("/project/XAML/XAML.zh-Hans.resx", "zh-Hans",
            ("Button_Apply", "应用")),
        new("/project/Interaction/Interaction.resx", "",
            ("Notice", "Notice")),
        new("/project/Interaction/Interaction.de.resx", "de",
            ("Notice", "Hinweis"))
    ];

    private const string BindingDeclarations = """
                                               public sealed class Target : AvaloniaObject
                                               {
                                                   public static readonly StyledProperty<string?> TextProperty =
                                                       AvaloniaProperty.Register<Target, string?>(nameof(Text));

                                                   public Target(BindingBase binding) => Bind(TextProperty, binding);

                                                   public string? Text => GetValue(TextProperty);
                                               }

                                               public sealed class Source : INotifyPropertyChanged
                                               {
                                                   public event PropertyChangedEventHandler? PropertyChanged;

                                                   public int Count
                                                   {
                                                       get;
                                                       set
                                                       {
                                                           field = value;
                                                           PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
                                                       }
                                                   }
                                               }
                                               """;

    [Test]
    public void RunGenerators_GeneratedSources_MatchSnapshot()
    {
        var output = GeneratorTestHelper.Run(
            new Dictionary<string, string>
            {
                ["DefaultLocalizationProvider"] = "XAML",
                ["XmlnsDefinitionNamespace"] = "https://example.com/localization"
            },
            CultureFiles);

        Snapshot.Validate(output.RunResult);
    }

    [Test]
    public async Task RunGenerators_Culture_FallsBackThroughParentCultures()
    {
        var texts = await GeneratorTestHelper.ExecuteAsync<string[]>(
            null,
            CultureFiles,
            """
            string Read(string culture)
            {
                LocalizationManager.Culture = CultureInfo.GetCultureInfo(culture);
                return $"{XAML.Button_Apply} | {XAML.Greeting} | {XAML.OnlyNeutral} | {XAML.SelectedCount(3)} | {Interaction.Notice}";
            }

            return new[] { "", "fr", "de", "de-AT", "zh", "zh-Hans", "zh-TW" }.Select(Read).ToArray();
            """);

        await Assert.That(texts).IsEquivalentTo(
            [
                "Apply | Hello | Only neutral | Selected 3 items | Notice",
                "Apply | Hello | Only neutral | Selected 3 items | Notice",
                "Anwenden | Hello | Only neutral | 3 ausgewählt | Hinweis",
                "Anwenden | Hello | Only neutral | 3 ausgewählt | Hinweis",
                "Apply | 你好 | Only neutral | Selected 3 items | Notice",
                "应用 | 你好 | Only neutral | Selected 3 items | Notice",
                "Apply | 你好 | Only neutral | Selected 3 items | Notice"
            ],
            CollectionOrdering.Matching);
    }

    [Test]
    public async Task RunGenerators_CultureChange_NotifiesLocalizedStrings()
    {
        var events = await GeneratorTestHelper.ExecuteAsync<string[]>(
            null,
            CultureFiles,
            """
            LocalizationManager.Culture = CultureInfo.InvariantCulture;
            var events = new List<string>();
            var selectedCount = XAML.SelectedCount(3);
            XAML.Button_Apply.PropertyChanged += (sender, e) => events.Add($"Button_Apply.{e.PropertyName}: {sender}");
            selectedCount.PropertyChanged += (sender, e) => events.Add($"SelectedCount.{e.PropertyName}: {sender}");
            LocalizationManager.CultureChanged += (_, _) => events.Add($"CultureChanged: {CultureInfo.CurrentUICulture.Name}, {XAML.Button_Apply}");

            LocalizationManager.Culture = CultureInfo.GetCultureInfo("de");
            return events.ToArray();
            """);

        await Assert.That(events).IsEquivalentTo(
        [
            "Button_Apply.Value: Anwenden",
            "SelectedCount.Value: 3 ausgewählt",
            "CultureChanged: de, Anwenden"
        ]);
    }

    [Test]
    public async Task RunGenerators_KeyLookups_ReturnOneSharedInstancePerKey()
    {
        var instances = await GeneratorTestHelper.ExecuteAsync<object[]>(
            null,
            CultureFiles,
            """
            return new object[] { XAML.Greeting, XAML.Get(XAMLKey.Greeting), XAML.Get("Greeting") };
            """);

        await Assert.That(instances.Distinct(ReferenceEqualityComparer.Instance).Count()).IsEqualTo(1);
    }

    [Test]
    public async Task RunGenerators_TextConversions_LookUpOnlyRequestedKeys()
    {
        var texts = await GeneratorTestHelper.ExecuteAsync<string[]>(
            null,
            CultureFiles,
            """
            LocalizationManager.Culture = CultureInfo.GetCultureInfo("de");
            string text = XAML.Button_Apply;
            return new string[] { text, XAML.Get("Button_Apply"), XAML.Get("Not a key"), LocalizedString.FromText("Button_Apply") };
            """);

        await Assert.That(texts).IsEquivalentTo(["Anwenden", "Anwenden", "Not a key", "Button_Apply"], CollectionOrdering.Matching);
    }

    [Test]
    public async Task RunGenerators_StringToLocalizedString_HasNoImplicitConversion()
    {
        var compilation = GeneratorTestHelper.Run(null, StringsFile).OutputCompilation.AddSyntaxTrees(
            CSharpSyntaxTree.ParseText(
                "public static class Probe { public static Sample.LocalizedString Wrap(string text) => text; }",
                GeneratorTestHelper.ParseOptions));

        await Assert.That(compilation.GetDiagnostics().Select(static diagnostic => diagnostic.Id)).Contains("CS0029");
    }

    [Test]
    public async Task RunGenerators_DefaultLocalizationProvider_SelectsTableForLocalize()
    {
        var texts = await GeneratorTestHelper.ExecuteAsync<string[]>(
            new Dictionary<string, string> { ["DefaultLocalizationProvider"] = "Interaction" },
            CultureFiles,
            """
            LocalizationManager.Culture = CultureInfo.GetCultureInfo("de");
            return new string[]
            {
                new Target(new LocalizeExtension(InteractionKey.Notice).ProvideValue(null!)).Text!,
                new Target(new LocalizeXAMLExtension(XAMLKey.Button_Apply).ProvideValue(null!)).Text!
            };
            """,
            BindingDeclarations);

        await Assert.That(texts).IsEquivalentTo(["Hinweis", "Anwenden"], CollectionOrdering.Matching);
    }

    [Test]
    public async Task RunGenerators_LocalizeExtension_FollowsCultureAndArguments()
    {
        var texts = await GeneratorTestHelper.ExecuteAsync<string[]>(
            null,
            CultureFiles,
            """
            LocalizationManager.Culture = CultureInfo.InvariantCulture;
            var source = new Source { Count = 1 };
            var count = new ReflectionBinding(nameof(Source.Count)) { Source = source };
            Target[] targets =
            [
                new(new LocalizeExtension(XAMLKey.Button_Apply).ProvideValue(null!)),
                new(new LocalizeExtension(XAMLKey.SelectedCount, count).ProvideValue(null!)),
                new(new LocalizeExtension { Key = XAMLKey.SelectedCount, Arguments = { count } }.ProvideValue(null!)),
                new(new LocalizeExtension(XAMLKey.SelectedCount, new LocalizeExtension(XAMLKey.Button_Apply).ProvideValue(null!)).ProvideValue(null!)),
                new(new LocalizeInteractionExtension(InteractionKey.Notice).ProvideValue(null!))
            ];

            string Read() => string.Join(" | ", targets.Select(static target => target.Text));
            var texts = new List<string> { Read() };
            source.Count = 2;
            texts.Add(Read());
            LocalizationManager.Culture = CultureInfo.GetCultureInfo("de");
            texts.Add(Read());
            return texts.ToArray();
            """,
            BindingDeclarations);

        await Assert.That(texts).IsEquivalentTo(
            [
                "Apply | Selected 1 items | Selected 1 items | Selected Apply items | Notice",
                "Apply | Selected 2 items | Selected 2 items | Selected Apply items | Notice",
                "Anwenden | 2 ausgewählt | 2 ausgewählt | Anwenden ausgewählt | Hinweis"
            ],
            CollectionOrdering.Matching);
    }

    [Test]
    public async Task RunGenerators_LocalizeExtensionArgumentWithoutValue_LeavesTextUnset()
    {
        var text = await GeneratorTestHelper.ExecuteAsync<string?>(
            null,
            CultureFiles,
            """
            var count = new ReflectionBinding("Missing") { Source = new Source() };
            return new Target(new LocalizeExtension(XAMLKey.SelectedCount, count).ProvideValue(null!)).Text;
            """,
            BindingDeclarations);

        await Assert.That(text).IsNull();
    }

    [Test]
    public async Task RunGenerators_SpecialKeysAndValues_KeepsKeysAndTextIntact()
    {
        var texts = await GeneratorTestHelper.ExecuteAsync<string[]>(
            null,
            [
                new TestResourceFile(
                    "/project/Strings.resx",
                    "",
                    ("class", "keyword"),
                    ("Key With Space", "  padded  "),
                    ("1st", "starts with digit"),
                    ("Escaped", "quote \" backslash \\ tab \t end"),
                    ("MultiLine", "first <line> & more\nsecond line"),
                    ("Summary", "{{0}} is {1:N0}"))
            ],
            """
            LocalizationManager.Culture = CultureInfo.InvariantCulture;
            return new string[]
            {
                Strings.@class,
                Strings.Key_With_Space,
                Strings.Get("Key With Space"),
                Strings._1st,
                Strings.Escaped,
                Strings.MultiLine,
                Strings.Summary("unused", 1234)
            };
            """);

        await Assert.That(texts).IsEquivalentTo(
            [
                "keyword",
                "  padded  ",
                "  padded  ",
                "starts with digit",
                "quote \" backslash \\ tab \t end",
                "first <line> & more\nsecond line",
                "{0} is 1,234"
            ],
            CollectionOrdering.Matching);
    }

    [Test]
    public async Task RunGenerators_Keys_DocumentedWithNeutralText()
    {
        var compilation = GeneratorTestHelper.Run(
                null,
                new TestResourceFile("/project/Strings.resx", "", ("MultiLine", "first <line> & more\nsecond line")))
            .OutputCompilation;

        using var _ = Assert.Multiple();
        foreach (var typeName in new[] { "Sample.Strings", "Sample.StringsKey" })
        {
            var documentation = compilation.GetTypeByMetadataName(typeName)!.GetMembers("MultiLine").Single().GetDocumentationCommentXml()!;
            var summaryLines = XElement.Parse(documentation).Element("summary")!.Value
                .Split('\n')
                .Select(static line => line.Trim())
                .Where(static line => line.Length > 0);
            await Assert.That(summaryLines).IsEquivalentTo(["first <line> & more", "second line"], CollectionOrdering.Matching);
        }
    }

    [Test]
    public async Task RunGenerators_XmlnsDefinitionNamespace_RegistersXamlNamespace()
    {
        var output = GeneratorTestHelper.Run(new Dictionary<string, string> { ["XmlnsDefinitionNamespace"] = "https://example.com/localization" }, StringsFile);

        using var _ = Assert.Multiple();
        await Assert.That(GetCompilationDiagnostics(output)).IsEmpty();
        await Assert.That(GetAssemblyAttributes(output)).IsEquivalentTo(
        [
            "XmlnsPrefixAttribute(https://example.com/localization, loc)",
            "XmlnsDefinitionAttribute(https://example.com/localization, Sample)",
            "XmlnsDefinitionAttribute(https://github.com/avaloniaui, Sample.Extensions.MarkupExtensions)"
        ]);
    }

    [Test]
    public async Task RunGenerators_NoXmlnsDefinitionNamespace_RegistersOnlyMarkupExtensions()
    {
        var output = GeneratorTestHelper.Run(null, StringsFile);

        await Assert.That(GetAssemblyAttributes(output)).IsEquivalentTo(["XmlnsDefinitionAttribute(https://github.com/avaloniaui, Sample.Extensions.MarkupExtensions)"]);
    }

    [Test]
    public async Task RunGenerators_ResxNamespace_OverridesRootNamespace()
    {
        var output = GeneratorTestHelper.Run(new Dictionary<string, string> { ["ResxNamespace"] = "Sample.Localization" }, StringsFile);

        using var _ = Assert.Multiple();
        await Assert.That(GetCompilationDiagnostics(output)).IsEmpty();
        await Assert.That(output.OutputCompilation.GetTypeByMetadataName("Sample.Localization.Strings")).IsNotNull();
        await Assert.That(output.OutputCompilation.GetTypeByMetadataName("Sample.Localization.LocalizationManager")).IsNotNull();
        await Assert.That(output.OutputCompilation.GetTypeByMetadataName("Sample.Localization.Extensions.MarkupExtensions.LocalizeExtension")).IsNotNull();
        await Assert.That(output.OutputCompilation.GetTypeByMetadataName("Sample.Strings")).IsNull();
    }

    [Test]
    public async Task RunGenerators_InvalidResourceFiles_IgnoresThem()
    {
        var output = GeneratorTestHelper.Run(
            new Dictionary<string, string> { ["DefaultLocalizationProvider"] = "Missing" },
            new TestResourceFile("/project/First/Strings.resx", "", ("Title", "Title")),
            new TestResourceFile("/project/Second/Strings.resx", "", ("Title", "Title")),
            new TestResourceFile("/project/First/Strings.de.resx", "de", ("Title", "Titel"), ("Typo", "Tippfehler")),
            new TestResourceFile("/project/Orphan.de.resx", "de", ("Title", "Titel")));

        using var _ = Assert.Multiple();
        await Assert.That(output.RunResult.Diagnostics).IsEmpty();
        await Assert.That(GetCompilationDiagnostics(output)).IsEmpty();
        await Assert.That(output.OutputCompilation.GetTypeByMetadataName("Sample.Strings")).IsNotNull();
        await Assert.That(output.OutputCompilation.GetTypeByMetadataName("Sample.Orphan")).IsNull();
    }

    private static IEnumerable<string> GetCompilationDiagnostics(GeneratorTestOutput output) =>
        output.OutputCompilation.GetDiagnostics()
            .Where(static diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning)
            .Select(static diagnostic => diagnostic.ToString());

    private static IEnumerable<string> GetAssemblyAttributes(GeneratorTestOutput output) =>
        output.OutputCompilation.Assembly.GetAttributes()
            .Select(static attribute => $"{attribute.AttributeClass!.Name}({string.Join(", ", attribute.ConstructorArguments.Select(static argument => argument.Value))})");
}
