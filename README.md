# ResXLocalize.Avalonia

**A source-generator–based runtime localization library for Avalonia, backed by standard `.resx` files.**

*Built primarily for personal use — new features are added on demand. Contributions are welcome.*

## What it does

- Generates strongly-typed accessors for every key in your `.resx` files (à la the legacy
  `Resources.Designer.cs`, but at compile time via a Roslyn source generator).
- Generates a `LocalizeExtension` XAML markup extension so you can write
  `{Localize {x:Static loc:ResourcesLiteral.MyKey}}` and have the bound text update automatically when the language changes.
- Exposes a `LocalizationManager.Culture` setter that fires a `CultureChanged` event — flip it at runtime and all bound text refreshes.

Two flavors are published:

| Package                    | Notification mechanism                                                                                                                                    | 
|----------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------|
| `ResXLocalize.Avalonia`    | Plain `EventHandler` on `LocalizationManager.CultureChanged`                                                                                              |
| `ResXLocalize.Avalonia.R3` | Same surface, but the `LocalizedString` subscribes via [`R3.Observable.FromEventHandler`](https://github.com/Cysharp/R3) for unified reactive composition |

Pick whichever fits your stack. They are mutually exclusive — only reference one.

## Installation

```sh
dotnet add package ResXLocalize.Avalonia
```

Or, equivalently:

```xml
<PackageReference Include="ResXLocalize.Avalonia" Version="2.0.0-preview2" />
```

## Quick start

**1. Add your `.resx` files** under any folder (typical: `Properties/Resources.resx`, `Properties/Resources.zh.resx`, …). Use the default
`EmbeddedResource` build action.

**2. Configure namespaces** in the consuming project's `.csproj`:

```xml
<PropertyGroup>
  <XmlnsDefinitionNamespace>https://yourapp.example/loc</XmlnsDefinitionNamespace>
  <!-- Optional, defaults to RootNamespace -->
  <!-- <ResxNamespace>MyApp.Localization</ResxNamespace> -->
  <!-- Optional, defaults to the first .resx class encountered -->
  <!-- <DefaultLocalizationProvider>Resources</DefaultLocalizationProvider> -->
</PropertyGroup>
```

**3. Use it in XAML:**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:loc="https://yourapp.example/loc">
    <TextBlock Text="{Localize {x:Static loc:ResourcesLiteral.Greeting}}" />
</UserControl>
```

**4. Switch language at runtime:**

```csharp
using System.Globalization;

LocalizationManager.Culture = CultureInfo.GetCultureInfo("zh");
// All `{Localize ...}` bindings re-evaluate automatically.
```

**5. Use it in code / bindings:**

```csharp
// Implicit conversion from string key → tracked LocalizedString
public ResourcesLiteral.LocalizedString Greeting { get; set; } = ResourcesLiteral.Greeting;

// Or for one-off lookups that don't track:
string current = Resources.Greeting;
```

A complete working example lives in [`sandbox/`](./sandbox).

## Configuration reference

All configured via MSBuild properties on the consuming project:

| Property                      | Default                                    | Description                                                                             |
|-------------------------------|--------------------------------------------|-----------------------------------------------------------------------------------------|
| `ResxNamespace`               | `$(RootNamespace)`                         | Namespace of the generated `*Literal`, `LocalizationManager`, `LocalizeExtension` types |
| `XmlnsDefinitionNamespace`    | *(none — no XAML extension is registered)* | URI used in `xmlns:loc="..."` for XAML imports                                          |
| `DefaultLocalizationProvider` | First `.resx` class found                  | Class used when `{Localize}` is called without specifying a provider                    |

## Requirements

- .NET 9.0 or later on the consuming project
- `LangVersion=preview` (the generator emits code that uses the C# `field` keyword)
- Avalonia 12 preview2 or later

## Building from source

```sh
dotnet build ResXLocalize.Avalonia.slnx
```

## License

MIT — see [LICENSE.txt](./LICENSE.txt).
