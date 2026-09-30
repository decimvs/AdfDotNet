# Getting started

## What AdfDotNet is

AdfDotNet is a .NET document model for the Atlassian Document Format (ADF) — the JSON structure Jira and
Confluence use to represent rich text. It gives you:

- A typed object model (`AdfNode`, `AdfDocument`, `AdfMark`) instead of hand-rolled JSON.
- A fluent builder for composing documents in code.
- Converters between ADF and HTML, Markdown, and JSON.
- Structural validation of a document's tree.

## Project layout

The library is split into a lightweight core plus opt-in satellite packages:

```
AdfDotNet.Core                — the model, schema/validation, fluent builder. Its only third-party
                                 dependency is HtmlAgilityPack (see below).
AdfDotNet.Converters.Html      — HTML <-> ADF, built on HtmlAgilityPack.
AdfDotNet.Converters.Markdown  — Markdown <-> ADF, built on Markdig.
AdfDotNet.Json.Newtonsoft      — ADF <-> JSON, built on Newtonsoft.Json.
AdfDotNet                      — meta-package: no code of its own, references Core + every satellite.
```

You can reference just `AdfDotNet.Core` if all you need is to build and validate documents in memory, or
add whichever converter satellite you need, or take the `AdfDotNet` meta-package for everything at once.

> `AdfDotNet.Core` currently carries an `HtmlAgilityPack` package reference even though it has no HTML
> conversion code of its own — `AdfStructure`'s HTML-tag-to-ADF mapping table lives in Core and its
> attribute extractors are typed against `HtmlNode`.

## Installing

Install the `AdfDotNet` meta-package from NuGet to get everything:

```
dotnet add package AdfDotNet
```

or install only the packages you need:

```
dotnet add package AdfDotNet.Core
dotnet add package AdfDotNet.Converters.Html
dotnet add package AdfDotNet.Converters.Markdown
dotnet add package AdfDotNet.Json.Newtonsoft
```

All packages target `netstandard2.0`, so they work on .NET Framework 4.6.2+ and .NET (Core) 2.0+.

## Two ways to use each converter

Every satellite (`Converters.Html`, `Converters.Markdown`, `Json.Newtonsoft`) exposes the same
functionality two ways:

**1. Static, no setup required** — good for scripts, console apps, and tests:

```csharp
using AdfDotNet.FormatConverters;

string html = AdfToHtmlConverter.Convert(document);
AdfDocument fromHtml = HtmlToAdfConverter.Convert(htmlString);
```

**2. Dependency-injected** — good for ASP.NET Core or any app already using `IServiceCollection`:

```csharp
using Microsoft.Extensions.DependencyInjection;

services.AddAdfHtmlConverter();       // registers IAdfHtmlConverter
services.AddAdfMarkdownConverter();   // registers IAdfMarkdownConverter
services.AddAdfNewtonsoftJson();      // registers IAdfDocumentSerializer
```

```csharp
public class MyService
{
    private readonly IAdfHtmlConverter _htmlConverter;

    public MyService(IAdfHtmlConverter htmlConverter) => _htmlConverter = htmlConverter;

    public string Render(AdfDocument document) => _htmlConverter.ConvertToHtml(document);
}
```

Both paths call the same stateless implementation underneath, so there's no behavioral difference —
pick whichever fits how the rest of your app is wired.

## A minimal end-to-end example

```csharp
using AdfDotNet.Builders;
using AdfDotNet.DataConverters;   // ToJson() extension method
using AdfDotNet.FormatConverters; // AdfToHtmlConverter

var document = AdfDocumentBuilder.Build(doc => doc
    .Heading(1, "Hello World")
    .Paragraph(p => p.Text("This is a sample paragraph.")));

Console.WriteLine(document.ToJson(prettyPrint: true));
Console.WriteLine(AdfToHtmlConverter.Convert(document));
```

## Where to go next

- [The document model](document-model.md) — `AdfNode`, `AdfDocument`, `AdfMark`, and the Attrs value contract.
- [The fluent builder](fluent-builder.md) — composing documents, marks, lists, tables.
- [HTML conversion](html-converter.md)
- [Markdown conversion](markdown-converter.md)
- [JSON serialization](json-serialization.md)
- [Validation](validation.md)
