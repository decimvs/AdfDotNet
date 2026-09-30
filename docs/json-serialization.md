# JSON serialization

Package: `AdfDotNet.Json.Newtonsoft`. Namespace: `AdfDotNet.DataConverters`. Built on Newtonsoft.Json.

## Static API

```csharp
using AdfDotNet.DataConverters;

string json = AdfJsonConverter.Serialize(document, prettyPrint: true);
AdfDocument document = AdfJsonConverter.Deserialize(json);
```

There's also an extension method, for non-DI consumers who just want to call it directly on the document:

```csharp
string json = document.ToJson(prettyPrint: true);
```

## Dependency injection

```csharp
services.AddAdfNewtonsoftJson(); // registers IAdfDocumentSerializer as a singleton
```

```csharp
public interface IAdfDocumentSerializer
{
    string Serialize(AdfDocument document, bool prettyPrint = false);
    AdfDocument Deserialize(string json);
}
```

App code that only needs to serialize/deserialize ADF should depend on `IAdfDocumentSerializer`, not on
Newtonsoft types directly — that's the seam a future `System.Text.Json`-based package (not implemented
yet) could slot into without an app-code change.

## What the serializer does

- Property names are camelCase (`bulletList`, not `BulletList`), matching the ADF spec.
- `null` values are omitted from the output (`NullValueHandling.Ignore`).
- `AdfNodeType`/`AdfMarkType` enum values serialize as their camelCase string form via a custom
  `CamelCaseStringEnumConverter` — this is what turns `AdfNodeType.BulletList` into `"bulletList"`. The
  names come from `AdfNodeSchema.GetTypeName`/`AdfMarkSchema.GetTypeName`, which match the spec where plain
  camelCase wouldn't: `AdfMarkType.SubSup` is written as `"subsup"`. Reading also accepts `"subSup"`.
- The tree is read and written node-by-node by a hand-written `JsonConverter<AdfDocument>`
  (`AdfDocumentConverter`), rather than relying on default reflection-based (de)serialization. This is
  necessary because `AdfNode.Attrs`/`AdfMark.Attrs` are loosely-typed `Dictionary<string, object>` values
  that need custom coercion from JSON tokens down to the plain CLR types described in the [Attrs value
  contract](document-model.md#the-attrs-value-contract), and because the root node is validated as having
  `type: "doc"` while deserializing.

If you add a new top-level field to `AdfNode` (not just a new `Attrs` key on an existing node type), you
need to add matching read/write logic to `AdfDocumentConverter` — it won't pick it up automatically the
way default reflection-based serialization would.

## Deserializing ADF types this library doesn't model

`AdfNodeType`/`AdfMarkType` model every node and mark type on the Jira ADF spec page — see the [README's
ADF scope section](../README.md#adf-scope-and-converter-limitations) for the checklist. A test in
`KitchenSinkTests` builds one document using every type and checks that it passes `Validate()` and
round-trips through JSON unchanged.

When `Deserialize`/`AdfJsonConverter.Deserialize` encounters a node or mark type outside the enums, it
checks the type name against a registry of types defined only by the ADF JSON schema
(`@atlaskit/adf-schema`), not by the spec page (`AdfSpecCoverage.IsNonSpecNodeTypeName`/
`IsNonSpecMarkTypeName`, `AdfDotNet.Core/Models`):

- If it's listed, that node (with its subtree) or mark is **silently dropped** and deserialization
  proceeds — the containing node keeps its other content/marks. This matters in practice: real
  Confluence documents contain these types, and failing the entire deserialization over one of them would
  make the library unusable against real-world content.
- If the type name isn't a recognized ADF construct at all (a typo, a non-ADF payload, genuinely malformed
  input), deserialization still throws `JsonSerializationException` — that distinction is the point: known
  ADF this library chose not to model degrades gracefully, a real mistake fails loudly.

The registered types are, for nodes: `blockCard`, `bodiedExtension`, `caption`,
`decisionItem`, `decisionList`, `embedCard`, `extension`, `inlineExtension`, `layoutColumn`, `layoutSection`,
`placeholder`. Marks: `alignment`, `annotation`, `breakout`, `dataConsumer`, `fontSize`, `fragment`,
`indentation`. They show up in Confluence content, but the spec page says schema-only types "may not be
valid in this implementation", so this library never models them. The one exception is `taskList`/`taskItem`:
they're modeled, because `taskList` is the only parent of the spec's `blockTaskItem`. A dropped
node takes its whole subtree with it, so for example the text inside a `layoutSection` is lost; what
remains is valid for Jira.

`AdfDocument.Validate()` never sees the dropped nodes/marks — they're filtered out during deserialization,
before the tree exists to validate.
