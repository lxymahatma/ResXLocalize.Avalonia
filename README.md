# ResXLocalize.Avalonia

**A source-generator–based runtime localization library for Avalonia, backed by standard `.resx` files.**

*Built primarily for personal use — new features are added on demand. Contributions are welcome.*

## What it does

- Compiles every `.resx` table into generated code. There is no `ResourceManager` and no satellite assembly, so
  NativeAOT and trimming need no extra setup.
- Generates, for every table (`XAML.resx` → `XAML`):
  - `XAMLKey`, an enum of the keys.
  - `XAML`, a static class with one shared, change-notifying `LocalizedString` per key. A key whose text has
    placeholders such as `{0}` becomes a method that takes the arguments.
- Generates a `{Localize}` XAML markup extension. The key is checked at compile time (a typo is build error AVLN3000),
  and the text follows language switches and argument changes.
- Exposes `LocalizationManager.Culture`: set it at runtime and all bound text refreshes.

## Installation

```sh
dotnet add package ResXLocalize.Avalonia
```

Or, equivalently:

```xml
<PackageReference Include="ResXLocalize.Avalonia" Version="3.0.0" />
```

## Quick start

**1. Add your `.resx` files** under any folder (typical: `Properties/Resources.resx`, `Properties/Resources.zh.resx`, …). Keep the
default `EmbeddedResource` build action.

**2. Configure namespaces** in the consuming project's `.csproj`:

```xml
<PropertyGroup>
  <XmlnsDefinitionNamespace>https://yourapp.example/loc</XmlnsDefinitionNamespace>
  <!-- Optional, defaults to RootNamespace -->
  <!-- <ResxNamespace>MyApp.Localization</ResxNamespace> -->
  <!-- Optional, defaults to the first .resx table -->
  <!-- <DefaultLocalizationProvider>Resources</DefaultLocalizationProvider> -->
</PropertyGroup>
```

**3. Use it in XAML:**

```xml
<UserControl xmlns="https://github.com/avaloniaui">
    <StackPanel>
        <TextBlock Text="{Localize Greeting}" />
        <TextBlock Text="{Localize SelectedCount, {Binding Count}}" />
        <TextBlock>
            <TextBlock.Text>
                <Localize Key="Downloading">
                    <Binding Path="FileName" />
                    <Binding Path="Size" />
                </Localize>
            </TextBlock.Text>
        </TextBlock>
    </StackPanel>
</UserControl>
```

Arguments follow the key: up to four inline, or any number as child elements. An argument can also be another
`{Localize ...}`.

`{Localize}` reads the default table. Every other table gets its own extension named after it, for example
`{LocalizeInteraction Notice}` for `Interaction.resx`. `{Localize}` works in styles (`<Setter Value="{Localize Greeting}" />`)
and data templates too. A formatted text updates when the language or any argument changes.

**4. Switch language at runtime** (on the UI thread):

```csharp
using System.Globalization;

LocalizationManager.Culture = CultureInfo.GetCultureInfo("zh");
// All `{Localize ...}` bindings and `LocalizedString` instances re-evaluate automatically.
```

The setter also sets `CultureInfo.CurrentUICulture`.

**5. Use it in code / bindings:**

```csharp
// Shared instance that notifies when the language changes; bind with {Binding Title.Value}
public LocalizedString Title => Resources.Greeting;

// Converts to the text in the current language, safe to read from any thread
string greeting = Resources.Greeting;

// A key known only at runtime returns the same shared instance; text that is not a key is shown unchanged
public LocalizedString DifficultyName => Resources.Get($"ChartDifficulty_{Difficulty}");

// Plain text that is never looked up as a key
public LocalizedString VersionText => LocalizedString.FromText(Version);

// A key with placeholders is a method; raise PropertyChanged for the property when an argument changes
public LocalizedString SelectedCountText => Resources.SelectedCount(SelectedCount);
```

A complete working example lives in [`sandbox/`](./sandbox).

## Fallback

A key missing from `XAML.zh-Hans.resx` comes from `XAML.zh.resx`, then from the neutral `XAML.resx`. The current culture is
matched through `CultureInfo.Parent`, so `zh-TW` uses the `zh-Hant` table if there is one, then `zh`, then the neutral table.

## Configuration reference

All configured via MSBuild properties on the consuming project:

| Property                      | Default                                    | Description                                                                                         |
|-------------------------------|--------------------------------------------|-----------------------------------------------------------------------------------------------------|
| `ResxNamespace`               | `$(RootNamespace)`                         | Namespace of the generated tables, `LocalizedString` and `LocalizationManager`                      |
| `XmlnsDefinitionNamespace`    | *(none — no XAML namespace is registered)* | URI used in `xmlns:loc="..."` for XAML imports, e.g. `x:DataType="loc:LocalizedString"`              |
| `DefaultLocalizationProvider` | First `.resx` table found                  | Table used by `{Localize}`                                                                          |

## Diagnostics

| ID      | Severity | Meaning                                                                                      |
|---------|----------|----------------------------------------------------------------------------------------------|
| RXL0001 | Warning  | A culture-specific `.resx` file has no neutral `.resx` file; it is ignored                   |
| RXL0002 | Warning  | A key in a culture-specific `.resx` file does not exist in the neutral file; it is ignored   |
| RXL0003 | Error    | `DefaultLocalizationProvider` does not match any neutral `.resx` file                        |
| RXL0004 | Error    | Two neutral `.resx` files have the same name                                                 |

## Building from source

```sh
dotnet build ResXLocalize.Avalonia.slnx
dotnet test --project tests/ResXLocalize.Avalonia.Tests
```

## License

MIT — see [LICENSE.txt](./LICENSE.txt).
