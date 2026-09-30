// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Enums;
using AdfDotNet.Models;
using Markdig;
using Markdig.Extensions.Alerts;
using Markdig.Extensions.Tables;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace AdfDotNet.FormatConverters
{
    /// <summary>
    /// Converts Markdown content to ADF (Atlassian Document Format) representation.
    /// </summary>
    public class MarkdownToAdfConverter
    {
        /// <summary>
        /// The Markdig pipeline used to parse Markdown. Advanced extensions bring in pipe tables (needed
        /// for ADF's <c>table</c> node) and strikethrough (<c>~~text~~</c>, needed for the <c>strike</c>
        /// mark) alongside several extensions this converter doesn't otherwise use.
        /// </summary>
        private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

        /// <summary>
        /// Creates a new instance of the MarkdownToAdfConverter class.
        /// </summary>
        /// <returns>A new instance of the MarkdownToAdfConverter class.</returns>
        public static MarkdownToAdfConverter Create() => new MarkdownToAdfConverter();

        /// <summary>
        /// Converts the given Markdown string to an ADF (Atlassian Document Format) document.
        /// </summary>
        /// <param name="markdown">The Markdown string to convert.</param>
        /// <returns>An ADF document representing the Markdown content.</returns>
        public static AdfDocument Convert(string markdown) => new MarkdownToAdfConverter().ConvertMarkdown(markdown);

        /// <summary>
        /// Converts the given Markdown string to an ADF (Atlassian Document Format) document.
        /// </summary>
        /// <param name="markdown">The Markdown string to convert.</param>
        /// <returns>An ADF document representing the Markdown content.</returns>
        public AdfDocument ConvertMarkdown(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
            {
                AdfDocument empty = AdfNode.CreateDocument();
                empty.AddContent(AdfNode.CreateParagraph());
                return empty;
            }

            MarkdownDocument parsed = Markdig.Markdown.Parse(markdown, Pipeline);
            AdfDocument document = AdfNode.CreateDocument();
            ConvertBlocks(parsed, document);

            // Markdown nesting is copied as-is above; reshape whatever ADF doesn't allow (a heading inside a
            // quote, a quote inside a list item, a body-less alert, code + strong, ...).
            document.Normalize();

            if (document.Content == null || document.Content.Count == 0)
                document.AddContent(AdfNode.CreateParagraph());

            return document;
        }

        /// <summary>
        /// Matches an HTML block that only opens a <c>&lt;details&gt;</c>, optionally with its
        /// <c>&lt;summary&gt;</c> (group 1 is the summary's inner HTML).
        /// </summary>
        private static readonly Regex DetailsOpenRegex = new Regex(
            @"^\s*<details(?:\s[^>]*)?>\s*(?:<summary(?:\s[^>]*)?>(.*?)</summary>\s*)?$",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// Matches an HTML block that only closes a <c>&lt;details&gt;</c>.
        /// </summary>
        private static readonly Regex DetailsCloseRegex = new Regex(@"^\s*</details>\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Matches a whole <c>&lt;details&gt;</c> written as one HTML block (no blank line inside); group 1 is the
        /// summary's inner HTML and group 2 the Markdown between the summary and <c>&lt;/details&gt;</c>.
        /// </summary>
        private static readonly Regex DetailsWholeRegex = new Regex(
            @"^\s*<details(?:\s[^>]*)?>\s*(?:<summary(?:\s[^>]*)?>(.*?)</summary>)?(.*)</details>\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex HtmlTagRegex = new Regex("<[^>]*>", RegexOptions.Compiled);

        /// <summary>
        /// Matches a raw inline <c>&lt;br&gt;</c> tag (<c>&lt;br&gt;</c>, <c>&lt;br/&gt;</c>, <c>&lt;BR /&gt;</c>).
        /// </summary>
        private static readonly Regex BrTagRegex = new Regex(@"^<br\s*/?>$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Converts a sequence of Markdig blocks and adds the corresponding ADF nodes to the parent node. A
        /// <c>&lt;details&gt;</c> HTML block and its matching <c>&lt;/details&gt;</c> HTML block become an
        /// <c>expand</c> holding the blocks between them (<see cref="AdfNormalizer"/> turns it into a
        /// <c>nestedExpand</c> where that's the legal form); an unmatched one is ignored, like other raw HTML.
        /// </summary>
        private void ConvertBlocks(IEnumerable<Block> blocks, AdfNode parent)
        {
            List<Block> list = blocks.ToList();

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is HtmlBlock html)
                {
                    string source = html.Lines.ToString();

                    Match open = DetailsOpenRegex.Match(source);
                    int close = open.Success ? FindDetailsClose(list, i + 1) : -1;
                    if (close >= 0)
                    {
                        AdfNode expand = AdfNode.CreateExpand(SummaryTitle(open.Groups[1]));
                        ConvertBlocks(list.GetRange(i + 1, close - i - 1), expand);
                        AddExpand(parent, expand);
                        i = close;
                        continue;
                    }

                    Match whole = DetailsWholeRegex.Match(source);
                    if (whole.Success)
                    {
                        AdfNode expand = AdfNode.CreateExpand(SummaryTitle(whole.Groups[1]));
                        ConvertBlocks(Markdig.Markdown.Parse(whole.Groups[2].Value, Pipeline), expand);
                        AddExpand(parent, expand);
                        continue;
                    }
                }

                AdfNode? node = ConvertBlock(list[i]);
                if (node != null)
                    parent.AddContent(node);
            }
        }

        /// <summary>
        /// Returns the index of the <c>&lt;/details&gt;</c> HTML block closing a <c>&lt;details&gt;</c> opened
        /// just before <paramref name="start"/>, skipping nested pairs, or -1 if there is none.
        /// </summary>
        private static int FindDetailsClose(List<Block> blocks, int start)
        {
            int depth = 0;
            for (int i = start; i < blocks.Count; i++)
            {
                if (blocks[i] is not HtmlBlock html)
                    continue;

                string source = html.Lines.ToString();
                if (DetailsOpenRegex.IsMatch(source))
                    depth++;
                else if (DetailsCloseRegex.IsMatch(source) && depth-- == 0)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Gets the plain-text title from a <c>&lt;summary&gt;</c>'s inner HTML (tags stripped, entities
        /// decoded), or <c>null</c> when there was no summary.
        /// </summary>
        private static string? SummaryTitle(Group summary)
        {
            if (!summary.Success)
                return null;
            string text = WebUtility.HtmlDecode(HtmlTagRegex.Replace(summary.Value, string.Empty));
            return Regex.Replace(text, @"\s+", " ").Trim();
        }

        /// <summary>
        /// Adds a converted expand to <paramref name="parent"/>, with an empty paragraph if it has no content
        /// (an expand needs at least one child).
        /// </summary>
        private static void AddExpand(AdfNode parent, AdfNode expand)
        {
            if (expand.Content == null || expand.Content.Count == 0)
                expand.AddContent(AdfNode.CreateParagraph());
            parent.AddContent(expand);
        }

        /// <summary>
        /// Converts a single Markdig block to an ADF node, or <c>null</c> if the block has no ADF
        /// equivalent (e.g. raw HTML blocks, link reference definitions).
        /// </summary>
        private AdfNode? ConvertBlock(Block block)
        {
            if (AdfMarkdownStructure.TryGetDirectNodeType(block, out AdfNodeType nodeType))
            {
                switch (nodeType)
                {
                    case AdfNodeType.Paragraph:
                        return ConvertParagraph((ParagraphBlock)block);
                    case AdfNodeType.Blockquote:
                        return ConvertBlockquote((QuoteBlock)block);
                    case AdfNodeType.Rule:
                        return AdfNode.CreateRule();
                }
            }

            switch (block)
            {
                case HeadingBlock heading:
                    return ConvertHeading(heading);
                case ListBlock list:
                    return ConvertList(list);
                case FencedCodeBlock fenced:
                    return ConvertCodeBlock(fenced, fenced.Info);
                case CodeBlock code:
                    return ConvertCodeBlock(code, null);
                case Table table:
                    return ConvertTable(table);
                case AlertBlock alert:
                    return ConvertPanel(alert);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Converts the given Markdig paragraph block to an ADF (Atlassian Document Format) node.
        /// </summary>
        private AdfNode ConvertParagraph(ParagraphBlock block)
        {
            List<AdfNode> children = block.Inline == null ? new List<AdfNode>() : ConvertInlineChildren(block.Inline, null);

            AdfNode paragraph = AdfNode.CreateParagraph();
            foreach (AdfNode child in children)
                paragraph.AddContent(child);
            return paragraph;
        }

        /// <summary>
        /// Converts the given Markdig heading block to an ADF (Atlassian Document Format) node.
        /// </summary>
        private AdfNode ConvertHeading(HeadingBlock block)
        {
            int level = block.Level < 1 || block.Level > 6 ? 1 : block.Level;
            AdfNode heading = AdfNode.CreateHeading(level);
            if (block.Inline != null)
                foreach (AdfNode child in ConvertInlineChildren(block.Inline, null))
                    heading.AddContent(child);
            return heading;
        }

        /// <summary>
        /// Converts the given Markdig blockquote block to an ADF (Atlassian Document Format) node.
        /// </summary>
        private AdfNode ConvertBlockquote(QuoteBlock block)
        {
            AdfNode blockquote = AdfNode.CreateBlockquote();
            ConvertBlocks(block, blockquote);
            return blockquote;
        }

        /// <summary>
        /// Converts a GitHub-flavored-Markdown alert (<c>&gt; [!NOTE]</c> ...) to an ADF panel node, mapping
        /// the alert kind to a panel type via <see cref="AdfMarkdownStructure.GetPanelType"/>.
        /// </summary>
        private AdfNode ConvertPanel(AlertBlock block)
        {
            AdfNode panel = AdfNode.CreatePanel(AdfMarkdownStructure.GetPanelType(block.Kind.ToString()));

            // Markdig keeps the "[!KIND]" marker line as a leading paragraph whose inlines were consumed
            // by the alert parser - skip it rather than emitting an empty paragraph into the panel.
            IEnumerable<Block> content = block.Count > 0 && block[0] is ParagraphBlock marker && marker.Inline?.FirstChild == null
                ? block.Skip(1)
                : block;

            ConvertBlocks(content, panel);
            return panel;
        }

        /// <summary>
        /// Converts the given Markdig list block to an ADF (Atlassian Document Format) node.
        /// </summary>
        private AdfNode ConvertList(ListBlock block)
        {
            if (IsTaskList(block))
                return ConvertTaskList(block);

            // A list starting at 1 is ADF's default, so it gets no order attribute.
            AdfNode list = block.IsOrdered
                ? AdfNode.CreateOrderedList(order: int.TryParse(block.OrderedStart, NumberStyles.None, CultureInfo.InvariantCulture, out int start) && start != 1 ? start : (int?)null)
                : AdfNode.CreateBulletList();

            foreach (Block child in block)
            {
                if (child is ListItemBlock listItem)
                {
                    AdfNode item = AdfNode.CreateListItem();
                    ConvertBlocks(listItem, item);
                    if (item.Content == null || item.Content.Count == 0)
                        item.AddContent(AdfNode.CreateParagraph());
                    list.AddContent(item);
                }
            }
            return list;
        }

        /// <summary>
        /// Determines whether a Markdig list is a GitHub-flavored-Markdown task list: every item starts with a
        /// <c>[ ]</c>/<c>[x]</c> marker. A list mixing task and plain items stays a plain list.
        /// </summary>
        private static bool IsTaskList(ListBlock block) =>
            block.Count > 0 && block.All(b => b is ListItemBlock item && TaskMarker(item) != null);

        /// <summary>
        /// Returns the <c>[ ]</c>/<c>[x]</c> marker a list item starts with, or <c>null</c>.
        /// </summary>
        private static TaskList? TaskMarker(ListItemBlock item) =>
            item.Count > 0 && item[0] is ParagraphBlock paragraph ? paragraph.Inline?.FirstChild as TaskList : null;

        /// <summary>
        /// Converts a GitHub-flavored-Markdown task list to an ADF task list. An item holding only its first
        /// paragraph becomes a <c>taskItem</c> with that paragraph's inline content; an item with more blocks
        /// becomes a <c>blockTaskItem</c> (<see cref="AdfNormalizer"/> reshapes blocks that aren't paragraphs). A
        /// task list nested in an item becomes a nested task list after it. Markdown has no <c>localId</c>, so
        /// every node gets a new GUID.
        /// </summary>
        private AdfNode ConvertTaskList(ListBlock block)
        {
            AdfNode taskList = AdfNode.CreateTaskList();

            foreach (ListItemBlock listItem in block.OfType<ListItemBlock>())
            {
                string state = TaskMarker(listItem)!.Checked ? "DONE" : "TODO";
                List<ListBlock> nestedLists = listItem.OfType<ListBlock>().Where(IsTaskList).ToList();

                // The [ ] marker is dropped by ConvertInlineChildren, like other unsupported inlines.
                AdfNode item = AdfNode.CreateBlockTaskItem(state);
                ConvertBlocks(listItem.Where(b => !(b is ListBlock list && nestedLists.Contains(list))), item);
                TrimLeadingSpace(item.Content![0]);

                taskList.AddContent(item.Content.Count > 1 ? item : AdfNode.CreateTaskItem(state, item.Content[0].Content));
                foreach (ListBlock nested in nestedLists)
                    taskList.AddContent(ConvertTaskList(nested));
            }
            return taskList;
        }

        /// <summary>
        /// Removes the whitespace a task item's text starts with (the space after the <c>[ ]</c> marker), dropping
        /// the first text node if nothing is left of it.
        /// </summary>
        private static void TrimLeadingSpace(AdfNode container)
        {
            if (container.Content == null || container.Content.Count == 0 || container.Content[0].Type != AdfNodeType.Text)
                return;

            AdfNode text = container.Content[0];
            text.Text = text.Text?.TrimStart();
            if (string.IsNullOrEmpty(text.Text))
                container.Content.RemoveAt(0);
        }

        /// <summary>
        /// Converts the given Markdig code block (fenced or indented) to an ADF (Atlassian Document Format) node.
        /// </summary>
        private AdfNode ConvertCodeBlock(CodeBlock block, string? language)
        {
            string text = block.Lines.ToString();
            AdfNode codeBlock = AdfNode.CreateCodeBlock(string.IsNullOrWhiteSpace(language) ? null : language!.Trim());
            codeBlock.AddContent(AdfNode.CreateText(text));
            return codeBlock;
        }

        /// <summary>
        /// Converts the given Markdig pipe table to an ADF (Atlassian Document Format) node. The pipe-table
        /// format only recognizes a single header row (the first), which <see cref="Markdig.Extensions.Tables.TableRow.IsHeader"/>
        /// reflects directly.
        /// </summary>
        private AdfNode ConvertTable(Table table)
        {
            AdfNode adfTable = AdfNode.CreateTable();

            foreach (Block rowBlock in table)
            {
                if (rowBlock is not TableRow row)
                    continue;

                AdfNode adfRow = AdfNode.CreateTableRow();
                foreach (Block cellBlock in row)
                {
                    if (cellBlock is not TableCell cell)
                        continue;

                    AdfNode adfCell = row.IsHeader ? AdfNode.CreateTableHeader() : AdfNode.CreateTableCell();
                    ConvertBlocks(cell, adfCell);
                    if (adfCell.Content == null || adfCell.Content.Count == 0)
                        adfCell.AddContent(AdfNode.CreateParagraph());
                    adfRow.AddContent(adfCell);
                }
                adfTable.AddContent(adfRow);
            }
            return adfTable;
        }

        /// <summary>
        /// Converts the children of a Markdig inline container to ADF text/hard-break/media nodes,
        /// threading marks down from enclosing emphasis/code/link inlines - the reverse direction of
        /// <c>HtmlToAdfConverter.CollectMarksFromAncestors</c>, which walks HTML marks up from a leaf
        /// instead, since Markdig's inline tree already nests the way marks compose.
        /// </summary>
        private List<AdfNode> ConvertInlineChildren(ContainerInline container, List<AdfMark>? inheritedMarks)
        {
            List<AdfNode> result = new List<AdfNode>();

            foreach (Markdig.Syntax.Inlines.Inline inline in container)
            {
                switch (inline)
                {
                    case LiteralInline literal:
                        string text = literal.Content.ToString();
                        if (text.Length > 0)
                            result.Add(AdfNode.CreateText(text, CopyMarks(inheritedMarks)));
                        break;

                    case LineBreakInline lineBreak when lineBreak.IsHard:
                        result.Add(AdfNode.CreateHardBreak());
                        break;

                    case CodeInline code:
                        result.Add(AdfNode.CreateText(code.Content, AppendMark(inheritedMarks, new AdfMark(AdfMarkType.Code))));
                        break;

                    case EmphasisInline emphasis:
                        AdfMarkType markType = AdfMarkdownStructure.GetEmphasisMarkType(emphasis.DelimiterChar, emphasis.DelimiterCount);
                        result.AddRange(ConvertInlineChildren(emphasis, AppendMark(inheritedMarks, new AdfMark(markType))));
                        break;

                    case LinkInline { IsImage: true }:
                        // No Media Services id/collection can be derived from Markdown image syntax (which
                        // carries only a URL) to build a spec-conformant `media` node - dropped, matching
                        // the default fallback for unsupported inline constructs. Explicit rather than
                        // falling through to the plain-LinkInline case below, which would otherwise
                        // misrender the image as a text link.
                        break;

                    case LinkInline link:
                        Dictionary<string, object> linkAttrs = new Dictionary<string, object>() { ["href"] = link.Url ?? string.Empty };
                        if (!string.IsNullOrEmpty(link.Title))
                            linkAttrs["title"] = link.Title!;
                        result.AddRange(ConvertInlineChildren(link, AppendMark(inheritedMarks, new AdfMark(AdfMarkType.Link, linkAttrs))));
                        break;

                    case AutolinkInline autolink:
                        // <https://...> (how inlineCard renders) or <user@example.com>: the address is both
                        // the text and the link target.
                        string href = autolink.IsEmail ? "mailto:" + autolink.Url : autolink.Url;
                        Dictionary<string, object> autolinkAttrs = new Dictionary<string, object>() { ["href"] = href };
                        result.Add(AdfNode.CreateText(autolink.Url, AppendMark(inheritedMarks, new AdfMark(AdfMarkType.Link, autolinkAttrs))));
                        break;

                    case HtmlInline html when BrTagRegex.IsMatch(html.Tag):
                        // AdfToMarkdownConverter joins the blocks of a pipe-table cell with <br>. Other raw
                        // inline HTML is still dropped (see the default branch).
                        result.Add(AdfNode.CreateHardBreak());
                        break;

                    default:
                        // Unsupported inline construct (e.g. raw inline HTML) - dropped, matching
                        // HtmlToAdfConverter's fallback-to-null pattern for unknown constructs.
                        break;
                }
            }
            return CoalesceAdjacentText(result);
        }

        /// <summary>
        /// Merges adjacent <c>text</c> nodes that carry the same marks into one. CommonMark's escape
        /// handling (e.g. <c>\.</c>) gives a backslash-escaped character its own <see cref="LiteralInline"/>
        /// separate from the literal text around it, so without this merge a single escaped ADF text node
        /// (as <see cref="AdfDotNet.FormatConverters.AdfToMarkdownConverter"/> produces via its uniform
        /// escaping) would come back as several adjacent text nodes instead of the one it started as.
        /// </summary>
        private static List<AdfNode> CoalesceAdjacentText(List<AdfNode> nodes)
        {
            List<AdfNode> merged = new List<AdfNode>();
            foreach (AdfNode node in nodes)
            {
                AdfNode? last = merged.Count > 0 ? merged[merged.Count - 1] : null;
                if (last != null && last.Type == AdfNodeType.Text && node.Type == AdfNodeType.Text && MarksEqual(last.Marks, node.Marks))
                    last.Text += node.Text;
                else
                    merged.Add(node);
            }
            return merged;
        }

        /// <summary>
        /// Compares two mark lists for equality by type and attribute content, in order.
        /// </summary>
        private static bool MarksEqual(List<AdfMark>? a, List<AdfMark>? b)
        {
            bool aEmpty = a == null || a.Count == 0;
            bool bEmpty = b == null || b.Count == 0;
            if (aEmpty || bEmpty)
                return aEmpty == bEmpty;

            if (a!.Count != b!.Count)
                return false;

            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].Type != b[i].Type || !AttrsEqual(a[i].Attrs, b[i].Attrs))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Compares two mark/node attribute dictionaries for equality (same keys and values).
        /// </summary>
        private static bool AttrsEqual(Dictionary<string, object>? a, Dictionary<string, object>? b)
        {
            bool aEmpty = a == null || a.Count == 0;
            bool bEmpty = b == null || b.Count == 0;
            if (aEmpty || bEmpty)
                return aEmpty == bEmpty;

            if (a!.Count != b!.Count)
                return false;

            foreach (KeyValuePair<string, object> kv in a)
            {
                if (!b!.TryGetValue(kv.Key, out object? value) || !Equals(kv.Value, value))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Returns a new list combining <paramref name="existing"/> with <paramref name="mark"/> appended,
        /// without mutating <paramref name="existing"/> (which may be shared by sibling inlines).
        /// </summary>
        private static List<AdfMark> AppendMark(List<AdfMark>? existing, AdfMark mark)
        {
            List<AdfMark> marks = existing == null ? new List<AdfMark>() : new List<AdfMark>(existing);
            marks.Add(mark);
            return marks;
        }

        /// <summary>
        /// Returns a defensive copy of <paramref name="marks"/> (or <c>null</c>), so each text node gets
        /// its own list rather than sharing one with siblings.
        /// </summary>
        private static List<AdfMark>? CopyMarks(List<AdfMark>? marks) => marks == null ? null : new List<AdfMark>(marks);
    }
}
