# The document model

Namespace: `AdfDotNet.Models` (`AdfDotNet.Core`).

## AdfNode

Every ADF node — paragraph, heading, text, list, table, and so on — is represented by a single class,
`AdfNode`. There's no per-node-type subclass; instead, `AdfNode` has:

```csharp
public class AdfNode
{
    public AdfNodeType Type { get; set; }
    public Dictionary<string, object>? Attrs { get; set; }
    public List<AdfNode>? Content { get; set; }
    public string? Text { get; set; }
    public List<AdfMark>? Marks { get; set; }
}
```

- `Type` says what kind of node this is (`AdfNodeType.Paragraph`, `AdfNodeType.Heading`, etc.).
- `Attrs` carries type-specific data — a heading's `level`, a code block's `language`, a media node's
  `id`/`type`/`collection`.
- `Content` holds child nodes, for container types (paragraph, list, table, ...).
- `Text` holds the literal text, only meaningful on `AdfNodeType.Text` nodes.
- `Marks` holds the formatting marks applied to a text run (see [AdfMark](#adfmark) below).

### Node types

`AdfNodeType` (`AdfDotNet.Enums`) currently covers: `Doc`, `Paragraph`, `Text`, `Heading`, `BulletList`,
`OrderedList`, `ListItem`, `Blockquote`, `CodeBlock`, `Rule`, `HardBreak`, `InlineCard`, `Table`,
`TableRow`, `TableCell`, `TableHeader`, `MediaSingle`, `Media`, `Emoji`, `Panel`, `Mention`, `Date`, `Status`,
`Expand`, `NestedExpand`, `MediaGroup`, `MediaInline`, `SyncBlock`, `BodiedSyncBlock`, `MultiBodiedExtension`,
`ExtensionFrame`, `TaskList`, `TaskItem`, `BlockTaskItem` — every node type on the spec page, plus
`TaskList`/`TaskItem` from the ADF JSON schema (the only parent of `BlockTaskItem`). See the [README's ADF
scope section](../README.md#adf-scope-and-converter-limitations) for the node/mark type checklist and how
deserializing a schema-only type (e.g. `decisionList`) behaves.

> `Media`/`MediaSingle` (and `MediaGroup`/`MediaInline`) carry Jira Cloud Media Services identifiers (`id`/`type`/`collection`), per the ADF
> spec — not a fetchable URL. That means the HTML and Markdown converters can't render or parse this node
> type at all (there's nothing to build an `<img src>`/`![]()` from, and no source to derive `id`/`collection`
> from either); only the document model, `Validate()`, and JSON serialization support it. See
> [HTML conversion](html-converter.md) and [Markdown conversion](markdown-converter.md).

### Factory methods

Rather than `new AdfNode(...)` plus manually wiring up `Attrs`/`Content`, use the `CreateXxx` static
factories:

```csharp
AdfNode.CreateDocument(content);
AdfNode.CreateParagraph(content);
AdfNode.CreateText(text, marks);
AdfNode.CreateHeading(level, content);        // or CreateHeading(level, "plain text")
AdfNode.CreateBulletList(content);
AdfNode.CreateOrderedList(content);
AdfNode.CreateListItem(content);
AdfNode.CreateBlockquote(content);
AdfNode.CreateCodeBlock(language, content);
AdfNode.CreateRule();
AdfNode.CreateHardBreak();
AdfNode.CreateMedia(id, type, collection, width, height, occurrenceKey, alt);
AdfNode.CreateMediaSingle(media, layout: "center", width, widthType);
AdfNode.CreateInlineCard(url);
AdfNode.CreateEmoji(shortName, id, text);
AdfNode.CreateMention(id, text, accessLevel, userType);
AdfNode.CreateDate(timestamp);             // Unix timestamp string, e.g. "1582152559"
AdfNode.CreateStatus(text, color, localId); // color: "neutral" | "purple" | "blue" | "red" | "yellow" | "green" | "#RRGGBB"
AdfNode.CreatePanel(panelType, content);   // panelType: "info" | "note" | "warning" | "success" | "error"
AdfNode.CreateExpand(title, content);       // top level only; title is optional
AdfNode.CreateNestedExpand(title, content); // only inside a tableCell/tableHeader
AdfNode.CreateMediaGroup(content);          // one or more media nodes
AdfNode.CreateMediaInline(id, collection, type, width, height, alt, occurrenceKey, localId); // type: "file" | "link" | "image"
AdfNode.CreateSyncBlock(resourceId, localId);
AdfNode.CreateBodiedSyncBlock(resourceId, content, localId);
AdfNode.CreateMultiBodiedExtension(extensionKey, extensionType, content, parameters, text, layout, localId);
AdfNode.CreateExtensionFrame(content);
AdfNode.CreateTaskList(content, localId);
AdfNode.CreateTaskItem(state, content, localId);      // state: "TODO" | "DONE"; content is inline
AdfNode.CreateBlockTaskItem(state, content, localId); // content is one or two paragraphs
AdfNode.CreateTable(content);
AdfNode.CreateTableRow(content);
AdfNode.CreateTableCell(content);
AdfNode.CreateTableHeader(content);
```

`type`, `collection`, `id`, `shortName`, and `url` above are ADF-spec-mandated strings (e.g. `media`'s
`type` is `"file"` or `"link"`) — see [the ADF spec](https://developer.atlassian.com/cloud/jira/platform/apis/document/structure/)
for the exact value sets. `CreateMediaSingle` takes the `media` node it wraps — per the spec, a
`mediaSingle`'s content must be exactly one `media` node:

```csharp
AdfNode media = AdfNode.CreateMedia("abc-123", "file", "my-collection");
AdfNode mediaSingle = AdfNode.CreateMediaSingle(media, layout: "wide");
```

The sync block and task factories generate a new GUID for a required `localId` you leave out.

Every `content` parameter is optional and defaults to an empty list; you can also build the list yourself
and pass it in, or call `node.AddContent(childNode)` afterwards.

For most day-to-day document construction, prefer the [fluent builder](fluent-builder.md) over calling
these factories directly — it reads more naturally for nested content and handles mark composition for you.

### `CanHaveChildren()` / `IsLeafNode()`

```csharp
bool canHaveChildren = node.CanHaveChildren();
bool isLeaf = node.IsLeafNode();
```

Both delegate to `AdfNodeSchema` (see below) — they tell you whether a given node type is a container
(paragraph, list, table, ...) or a leaf (text, rule, hard break, ...).

## AdfDocument

```csharp
public class AdfDocument : AdfNode
{
    public int Version { get; set; } = 1;
    public AdfValidationResult Validate();
    public AdfDocument Normalize();
    public AdfDocument Merge(AdfDocument other);
}
```

`AdfDocument` is the root `doc` node — create one with `AdfNode.CreateDocument(...)` or
`AdfDocumentBuilder.Build(...)`. It adds a `Version` (defaults to `1`, matching the ADF spec),
`Validate()`, and `Normalize()`, which reshapes illegal nesting and mark clashes in place (see
[Validation](validation.md) for both).

`Merge(other)` appends `other`'s top-level content after this document's own content and returns this
document, so the document you call it on stays the "parent" — the one whose reference and `Version` you
keep using — and `other`'s nodes become trailing children of it:

```csharp
AdfDocument combined = documentA.Merge(documentB).Merge(documentC); // A's content, then B's, then C's
```

The merged-in nodes are the same `AdfNode` instances from `other`, not copies, and `other` itself is left
as a loose document afterward (still valid, just no longer referenced by the merge). This mirrors
`AdfDocumentBuilder.Extend`/`Prepend` (see [the fluent builder](fluent-builder.md)) for the case where the
content to append/prepend is itself already a whole document rather than something you're composing fresh;
combine `Merge` with `Extend`/`Prepend` if you need a specific interleaving instead of always appending
whole documents at the end.

There is **no** `AdfDocument.FromJson()`/`ToAdfJson()` on the model itself — JSON (de)serialization lives
in `AdfDotNet.Json.Newtonsoft` (see [JSON serialization](json-serialization.md)); `document.ToJson()` is
available as an extension method from that package.

## AdfMark

```csharp
public class AdfMark
{
    public AdfMarkType Type { get; set; }
    public Dictionary<string, object>? Attrs { get; set; }

    public AdfMark(AdfMarkType type, Dictionary<string, object>? attrs = null);
}
```

Marks are attached to `Text` nodes via `AdfNode.Marks` and represent inline formatting. `AdfMarkType`
(`AdfDotNet.Enums`) covers `Strong` (bold), `Em` (italic), `Code`, `Link`, `Strike`, `Underline`, `SubSup`
(subscript/superscript), `TextColor`, `BackgroundColor` (highlight), and `Border`. `Link`, `SubSup`,
`TextColor`, and `BackgroundColor` carry attributes (`href`, `type`, `color`, `color` respectively), and
`Border` carries `size` and `color`; the others don't need any. `Border` is the odd one out: it goes on
`media`/`mediaInline` nodes, not on text (`media` may also carry `Link`).

```csharp
var bold = new AdfMark(AdfMarkType.Strong);
var link = new AdfMark(AdfMarkType.Link, new Dictionary<string, object> { ["href"] = "https://example.com" });

var text = AdfNode.CreateText("click here", new List<AdfMark> { link });
```

Building marks by hand like this works, but the [fluent builder's `AdfMarkSetBuilder`](fluent-builder.md#marks)
is usually more convenient, especially when combining multiple marks on one run.

## The Attrs value contract

`AdfNode.Attrs` and `AdfMark.Attrs` are both `Dictionary<string, object>?` — intentionally loosely typed,
since ADF's attribute shape varies per node/mark type. Regardless of how a value got there (JSON
deserialization, HTML/Markdown parsing, or manual construction), every value must be one of:

- `string`
- `int` or `long` for whole numbers — both are accepted; code reading `Attrs` should tolerate either
- `double`
- `bool`
- `DateTime`
- a nested `Dictionary<string, object>`, following this same contract recursively
- a `List<object>`, whose elements follow this same contract
- or `null`

Values must **never** be a serializer-specific token/wrapper type — no Newtonsoft `JValue`/`JObject`/
`JArray`, no System.Text.Json `JsonElement`. `AdfDotNet.Json.Newtonsoft`'s `AdfDocumentConverter.ReadValue`
is the reference implementation of normalizing JSON scalars down to this contract; any future serializer
package needs to do the same. Code that bypasses the sanctioned converters (e.g. calling
`JsonConvert.DeserializeObject<AdfNode>` directly instead of going through `AdfJsonConverter`/
`IAdfDocumentSerializer`) is outside this contract and may hand you raw JSON tokens.

## AdfNodeSchema and AdfStructure

Three related but distinct schema tables live in `AdfDotNet.Core/Models`:

- **`AdfNodeSchema`** is keyed by `AdfNodeType` and is the source of truth for structural legality:
  `CanHaveChildren`, `IsLeaf`, `IsValidChildType`, `GetAllowedChildTypes`, plus `GetTypeName`/
  `TryParseTypeName` for converting between the enum and its camelCase JSON/HTML-tag string (e.g.
  `AdfNodeType.BulletList` <-> `"bulletList"`). It also holds each node type's attribute rules, child
  counts and legal marks (`IsValidMark`). `AdfNode.CanHaveChildren()`/`IsLeafNode()` and
  `AdfDocument.Validate()` are both built on top of it.
- **`AdfMarkSchema`** is its counterpart for marks, keyed by `AdfMarkType`. It holds each mark's attribute
  rules and which marks can't be combined (`CanCombine`), plus `GetTypeName` (e.g. `SubSup` ->
  `"subsup"`). See [validation](validation.md).
- **`AdfStructure`** is keyed by HTML tag name and is the source of truth for HTML<->ADF conversion: which
  ADF node type and marks a given HTML tag maps to, and how to extract that node's `Attrs` from the
  `HtmlNode`. It delegates to `AdfNodeSchema` internally for its own `IsValidChildType`/
  `GetAllowedChildTypes` overloads (which keep string signatures for HTML-tag-facing callers).

You generally don't call either directly unless you're extending the library with a new node or mark type.
In that case, add the enum member, register its rules in `AdfNodeSchema`/`AdfMarkSchema`, and add it to
`KitchenSinkTests`, which fails until the new type is built, validated, and classified for every format.
