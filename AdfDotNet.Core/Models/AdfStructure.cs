// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Enums;
using HtmlAgilityPack;

namespace AdfDotNet.Models
{
    /// <summary>
    /// Represents the structure of an ADF (Atlassian Document Format) document, including mappings of HTML nodes to ADF content types and their legal child types.
    /// </summary>
    public static class AdfStructure
    {
        /// <summary>
        /// The <c>backgroundColor</c> given to text inside an HTML <c>&lt;mark&gt;</c> element that has no
        /// <c>background-color</c> style of its own: "Yellow - light" from the text background palette of
        /// <c>@atlaskit/editor-palette</c>, which the <c>backgroundColor</c> spec page recommends. Yellow matches
        /// how browsers render <c>&lt;mark&gt;</c> by default.
        /// </summary>
        public const string DefaultHighlightColor = "#f8e6a0";

        /// <summary>
        /// A mapping of HTML node names to their corresponding ADF content types. This dictionary is used to determine the ADF content type for a given HTML node.
        /// </summary>
        private static readonly Dictionary<string, AdfContentType> _nodeMap = new Dictionary<string, AdfContentType>()
        {
            ["p"] = AdfContentType.FromName("paragraph"),
            ["div"] = AdfContentType.FromName("paragraph"),
            ["ul"] = AdfContentType.FromName("bulletList"),
            ["ol"] = AdfContentType.FromName("orderedList"),
            ["li"] = AdfContentType.FromName("listItem"),
            ["blockquote"] = AdfContentType.FromName("blockquote"),
            ["pre"] = AdfContentType.FromName("codeBlock"),
            ["hr"] = AdfContentType.FromName("rule"),
            ["br"] = AdfContentType.FromName("hardBreak"),
            ["table"] = AdfContentType.FromName("table"),
            ["tr"] = AdfContentType.FromName("tableRow"),
            ["td"] = AdfContentType.FromName("tableCell"),
            ["th"] = AdfContentType.FromName("tableHeader"),
            ["b"] = AdfContentType.FromNameAndMarks("text", new AdfMarkDefinition(AdfMarkType.Strong)),
            ["strong"] = AdfContentType.FromNameAndMarks("text", new AdfMarkDefinition(AdfMarkType.Strong)),
            ["i"] = AdfContentType.FromNameAndMarks("text", new AdfMarkDefinition(AdfMarkType.Em)),
            ["em"] = AdfContentType.FromNameAndMarks("text", new AdfMarkDefinition(AdfMarkType.Em)),
            ["u"] = AdfContentType.FromNameAndMarks("text", new AdfMarkDefinition(AdfMarkType.Underline)),
            ["code"] = AdfContentType.FromNameAndMarks("text", new AdfMarkDefinition(AdfMarkType.Code)),
            ["s"] = AdfContentType.FromNameAndMarks("text", new AdfMarkDefinition(AdfMarkType.Strike)),
            ["strike"] = AdfContentType.FromNameAndMarks("text", new AdfMarkDefinition(AdfMarkType.Strike)),
            ["del"] = AdfContentType.FromNameAndMarks("text", new AdfMarkDefinition(AdfMarkType.Strike)),
            ["a"] = AdfContentType.FromNameAndMarks("text", new AdfMarkDefinition(AdfMarkType.Link, ExtractLinkAttributes)),
            ["sub"] = AdfContentType.FromNameAndMarks("text", new AdfMarkDefinition(AdfMarkType.SubSup, _ => new Dictionary<string, object> { ["type"] = "sub" })),
            ["sup"] = AdfContentType.FromNameAndMarks("text", new AdfMarkDefinition(AdfMarkType.SubSup, _ => new Dictionary<string, object> { ["type"] = "sup" })),
            // A background-color style on the <mark> itself overrides this default (see HtmlToAdfConverter.CollectMarksFromAncestors).
            ["mark"] = AdfContentType.FromNameAndMarks("text", new AdfMarkDefinition(AdfMarkType.BackgroundColor, _ => new Dictionary<string, object> { ["color"] = DefaultHighlightColor })),
            ["h1"] = HeadingContentType(1),
            ["h2"] = HeadingContentType(2),
            ["h3"] = HeadingContentType(3),
            ["h4"] = HeadingContentType(4),
            ["h5"] = HeadingContentType(5),
            ["h6"] = HeadingContentType(6)
        };

        /// <summary>
        /// The default ADF content type for text nodes. If a node does not have a specific mapping in the _nodeMap, this default type will be used.
        /// </summary>
        private static readonly AdfContentType _defaultTextType = AdfContentType.FromName("text");

        /// <summary>
        /// Gets the ADF content type for a given HTML node name.
        /// </summary>
        /// <param name="nodeName">The name of the HTML node.</param>
        /// <returns>The corresponding ADF content type. If the node name is not found in the mapping, the default text type is returned.</returns>
        public static AdfContentType GetContentTypeForNode(string nodeName)
        {
            return _nodeMap.TryGetValue(nodeName.ToLowerInvariant(), out AdfContentType contentType) ? contentType : _defaultTextType;
        }
        
        /// <summary>
        /// Determines whether a given child type is valid for a specified parent type. Backed by
        /// <see cref="AdfNodeSchema"/>, which is keyed by <see cref="AdfNodeType"/> rather than by name -
        /// this method parses both names (case-insensitively) before delegating.
        /// </summary>
        /// <param name="parentType">The parent ADF content type name (e.g. "paragraph").</param>
        /// <param name="childType">The child ADF content type name (e.g. "text").</param>
        /// <returns><c>true</c> if the child type is valid for the parent type; otherwise, <c>false</c>.</returns>
        public static bool IsValidChildType(string parentType, string childType)
        {
            return AdfNodeSchema.TryParseTypeName(parentType, out AdfNodeType parent)
                && AdfNodeSchema.TryParseTypeName(childType, out AdfNodeType child)
                && AdfNodeSchema.IsValidChildType(parent, child);
        }

        /// <summary>
        /// Gets the allowed child types for a specified parent type. Backed by <see cref="AdfNodeSchema"/>.
        /// </summary>
        /// <param name="parentType">The parent ADF content type name (e.g. "paragraph").</param>
        /// <returns>A list of allowed child type names for the specified parent type. If the parent type is not found, an empty list is returned.</returns>
        public static List<string> GetAllowedChildTypes(string parentType)
        {
            if (!AdfNodeSchema.TryParseTypeName(parentType, out AdfNodeType parent))
                return new List<string>();

            return AdfNodeSchema.GetAllowedChildTypes(parent).Select(AdfNodeSchema.GetTypeName).ToList();
        }
        
        /// <summary>
        /// Extracts the link attributes from an HTML node.
        /// </summary>
        /// <param name="node">The HTML node representing a link.</param>
        /// <returns>A dictionary containing the link attributes (<c>href</c>, plus <c>title</c> when present), or
        /// <c>null</c> if the href attribute is not present.</returns>
        private static Dictionary<string, object>? ExtractLinkAttributes(HtmlNode node)
        {
            string href = node.GetAttributeValue("href", "");
            if (string.IsNullOrEmpty(href))
                return null;

            Dictionary<string, object> attrs = new Dictionary<string, object> { ["href"] = href };
            if (node.Attributes["title"] != null)
                attrs["title"] = HtmlEntity.DeEntitize(node.GetAttributeValue("title", ""));
            return attrs;
        }

        /// <summary>
        /// Creates the heading content type for the given heading level.
        /// </summary>
        /// <param name="level">The heading level (1-6).</param>
        /// <returns>A heading content type whose attribute extractor yields the given level.</returns>
        private static AdfContentType HeadingContentType(int level)
        {
            return AdfContentType.FromNameAndAttributes("heading", _ => new Dictionary<string, object> { ["level"] = level });
        }
    }
}