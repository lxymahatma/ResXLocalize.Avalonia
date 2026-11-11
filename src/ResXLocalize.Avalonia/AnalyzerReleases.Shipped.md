; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 3.0.0

### New Rules

Rule ID | Category     | Severity | Notes
--------|--------------|----------|-------------------------------------------------------
RXL0001 | ResXLocalize | Warning  | Culture-specific resource file has no neutral resource file
RXL0002 | ResXLocalize | Warning  | Key is missing from the neutral resource file
RXL0003 | ResXLocalize | Error    | DefaultLocalizationProvider does not match a resource file
RXL0004 | ResXLocalize | Error    | Multiple neutral resource files share a name
