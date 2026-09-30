// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Enums;
using System.Linq;
using System.Text.RegularExpressions;
using static AdfDotNet.Models.AdfAttrValues;

namespace AdfDotNet.Models
{
    /// <summary>
    /// Describes, for every <see cref="AdfNodeType"/>, whether it is a container or a leaf and which child
    /// types are legal beneath it. This is the single source of truth that <see cref="AdfNode.CanHaveChildren"/>,
    /// <see cref="AdfNode.IsLeafNode"/>, <see cref="AdfStructure.IsValidChildType"/>,
    /// <see cref="AdfStructure.GetAllowedChildTypes"/> and <see cref="AdfDocument.Validate"/> are all built on.
    /// </summary>
    public static class AdfNodeSchema
    {
        private sealed class Entry
        {
            public bool CanHaveChildren { get; set; }

            public bool IsLeaf { get; set; }

            public IReadOnlyList<AdfNodeType> LegalChildTypes { get; set; } = Array.Empty<AdfNodeType>();

            public IReadOnlyList<AdfAttrRule> Attrs { get; private set; } = Array.Empty<AdfAttrRule>();

            public IReadOnlyList<AdfMarkType> LegalMarks { get; private set; } = Array.Empty<AdfMarkType>();

            public int MinChildren { get; private set; }

            public int? MaxChildren { get; private set; }

            /// <summary>
            /// When set, children may not carry marks (<c>codeBlock</c>: "text nodes without marks").
            /// </summary>
            public bool ChildrenUnmarked { get; private set; }

            /// <summary>
            /// A rule that doesn't fit the tables above (e.g. <c>inlineCard</c>'s "url or data, not both").
            /// Adds <c>path: message</c> errors for the given node.
            /// </summary>
            public Action<AdfNode, string, List<string>>? Check { get; private set; }

            public Entry WithAttrs(params AdfAttrRule[] attrs) { Attrs = attrs; return this; }

            public Entry WithMarks(params AdfMarkType[] marks) { LegalMarks = marks; return this; }

            public Entry WithChildCount(int min, int? max = null) { MinChildren = min; MaxChildren = max; return this; }

            public Entry WithUnmarkedChildren() { ChildrenUnmarked = true; return this; }

            public Entry WithCheck(Action<AdfNode, string, List<string>> check) { Check = check; return this; }
        }

        private static Entry Container(params AdfNodeType[] legalChildTypes)
        {
            return new Entry { CanHaveChildren = true, LegalChildTypes = legalChildTypes };
        }

        private static Entry Leaf() => new Entry { IsLeaf = true };

        private static readonly Entry _unclassified = new Entry();

        private static readonly AdfAttrRule LocalId = AdfAttrRule.Optional("localId", "a string", IsString);

        private static readonly AdfMarkType[] TextMarks =
        {
            AdfMarkType.Code,
            AdfMarkType.Em,
            AdfMarkType.Link,
            AdfMarkType.Strike,
            AdfMarkType.Strong,
            AdfMarkType.SubSup,
            AdfMarkType.TextColor,
            AdfMarkType.Underline,
            // The text page (2020) predates backgroundColor; the backgroundColor page (2024) says it applies to text.
            AdfMarkType.BackgroundColor,
        };

        private static readonly AdfMarkType[] MediaMarks = { AdfMarkType.Link, AdfMarkType.Border };

        private static readonly string[] StatusColorNames = { "neutral", "purple", "blue", "red", "yellow", "green" };

        private static readonly Regex SixDigitHexColor = new Regex("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);

        /// <summary>
        /// The inline node types legal inside a <c>paragraph</c> or <c>heading</c>.
        /// </summary>
        private static readonly AdfNodeType[] InlineNodeTypes =
        {
            AdfNodeType.Text,
            AdfNodeType.HardBreak,
            AdfNodeType.InlineCard,
            AdfNodeType.Emoji,
            AdfNodeType.Mention,
            AdfNodeType.Date,
            AdfNodeType.Status,
            AdfNodeType.MediaInline,
        };

        private static readonly AdfNodeType[] TableCellChildTypes =
        {
            AdfNodeType.Paragraph,
            AdfNodeType.Heading,
            AdfNodeType.BulletList,
            AdfNodeType.OrderedList,
            AdfNodeType.Blockquote,
            AdfNodeType.CodeBlock,
            AdfNodeType.Panel,
            AdfNodeType.Rule,
            AdfNodeType.MediaGroup,
            AdfNodeType.NestedExpand,
            AdfNodeType.TaskList,
        };

        /// <summary>
        /// The block content of <c>bodiedSyncBlock</c> and <c>extensionFrame</c>: every type the ADF JSON schema
        /// allows there that this library models (their spec pages return 404). <c>bodiedSyncBlock</c> also
        /// allows <c>expand</c>, added on its own entry.
        /// </summary>
        private static readonly AdfNodeType[] SyncAndFrameChildTypes =
        {
            AdfNodeType.Blockquote,
            AdfNodeType.BulletList,
            AdfNodeType.CodeBlock,
            AdfNodeType.Heading,
            AdfNodeType.MediaGroup,
            AdfNodeType.MediaSingle,
            AdfNodeType.OrderedList,
            AdfNodeType.Panel,
            AdfNodeType.Paragraph,
            AdfNodeType.Rule,
            AdfNodeType.Table,
            AdfNodeType.TaskList,
        };

        private static readonly AdfAttrRule[] TaskItemAttrs =
        {
            AdfAttrRule.Required("localId", "a string", IsString),
            AdfAttrRule.Required("state", "\"TODO\" or \"DONE\"", OneOf("TODO", "DONE")),
        };

        private static readonly AdfAttrRule[] SyncBlockAttrs =
        {
            AdfAttrRule.Required("resourceId", "a string", IsString),
            AdfAttrRule.Required("localId", "a string", IsString),
        };

        // expand and nestedExpand: the spec pages mark attrs as required, but attrs.title as optional.
        private static readonly AdfAttrRule[] ExpandAttrs =
        {
            AdfAttrRule.Optional("title", "a string", IsString),
            LocalId,
        };

        private static readonly AdfAttrRule[] TableCellAttrs =
        {
            AdfAttrRule.Optional("background", "a string", IsString),
            AdfAttrRule.Optional("colspan", "a whole number of at least 1", Integer(1)),
            AdfAttrRule.Optional("rowspan", "a whole number of at least 1", Integer(1)),
            // The tableCell/tableHeader pages allow 0 for a column with no fixed width.
            AdfAttrRule.Optional("colwidth", "a list of numbers of 0 or more", NonNegativeNumberList),
        };

        /// <summary>
        /// The unified schema table. <see cref="AdfNodeType.InlineCard"/> is neither a container nor a leaf,
        /// carrying its data entirely in <see cref="AdfNode.Attrs"/> and never expected to have
        /// <see cref="AdfNode.Content"/>. <see cref="AdfNodeType.MediaSingle"/> is a real container per the ADF
        /// spec: its content must be exactly one <see cref="AdfNodeType.Media"/> node.
        /// </summary>
        /// <remarks>
        /// Legal children, attribute rules, legal marks and child counts all follow each node's spec page
        /// (audited 2026-09-29), except for the types whose pages return 404 (mediaInline, syncBlock,
        /// bodiedSyncBlock, multiBodiedExtension, extensionFrame, blockTaskItem), which follow the ADF JSON schema
        /// instead, and for taskList/taskItem, which aren't on the spec page at all: they're the only parent the
        /// schema gives blockTaskItem, so they're modeled from the schema and placed wherever it allows them.
        /// Where the spec says "integer" for a size in pixels
        /// (<c>table.width</c>, <c>colwidth</c> (which may also be 0), <c>media.width</c>/<c>height</c>), any positive number is
        /// accepted, since JSON doesn't distinguish <c>760</c> from <c>760.0</c>; counts (<c>heading.level</c>,
        /// <c>orderedList.order</c>, <c>colspan</c>, <c>rowspan</c>) must be whole numbers.
        /// </remarks>
        private static readonly Dictionary<AdfNodeType, Entry> _schema = new Dictionary<AdfNodeType, Entry>()
        {
            [AdfNodeType.Doc] = Container(
                AdfNodeType.Paragraph,
                AdfNodeType.Heading,
                AdfNodeType.BulletList,
                AdfNodeType.OrderedList,
                AdfNodeType.Blockquote,
                AdfNodeType.CodeBlock,
                AdfNodeType.Rule,
                AdfNodeType.Table,
                AdfNodeType.MediaSingle,
                AdfNodeType.Panel,
                AdfNodeType.Expand,
                AdfNodeType.MediaGroup,
                AdfNodeType.SyncBlock,
                AdfNodeType.BodiedSyncBlock,
                AdfNodeType.MultiBodiedExtension,
                AdfNodeType.TaskList),
            [AdfNodeType.Paragraph] = Container(InlineNodeTypes)
                .WithAttrs(LocalId),
            [AdfNodeType.Heading] = Container(InlineNodeTypes)
                .WithAttrs(
                    AdfAttrRule.Required("level", "a whole number from 1 to 6", Integer(1, 6)),
                    LocalId),
            [AdfNodeType.BulletList] = Container(AdfNodeType.ListItem)
                .WithChildCount(1),
            [AdfNodeType.OrderedList] = Container(AdfNodeType.ListItem)
                .WithChildCount(1)
                .WithAttrs(AdfAttrRule.Optional("order", "a whole number of at least 0", Integer(0))),
            [AdfNodeType.ListItem] = Container(
                    AdfNodeType.Paragraph,
                    AdfNodeType.BulletList,
                    AdfNodeType.OrderedList,
                    AdfNodeType.CodeBlock,
                    AdfNodeType.MediaSingle,
                    AdfNodeType.TaskList)
                .WithChildCount(1),
            [AdfNodeType.Blockquote] = Container(
                    AdfNodeType.Paragraph,
                    AdfNodeType.BulletList,
                    AdfNodeType.OrderedList,
                    AdfNodeType.CodeBlock,
                    AdfNodeType.MediaSingle,
                    AdfNodeType.MediaGroup)
                .WithChildCount(1),
            // The page says "one or more text nodes", but also lists content as optional; Jira's editor
            // saves an empty code block with no content, so an empty codeBlock is accepted.
            [AdfNodeType.CodeBlock] = Container(AdfNodeType.Text)
                .WithUnmarkedChildren()
                .WithAttrs(
                    AdfAttrRule.Optional("language", "a string", IsString),
                    AdfAttrRule.Optional("wrap", "true, false or null", IsBoolean),
                    AdfAttrRule.Optional("hideLineNumbers", "true or false", IsBoolean)),
            [AdfNodeType.Table] = Container(AdfNodeType.TableRow)
                .WithChildCount(1)
                .WithAttrs(
                    AdfAttrRule.Optional("isNumberColumnEnabled", "true or false", IsBoolean),
                    AdfAttrRule.Optional("width", "a positive number of pixels", IsPositiveNumber),
                    AdfAttrRule.Optional("layout", "\"center\" or \"align-start\"", OneOf("center", "align-start")),
                    AdfAttrRule.Optional("displayMode", "\"default\" or \"fixed\"", OneOf("default", "fixed"))),
            [AdfNodeType.TableRow] = Container(AdfNodeType.TableCell, AdfNodeType.TableHeader)
                .WithChildCount(1),
            [AdfNodeType.TableCell] = Container(TableCellChildTypes)
                .WithChildCount(1)
                .WithAttrs(TableCellAttrs),
            [AdfNodeType.TableHeader] = Container(TableCellChildTypes)
                .WithChildCount(1)
                .WithAttrs(TableCellAttrs),
            [AdfNodeType.Text] = Leaf()
                .WithMarks(TextMarks)
                .WithCheck(CheckText),
            [AdfNodeType.Rule] = Leaf(),
            [AdfNodeType.HardBreak] = Leaf()
                .WithAttrs(AdfAttrRule.Optional("text", "a string", IsString)),
            [AdfNodeType.Media] = Leaf()
                .WithMarks(MediaMarks)
                .WithAttrs(
                    AdfAttrRule.Required("id", "a string", IsString),
                    AdfAttrRule.Required("type", "\"file\" or \"link\"", OneOf("file", "link")),
                    AdfAttrRule.Required("collection", "a string", IsString),
                    AdfAttrRule.Optional("width", "a positive number of pixels", IsPositiveNumber),
                    AdfAttrRule.Optional("height", "a positive number of pixels", IsPositiveNumber),
                    AdfAttrRule.Optional("occurrenceKey", "a non-empty string", IsNonEmptyString)),
            [AdfNodeType.Emoji] = Leaf()
                .WithAttrs(
                    AdfAttrRule.Required("shortName", "a string", IsString),
                    AdfAttrRule.Optional("id", "a string", IsString),
                    AdfAttrRule.Optional("text", "a string", IsString)),
            [AdfNodeType.Mention] = Leaf()
                .WithAttrs(
                    AdfAttrRule.Required("id", "a string", IsString),
                    AdfAttrRule.Optional("text", "a string", IsString),
                    AdfAttrRule.Optional("accessLevel", "\"NONE\", \"SITE\", \"APPLICATION\" or \"CONTAINER\"", OneOf("NONE", "SITE", "APPLICATION", "CONTAINER")),
                    AdfAttrRule.Optional("userType", "\"DEFAULT\", \"SPECIAL\" or \"APP\"", OneOf("DEFAULT", "SPECIAL", "APP"))),
            [AdfNodeType.Date] = Leaf()
                .WithAttrs(AdfAttrRule.Required("timestamp", "a Unix timestamp string of digits", IsDigits)),
            [AdfNodeType.Status] = Leaf()
                .WithAttrs(
                    AdfAttrRule.Required("text", "a string", IsString),
                    AdfAttrRule.Required("color", "\"neutral\", \"purple\", \"blue\", \"red\", \"yellow\", \"green\" or a six-digit hex color", IsStatusColor),
                    LocalId),
            [AdfNodeType.InlineCard] = new Entry()
                .WithAttrs(
                    AdfAttrRule.Optional("url", "a string", IsString),
                    AdfAttrRule.Optional("data", "an object", value => value is IDictionary<string, object>))
                .WithCheck(CheckInlineCard),
            [AdfNodeType.MediaSingle] = Container(AdfNodeType.Media)
                .WithChildCount(1, 1)
                .WithAttrs(
                    AdfAttrRule.Required("layout", "one of \"wrap-left\", \"center\", \"wrap-right\", \"wide\", \"full-width\", \"align-start\", \"align-end\"",
                        OneOf("wrap-left", "center", "wrap-right", "wide", "full-width", "align-start", "align-end")),
                    AdfAttrRule.Optional("widthType", "\"pixel\" or \"percentage\"", OneOf("pixel", "percentage")))
                .WithCheck(CheckMediaSingleWidth),
            [AdfNodeType.Panel] = Container(
                    AdfNodeType.Paragraph,
                    AdfNodeType.Heading,
                    AdfNodeType.BulletList,
                    AdfNodeType.OrderedList,
                    AdfNodeType.TaskList)
                .WithChildCount(1)
                .WithAttrs(AdfAttrRule.Required("panelType", "\"info\", \"note\", \"warning\", \"success\" or \"error\"", OneOf("info", "note", "warning", "success", "error"))),
            // The page allows "an optional mark" without naming one; the JSON schema allows only the
            // schema-only breakout mark, so no spec mark is legal here.
            [AdfNodeType.Expand] = Container(
                    AdfNodeType.BulletList,
                    AdfNodeType.Blockquote,
                    AdfNodeType.CodeBlock,
                    AdfNodeType.Heading,
                    AdfNodeType.MediaGroup,
                    AdfNodeType.MediaSingle,
                    AdfNodeType.OrderedList,
                    AdfNodeType.Panel,
                    AdfNodeType.Paragraph,
                    AdfNodeType.Rule,
                    AdfNodeType.Table,
                    AdfNodeType.MultiBodiedExtension,
                    AdfNodeType.ExtensionFrame,
                    AdfNodeType.NestedExpand,
                    AdfNodeType.TaskList)
                .WithChildCount(1)
                .WithAttrs(ExpandAttrs)
                .WithCheck(CheckAttrsPresent),
            // Legal only under tableCell/tableHeader, which is enforced by those being its only parents.
            [AdfNodeType.NestedExpand] = Container(
                    AdfNodeType.Paragraph,
                    AdfNodeType.Heading,
                    AdfNodeType.MediaGroup,
                    AdfNodeType.MediaSingle,
                    AdfNodeType.TaskList)
                .WithChildCount(1)
                .WithAttrs(ExpandAttrs)
                .WithCheck(CheckAttrsPresent),
            [AdfNodeType.MediaGroup] = Container(AdfNodeType.Media)
                .WithChildCount(1),
            // The mediaInline page returns 404; these rules come from the ADF JSON schema's mediaInline_node.
            [AdfNodeType.MediaInline] = Leaf()
                .WithMarks(MediaMarks)
                .WithAttrs(
                    AdfAttrRule.Required("id", "a non-empty string", IsNonEmptyString),
                    AdfAttrRule.Required("collection", "a string", IsString),
                    AdfAttrRule.Optional("type", "\"link\", \"file\" or \"image\"", OneOf("link", "file", "image")),
                    AdfAttrRule.Optional("alt", "a string", IsString),
                    AdfAttrRule.Optional("occurrenceKey", "a non-empty string", IsNonEmptyString),
                    AdfAttrRule.Optional("width", "a number", IsNumber),
                    AdfAttrRule.Optional("height", "a number", IsNumber),
                    LocalId),
            // syncBlock, bodiedSyncBlock, multiBodiedExtension, extensionFrame and blockTaskItem: their spec pages
            // return 404, so these rules come from the ADF JSON schema (multiBodiedExtension and extensionFrame
            // only from its stage-0 variant). The only marks the schema allows on any of them are schema-only
            // (breakout, dataConsumer, fragment), so none carries a spec mark.
            [AdfNodeType.SyncBlock] = Leaf()
                .WithAttrs(SyncBlockAttrs),
            [AdfNodeType.BodiedSyncBlock] = Container(SyncAndFrameChildTypes.Concat(new[] { AdfNodeType.Expand }).ToArray())
                .WithChildCount(1)
                .WithAttrs(SyncBlockAttrs),
            // The schema has two variants: content may be empty, or (at the root only) must hold at least one
            // frame. Both are legal under doc, so an empty multiBodiedExtension is accepted.
            [AdfNodeType.MultiBodiedExtension] = Container(AdfNodeType.ExtensionFrame)
                .WithAttrs(
                    AdfAttrRule.Required("extensionKey", "a non-empty string", IsNonEmptyString),
                    AdfAttrRule.Required("extensionType", "a non-empty string", IsNonEmptyString),
                    AdfAttrRule.Optional("text", "a string", IsString),
                    AdfAttrRule.Optional("layout", "\"default\", \"wide\" or \"full-width\"", OneOf("default", "wide", "full-width")),
                    AdfAttrRule.Optional("localId", "a non-empty string", IsNonEmptyString)),
            [AdfNodeType.ExtensionFrame] = Container(SyncAndFrameChildTypes)
                .WithChildCount(1),
            // Schema-only (see the remarks above). taskList may nest another taskList.
            [AdfNodeType.TaskList] = Container(AdfNodeType.TaskItem, AdfNodeType.BlockTaskItem, AdfNodeType.TaskList)
                .WithChildCount(1)
                .WithAttrs(AdfAttrRule.Required("localId", "a string", IsString)),
            [AdfNodeType.TaskItem] = Container(InlineNodeTypes)
                .WithAttrs(TaskItemAttrs),
            // The schema lists content as a two-item tuple (without forbidding more), read here as 1-2 paragraphs.
            [AdfNodeType.BlockTaskItem] = Container(AdfNodeType.Paragraph)
                .WithChildCount(1, 2)
                .WithAttrs(TaskItemAttrs),
        };

        private static void CheckAttrsPresent(AdfNode node, string path, List<string> errors)
        {
            if (node.Attrs == null)
                errors.Add($"{path}: node of type '{GetTypeName(node.Type)}' must have an 'attrs' object (it may be empty).");
        }

        private static bool IsStatusColor(object value) =>
            value is string s && (StatusColorNames.Contains(s) || SixDigitHexColor.IsMatch(s));

        private static void CheckText(AdfNode node, string path, List<string> errors)
        {
            if (string.IsNullOrEmpty(node.Text))
                errors.Add($"{path}: text node must have non-empty text.");
        }

        private static void CheckInlineCard(AdfNode node, string path, List<string> errors)
        {
            bool hasUrl = node.Attrs?.TryGetValue("url", out object? url) == true && url != null;
            bool hasData = node.Attrs?.TryGetValue("data", out object? data) == true && data != null;

            if (hasUrl == hasData)
                errors.Add($"{path}: 'inlineCard' must have exactly one of the attributes 'url' and 'data'.");
        }

        // width is a percentage (0-100) unless widthType is "pixel", per the mediaSingle page.
        private static void CheckMediaSingleWidth(AdfNode node, string path, List<string> errors)
        {
            object? width = null;
            if (node.Attrs?.TryGetValue("width", out width) != true || width == null)
                return;

            bool isPixel = node.Attrs.TryGetValue("widthType", out object? widthType) && Equals(widthType, "pixel");
            AdfAttrRule rule = isPixel
                ? AdfAttrRule.Optional("width", "a positive number of pixels", IsPositiveNumber)
                : AdfAttrRule.Optional("width", "a percentage from 0 to 100", Number(0, 100));

            AdfAttrRule.Check(new[] { rule }, node.Attrs, "'mediaSingle'", path, errors);
        }

        private static Entry Get(AdfNodeType type) => _schema.TryGetValue(type, out Entry? entry) ? entry : _unclassified;

        /// <summary>
        /// Determines whether nodes of the given type may carry <see cref="AdfNode.Content"/>.
        /// </summary>
        public static bool CanHaveChildren(AdfNodeType type) => Get(type).CanHaveChildren;

        /// <summary>
        /// Determines whether the given type is a leaf (i.e. it cannot have child nodes).
        /// </summary>
        public static bool IsLeaf(AdfNodeType type) => Get(type).IsLeaf;

        /// <summary>
        /// Gets the child types that are legal beneath the given parent type. Empty for non-container types.
        /// </summary>
        public static IReadOnlyList<AdfNodeType> GetAllowedChildTypes(AdfNodeType parentType) => Get(parentType).LegalChildTypes;

        /// <summary>
        /// Determines whether <paramref name="childType"/> is a legal child of <paramref name="parentType"/>.
        /// </summary>
        public static bool IsValidChildType(AdfNodeType parentType, AdfNodeType childType) =>
            Get(parentType).LegalChildTypes.Contains(childType);

        /// <summary>
        /// Gets the minimum number of children a node of the given type must have (0 when content is optional).
        /// </summary>
        internal static int GetMinChildren(AdfNodeType type) => Get(type).MinChildren;

        internal static int? GetMaxChildren(AdfNodeType type) => Get(type).MaxChildren;

        /// <summary>
        /// Converts an <see cref="AdfNodeType"/> to the camelCase name used for its ADF JSON "type" field
        /// (e.g. <see cref="AdfNodeType.BulletList"/> -&gt; "bulletList"), matching the naming strategy
        /// <c>AdfDotNet.Json.Newtonsoft</c>'s <c>CamelCaseStringEnumConverter</c> applies at serialization time.
        /// </summary>
        public static string GetTypeName(AdfNodeType type) => ToCamelCase(type.ToString());

        internal static string ToCamelCase(string name) =>
            name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name.Substring(1);

        /// <summary>
        /// Determines whether a node of type <paramref name="nodeType"/> may carry a mark of type
        /// <paramref name="markType"/> (e.g. <c>text</c> takes every text mark, <c>media</c> only <c>link</c> and <c>border</c>,
        /// most other nodes none). See <see cref="AdfMarkSchema"/> for which marks combine with each other.
        /// </summary>
        public static bool IsValidMark(AdfNodeType nodeType, AdfMarkType markType) =>
            Get(nodeType).LegalMarks.Contains(markType);

        /// <summary>
        /// Parses an ADF type name (any casing) back into an <see cref="AdfNodeType"/>.
        /// </summary>
        public static bool TryParseTypeName(string? typeName, out AdfNodeType type)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                type = default;
                return false;
            }

            return Enum.TryParse(typeName, ignoreCase: true, out type);
        }

        /// <summary>
        /// Recursively validates every node beneath (and including) <paramref name="node"/> against the spec:
        /// legal child types and child counts, required attributes and their values, which marks each node
        /// type may carry, and which marks may be combined (<see cref="AdfMarkSchema"/>). Used by
        /// <see cref="AdfDocument.Validate"/>.
        /// </summary>
        /// <param name="node">The node to validate, treated as the root of the (sub)tree being checked.</param>
        /// <returns>The list of validation error messages; empty when the (sub)tree is valid.</returns>
        public static List<string> Validate(AdfNode node)
        {
            List<string> errors = new List<string>();
            ValidateNode(node, GetTypeName(node.Type), errors);
            return errors;
        }

        private static void ValidateNode(AdfNode node, string path, List<string> errors)
        {
            Entry entry = Get(node.Type);
            string typeName = GetTypeName(node.Type);

            AdfAttrRule.Check(entry.Attrs, node.Attrs, $"'{typeName}'", path, errors);
            entry.Check?.Invoke(node, path, errors);
            ValidateMarks(node, entry, typeName, path, errors);

            int childCount = node.Content?.Count ?? 0;

            if (!entry.CanHaveChildren)
            {
                if (childCount > 0)
                    errors.Add($"{path}: node of type '{typeName}' cannot have child content.");
                return;
            }

            if (childCount < entry.MinChildren)
                errors.Add($"{path}: node of type '{typeName}' must have at least {entry.MinChildren} child node(s), but has {childCount}.");
            else if (entry.MaxChildren.HasValue && childCount > entry.MaxChildren.Value)
                errors.Add($"{path}: node of type '{typeName}' must have at most {entry.MaxChildren.Value} child node(s), but has {childCount}.");

            for (int i = 0; i < childCount; i++)
            {
                AdfNode child = node.Content![i];
                string childTypeName = GetTypeName(child.Type);
                string childPath = $"{path}/{childTypeName}[{i}]";

                if (!entry.LegalChildTypes.Contains(child.Type))
                {
                    errors.Add($"{childPath}: node of type '{childTypeName}' is not a legal child of '{typeName}'.");
                    continue;
                }

                if (entry.ChildrenUnmarked && child.Marks != null && child.Marks.Count > 0)
                    errors.Add($"{childPath}: a '{childTypeName}' node inside '{typeName}' cannot have marks.");

                ValidateNode(child, childPath, errors);
            }
        }

        private static void ValidateMarks(AdfNode node, Entry entry, string typeName, string path, List<string> errors)
        {
            if (node.Marks == null || node.Marks.Count == 0)
                return;

            foreach (AdfMark mark in node.Marks)
            {
                if (!entry.LegalMarks.Contains(mark.Type))
                    errors.Add($"{path}: mark '{AdfMarkSchema.GetTypeName(mark.Type)}' is not allowed on '{typeName}'.");
            }

            AdfMarkSchema.Validate(node.Marks, path, errors);
        }
    }
}
