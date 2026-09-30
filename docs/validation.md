# Validation

Namespace: `AdfDotNet.Models` (`AdfDotNet.Core`).

```csharp
AdfValidationResult result = document.Validate();

if (!result.IsValid)
{
    foreach (string error in result.Errors)
        Console.WriteLine(error);
}
```

`AdfDocument.Validate()` checks the tree against the rules on each node's and mark's page in the ADF spec,
encoded as data in `AdfNodeSchema` (nodes) and `AdfMarkSchema` (marks). The checks cover nesting, child
counts, attributes and marks. Every node and mark type on the spec page has rules; the test suite builds one
document that uses all of them (`KitchenSinkTests`) and checks that it validates.

### Nesting and child counts

- Every node's type must be legal beneath its parent (e.g. a `table` can appear directly under `doc`, but
  not under a `listItem` or `blockquote`). The legal-child lists follow each node's page in the
  [ADF spec](https://developer.atlassian.com/cloud/jira/platform/apis/document/structure/), which is
  stricter than you might expect. A `blockquote` may hold only `paragraph`, `bulletList`, `orderedList`,
  `codeBlock`, `mediaSingle` and `mediaGroup`, so no `heading` and no nested `blockquote`. A `listItem` may
  hold only `paragraph`, `bulletList`, `orderedList`, `codeBlock` and `mediaSingle`, so no `blockquote`.
  An `expand` is legal only directly under `doc`. A `nestedExpand` is legal only in a `tableCell` or
  `tableHeader`, and may hold only `paragraph`, `heading`, `mediaGroup`, `mediaSingle` and `taskList`.
  `syncBlock`, `bodiedSyncBlock` and `multiBodiedExtension` are legal only under `doc` (and
  `multiBodiedExtension` under `expand`). An `extensionFrame` belongs in a `multiBodiedExtension` or an
  `expand`. `taskItem` and `blockTaskItem` are legal only in a `taskList`.
- The pages of `mediaInline`, `syncBlock`, `bodiedSyncBlock`, `multiBodiedExtension`, `extensionFrame` and
  `blockTaskItem` return 404, so their rules come from the ADF JSON schema instead. `taskList`/`taskItem`
  aren't on the spec page at all. They're modeled from the schema because `taskList` is the only parent of
  `blockTaskItem`, and they're legal wherever the schema allows them: `doc`, `listItem`, `panel`, table
  cells, `expand`, `nestedExpand`, `bodiedSyncBlock`, `extensionFrame` and another `taskList`.
- Only container node types may carry `Content`.
- Containers the spec says hold "one or more" nodes must not be empty. That covers `bulletList`,
  `orderedList`, `listItem`, `blockquote`, `panel`, `table`, `tableRow`, `tableCell`, `tableHeader`,
  `expand`, `nestedExpand`, `mediaGroup`, `bodiedSyncBlock`, `extensionFrame` and `taskList`.
  `mediaSingle` must hold exactly one `media`, and `blockTaskItem` one or two paragraphs. `taskItem` and
  `multiBodiedExtension` may be empty. `doc`, `paragraph` and `heading` may be empty. `codeBlock`
  may be empty too: its page says "one or more text nodes", but it also marks `content` as optional, and
  Jira's editor saves an empty code block that way.
- The root node must have type `doc` and `Version` 1.

### Attributes

- Required attributes must be present: `heading.level`, `panel.panelType`, `media.id`/`type`/`collection`,
  `mediaSingle.layout`, `mediaInline.id`/`collection`, `mention.id`, `emoji.shortName`, `date.timestamp`,
  `status.text`/`color`, `syncBlock`/`bodiedSyncBlock` `resourceId`/`localId`,
  `multiBodiedExtension.extensionKey`/`extensionType`, `taskList.localId`, `taskItem`/`blockTaskItem`
  `localId`/`state` (`TODO` or `DONE`), `link.href`, `subsup.type`, `textColor.color`, `backgroundColor.color` and `border.size`/`color`. `expand` and `nestedExpand` must
  have an `attrs` object, but it may be empty (`title` is optional).
- Values must have the right type, range or enum. Examples: `heading.level` 1–6; `panelType` one of
  `info`/`note`/`warning`/`success`/`error`; `status.color` a named color or a six-digit hex; `textColor`
  and `backgroundColor` a hex color, never a CSS name like `red`; `border.size` 1–3 and `border.color` a
  six- or eight-digit hex (from the JSON schema, since the `border` page returns 404); `date.timestamp` a string of digits; `mediaSingle.width` 0–100
  unless `widthType` is `pixel`.
- An `inlineCard` needs exactly one of `url` and `data`, and a `text` node needs non-empty text.
- Whole-number counts such as `level`, `order`, `colspan` and `rowspan` must be integers (`int` or `long`).
  Pixel sizes such as `table.width`, `colwidth` and `media.width`/`height` may be any positive number, since
  JSON doesn't distinguish `760` from `760.0`. A `colwidth` entry may also be `0`, which the table cell pages
  allow for a column with no fixed width.
- Attributes a spec page doesn't list are **not** reported. Real Jira documents carry extras such as
  `localId` on most block nodes.

### Marks

- Only `text` may carry the text marks (everything except `border`). `media` and `mediaInline` may carry
  `link` and `border`, and every other node type carries none. `AdfNodeSchema.IsValidMark(nodeType,
  markType)` exposes this table.
- Text inside a `codeBlock` may not carry marks.
- `code` combines only with `link`, `textColor` can't combine with `code` or `link`, and `backgroundColor`
  can't combine with `code`.
  `AdfMarkSchema.CanCombine(a, b)` exposes these rules.

`AdfValidationResult.IsValid` is `true` when `Errors` is empty; otherwise `Errors` is a list of
human-readable messages describing what's wrong and where.

## When to call it

**Nothing in this library calls `Validate()` for you.** It's opt-in by design — no converter, serializer,
or builder method invokes it automatically, and building an illegal tree (e.g. a `table` inside a
`listItem` via the [fluent builder](fluent-builder.md)) will not throw. Call `Validate()` explicitly at
whatever point in your code makes sense to catch structural mistakes — typically right after building a
document, or before serializing one you've constructed programmatically:

```csharp
var document = AdfDocumentBuilder.Build(doc => doc
    .Blockquote(bq => bq.Table(t => t.Row(r => r.Cell(c => c.Paragraph(p => p.Text("oops")))))));

var result = document.Validate(); // catches the illegal table-inside-blockquote nesting
```

Each error is a `path: message` string, where the path locates the node:

```text
doc/heading[0]: attribute 'level' on 'heading' must be a whole number from 1 to 6, but was 7.
doc/paragraph[1]/text[0]: mark 'code' cannot be combined with mark 'strong'.
doc/bulletList[2]: node of type 'bulletList' must have at least 1 child node(s), but has 0.
```

This applies equally regardless of how the tree was produced — HTML parsing, Markdown parsing, JSON
deserialization, the fluent builder, or manual `AdfNode` construction all produce a plain `AdfDocument`
tree that `Validate()` checks the same way.

The HTML and Markdown converters normalize their output (below), so its nesting and marks pass. Attribute
values copied from the input aren't fixed, so an unknown `data-panel-type` or a non-hex `data-status-color`
in HTML still fails `Validate()`.

## Normalizing a document

```csharp
document.Normalize(); // or AdfNormalizer.Normalize(anyNode)
```

`AdfNormalizer` reshapes a tree in place so its nesting and marks follow the same schema `Validate()`
checks. Both converters run it on their output, because HTML and Markdown allow nesting ADF doesn't
(`<blockquote><h2>`, `> > nested`, `- > quote`, a table inside a list item, ...). You can also run it on a
document you built or edited by hand. It works bottom-up:

- A child that isn't legal in its parent is reshaped:
  - a `heading` becomes a `paragraph` with the same content;
  - a `codeBlock` becomes a `paragraph` of `code`-marked text, with a `hardBreak` per line;
  - anything directly inside a `bulletList`/`orderedList` is wrapped in a `listItem`;
  - an `expand` becomes a `nestedExpand` where only that is legal (a table cell, another `expand`), and a
    `nestedExpand` becomes an `expand` at the top level. One that fits neither way (e.g. in a `blockquote`)
    is replaced by its children like any other container, with its title kept as a bold paragraph first;
  - loose inline nodes are gathered into one `paragraph`;
  - any other container (`blockquote`, `panel`, `table`, ...) is replaced by its own children, each placed
    by the same rules. A nested `blockquote` is flattened into its parent, and a table inside a list item
    becomes its cells' paragraphs;
  - any other leaf (e.g. a `rule` in a `blockquote`) is dropped.
- A container that must have children but ended up empty (`<ul></ul>`, `<table></table>`, a Markdown
  `> [!NOTE]` with no body) is dropped. A `listItem`, table cell or `blockTaskItem` gets an empty paragraph
  instead, so its list or table keeps its shape.
- A `blockTaskItem` with more than two paragraphs keeps two: the extra ones are appended to the second,
  each after a `hardBreak`.
- Empty `text` nodes are dropped (e.g. from an empty `<pre></pre>`).
- Marks: a `link` without a string `href` (an `<a>` with no `href`) is dropped, as is a mark the node can't
  carry or a repeat of a mark already on the node. When two marks can't combine, `code` wins over
  everything, then `link` (so `**bold `code`**` keeps `code` and `<a style="color:…">` keeps `link`);
  otherwise the earlier mark wins.

A document that already passes `Validate()` is left unchanged.

## Why validation isn't automatic

Enforcing legality *while* building (giving each container type its own builder class exposing only its
own legal child methods) was considered and rejected in favor of one shared builder type checked once at
the end — see [the fluent builder](fluent-builder.md) for why. The same reasoning extends to converters
and serializers: they trust their input's shape rather than re-validating it on every call, so a document
that's already known-good doesn't pay a validation cost on every conversion. (The HTML and Markdown
converters do normalize, since their input formats can't express ADF's nesting rules at all; the JSON
serializer doesn't, since its input is ADF already.)
