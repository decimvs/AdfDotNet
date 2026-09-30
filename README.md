# AdfDotNet

A .NET document model for the Atlassian Document Format (ADF) — the JSON structure Jira and Confluence
use for rich text. AdfDotNet gives you a typed ADF model, a fluent builder to compose documents in code,
and converters to move between ADF, HTML, Markdown, and JSON.

```csharp
var document = AdfDocumentBuilder.Build(doc => doc
    .Heading(1, "Hello World")
    .Paragraph(p => p
        .Text("This is ")
        .Text("bold", m => m.Strong())
        .Text(" and this is a ")
        .Text("link", m => m.Link("https://example.com"))
        .Text(".")));

string json = document.ToJson(prettyPrint: true);
```

## Packages

The library is split into a lightweight core plus opt-in satellite packages, so you only pull in what
you use. All packages target `netstandard2.0` (.NET Framework 4.6.2+, .NET Core 2.0+, .NET 5+).

| Package | What it's for | Depends on |
|---|---|---|
| `AdfDotNet.Core` | The ADF document model, validation against the ADF spec, and the fluent builder | HtmlAgilityPack |
| `AdfDotNet.Converters.Html` | HTML ↔ ADF conversion | Core, HtmlAgilityPack |
| `AdfDotNet.Converters.Markdown` | Markdown ↔ ADF conversion | Core, Markdig |
| `AdfDotNet.Json.Newtonsoft` | ADF ↔ JSON serialization | Core, Newtonsoft.Json |
| `AdfDotNet` | Meta-package: references Core + every satellite | all of the above |

## Getting started

Install the `AdfDotNet` meta-package to get the model, builder, and all three converters:

```
dotnet add package AdfDotNet
```

or install just `AdfDotNet.Core` plus the individual satellite(s) you need:

```
dotnet add package AdfDotNet.Core
dotnet add package AdfDotNet.Converters.Html
```

Every satellite works two ways:
- **Static, no DI required** — call `AdfHtmlConverter`/`AdfMarkdownConverter`/`AdfJsonConverter`-style
  static methods directly, good for scripts, console apps, and tests.
- **DI-registered** — call `services.AddAdfHtmlConverter()` / `AddAdfMarkdownConverter()` /
  `AddAdfNewtonsoftJson()` in an ASP.NET Core (or any `IServiceCollection`-based) app and inject
  `IAdfHtmlConverter` / `IAdfMarkdownConverter` / `IAdfDocumentSerializer`.

## Examples

### Build a document with the fluent builder

```csharp
using AdfDotNet.Builders;

var document = AdfDocumentBuilder.Build(doc => doc
    .Heading(1, "Release notes")
    .Paragraph(p => p.Text("Changes in this release:"))
    .BulletList(list => list
        .Item(i => i.Paragraph(p => p.Text("Fixed a bug")))
        .Item(i => i.Paragraph(p => p.Text("Added a feature"))))
    .Rule());

// Structural legality (e.g. which node types are allowed as children of which) is not enforced while
// building — call Validate() when you want to confirm the tree is well-formed.
var result = document.Validate();
if (!result.IsValid)
{
    foreach (var error in result.Errors)
        Console.WriteLine(error);
}
```

### Serialize to / from ADF JSON

```csharp
using AdfDotNet.DataConverters; // AdfJsonConverter, and the ToJson() extension method

string json = document.ToJson(prettyPrint: true);
AdfDocument roundTripped = AdfJsonConverter.Deserialize(json);
```

### Convert to and from HTML

```csharp
using AdfDotNet.FormatConverters;

string html = AdfToHtmlConverter.Convert(document);
AdfDocument fromHtml = HtmlToAdfConverter.Convert("<h1>Hello</h1><p>World</p>");
```

### Convert to and from Markdown

```csharp
using AdfDotNet.FormatConverters;

string markdown = AdfToMarkdownConverter.Convert(document);
AdfDocument fromMarkdown = MarkdownToAdfConverter.Convert("# Hello\n\nWorld");
```

### Register converters for dependency injection

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddAdfHtmlConverter();
services.AddAdfMarkdownConverter();
services.AddAdfNewtonsoftJson();

var provider = services.BuildServiceProvider();
var htmlConverter = provider.GetRequiredService<IAdfHtmlConverter>();
string html = htmlConverter.ConvertToHtml(document);
```

## ADF scope and converter limitations

ADF is a format defined by, and tied to, Jira Cloud/Confluence Cloud — some of it (e.g. `media`'s Jira
Media Services identifiers) is meaningless outside a Jira Cloud tenant. AdfDotNet's document model and
JSON serialization aim for full spec fidelity; the HTML and Markdown converters are convenience helpers for
the self-contained subset of ADF, not a complete Jira/Confluence renderer — some node types can't be
represented in HTML or Markdown at all, for reasons rooted in the format itself, not converter bugs.

### Document model + JSON: full spec coverage

`AdfNodeType`/`AdfMarkType` (`AdfDotNet.Core/Enums`) model every node and mark type on the [Jira ADF
spec](https://developer.atlassian.com/cloud/jira/platform/apis/document/structure/): 32 of 32 node types
and 10 of 10 marks. All of them can be built with the fluent builder, are checked by `Validate()`, and
round-trip losslessly through `AdfDotNet.Json.Newtonsoft`. A test builds one document containing every
type and checks all three.

**Node types**

| Category | Implemented |
|---|---|
| Root | `doc` |
| Top-level block | `blockquote`, `bodiedSyncBlock`, `bulletList`, `codeBlock`, `expand`, `heading`, `mediaGroup`, `mediaSingle`, `multiBodiedExtension`, `orderedList`, `panel`, `paragraph`, `rule`, `syncBlock`, `table` |
| Child block | `blockTaskItem`, `extensionFrame`, `listItem`, `media`, `nestedExpand`, `tableCell`, `tableHeader`, `tableRow` |
| Inline | `date`, `emoji`, `hardBreak`, `inlineCard`, `mediaInline`, `mention`, `status`, `text` |

The spec page lists `blockTaskItem` without saying what contains it. Its only parent in the ADF JSON schema
is `taskList`, so `taskList` and `taskItem` (ordinary checkbox lists) are modeled too, although they aren't
on the spec page.

**Marks**

| Implemented |
|---|
| `backgroundColor`, `border`, `code`, `em`, `link`, `strike`, `strong`, `subsup`, `textColor`, `underline` |

**What happens with a type this library doesn't model during JSON deserialization:** some types exist only
in the ADF JSON schema (`@atlaskit/adf-schema`) and not in the spec: `decisionList`, `layoutSection`,
`extension`, `blockCard`, the `alignment`/`indentation`/`breakout`/`annotation` marks, and so on. They occur
in Confluence content, but this library deliberately doesn't model them. They are **silently dropped** (a
node together with its content) and the rest of the document is kept, so such documents still load and what
remains is valid for Jira. A type name that isn't ADF at all (a typo, a non-ADF payload, genuinely malformed
input) still throws `JsonSerializationException`, so real mistakes keep failing loudly. See
`AdfSpecCoverage` in `AdfDotNet.Core/Models` for the registry backing this distinction, and [JSON
serialization](https://github.com/decimvs/AdfDotNet/blob/master/docs/json-serialization.md) for more detail.

### HTML and Markdown converters: inherent, permanent limitations

Every type the spec defines is handled by both converters, but not every type can be expressed in HTML or
Markdown. Each one either **round-trips**, renders **one-way** as something else (which is what comes
back), or is **dropped** (renders as nothing). Nothing throws. `KitchenSinkTests` checks every row below by
round-tripping one document that contains every node and mark type.

| Type | HTML | Markdown |
|---|---|---|
| Paragraphs, headings, lists, blockquotes, code blocks, rules, hard breaks, tables | round-trips | round-trips (a first table row always comes back as a header row) |
| `strong`, `em`, `code`, `link`, `strike` | round-trips | round-trips |
| `underline`, `subsup`, `textColor`, `backgroundColor` | round-trips (`<u>`, `<sub>`/`<sup>`, inline `style`) | dropped, text kept |
| `panel` | round-trips (`<div data-panel-type>`) | round-trips (`> [!NOTE]`-style alert) |
| `expand`, `nestedExpand` | round-trips (`<details>`/`<summary>`) | round-trips (raw-HTML `<details>` block); one-way as a bold title line in a table cell |
| `taskList`, `taskItem`, `blockTaskItem` | round-trips (`<ul data-adf-type="taskList">`) | round-trips as GFM `- [ ]`/`- [x]`, with new `localId`s; a one-paragraph `blockTaskItem` comes back as a `taskItem` |
| `mention`, `date`, `status` | round-trips (data attributes) | one-way, as text |
| `inlineCard` | one-way, as linked text | one-way, as linked text (an autolink) |
| `emoji` | one-way, as text | one-way, as text |
| `bodiedSyncBlock`, `multiBodiedExtension`, `extensionFrame` | one-way, as their body | one-way, as their body |
| `media`, `mediaSingle`, `mediaGroup`, `mediaInline`, `syncBlock`, the `border` mark | dropped | dropped |

Attributes: an ordered list's `order` and a link's `title` round-trip through both formats; table and cell
attrs (`colspan`/`rowspan`, cell `background`, `colwidth`, the table's `layout`/`width`/...) round-trip
through HTML only, since pipe tables can't carry them.

The gaps have two different causes:

- **Lacking context outside a Jira Cloud tenant.** `media`'s attrs (`id`/`type`/`collection`) are Jira Cloud
  Media Services identifiers, not a fetchable URL. There's nothing to render an `<img src>` or Markdown
  `![]()` from, and no way to derive those identifiers from an incoming `<img>`/image link either — so the
  media types are supported by the document model and JSON only. `syncBlock` and `bodiedSyncBlock`
  reference Jira-side resources, and `multiBodiedExtension`/`extensionFrame` Jira apps, so a body can be
  shown but not rebuilt into its wrapper.
- **Target format limitations.** Markdown/CommonMark has no syntax for the four decoration marks, mentions,
  dates or statuses, and GFM pipe tables require the delimiter row immediately after the first row.

Both are covered in more depth in [HTML conversion](https://github.com/decimvs/AdfDotNet/blob/master/docs/html-converter.md) and
[Markdown conversion](https://github.com/decimvs/AdfDotNet/blob/master/docs/markdown-converter.md).

Unmapped/unrecognized HTML tags and Markdown constructs are already dropped by both converters (not
thrown) — this is the existing, established fallback behavior for content the converters can't represent,
consistent with how the JSON layer handles schema-only ADF types above.

HTML and Markdown also nest more freely than ADF (a heading or a nested quote inside a blockquote, a quote
or table inside a list item, `code` inside bold text). Both converters reshape their output with
`AdfNormalizer` so its nesting and marks pass `Validate()`. Illegal headings become paragraphs, illegal
wrappers are unwrapped into their content, empty lists and tables are dropped, and `code`/`link` win mark
clashes. You can run the same pass on any document with `document.Normalize()`. See
[Validation](https://github.com/decimvs/AdfDotNet/blob/master/docs/validation.md#normalizing-a-document).

## Documentation

See [`docs/`](https://github.com/decimvs/AdfDotNet/tree/master/docs) for more depth:

- [Getting started](https://github.com/decimvs/AdfDotNet/blob/master/docs/getting-started.md) — installation, project layout, DI vs. static usage
- [The document model](https://github.com/decimvs/AdfDotNet/blob/master/docs/document-model.md) — `AdfNode`, `AdfDocument`, `AdfMark`, and the Attrs
  value contract
- [The fluent builder](https://github.com/decimvs/AdfDotNet/blob/master/docs/fluent-builder.md) — composing documents with `AdfDocumentBuilder`, marks,
  lists, and tables
- [HTML conversion](https://github.com/decimvs/AdfDotNet/blob/master/docs/html-converter.md)
- [Markdown conversion](https://github.com/decimvs/AdfDotNet/blob/master/docs/markdown-converter.md)
- [JSON serialization](https://github.com/decimvs/AdfDotNet/blob/master/docs/json-serialization.md)
- [Validation](https://github.com/decimvs/AdfDotNet/blob/master/docs/validation.md)

## Building from source

```
git clone https://github.com/decimvs/AdfDotNet.git
cd AdfDotNet
dotnet build AdfDotNet.slnx
```

`AdfDotNet.TestingApp` is a small console app for manually exercising the API — not an automated test
project. Run it with:

```
dotnet run --project AdfDotNet.TestingApp
```

The automated test suite lives in `AdfDotNet.Tests` (xUnit, targets `net10.0` and `net472`):

```
dotnet test AdfDotNet.slnx
```

## Author

Created and maintained by **Guillermo Espert Carrasquer**.

## License

Copyright (c) 2026 Guillermo Espert Carrasquer.

Licensed under the MIT License — see [LICENSE.txt](https://github.com/decimvs/AdfDotNet/blob/master/LICENSE.txt).
