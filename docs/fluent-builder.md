# The fluent builder

Namespace: `AdfDotNet.Builders` (`AdfDotNet.Core`).

The fluent builder composes an `AdfDocument` via nested lambdas that mirror how the ADF tree actually
nests, rather than manually calling `AdfNode.CreateXxx` and wiring `Content` lists together by hand.

```csharp
using AdfDotNet.Builders;

AdfDocument document = AdfDocumentBuilder.Build(doc => doc
    .Heading(1, "Title")
    .Paragraph(p => p
        .Text("Hello ")
        .Text("world", m => m.Strong().Em())
        .Text("!")));
```

`AdfDocumentBuilder.Build` creates a new document and hands you an `AdfBlockContentBuilder` for its
top-level content.

To add more top-level content to a document you already have — e.g. one just deserialized from stored ADF
JSON via `AdfJsonConverter.Deserialize`/`IAdfDocumentSerializer.Deserialize` — use `Extend` (appends after
the existing content) or `Prepend` (inserts before it). Both mutate the document in place and return it for
chaining:

```csharp
AdfDocument document = AdfJsonConverter.Deserialize(storedJson);

AdfDocumentBuilder.Extend(document, doc => doc
    .Rule()
    .Paragraph(p => p.Text("Appended after existing content")));

AdfDocumentBuilder.Prepend(document, doc => doc
    .Heading(1, "Prepended before existing content"));
```

`Prepend`'s new nodes keep the order they were built in — they aren't reversed one-at-a-time — so the
example above puts the heading first.

> **The builder does not enforce structural legality while you build.** You *can* build a `table` inside a
> `listItem`, even though that's not legal ADF — it will compile and the builder won't throw. Call
> `document.Validate()` afterwards if you need to confirm the tree is well-formed; see
> [Validation](validation.md). This is a deliberate design choice: one shared builder type per kind of
> content, with legality checked once at the end, instead of a proliferation of narrower builder types
> that each only expose their own legal methods.

## Block content — `AdfBlockContentBuilder`

Used for a document's top level, a list item's content, a blockquote's content, and a table cell/header's
content — the same builder type everywhere block content can appear:

```csharp
AdfDocumentBuilder.Build(doc => doc
    .Paragraph(p => p.Text("A paragraph"))
    .Heading(1, "A heading with plain text")
    .Heading(2, p => p.Text("A heading with ").Text("mixed formatting", m => m.Em())) // or nested inline content
    .BulletList(list => list.Item(i => i.Paragraph(p => p.Text("An item"))))
    .OrderedList(list => list.Item(i => i.Paragraph(p => p.Text("An item"))), order: 3) // order is optional; numbering starts at 1 without it
    .Blockquote(bq => bq.Paragraph(p => p.Text("A quote")))
    .Panel("warning", b => b.Paragraph(p => p.Text("Heads up"))) // paragraphs, headings and lists only
    .CodeBlock("var x = 1;", language: "csharp")
    .Rule()
    .MediaSingle("abc-123", "file", "my-collection", layout: "wide")
    .MediaGroup(g => g.Media("abc-123", "file", "my-collection").Media("def-456", "file", "my-collection"))
    .Expand("Details", e => e.Paragraph(p => p.Text("Hidden until expanded")))
    .Table(t => t.Row(r => r.Header(h => h
        .Paragraph(p => p.Text("Col 1"))
        .NestedExpand("More", n => n.Paragraph(p => p.Text("Inside a cell")))))));
```

Available methods: `Paragraph`, `Heading` (two overloads — plain text, or nested inline content),
`BulletList`, `OrderedList`, `Blockquote`, `Panel`, `CodeBlock`, `Rule`, `MediaSingle`, `MediaGroup`, `Expand`,
`NestedExpand`, `TaskList`, `SyncBlock`, `BodiedSyncBlock`, `MultiBodiedExtension`, `ExtensionFrame`, `Table`.
Each returns the same `AdfBlockContentBuilder` so calls chain.

`Expand(title, ...)` is only legal at the top level of a document. Inside a table cell or header, use
`NestedExpand(title, ...)`, which may only contain paragraphs, headings, media groups and media singles. The
title is optional (pass `null`). `MediaGroup` takes an `AdfMediaGroupBuilder` lambda whose only method is
`Media(id, type, collection, width, height, marks)`.

`MediaSingle(id, type, collection, layout, width, height, marks)` builds the `media` child (`id`/`type`/
`collection` are Jira Cloud Media Services identifiers per the ADF spec — `type` is `"file"` or `"link"`)
and wraps it in a `mediaSingle`, since the spec requires `mediaSingle`'s content to be exactly one `media`
node. There's no `url`-based overload — see [the document model](document-model.md#node-types) for why.

## Inline content — `AdfInlineContentBuilder`

Used for a paragraph's or heading's content — text runs, hard breaks, inline cards, emoji, mentions,
dates, status lozenges, and inline media:

```csharp
p => p
    .Text("plain text")
    .Text("marked text", m => m.Strong())
    .HardBreak()
    .Text("more text")
    .InlineCard("https://example.com")
    .Emoji(":grinning:", text: "\U0001F600")
    .Mention("5b10ac8d82e05b22cc7d4ef5", "@Jane Doe")
    .Date("1582152559")
    .Status("In Progress", "yellow")
    .MediaInline("abc-123", "my-collection", type: "file")
```

## Marks — `AdfMarkSetBuilder`

ADF represents marks as a flat `List<AdfMark>` per text node, so combining multiple marks (bold *and*
italic) on one run needs its own mechanism. `Text(string, Action<AdfMarkSetBuilder>)` takes a nested lambda
for exactly this:

```csharp
p.Text("bold and italic", m => m.Strong().Em())
```

`AdfMarkSetBuilder` methods:

```csharp
m.Strong();               // bold
m.Em();                   // italic
m.Code();                 // inline code
m.Strike();                // strikethrough
m.Underline();
m.Link("https://example.com");   // carries the href attribute
m.Link("https://example.com", title: "Example"); // plus an optional title
m.TextColor("#ff0000");          // carries the color attribute (hex or CSS color name)
m.SubSup("sub");                 // or "sup" — carries the type attribute
m.BackgroundColor("#fedec8");    // highlight; hex color, can't combine with Code()
m.Border(2, "#091e4224");        // size 1-3 and a six- or eight-digit hex; media only (see below)
```

`Border` is only legal on `media` and `mediaInline`, so it goes in the optional `marks` lambda that
`MediaSingle`, `AdfMediaGroupBuilder.Media` and `MediaInline` take (as does `Link`, the other mark media
may carry):

```csharp
doc.MediaSingle("abc-123", "file", "my-collection", marks: m => m.Border(2, "#091e4224"));
p.MediaInline("abc-123", "my-collection", marks: m => m.Border(1, "#ff0000").Link("https://example.com"));
```

All of them return the same `AdfMarkSetBuilder`, so they chain: `m.Strong().Em().Link("...")`.

> ADF restricts some combinations: `code` combines only with `link`, `textColor` not with `code` or `link`,
> and `backgroundColor` not with `code`. The builder won't stop you, but the document fails `Validate()`.
> The HTML and Markdown converters resolve these clashes for you via `AdfNormalizer`; the builder doesn't.
> Call `document.Normalize()` on a built document if you want the same cleanup (see
> [validation](validation.md#normalizing-a-document)).

## Lists — `AdfListBuilder`

Used inside `BulletList(...)`/`OrderedList(...)`:

```csharp
list => list
    .Item(i => i.Paragraph(p => p.Text("First item")))
    .Item(i => i.Paragraph(p => p.Text("Second item"))
                .BulletList(nested => nested.Item(i2 => i2.Paragraph(p => p.Text("Nested item")))))
```

`Item` takes an `Action<AdfBlockContentBuilder>` — a list item's content is block content, so it can
contain more than just a paragraph (including a nested list, as above).

## Tables — `AdfTableBuilder` / `AdfTableRowBuilder`

```csharp
t => t
    .Row(r => r
        .Header(h => h.Paragraph(p => p.Text("Name")))
        .Header(h => h.Paragraph(p => p.Text("Value"))))
    .Row(r => r
        .Cell(c => c.Paragraph(p => p.Text("foo")))
        .Cell(c => c.Paragraph(p => p.Text("bar"))))
```

`Row` takes an `Action<AdfTableRowBuilder>`; `Cell`/`Header` each take an `Action<AdfBlockContentBuilder>`
(a cell's content is block content, same as a list item's).

## Task lists — `AdfTaskListBuilder`

Used inside `TaskList(...)`:

```csharp
doc => doc.TaskList(t => t
    .Item(p => p.Text("Open task"))
    .Item(p => p.Text("Finished task"), done: true)
    .BlockItem(b => b.Paragraph(p => p.Text("A task whose content is paragraphs")))
    .List(nested => nested.Item(p => p.Text("Indented task"))))
```

`Item` builds a `taskItem` (inline content), `BlockItem` a `blockTaskItem` (one or two paragraphs), and
`List` a nested `taskList`. Every one takes an optional `localId`; when you leave it out, a new GUID is used.

## Sync blocks and extensions

```csharp
doc => doc
    .SyncBlock("resource-id")
    .BodiedSyncBlock("resource-id", b => b.Paragraph(p => p.Text("Synced content")))
    .MultiBodiedExtension("tabs", "com.example.tabs", f => f
        .Frame(b => b.Paragraph(p => p.Text("Tab one")))
        .Frame(b => b.Paragraph(p => p.Text("Tab two")))))
```

`MultiBodiedExtension` takes an `AdfExtensionFrameBuilder` whose only method is `Frame(...)`. For
`parameters`, `text` or `layout`, use `AdfNode.CreateMultiBodiedExtension` and add the node with
`AddContent`.

## What's not covered

Every node type with an `AdfNode.CreateXxx` factory has fluent-builder support. If a future node type is
added to `AdfNodeType` without a factory (see [the document model](document-model.md#factory-methods)),
construct the `AdfNode` manually and add it with `node.AddContent(...)` until one exists.
