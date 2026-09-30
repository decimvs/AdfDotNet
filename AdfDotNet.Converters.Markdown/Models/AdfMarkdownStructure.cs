// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Enums;
using Markdig.Syntax;

namespace AdfDotNet.Models
{
    /// <summary>
    /// Maps Markdig's block/inline vocabulary onto ADF, the Markdown-side equivalent of
    /// <see cref="AdfStructure"/>'s HTML-tag-name map. Markdown constructs don't map 1:1 onto HTML tag
    /// names (there is no single "tag name" concept - Markdig represents constructs as distinct .NET types
    /// in <c>Markdig.Syntax</c>/<c>Markdig.Syntax.Inlines</c>), so this holds only the mappings that are
    /// pure data (a block type that always becomes the same <see cref="AdfNodeType"/>, or an emphasis
    /// delimiter that always becomes the same <see cref="AdfMarkType"/>). Constructs that need extra
    /// parsing beyond a straight lookup (heading level, list ordered-flag, code block language/text, table
    /// rows/cells) are handled directly by <c>MarkdownToAdfConverter</c> instead of being forced into this
    /// table, mirroring how <see cref="AdfStructure"/> itself only maps HTML tag -> ADF type/marks/attribute
    /// extractor rather than embedding full conversion logic.
    /// </summary>
    public static class AdfMarkdownStructure
    {
        /// <summary>
        /// Markdig block types that always convert to the same <see cref="AdfNodeType"/>, with no
        /// additional attributes to extract.
        /// </summary>
        private static readonly Dictionary<Type, AdfNodeType> _directBlockTypes = new Dictionary<Type, AdfNodeType>()
        {
            [typeof(ParagraphBlock)] = AdfNodeType.Paragraph,
            [typeof(QuoteBlock)] = AdfNodeType.Blockquote,
            [typeof(ThematicBreakBlock)] = AdfNodeType.Rule,
        };

        /// <summary>
        /// Attempts to look up the direct <see cref="AdfNodeType"/> for the given Markdig block.
        /// </summary>
        /// <param name="block">The Markdig block to look up.</param>
        /// <param name="nodeType">The corresponding ADF node type, if found.</param>
        /// <returns><c>true</c> if the block's concrete type has a direct mapping; otherwise, <c>false</c>.</returns>
        public static bool TryGetDirectNodeType(Block block, out AdfNodeType nodeType)
        {
            return _directBlockTypes.TryGetValue(block.GetType(), out nodeType);
        }

        /// <summary>
        /// Maps a Markdig emphasis delimiter to the corresponding <see cref="AdfMarkType"/>: <c>~~</c>
        /// (Markdig's Strikethrough extension, delimiter char <c>~</c>) becomes <see cref="AdfMarkType.Strike"/>,
        /// a two-or-more character run becomes <see cref="AdfMarkType.Strong"/>, and a single-character run
        /// becomes <see cref="AdfMarkType.Em"/>.
        /// </summary>
        /// <param name="delimiterChar">The emphasis delimiter character.</param>
        /// <param name="delimiterCount">The number of delimiter characters in the run.</param>
        /// <returns>The corresponding ADF mark type.</returns>
        public static AdfMarkType GetEmphasisMarkType(char delimiterChar, int delimiterCount)
        {
            if (delimiterChar == '~')
                return AdfMarkType.Strike;

            return delimiterCount >= 2 ? AdfMarkType.Strong : AdfMarkType.Em;
        }

        /// <summary>
        /// One-to-one mapping between ADF <c>panelType</c> values and GitHub-flavored-Markdown alert kinds,
        /// paired by meaning/color (info = blue NOTE, note = purple IMPORTANT, success = green TIP,
        /// warning = yellow WARNING, error = red CAUTION).
        /// </summary>
        private static readonly Dictionary<string, string> _panelTypeToAlertKind = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["info"] = "NOTE",
            ["note"] = "IMPORTANT",
            ["success"] = "TIP",
            ["warning"] = "WARNING",
            ["error"] = "CAUTION",
        };

        private static readonly Dictionary<string, string> _alertKindToPanelType =
            _panelTypeToAlertKind.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Maps an ADF panel type to its GitHub-flavored-Markdown alert kind. Unknown panel types fall back to <c>NOTE</c>.
        /// </summary>
        /// <param name="panelType">The ADF panel type (e.g. <c>"warning"</c>).</param>
        /// <returns>The alert kind (e.g. <c>"WARNING"</c>).</returns>
        public static string GetAlertKind(string panelType)
        {
            return _panelTypeToAlertKind.TryGetValue(panelType, out string kind) ? kind : "NOTE";
        }

        /// <summary>
        /// Maps a GitHub-flavored-Markdown alert kind to its ADF panel type. Unknown kinds fall back to <c>info</c>.
        /// </summary>
        /// <param name="alertKind">The alert kind (e.g. <c>"WARNING"</c>).</param>
        /// <returns>The ADF panel type (e.g. <c>"warning"</c>).</returns>
        public static string GetPanelType(string alertKind)
        {
            return _alertKindToPanelType.TryGetValue(alertKind, out string panelType) ? panelType : "info";
        }
    }
}
