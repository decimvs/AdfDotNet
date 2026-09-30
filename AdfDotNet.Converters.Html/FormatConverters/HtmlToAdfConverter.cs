// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Enums;
using AdfDotNet.Models;
using AdfDotNet.Utils;
using HtmlAgilityPack;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace AdfDotNet.FormatConverters
{
    /// <summary>
    /// Converts HTML content to ADF (Atlassian Document Format) representation.
    /// </summary>
    public class HtmlToAdfConverter
    {
        /// <summary>
        /// Regular expression to match &lt;hr&gt; tags in HTML, used for escaping them during conversion.
        /// </summary>
        private static readonly Regex HrRegex = new Regex("</?hr/?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Dispatch table mapping HTML tag names to the converter method that handles them.
        /// Tags not present here fall back to <see cref="ConvertInlineOrUnknownElement"/>.
        /// </summary>
        private static readonly Dictionary<string, Func<HtmlToAdfConverter, HtmlNode, AdfNode?>> ElementConverters =
            new Dictionary<string, Func<HtmlToAdfConverter, HtmlNode, AdfNode?>>(StringComparer.OrdinalIgnoreCase)
            {
                ["hrbr"] = (c, e) => AdfNode.CreateRule(),
                ["hr"] = (c, e) => AdfNode.CreateRule(),
                ["br"] = (c, e) => AdfNode.CreateHardBreak(),
                ["p"] = (c, e) => c.ConvertParagraph(e),
                ["h1"] = (c, e) => c.ConvertHeading(e, 1),
                ["h2"] = (c, e) => c.ConvertHeading(e, 2),
                ["h3"] = (c, e) => c.ConvertHeading(e, 3),
                ["h4"] = (c, e) => c.ConvertHeading(e, 4),
                ["h5"] = (c, e) => c.ConvertHeading(e, 5),
                ["h6"] = (c, e) => c.ConvertHeading(e, 6),
                ["td"] = (c, e) => c.ConvertTableCell(e),
                ["th"] = (c, e) => c.ConvertTableHeader(e),
                ["li"] = (c, e) => c.ConvertListItem(e),
                ["ul"] = (c, e) => IsTaskList(e) ? c.ConvertTaskList(e) : c.ConvertBulletList(e),
                ["ol"] = (c, e) => IsTaskList(e) ? c.ConvertTaskList(e) : c.ConvertOrderedList(e),
                ["tr"] = (c, e) => c.ConvertTableRow(e),
                ["div"] = (c, e) => c.ConvertDiv(e),
                ["pre"] = (c, e) => c.ConvertCodeBlock(e),
                ["table"] = (c, e) => c.ConvertTable(e),
                ["blockquote"] = (c, e) => c.ConvertBlockquote(e),
                ["details"] = (c, e) => c.ConvertDetails(e),
                ["head"] = (c, e) => null,
                ["meta"] = (c, e) => null,
                ["title"] = (c, e) => null,
                ["style"] = (c, e) => null,
                ["script"] = (c, e) => null,
            };

        /// <summary>
        /// Creates a new instance of the HtmlToAdfConverter class.
        /// </summary>
        /// <returns>A new instance of the HtmlToAdfConverter class.</returns>
        public static HtmlToAdfConverter Create()
        {
            return new HtmlToAdfConverter();
        }

        /// <summary>
        /// Converts the given HTML string to an ADF (Atlassian Document Format) document.
        /// </summary>
        /// <param name="html">The HTML string to convert.</param>
        /// <returns>An ADF document representing the HTML content.</returns>
        public static AdfDocument Convert(string html)
        {
            return new HtmlToAdfConverter().ConvertHtml(html);
        }

        /// <summary>
        /// Converts the given HTML string to an ADF (Atlassian Document Format) document.
        /// </summary>
        /// <param name="html">The HTML string to convert.</param>
        /// <returns>An ADF document representing the HTML content.</returns>
        public AdfDocument ConvertHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                AdfDocument document = AdfNode.CreateDocument();
                document.AddContent(AdfNode.CreateParagraph());
                return document;
            }

            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(EscapeHrTags(html));

            AdfDocument adfDocument = AdfNode.CreateDocument();
            ProcessBlockElements(htmlDocument.DocumentNode.SelectSingleNode("//body") ?? htmlDocument.DocumentNode, adfDocument);

            // HTML nesting is copied as-is above; reshape whatever ADF doesn't allow (a heading inside a
            // blockquote, an empty <ul>, code + strong, ...).
            adfDocument.Normalize();

            if (adfDocument.Content!.Count == 0)
                adfDocument.AddContent(AdfNode.CreateParagraph());

            return adfDocument;
        }

        /// <summary>
        /// Escapes &lt;hr&gt; tags in the given HTML string by replacing them with &lt;hrbr&gt; tags.
        /// </summary>
        /// <param name="html">The HTML string to process.</param>
        /// <returns>The HTML string with &lt;hr&gt; tags escaped.</returns>
        private string EscapeHrTags(string html)
        {
            return HrRegex.Replace(html, "<hrbr></hrbr>");
        }

        /// <summary>
        /// Processes the block-level elements of the given HTML node and adds the corresponding ADF nodes to the parent ADF node.
        /// </summary>
        /// <param name="node">The HTML node to process.</param>
        /// <param name="parentAdfNode">The parent ADF node to which the converted nodes will be added.</param>
        private void ProcessBlockElements(HtmlNode node, AdfNode parentAdfNode)
        {
            // Consecutive inline content (text, <strong>, <a>, <br>, mention spans, ...) between block
            // elements is gathered into one paragraph, so loose inline content never lands directly under
            // a block container (which ADF doesn't allow) and isn't split into a paragraph per fragment.
            List<HtmlNode> inlineRun = new List<HtmlNode>();

            foreach (HtmlNode childNode in node.ChildNodes)
            {
                if (childNode.NodeType == HtmlNodeType.Text)
                {
                    inlineRun.Add(childNode);
                    continue;
                }

                if (childNode.NodeType != HtmlNodeType.Element)
                    continue;

                if (IsTransparentContainer(childNode))
                {
                    // Wrappers with no ADF node of their own (<html>/<body>, a <div>/<section>/<span>
                    // around block content): a single ConvertElementToAdf call can't represent their
                    // (potentially many) block children, so flatten those into the current parent.
                    FlushInlineRun(inlineRun, parentAdfNode);
                    ProcessBlockElements(childNode, parentAdfNode);
                }
                else if (IsBlockLevel(childNode.Name))
                {
                    FlushInlineRun(inlineRun, parentAdfNode);
                    AdfNode? adf = ConvertElementToAdf(childNode);
                    if (adf != null)
                        parentAdfNode.AddContent(adf);
                }
                else
                {
                    inlineRun.Add(childNode);
                }
            }

            FlushInlineRun(inlineRun, parentAdfNode);
        }

        /// <summary>
        /// Wraps the pending inline nodes in a paragraph and adds it to <paramref name="parentAdfNode"/>
        /// (unless they produce no content, e.g. whitespace between blocks), then clears the run.
        /// </summary>
        /// <param name="inlineRun">The pending inline HTML nodes.</param>
        /// <param name="parentAdfNode">The ADF node the paragraph is added to.</param>
        private void FlushInlineRun(List<HtmlNode> inlineRun, AdfNode parentAdfNode)
        {
            if (inlineRun.Count == 0)
                return;

            AdfNode paragraph = AdfNode.CreateParagraph();
            ProcessInlineNodes(inlineRun, paragraph);
            inlineRun.Clear();

            if (paragraph.Content!.Count > 0)
                parentAdfNode.AddContent(paragraph);
        }

        /// <summary>
        /// Tag names handled at block level even though <see cref="IsBlockElement"/> doesn't list them: the
        /// synthetic <c>hrbr</c> rule tag, and document-metadata elements that convert to nothing.
        /// </summary>
        private static readonly HashSet<string> OtherBlockLevelTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "hrbr", "head", "meta", "title", "style", "script"
        };

        /// <summary>
        /// Determines whether an element is converted as its own block (rather than as part of an inline run).
        /// </summary>
        private static bool IsBlockLevel(string tagName)
        {
            return IsBlockElement(tagName) || OtherBlockLevelTags.Contains(tagName);
        }

        /// <summary>
        /// Determines whether an element is a pure wrapper whose children should be flattened into the
        /// current block container: <c>&lt;html&gt;</c>/<c>&lt;body&gt;</c> always; a <c>&lt;div&gt;</c>
        /// (other than a panel) or any non-block element (<c>&lt;section&gt;</c>, <c>&lt;article&gt;</c>,
        /// a <c>&lt;span&gt;</c> wrapper, ...) when it contains block-level content.
        /// </summary>
        private static bool IsTransparentContainer(HtmlNode element)
        {
            string name = element.Name;

            if (string.Equals(name, "html", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "body", StringComparison.OrdinalIgnoreCase))
                return true;

            bool isDiv = string.Equals(name, "div", StringComparison.OrdinalIgnoreCase);
            if (isDiv && element.Attributes["data-panel-type"] != null)
                return false;

            return (isDiv || !IsBlockLevel(name)) && HasBlockDescendant(element);
        }

        /// <summary>
        /// Determines whether any descendant of the given element is block-level.
        /// </summary>
        private static bool HasBlockDescendant(HtmlNode element)
        {
            return element.Descendants().Any(n => n.NodeType == HtmlNodeType.Element && IsBlockLevel(n.Name));
        }

        /// <summary>
        /// Converts the given HTML element to an ADF (Atlassian Document Format) node.
        /// </summary>
        /// <param name="element">The HTML element to convert.</param>
        /// <returns>The corresponding ADF node, or null if the element cannot be converted.</returns>
        private AdfNode? ConvertElementToAdf(HtmlNode element)
        {
            string tagName = element.Name ?? string.Empty;
            return ElementConverters.TryGetValue(tagName, out Func<HtmlToAdfConverter, HtmlNode, AdfNode?> converter)
                ? converter(this, element)
                : ConvertInlineOrUnknownElement(element);
        }

        /// <summary>
        /// Converts the given inline or unknown HTML element to an ADF (Atlassian Document Format) node.
        /// </summary>
        /// <param name="element">The HTML element to convert.</param>
        /// <returns>The corresponding ADF node, or null if the element cannot be converted.</returns>
        private AdfNode? ConvertInlineOrUnknownElement(HtmlNode element)
        {
            // Marks implied by the element's tag (e.g. <b>, <a>) are picked up per text node by
            // CollectMarksFromAncestors, so no tag-specific handling is needed here.
            return TryConvertInlineNode(element) ?? ProcessInlineContent(element);
        }

        /// <summary>
        /// Processes the inline content of the given HTML element and converts it to an ADF (Atlassian Document Format) node.
        /// </summary>
        /// <param name="element">The HTML element to process.</param>
        /// <returns>The corresponding ADF node, or null if the element cannot be converted.</returns>
        private AdfNode? ProcessInlineContent(HtmlNode element)
        {
            List<AdfNode> adfNodeList = new List<AdfNode>();

            foreach (HtmlNode childNode in element.ChildNodes)
            {
                if (childNode.NodeType == HtmlNodeType.Text)
                {
                    string text = TextUtils.NormalizeWhitespace(WebUtility.HtmlDecode(childNode.InnerText));
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        List<AdfMark> adfMarkList = CollectMarksFromAncestors(childNode);
                        adfNodeList.Add(AdfNode.CreateText(text, adfMarkList.Count > 0 ? adfMarkList : null));
                    }
                }
                else if (childNode.NodeType == HtmlNodeType.Element)
                {
                    AdfNode? adfNode = TryConvertInlineNode(childNode) ?? ProcessInlineContent(childNode);

                    if (adfNode != null)
                        adfNodeList.Add(adfNode);
                }
            }

            return adfNodeList.Count switch
            {
                0 => null,
                1 => adfNodeList[0],
                _ => AdfNode.CreateParagraph(adfNodeList)
            };
        }

        /// <summary>
        /// Collects the ADF marks from the ancestors of the given HTML node.
        /// </summary>
        /// <param name="node">The HTML node whose ancestors' marks are to be collected.</param>
        /// <returns>A list of ADF marks collected from the ancestors.</returns>
        private List<AdfMark> CollectMarksFromAncestors(HtmlNode node)
        {
            List<AdfMark> marks = new List<AdfMark>();

            for (HtmlNode parentNode = node.ParentNode; parentNode != null && parentNode.NodeType == HtmlNodeType.Element; parentNode = parentNode.ParentNode)
            {
                List<AdfMark> cssMarks = ExtractCssMarks(parentNode);

                // A table cell's background color is the cell's background attr (ConvertTableCellAttrs), not a
                // highlight on its text.
                if (IsTableCell(parentNode))
                    cssMarks.RemoveAll(mark => mark.Type == AdfMarkType.BackgroundColor);

                // The element's own style overrides its tag's mark of the same type
                // (<mark style="background-color: ..."> keeps its color rather than the default).
                foreach (AdfMarkDefinition mark in AdfStructure.GetContentTypeForNode(parentNode.Name).Marks)
                {
                    if (!cssMarks.Any(cssMark => cssMark.Type == mark.Type))
                        marks.Add(new AdfMark(mark.Type, mark.AttributeExtractor?.Invoke(parentNode)));
                }

                marks.AddRange(cssMarks);
            }

            // Illegal combinations (e.g. code + strong) are resolved by AdfNormalizer once the tree is built.
            return marks;
        }

        /// <summary>
        /// Extracts ADF marks from the CSS styles of the given HTML node.
        /// </summary>
        /// <param name="node">The HTML node from which to extract CSS marks.</param>
        /// <returns>A list of ADF marks extracted from the CSS styles.</returns>
        private List<AdfMark> ExtractCssMarks(HtmlNode node)
        {
            List<AdfMark> cssMarks = new List<AdfMark>();
            string attributeValue = node.GetAttributeValue("style", "");

            if (string.IsNullOrEmpty(attributeValue))
                return cssMarks;

            foreach ((string property, string value) in ParseCssStyle(attributeValue))
            {
                AdfMark? mark = property.ToLowerInvariant() switch
                {
                    "font-weight" when value.Contains("bold") => new AdfMark(AdfMarkType.Strong),
                    "font-style" when value.Contains("italic") => new AdfMark(AdfMarkType.Em),
                    "text-decoration" when value.Contains("underline") => new AdfMark(AdfMarkType.Underline),
                    "text-decoration" when value.Contains("line-through") => new AdfMark(AdfMarkType.Strike),
                    "color" => CreateColorMark(AdfMarkType.TextColor, value),
                    // The shorthand only counts when its whole value is a color (not "url(...) #fff").
                    "background-color" or "background" => CreateColorMark(AdfMarkType.BackgroundColor, value),
                    _ => null
                };

                if (mark != null)
                    cssMarks.Add(mark);
            }
            return cssMarks;
        }

        /// <summary>
        /// Creates a color mark (<c>textColor</c> or <c>backgroundColor</c>) from a CSS color value, if it
        /// resolves to a hex color.
        /// </summary>
        /// <param name="markType">The type of mark to create.</param>
        /// <param name="cssColor">The CSS color value (a hex code or a named color).</param>
        /// <returns>The color mark, or <c>null</c> if the value couldn't be resolved.</returns>
        private static AdfMark? CreateColorMark(AdfMarkType markType, string cssColor)
        {
            string? hexColor = TextUtils.ExtractHexColor("color: " + cssColor);
            return string.IsNullOrEmpty(hexColor)
                ? null
                : new AdfMark(markType, new Dictionary<string, object> { ["color"] = hexColor! });
        }

        /// <summary>
        /// Parses a CSS style string into a list of property-value pairs.
        /// </summary>
        /// <param name="style">The CSS style string to parse.</param>
        /// <returns>A list of property-value pairs extracted from the CSS style string.</returns>
        private static List<(string property, string value)> ParseCssStyle(string style)
        {
            List<(string property, string value)> cssStyle = new List<(string property, string value)>();
            foreach (string declaration in style.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int colonIndex = declaration.IndexOf(':');
                if (colonIndex > 0)
                {
                    string property = declaration.Substring(0, colonIndex).Trim();
                    string value = declaration.Substring(colonIndex + 1).Trim();
                    cssStyle.Add((property, value));
                }
            }
            return cssStyle;
        }

        /// <summary>
        /// Converts the given HTML paragraph element to an ADF (Atlassian Document Format) node.
        /// </summary>
        /// <param name="element">The HTML paragraph element to convert.</param>
        /// <returns>The corresponding ADF node.</returns>
        private AdfNode ConvertParagraph(HtmlNode element)
        {
            AdfNode paragraph = AdfNode.CreateParagraph();
            ProcessInlineElements(element, paragraph);
            return paragraph;
        }

        /// <summary>
        /// Converts the given HTML div element to an ADF (Atlassian Document Format) node.
        /// </summary>
        /// <param name="element">The HTML div element to convert.</param>
        /// <returns>The corresponding ADF node, or null if the div contains block elements.</returns>
        private AdfNode? ConvertDiv(HtmlNode element)
        {
            if (element.Attributes["data-panel-type"] != null)
                return ConvertPanel(element);

            return HasBlockChild(element) ? null : ConvertParagraph(element);
        }

        /// <summary>
        /// Converts a <c>&lt;div data-panel-type&gt;</c> element (as rendered by <see cref="AdfToHtmlConverter"/>)
        /// to an ADF panel node. Purely inline content is wrapped in a paragraph, like list items and table cells.
        /// </summary>
        /// <param name="element">The HTML div element carrying a <c>data-panel-type</c> attribute.</param>
        /// <returns>The corresponding ADF panel node.</returns>
        private AdfNode ConvertPanel(HtmlNode element)
        {
            string panelType = element.GetAttributeValue("data-panel-type", "");
            AdfNode panel = AdfNode.CreatePanel(string.IsNullOrWhiteSpace(panelType) ? "info" : panelType);
            ConvertContainerContent(element, panel);
            return panel;
        }

        /// <summary>
        /// Converts an element carrying one of the data attributes <see cref="AdfToHtmlConverter"/> uses for
        /// ADF inline nodes that have no native HTML tag: <c>data-mention-id</c> (mention), <c>data-timestamp</c>
        /// (date) or <c>data-status-color</c> (status).
        /// </summary>
        /// <param name="element">The HTML element to inspect.</param>
        /// <returns>The inline node, or <c>null</c> if <paramref name="element"/> carries none of those attributes.</returns>
        private static AdfNode? TryConvertInlineNode(HtmlNode element)
        {
            if (element.NodeType != HtmlNodeType.Element)
                return null;

            if (element.Attributes["data-mention-id"] != null)
            {
                // The span's text becomes the mention's text attribute.
                return AdfNode.CreateMention(
                    GetDataAttribute(element, "data-mention-id"),
                    InnerTextOf(element),
                    NullIfEmpty(GetDataAttribute(element, "data-access-level")),
                    NullIfEmpty(GetDataAttribute(element, "data-user-type")));
            }

            if (element.Attributes["data-timestamp"] != null)
                return AdfNode.CreateDate(GetDataAttribute(element, "data-timestamp"));

            if (element.Attributes["data-status-color"] != null)
            {
                return AdfNode.CreateStatus(
                    InnerTextOf(element),
                    GetDataAttribute(element, "data-status-color"),
                    NullIfEmpty(GetDataAttribute(element, "data-local-id")));
            }

            return null;
        }

        /// <summary>
        /// Gets an attribute's decoded value, or an empty string when absent.
        /// </summary>
        private static string GetDataAttribute(HtmlNode element, string name)
        {
            return WebUtility.HtmlDecode(element.GetAttributeValue(name, ""));
        }

        /// <summary>
        /// Gets an element's decoded, trimmed text content.
        /// </summary>
        private static string InnerTextOf(HtmlNode element)
        {
            return WebUtility.HtmlDecode(element.InnerText).Trim();
        }

        /// <summary>
        /// Returns <c>null</c> for an empty string, otherwise the string itself.
        /// </summary>
        private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;

        /// <summary>
        /// HTML tag names treated as block-level elements.
        /// </summary>
        private static readonly HashSet<string> BlockElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "p", "div", "h1", "h2", "h3", "h4", "h5", "h6", "ul", "ol", "li", "blockquote", "pre", "hr",
            "table", "tr", "td", "th", "thead", "tbody", "tfoot", "details"
        };

        /// <summary>
        /// Determines whether the given HTML tag name corresponds to a block-level element.
        /// </summary>
        /// <param name="tagName">The HTML tag name to check.</param>
        /// <returns>True if the tag is a block-level element; otherwise, false.</returns>
        private static bool IsBlockElement(string tagName)
        {
            return BlockElements.Contains(tagName);
        }

        /// <summary>
        /// Determines whether the given HTML element has at least one block-level child element.
        /// </summary>
        /// <param name="element">The HTML element to check.</param>
        /// <returns>True if any direct child element is block-level; otherwise, false.</returns>
        private static bool HasBlockChild(HtmlNode element)
        {
            return element.ChildNodes.Any(n => n.NodeType == HtmlNodeType.Element && IsBlockElement(n.Name));
        }

        /// <summary>
        /// Processes the inline content of the given HTML element and adds it to the specified ADF (Atlassian Document Format) container node.
        /// </summary>
        /// <param name="element">The HTML element whose inline content is to be processed.</param>
        /// <param name="container">The ADF container node to which the processed content will be added.</param>
        private void ProcessInlineElements(HtmlNode element, AdfNode container)
        {
            ProcessInlineNodes(element.ChildNodes, container);
        }

        /// <summary>
        /// Converts a sequence of inline HTML nodes (text and inline elements) and adds the results to the
        /// specified ADF container node.
        /// </summary>
        /// <param name="nodes">The HTML nodes to convert.</param>
        /// <param name="container">The ADF container node to which the converted content will be added.</param>
        private void ProcessInlineNodes(IEnumerable<HtmlNode> nodes, AdfNode container)
        {
            List<HtmlNode> nodeList = nodes.ToList();

            for (int i = 0; i < nodeList.Count; i++)
            {
                HtmlNode childNode = nodeList[i];

                if (childNode.NodeType == HtmlNodeType.Text)
                {
                    string text = TextUtils.NormalizeWhitespace(WebUtility.HtmlDecode(childNode.InnerText));

                    // Whitespace-only text is formatting noise at the edges of a container, but between two
                    // pieces of inline content (e.g. "<em>a</em> <b>b</b>") it's a real word separator.
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        if (text.Length > 0 && container.Content?.Count > 0 && HasInlineContentAfter(nodeList, i))
                            container.AddContent(AdfNode.CreateText(" "));
                        continue;
                    }

                    List<AdfMark> adfMarkList = CollectMarksFromAncestors(childNode);
                    container.AddContent(AdfNode.CreateText(text, adfMarkList.Count > 0 ? adfMarkList : null));
                }
                else if (childNode.NodeType == HtmlNodeType.Element)
                {
                    AdfNode? inlineNode = TryConvertInlineNode(childNode);

                    if (inlineNode != null)
                        container.AddContent(inlineNode);
                    else if (string.Equals(childNode.Name, "br", StringComparison.OrdinalIgnoreCase))
                        container.AddContent(AdfNode.CreateHardBreak());
                    else if (IsBlockElement(childNode.Name))
                    {
                        AdfNode? adf = ConvertElementToAdf(childNode);
                        if (adf != null)
                            container.AddContent(adf);
                    }
                    else
                        ProcessInlineElements(childNode, container);
                }
            }
        }

        /// <summary>
        /// Converts the given HTML heading element to an ADF (Atlassian Document Format) node.
        /// </summary>
        /// <param name="element">The HTML heading element to convert.</param>
        /// <param name="level">The level of the heading (e.g., 1 for h1, 2 for h2).</param>
        /// <returns>The corresponding ADF node.</returns>
        private AdfNode ConvertHeading(HtmlNode element, int level)
        {
            AdfNode heading = AdfNode.CreateHeading(level);
            ProcessInlineElements(element, heading);
            return heading;
        }

        /// <summary>
        /// Converts the given HTML unordered list (ul) element to an ADF (Atlassian Document Format) node.
        /// </summary>
        /// <param name="element">The HTML unordered list element to convert.</param>
        /// <returns>The corresponding ADF node.</returns>
        private AdfNode ConvertBulletList(HtmlNode element)
        {
            AdfNode bulletList = AdfNode.CreateBulletList();
            AddListItems(element, bulletList);
            return bulletList;
        }

        /// <summary>
        /// Converts the given HTML ordered list (ol) element to an ADF (Atlassian Document Format) node.
        /// </summary>
        /// <param name="element">The HTML ordered list element to convert.</param>
        /// <returns>The corresponding ADF node.</returns>
        private AdfNode ConvertOrderedList(HtmlNode element)
        {
            // A start below 0 (legal in HTML) has no ADF equivalent and is ignored.
            AdfNode orderedList = AdfNode.CreateOrderedList(order: ParseInteger(element.GetAttributeValue("start", ""), min: 0));
            AddListItems(element, orderedList);
            return orderedList;
        }

        /// <summary>
        /// Parses a whole number of at least <paramref name="min"/>, or returns <c>null</c>.
        /// </summary>
        private static int? ParseInteger(string value, int min)
        {
            return int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) && result >= min
                ? result
                : (int?)null;
        }

        /// <summary>
        /// Parses a number of at least <paramref name="min"/> (and above it, unless <paramref name="allowMin"/>) as a
        /// boxed <see cref="int"/> when it's whole, otherwise a <see cref="double"/>, per the Attrs contract; or
        /// returns <c>null</c>.
        /// </summary>
        private static object? ParseNumber(string value, double min, bool allowMin)
        {
            if (!double.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double result)
                || double.IsNaN(result) || double.IsInfinity(result) || result < min || (!allowMin && result == min))
                return null;
            return result == Math.Floor(result) && result <= int.MaxValue ? (object)(int)result : result;
        }

        /// <summary>
        /// Determines whether a <c>&lt;ul&gt;</c>/<c>&lt;ol&gt;</c> is a task list: it carries
        /// <c>data-adf-type="taskList"</c> (as <see cref="AdfToHtmlConverter"/> renders it), or it has items and
        /// every item starts with a checkbox (as GitHub-flavored-Markdown renderers produce).
        /// </summary>
        private static bool IsTaskList(HtmlNode element)
        {
            if (string.Equals(element.GetAttributeValue("data-adf-type", ""), "taskList", StringComparison.OrdinalIgnoreCase))
                return true;

            List<HtmlNode> items = ChildElements(element, "li").ToList();
            return items.Count > 0 && items.All(item => LeadingCheckbox(item) != null);
        }

        /// <summary>
        /// Returns the checkbox <c>&lt;input&gt;</c> a list item starts with (directly, or at the start of a leading
        /// <c>&lt;p&gt;</c>, as loose GFM task lists render), or <c>null</c> if it doesn't start with one.
        /// </summary>
        private static HtmlNode? LeadingCheckbox(HtmlNode element)
        {
            HtmlNode? first = element.ChildNodes.FirstOrDefault(n =>
                n.NodeType == HtmlNodeType.Element || (n.NodeType == HtmlNodeType.Text && !string.IsNullOrWhiteSpace(n.InnerText)));

            if (first == null || first.NodeType != HtmlNodeType.Element)
                return null;
            if (string.Equals(first.Name, "input", StringComparison.OrdinalIgnoreCase))
                return string.Equals(first.GetAttributeValue("type", ""), "checkbox", StringComparison.OrdinalIgnoreCase) ? first : null;
            return string.Equals(first.Name, "p", StringComparison.OrdinalIgnoreCase) ? LeadingCheckbox(first) : null;
        }

        /// <summary>
        /// Converts a task list <c>&lt;ul&gt;</c>/<c>&lt;ol&gt;</c> (see <see cref="IsTaskList"/>) to an ADF task
        /// list. A list nested directly in it, or a task list nested in one of its items, becomes a nested task
        /// list after that item. A missing <c>data-local-id</c> gets a new GUID.
        /// </summary>
        /// <param name="element">The HTML list element to convert.</param>
        /// <returns>The corresponding ADF task list node.</returns>
        private AdfNode ConvertTaskList(HtmlNode element)
        {
            AdfNode taskList = AdfNode.CreateTaskList(localId: NullIfEmpty(GetDataAttribute(element, "data-local-id")));

            foreach (HtmlNode childNode in element.ChildNodes.Where(n => n.NodeType == HtmlNodeType.Element).ToList())
            {
                switch (childNode.Name.ToLowerInvariant())
                {
                    case "li":
                        AddTaskItem(childNode, taskList);
                        break;
                    case "ul":
                    case "ol":
                        taskList.AddContent(ConvertTaskList(childNode));
                        break;
                }
            }
            return taskList;
        }

        /// <summary>
        /// Converts a task list's <c>&lt;li&gt;</c> to a task item (or, with <c>data-adf-type="blockTaskItem"</c>,
        /// a block task item) and adds it to <paramref name="taskList"/>, followed by any task lists nested in the
        /// item. The state is <c>data-task-state</c> if present, otherwise <c>DONE</c> when the checkbox is
        /// checked.
        /// </summary>
        /// <param name="element">The HTML list item element.</param>
        /// <param name="taskList">The ADF task list to add the item to.</param>
        private void AddTaskItem(HtmlNode element, AdfNode taskList)
        {
            HtmlNode? checkbox = LeadingCheckbox(element);
            string state = element.Attributes["data-task-state"] != null
                ? GetDataAttribute(element, "data-task-state")
                : checkbox?.Attributes["checked"] != null ? "DONE" : "TODO";
            string? localId = NullIfEmpty(GetDataAttribute(element, "data-local-id"));

            // The checkbox is the state, not content, and nested task lists become siblings of the item in ADF.
            // The parsed document is private to this conversion.
            checkbox?.Remove();
            List<HtmlNode> nestedLists = element.ChildNodes
                .Where(n => n.NodeType == HtmlNodeType.Element && (n.Name == "ul" || n.Name == "ol") && IsTaskList(n))
                .ToList();
            foreach (HtmlNode nested in nestedLists)
                nested.Remove();

            AdfNode item;
            if (string.Equals(element.GetAttributeValue("data-adf-type", ""), "blockTaskItem", StringComparison.OrdinalIgnoreCase))
            {
                item = AdfNode.CreateBlockTaskItem(state, localId: localId);
                ConvertContainerContent(element, item);
                if (item.Content![0].Type == AdfNodeType.Paragraph)
                    TrimEdgeSpace(item.Content[0], trimEnd: false);
            }
            else
            {
                item = AdfNode.CreateTaskItem(state, localId: localId);
                ProcessInlineNodes(element.ChildNodes, item);
                TrimEdgeSpace(item, nestedLists.Count > 0);
            }

            taskList.AddContent(item);
            foreach (HtmlNode nested in nestedLists)
                taskList.AddContent(ConvertTaskList(nested));
        }

        /// <summary>
        /// Removes the whitespace a task item's text starts with (the space after the checkbox) and ends with
        /// (the line break before a nested list that was taken out, when <paramref name="trimEnd"/> is set), dropping
        /// an edge text node if nothing is left of it. An edge paragraph (a loose item's <c>&lt;p&gt;</c>, which
        /// <see cref="AdfNormalizer"/> later unwraps into a task item) is trimmed inside.
        /// </summary>
        private static void TrimEdgeSpace(AdfNode container, bool trimEnd)
        {
            TrimEdge(container, fromStart: true);
            if (trimEnd)
                TrimEdge(container, fromStart: false);
        }

        private static void TrimEdge(AdfNode container, bool fromStart)
        {
            if (container.Content == null || container.Content.Count == 0)
                return;

            int index = fromStart ? 0 : container.Content.Count - 1;
            AdfNode edge = container.Content[index];

            if (edge.Type == AdfNodeType.Paragraph)
            {
                TrimEdge(edge, fromStart);
                return;
            }
            if (edge.Type != AdfNodeType.Text)
                return;

            edge.Text = fromStart ? edge.Text?.TrimStart() : edge.Text?.TrimEnd();
            if (string.IsNullOrEmpty(edge.Text))
                container.Content.RemoveAt(index);
        }

        /// <summary>
        /// Determines whether any node after <paramref name="index"/> is an element or non-whitespace text.
        /// </summary>
        private static bool HasInlineContentAfter(List<HtmlNode> nodes, int index)
        {
            for (int i = index + 1; i < nodes.Count; i++)
            {
                if (nodes[i].NodeType == HtmlNodeType.Element || !string.IsNullOrWhiteSpace(nodes[i].InnerText))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Converts the &lt;li&gt; children of an HTML list element and adds them to the given ADF list node.
        /// </summary>
        /// <param name="element">The HTML list (ul/ol) element.</param>
        /// <param name="list">The ADF list node to add the converted list items to.</param>
        private void AddListItems(HtmlNode element, AdfNode list)
        {
            foreach (HtmlNode childNode in ChildElements(element, "li"))
                list.AddContent(ConvertListItem(childNode));
        }

        /// <summary>
        /// Returns the direct child elements of the given HTML node whose tag name matches (case-insensitively).
        /// </summary>
        /// <param name="element">The parent HTML node.</param>
        /// <param name="tagName">The tag name to match.</param>
        /// <returns>The matching child elements, in document order.</returns>
        private static IEnumerable<HtmlNode> ChildElements(HtmlNode element, string tagName)
        {
            return element.ChildNodes.Where(n => n.NodeType == HtmlNodeType.Element
                && string.Equals(n.Name, tagName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Converts the given HTML list item (li) element to an ADF (Atlassian Document Format) node.
        /// </summary>
        /// <param name="element">The HTML list item element to convert.</param>
        /// <returns>The corresponding ADF node.</returns>
        private AdfNode ConvertListItem(HtmlNode element)
        {
            AdfNode listItem = AdfNode.CreateListItem();
            ConvertContainerContent(element, listItem);
            return listItem;
        }

        /// <summary>
        /// Converts the children of a container HTML element (e.g. a list item or table cell) into ADF content,
        /// added to the given ADF container node. Block-level children (such as the &lt;p&gt; that
        /// <see cref="AdfToHtmlConverter"/> nests inside list items and table cells) are converted directly rather
        /// than being folded into an extra wrapping paragraph; runs of inline content are each wrapped in one
        /// paragraph. A container that yields no content at all gets a single empty paragraph, since every
        /// ADF container node here requires at least one child.
        /// </summary>
        /// <param name="element">The HTML container element whose children are to be converted.</param>
        /// <param name="container">The ADF node to which the converted content will be added.</param>
        private void ConvertContainerContent(HtmlNode element, AdfNode container)
        {
            ProcessBlockElements(element, container);

            if (container.Content == null || container.Content.Count == 0)
                container.AddContent(AdfNode.CreateParagraph());
        }

        /// <summary>
        /// Converts the given HTML blockquote element to an ADF (Atlassian Document Format) node.
        /// </summary>
        /// <param name="element">The HTML blockquote element to convert.</param>
        /// <returns>The corresponding ADF node.</returns>
        private AdfNode ConvertBlockquote(HtmlNode element)
        {
            AdfNode blockquote = AdfNode.CreateBlockquote();
            ProcessBlockElements(element, blockquote);
            return blockquote;
        }

        /// <summary>
        /// Converts a <c>&lt;details&gt;</c> element to an ADF expand: a <c>nestedExpand</c> when it carries
        /// <c>data-adf-type="nestedExpand"</c>, otherwise an <c>expand</c>. The first <c>&lt;summary&gt;</c>'s text
        /// becomes the title (no summary means no title). Plain <c>&lt;details&gt;</c> from arbitrary HTML gets the
        /// right type from its placement: <see cref="AdfNormalizer"/> turns an <c>expand</c> in a table cell or
        /// another expand into a <c>nestedExpand</c>, and a <c>nestedExpand</c> at top level into an <c>expand</c>.
        /// </summary>
        /// <param name="element">The HTML details element to convert.</param>
        /// <returns>The corresponding ADF node.</returns>
        private AdfNode ConvertDetails(HtmlNode element)
        {
            HtmlNode? summary = ChildElements(element, "summary").FirstOrDefault();
            string? title = summary == null ? null : Regex.Replace(InnerTextOf(summary), @"\s+", " ");

            // The summary is the title, not content. The parsed document is private to this conversion.
            summary?.Remove();

            AdfNode expand = string.Equals(element.GetAttributeValue("data-adf-type", ""), "nestedExpand", StringComparison.OrdinalIgnoreCase)
                ? AdfNode.CreateNestedExpand(title)
                : AdfNode.CreateExpand(title);
            ConvertContainerContent(element, expand);
            return expand;
        }

        /// <summary>
        /// Converts the given HTML code block element to an ADF (Atlassian Document Format) node.
        /// </summary>
        /// <param name="element">The HTML code block element to convert.</param>
        /// <returns>The corresponding ADF node.</returns>
        private AdfNode ConvertCodeBlock(HtmlNode element)
        {
            HtmlNode? codeElement = element.SelectSingleNode(".//code");
            string? language = null;

            if (codeElement != null)
            {
                Match match = Regex.Match(codeElement.GetAttributeValue("class", ""), "(?:language-|lang-)(\\w+)");
                if (match.Success)
                    language = match.Groups[1].Value;
            }

            string text = WebUtility.HtmlDecode((codeElement ?? element).InnerText);

            AdfNode codeBlock = AdfNode.CreateCodeBlock(language);
            codeBlock.AddContent(AdfNode.CreateText(text));
            return codeBlock;
        }

        /// <summary>
        /// Converts the given HTML table element to an ADF (Atlassian Document Format) node.
        /// </summary>
        /// <param name="element">The HTML table element to convert.</param>
        /// <returns>The corresponding ADF node.</returns>
        private AdfNode ConvertTable(HtmlNode element)
        {
            AdfNode table = AdfNode.CreateTable();
            ConvertTableAttrs(element, table);

            foreach (HtmlNode childNode in element.ChildNodes.Where(n => n.NodeType == HtmlNodeType.Element))
            {
                switch (childNode.Name.ToLowerInvariant())
                {
                    case "tr":
                        table.AddContent(ConvertTableRow(childNode));
                        break;
                    case "tbody":
                    case "thead":
                    case "tfoot":
                        foreach (HtmlNode row in ChildElements(childNode, "tr"))
                            table.AddContent(ConvertTableRow(row));
                        break;
                }
            }
            return table;
        }

        /// <summary>
        /// Converts the given HTML table row (tr) element to an ADF (Atlassian Document Format) node.
        /// </summary>
        /// <param name="element">The HTML table row element to convert.</param>
        /// <returns>The corresponding ADF node.</returns>
        private AdfNode ConvertTableRow(HtmlNode element)
        {
            AdfNode tableRow = AdfNode.CreateTableRow();
            foreach (HtmlNode childNode in element.ChildNodes.Where(n => n.NodeType == HtmlNodeType.Element))
            {
                switch (childNode.Name.ToLowerInvariant())
                {
                    case "td":
                        tableRow.AddContent(ConvertTableCell(childNode));
                        break;
                    case "th":
                        tableRow.AddContent(ConvertTableHeader(childNode));
                        break;
                }
            }
            return tableRow;
        }

        /// <summary>
        /// Converts the given HTML table cell (td) element to an ADF (Atlassian Document Format) node.
        /// </summary>
        /// <param name="element">The HTML table cell element to convert.</param>
        /// <returns>The corresponding ADF node.</returns>
        private AdfNode ConvertTableCell(HtmlNode element)
        {
            AdfNode tableCell = AdfNode.CreateTableCell();
            ConvertTableCellAttrs(element, tableCell);
            if (HasTextContent(element))
                ConvertContainerContent(element, tableCell);
            else
                tableCell.AddContent(AdfNode.CreateParagraph());
            return tableCell;
        }

        /// <summary>
        /// Converts the given HTML table header (th) element to an ADF (Atlassian Document Format) node.
        /// </summary>
        /// <param name="element">The HTML table header element to convert.</param>
        /// <returns>The corresponding ADF node.</returns>
        private AdfNode ConvertTableHeader(HtmlNode element)
        {
            AdfNode tableHeader = AdfNode.CreateTableHeader();
            ConvertTableCellAttrs(element, tableHeader);
            ConvertContainerContent(element, tableHeader);
            return tableHeader;
        }

        /// <summary>
        /// Matches a CSS value the table cell pages accept as a <c>background</c>: a short or long hex color, or a
        /// color name.
        /// </summary>
        private static readonly Regex CellBackgroundRegex = new Regex(
            "^(#[0-9a-fA-F]{3}|#[0-9a-fA-F]{6}|(?!(transparent|none|inherit|initial|unset|revert|currentcolor)$)[a-zA-Z]+)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Determines whether the given HTML node is a <c>&lt;td&gt;</c> or <c>&lt;th&gt;</c>.
        /// </summary>
        private static bool IsTableCell(HtmlNode node)
        {
            return string.Equals(node.Name, "td", StringComparison.OrdinalIgnoreCase)
                || string.Equals(node.Name, "th", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Reads the <c>data-layout</c>, <c>data-width</c>, <c>data-display-mode</c> and
        /// <c>data-number-column-enabled</c> attributes that <see cref="AdfToHtmlConverter"/> writes into the table's
        /// attrs. Values ADF wouldn't accept (e.g. a non-numeric width) are skipped.
        /// </summary>
        private static void ConvertTableAttrs(HtmlNode element, AdfNode table)
        {
            string layout = GetDataAttribute(element, "data-layout");
            if (layout.Length > 0)
                SetAttr(table, "layout", layout);

            object? width = ParseNumber(GetDataAttribute(element, "data-width"), min: 0, allowMin: false);
            if (width != null)
                SetAttr(table, "width", width);

            string displayMode = GetDataAttribute(element, "data-display-mode");
            if (displayMode.Length > 0)
                SetAttr(table, "displayMode", displayMode);

            switch (GetDataAttribute(element, "data-number-column-enabled").ToLowerInvariant())
            {
                case "true":
                    SetAttr(table, "isNumberColumnEnabled", true);
                    break;
                case "false":
                    SetAttr(table, "isNumberColumnEnabled", false);
                    break;
            }
        }

        /// <summary>
        /// Reads a <c>&lt;td&gt;</c>/<c>&lt;th&gt;</c>'s <c>colspan</c>/<c>rowspan</c>, its background color (the
        /// <c>background-color</c> style, a <c>background</c> shorthand that's only a color, or the legacy
        /// <c>bgcolor</c> attribute) and <c>data-colwidth</c> into the cell's attrs. Values ADF wouldn't accept (a
        /// span below 1, a background such as <c>rgb(…)</c>) are skipped.
        /// </summary>
        private static void ConvertTableCellAttrs(HtmlNode element, AdfNode cell)
        {
            int? colspan = ParseInteger(element.GetAttributeValue("colspan", ""), min: 1);
            if (colspan != null)
                SetAttr(cell, "colspan", colspan.Value);

            int? rowspan = ParseInteger(element.GetAttributeValue("rowspan", ""), min: 1);
            if (rowspan != null)
                SetAttr(cell, "rowspan", rowspan.Value);

            string? background = null;
            foreach ((string property, string value) in ParseCssStyle(element.GetAttributeValue("style", "")))
            {
                string name = property.ToLowerInvariant();
                if ((name == "background-color" || name == "background") && CellBackgroundRegex.IsMatch(value))
                    background = value;
            }
            background ??= CellBackgroundRegex.IsMatch(element.GetAttributeValue("bgcolor", "")) ? element.GetAttributeValue("bgcolor", "") : null;
            if (background != null)
                SetAttr(cell, "background", background);

            string colwidth = GetDataAttribute(element, "data-colwidth");
            if (colwidth.Length > 0)
            {
                List<object?> widths = colwidth.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(width => ParseNumber(width, min: 0, allowMin: true))
                    .ToList();
                if (widths.Count > 0 && widths.All(width => width != null))
                    SetAttr(cell, "colwidth", widths.Cast<object>().ToList());
            }
        }

        /// <summary>
        /// Sets an attribute on the node, creating its attrs dictionary if needed.
        /// </summary>
        private static void SetAttr(AdfNode node, string key, object value)
        {
            node.Attrs ??= new Dictionary<string, object>();
            node.Attrs[key] = value;
        }

        /// <summary>
        /// Determines whether the given HTML node has any text content.
        /// </summary>
        /// <param name="node">The HTML node to check.</param>
        /// <returns><c>true</c> if the node has text content; otherwise, <c>false</c>.</returns>
        private bool HasTextContent(HtmlNode node)
        {
            return node.Descendants().Any(n =>
                (n.NodeType == HtmlNodeType.Text && !string.IsNullOrWhiteSpace(n.InnerText))
                || string.Equals(n.Name, "br", StringComparison.OrdinalIgnoreCase));
        }
    }
}