# HTML conversion

Package: `AdfDotNet.Converters.Html`. Namespace: `AdfDotNet.FormatConverters`.

## Static API

```csharp
using AdfDotNet.FormatConverters;

string html = AdfToHtmlConverter.Convert(document);       // AdfDocument -> HTML
string html2 = AdfToHtmlConverter.Convert(adfJsonString);  // ADF JSON -> HTML, in one step
AdfDocument fromHtml = HtmlToAdfConverter.Convert(htmlString); // HTML -> AdfDocument
```

## Dependency injection

```csharp
services.AddAdfHtmlConverter(); // registers IAdfHtmlConverter as a singleton
```

```csharp
public interface IAdfHtmlConverter
{
    string ConvertToHtml(AdfDocument adfDocument);
    string ConvertToHtml(string adfJson);
    AdfDocument ConvertToAdf(string html);
}
```

## ADF → HTML

`AdfToHtmlConverter` walks the `AdfDocument`/`AdfNode` tree and renders HTML by string concatenation — no
templating library involved. Text and attribute values are HTML-encoded at the boundary.

## HTML → ADF

`HtmlToAdfConverter` parses the HTML via HtmlAgilityPack and walks the DOM. A few things worth knowing:

- **Tag mapping** comes from `AdfStructure` (`AdfDotNet.Core`) — the table of which HTML tags map to which
  ADF node types and marks. Tags not in that table fall back to being treated as inline/unknown content.
- **Nested block content in list items and table cells/headers is preserved.** An `<li>` containing a
  nested `<ul>`, for example, round-trips as a `listItem` whose content includes both a paragraph and a
  nested `bulletList`, rather than collapsing everything into a single paragraph.
- **Marks are resolved by walking up the ancestor chain** from each text node — so `<strong><em>text</em>
  </strong>` correctly picks up both `Strong` and `Em`. Inline `style` attributes (`font-weight`,
  `font-style`, `text-decoration`, `color`, `background-color`) are also read and mapped to the same mark
  types.
- **The output is normalized** (`AdfNormalizer`, see [Validation](validation.md#normalizing-a-document)), so
  its nesting and marks pass `Validate()` even when the HTML nests things ADF doesn't allow:
  `<blockquote><h2>` gives a paragraph, a nested `<blockquote>` is flattened into its parent, a
  `<blockquote>` or `<table>` inside an `<li>` is unwrapped into its content, and an `<hr>` inside a
  `<blockquote>` is dropped. Empty `<ul>`/`<ol>`/`<table>` elements are dropped. When marks can't combine,
  `code` wins over everything else and `link` over `textColor` (`<a href style="color:…">` keeps only the
  link). An `<a>` without `href` gives plain text. Attribute values aren't fixed: an unknown
  `data-panel-type`, for example, still fails `Validate()`.
- **Wrapper elements are flattened.** A `<div>`, `<section>`, `<article>`, `<main>` (or any other wrapper)
  around block content has no ADF equivalent, so its children are lifted into the surrounding container —
  `<div><h2>A</h2><p>B</p></div>` becomes a heading and a paragraph. A `<div>` with only inline content
  becomes a paragraph.
- **Loose inline content is grouped into paragraphs.** Text and inline elements sitting between blocks
  (e.g. `<li><strong>Parent</strong> item<ul>…</ul></li>`, or bare text in `<body>`) are gathered into one
  paragraph per run, so the result always passes `Validate()`. Whitespace between inline elements is kept
  as a single space; whitespace used only for indentation is dropped.
- **`<hr>` tags** are pre-escaped to a synthetic `<hrbr>` tag before parsing to work around an
  HtmlAgilityPack void-element quirk, then converted to `AdfNodeType.Rule` as expected — this is an
  internal implementation detail, not something you need to do yourself.
- **Character references are decoded** with `WebUtility.HtmlDecode`: named (`&amp;`, `&nbsp;`), decimal
  and hex, including characters outside the Basic Multilingual Plane such as `&#128512;` (😀), which
  `AdfToHtmlConverter` writes for emoji on .NET Core.
- **`mediaSingle`/`media` have no HTML representation at all**, in either direction. Per the ADF spec,
  `media`'s attrs (`id`/`type`/`collection`) are Jira Cloud Media Services identifiers, not a fetchable
  URL — there's no `id`/`collection` derivable from an incoming `<img src>`, and nothing to build an
  `<img>` tag from on the way out. `<img>` isn't in `HtmlToAdfConverter`'s tag table (it falls back to the
  unknown-element path); `AdfToHtmlConverter` produces empty output for this node type. This is a
  permanent format limitation, not a converter bug — see [the document model](document-model.md#node-types).
  `mediaGroup` and `mediaInline` are the same: a `mediaGroup` renders as nothing, and so does a
  `mediaInline`. The `border` mark, which only decorates media, goes with them.
- **`backgroundColor` renders as `<span style="background-color: #fedec8">`** and round-trips. On the way
  in, a `background-color` style (or a `background` shorthand whose whole value is a color) gives the mark;
  like `color`, the value must be a hex code or one of the few named colors `TextUtils` knows, so `rgb(…)`
  and `transparent` are ignored. A `<mark>` element also gives the mark, with `#f8e6a0`
  (`AdfStructure.DefaultHighlightColor`, "Yellow - light" from the editor's text background palette, which
  the spec recommends) unless the `<mark>` has a `background-color` style of its own. When backgrounds are
  nested, the innermost wins. On `<code>` the background is dropped, since ADF doesn't allow `code` with
  `backgroundColor`.
- **Attributes of lists, links and tables round-trip.** An ordered list's `order` is `<ol start>`; a link's
  `title` is `<a title>`; a table cell's or header's `colspan`/`rowspan` are the native attributes, its
  `background` a `background-color` style, and its `colwidth` a comma-separated `data-colwidth`. The table's
  own attrs have no HTML equivalent, so they're `data-layout`, `data-width`, `data-display-mode` and
  `data-number-column-enabled`:

  ```html
  <table data-layout="center" data-width="900" data-number-column-enabled="true">
    <tr><th colspan="2" style="background-color: #deebff" data-colwidth="150,200">…</th></tr>
  </table>
  ```

  On the way in, a `<td>`/`<th>`'s `background-color` style, a `background` shorthand that's only a color,
  or a legacy `bgcolor` gives the cell's `background` (a short or long hex, or a color name, as the spec
  allows), not a highlight mark on its text. Values ADF can't hold are skipped rather than copied: a
  negative `start`, a `colspan` of 0, a non-numeric width, a background such as `rgb(…)` or `transparent`.
- **`expand`/`nestedExpand` render as `<details>`**:
  `<details data-adf-type="expand"><summary>Title</summary>…</details>` (`data-adf-type="nestedExpand"` for
  a nested expand; no `<summary>` when there's no title). They round-trip. A plain `<details>` from any
  other HTML is parsed too, and its type follows its placement: an `expand` at the top level, a
  `nestedExpand` in a table cell or inside another `<details>`. The title is the summary's plain text, so
  markup inside `<summary>` is lost. A `<details>` where neither type is legal (e.g. in a `<blockquote>`) is
  unwrapped, and its title becomes a bold paragraph.
- **Task lists render as checkbox lists** and round-trip with every attr:

  ```html
  <ul data-adf-type="taskList" data-local-id="…">
    <li data-task-state="TODO" data-local-id="…"><input type="checkbox" disabled/>Write tests</li>
    <li data-adf-type="blockTaskItem" data-task-state="DONE" data-local-id="…"><input type="checkbox" checked disabled/><p>Ship it</p></li>
  </ul>
  ```

  A `taskItem`'s inline content follows the checkbox directly; a `blockTaskItem` keeps its `<p>`s. A nested
  `taskList` renders directly inside its parent `<ul>`, the way ADF nests it. On the way in, a `<ul>`/`<ol>`
  is a task list if it has `data-adf-type="taskList"` or if every `<li>` starts with a checkbox (the shape
  GitHub and Markdig render, including a loose item's `<p><input …>`). Without `data-task-state`, a checked
  checkbox means `DONE`. A task list inside an `<li>` becomes a nested `taskList` after that item. A missing
  `data-local-id` gets a new GUID. Items are `taskItem`s unless they carry
  `data-adf-type="blockTaskItem"`. A block item with more than two paragraphs keeps two, and the extra ones
  are joined onto the second with line breaks. A list where only some items have a checkbox stays a
  `bulletList`.
- **`syncBlock`, `bodiedSyncBlock`, `multiBodiedExtension` and `extensionFrame` have no HTML form.** They
  reference Jira-side resources and apps. A `bodiedSyncBlock`'s or frame's content renders as plain blocks
  (every frame of a `multiBodiedExtension`, one after another), and a `syncBlock` renders as nothing. This
  is one-way: the body parses back as ordinary blocks, without the wrapper.
- **`inlineCard` and `emoji` render to HTML but don't parse back.** `AdfToHtmlConverter` renders both (an
  inline card as a link, an emoji as its `text`/`shortName`), but no HTML tag maps to either on the way
  in — they're one-directional (ADF → HTML only). An inline card comes back as its URL with a `link` mark,
  and an emoji as plain text.
- **`panel`, `mention`, `date` and `status` use data attributes.** HTML has no native equivalent, so a
  panel renders as `<div data-panel-type="info">…</div>`, a mention as
  `<span data-mention-id="…" data-access-level="…" data-user-type="…">@Name</span>`, a date as
  `<time data-timestamp="1582152559">2020-02-19</time>` (UTC), and a status as
  `<span data-status-color="yellow" data-local-id="…">In Progress</span>` (optional attributes only when
  set). `HtmlToAdfConverter` recognizes all four shapes, so they round-trip. The span's text becomes
  the mention's `text` attribute, so a mention created without `text` comes back with `text: "@<id>"`.

## Round-trip expectations

ADF → HTML → ADF is expected to be structurally lossless for every node/mark type `AdfStructure` knows
about (see `AdfDotNet.Tests`'s `AdfHtmlRoundTripTests`/`NodeTypeRoundTripTests`/`MarkRoundTripTests`).
Parsing arbitrary HTML you didn't generate yourself is inherently lossier — unknown tags keep their text
but lose any meaning beyond the marks and structure described above.

Every node and mark type at a glance (checked by `KitchenSinkTests`, which round-trips one document
containing all of them):

| Fate | Types |
|---|---|
| Round-trips | every node except the ones below; every mark except `border` |
| One-way | `inlineCard` (linked text), `emoji` (text), `bodiedSyncBlock`/`multiBodiedExtension`/`extensionFrame` (their body) |
| Dropped | `media`, `mediaSingle`, `mediaGroup`, `mediaInline`, `syncBlock`; the `border` mark |
