# Markdown conversion

Package: `AdfDotNet.Converters.Markdown`. Namespace: `AdfDotNet.FormatConverters`. Built on
[Markdig](https://github.com/xoofx/markdig).

## Static API

```csharp
using AdfDotNet.FormatConverters;

string markdown = AdfToMarkdownConverter.Convert(document);        // AdfDocument -> Markdown
string markdown2 = AdfToMarkdownConverter.Convert(adfJsonString);   // ADF JSON -> Markdown, in one step
AdfDocument fromMarkdown = MarkdownToAdfConverter.Convert(markdownString); // Markdown -> AdfDocument
```

## Dependency injection

```csharp
services.AddAdfMarkdownConverter(); // registers IAdfMarkdownConverter as a singleton
```

```csharp
public interface IAdfMarkdownConverter
{
    string ConvertToMarkdown(AdfDocument adfDocument);
    string ConvertToMarkdown(string adfJson);
    AdfDocument ConvertToAdf(string markdown);
}
```

## Nesting and marks

Markdown nests more freely than ADF, so `MarkdownToAdfConverter` normalizes its output (`AdfNormalizer`,
see [Validation](validation.md#normalizing-a-document)) and the result's nesting and marks pass
`Validate()`. `> # Heading` gives a paragraph inside the quote, `> > nested` is flattened into the outer
quote, `- > quote` gives the quoted paragraph directly in the list item, and a `> [!NOTE]` alert with no
body is dropped. `**bold `code`**` keeps only `code` on the code span, since `code` combines only with
`link`.

## Known format gaps

These aren't converter bugs — they're inherent limitations of what Markdown/CommonMark can represent, so
they're worth knowing about rather than being surprised by:

- **`Underline`, `SubSup`, `TextColor`, and `BackgroundColor` marks are not supported.** Markdown has no
  native syntax for any of the four (unlike HTML's `<u>`, `<sub>`/`<sup>`, and inline `style`). Text content survives a
  round-trip; the mark itself is dropped. (Rendering them via raw inline-HTML passthrough was considered
  and rejected — Markdig returns raw HTML tokens as a flat, unpaired sequence, which would need new
  tag-pairing logic to reconstruct correctly.) The one raw inline tag that is read is `<br>`, which needs no
  pairing: it becomes a `hardBreak`, since that's how the converter separates blocks in a table cell.
- **A table's first row always becomes its header row.** GitHub-flavored-Markdown pipe tables require the
  delimiter row immediately after the first row — there's no valid pipe-table shape where the first row
  isn't the header. So a table whose first ADF row is `tableCell` (not `tableHeader`) round-trips with that
  row reclassified as `tableHeader`.
- **Table attributes are lost.** Pipe tables have no spans, cell colors or widths, so the table's
  `layout`/`width`/`displayMode`/`isNumberColumnEnabled` and the cells' `colspan`/`rowspan`/`background`/
  `colwidth` are dropped; the text survives. Use HTML if you need them. (An ordered list's `order` and a
  link's `title` do survive: `3. item` starts the list at 3, and `[text](<url> "title")` carries the
  title. A list starting at 1 comes back without `order`, since 1 is the default.)
- **`mediaSingle`/`media` have no Markdown representation at all**, in either direction. Per the ADF spec,
  `media`'s attrs (`id`/`type`/`collection`) are Jira Cloud Media Services identifiers, not a fetchable
  URL — there's nothing to render a Markdown image from, and no `id`/`collection` derivable from incoming
  `![alt](url)` syntax either. `MarkdownToAdfConverter` drops Markdown images (an explicit no-op, so they
  don't get misread as plain links); `AdfToMarkdownConverter` has no `MediaSingle` render case, so it
  produces empty output for this node type. This is a permanent format limitation, not a converter bug —
  see [the document model](document-model.md#node-types). `mediaGroup` and `mediaInline` are the same.
- **`expand`/`nestedExpand` render as a raw-HTML `<details>` block**, the way GitHub writes collapsible
  sections, and parse back:

  ```markdown
  <details><summary>Title</summary>

  Markdown content, still parsed as Markdown.

  </details>
  ```

  `MarkdownToAdfConverter` pairs the opening and closing HTML blocks, including nested pairs, and also
  accepts a `<details>` written without blank lines. The type follows placement, as in HTML: a
  `<details>` inside another becomes a `nestedExpand`. A `<details>` with no closing tag is ignored and its
  content kept. **Exception:** a pipe-table cell can't hold an HTML block, so a `nestedExpand` in a table
  cell renders one-way as its bold title followed by its content (`**Title**<br>content`), which comes back
  as a paragraph with the bold title, a line break, and the content.
- **Task lists render as GFM task lists** and parse back:

  ```markdown
  - [ ] Write tests
  - [x] Review
    - [ ] Nested task
  - [x] Block task item

    Its second paragraph
  ```

  Markdown only nests a list inside an item, so a nested `taskList` is indented under the item before it
  and comes back as a nested `taskList` after that item. Three things don't survive:
  - **`localId`s.** Markdown can't carry them, so parsing gives every `taskList`/`taskItem`/`blockTaskItem`
    a new GUID, as the factories do.
  - **A `blockTaskItem` with one paragraph.** It looks the same as a `taskItem`, so it comes back as one.
    An item with more than one block becomes a `blockTaskItem`. A block that isn't a paragraph is reshaped
    by the normalizer, and paragraphs past the second are joined onto it with line breaks.
  - **Mixed lists.** A list where only some items have a `[ ]` marker stays a `bulletList`.

  A task list in a pipe-table cell renders one-way as text.
- **`syncBlock`, `bodiedSyncBlock`, `multiBodiedExtension` and `extensionFrame` have no Markdown form.** A
  `bodiedSyncBlock`'s or frame's content renders as plain blocks (every frame of a `multiBodiedExtension`,
  one after another), and a `syncBlock` renders as nothing. This is one-way: the body parses back as
  ordinary blocks, without the wrapper.
- **`inlineCard` and `emoji` render to Markdown but don't parse back.** `AdfToMarkdownConverter` renders
  both (an inline card as an autolink, an emoji as its `text`/`shortName`), but no Markdown source
  construct maps to either on the way in — they're one-directional (ADF → Markdown only). An autolink
  (`<https://…>`, or `<user@example.com>` as a `mailto:` link) comes back as its address with a `link`
  mark, and an emoji as plain text.
- **`mention`, `date` and `status` render as plain text.** Markdown has no syntax for them, so a mention
  becomes its `@Name` text, a date `yyyy-MM-dd` (UTC), and a status its text — all parse back as ordinary
  text (ADF → Markdown only).
- **`panel` renders as a GitHub-flavored-Markdown alert** and parses back to the same panel type, using a
  one-to-one mapping: `info` ↔ `[!NOTE]`, `note` ↔ `[!IMPORTANT]`, `success` ↔ `[!TIP]`,
  `warning` ↔ `[!WARNING]`, `error` ↔ `[!CAUTION]`:

  ```markdown
  > [!WARNING]
  > Deploys are frozen until Friday.
  ```

## Round-trip expectations

ADF → Markdown → ADF is expected to be structurally lossless for every node/mark type
`AdfMarkdownStructure` knows about, modulo the gaps listed above (see `AdfDotNet.Tests`'s
`MarkdownNodeTypeRoundTripTests`/`MarkdownMarkRoundTripTests`).

Every node and mark type at a glance (checked by `KitchenSinkTests`, which round-trips one document
containing all of them):

| Fate | Types |
|---|---|
| Round-trips | every node except the ones below; `strong`, `em`, `code`, `link`, `strike` |
| One-way | `inlineCard` (linked text); `emoji`, `mention`, `date`, `status` (text); `nestedExpand` in a table cell (bold title line); a one-paragraph `blockTaskItem` (a `taskItem`); `bodiedSyncBlock`/`multiBodiedExtension`/`extensionFrame` (their body) |
| Dropped | `media`, `mediaSingle`, `mediaGroup`, `mediaInline`, `syncBlock`; the `underline`, `subsup`, `textColor`, `backgroundColor` and `border` marks |

Task-list `localId`s are regenerated rather than kept.
