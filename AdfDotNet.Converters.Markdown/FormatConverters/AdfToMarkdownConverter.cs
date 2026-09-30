// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.DataConverters;
using AdfDotNet.Enums;
using AdfDotNet.Models;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace AdfDotNet.FormatConverters
{
    /// <summary>
    /// Converts an ADF (Atlassian Document Format) document to Markdown format.
    /// </summary>
    public class AdfToMarkdownConverter
    {
        /// <summary>
        /// Matches ASCII punctuation characters that are significant to CommonMark and must be
        /// backslash-escaped when they appear in literal text, so plain text never gets misread as markup.
        /// Backslash-escaping is applied uniformly (rather than only where ambiguous) to guarantee the
        /// Markdown -&gt; ADF direction always recovers the exact original text, mirroring how
        /// <c>AdfToHtmlConverter</c> uses <see cref="System.Net.WebUtility.HtmlEncode(string)"/> uniformly
        /// rather than only where ambiguous.
        /// </summary>
        private static readonly Regex EscapeRegex = new Regex(@"[\\`*_{}\[\]()#+\-.!|~<>]", RegexOptions.Compiled);

        /// <summary>
        /// Creates a new instance of the AdfToMarkdownConverter class.
        /// </summary>
        /// <returns>A new instance of the AdfToMarkdownConverter class.</returns>
        public static AdfToMarkdownConverter Create() => new AdfToMarkdownConverter();

        /// <summary>
        /// Converts an ADF document to Markdown format.
        /// </summary>
        /// <param name="adfDocument">The ADF document to convert.</param>
        /// <returns>The Markdown representation of the ADF document.</returns>
        public static string Convert(AdfDocument adfDocument) => new AdfToMarkdownConverter().ConvertAdf(adfDocument);

        /// <summary>
        /// Converts an ADF JSON string to Markdown format.
        /// </summary>
        /// <param name="adfJson">The ADF JSON string to convert.</param>
        /// <returns>The Markdown representation of the ADF JSON string.</returns>
        public static string Convert(string adfJson) => new AdfToMarkdownConverter().ConvertAdf(adfJson);

        /// <summary>
        /// Converts an ADF document to Markdown format.
        /// </summary>
        /// <param name="adfDocument">The ADF document to convert.</param>
        /// <returns>The Markdown representation of the ADF document.</returns>
        public string ConvertAdf(AdfDocument adfDocument)
        {
            return adfDocument.Content == null || adfDocument.Content.Count == 0
                ? string.Empty
                : RenderBlocks(adfDocument.Content);
        }

        /// <summary>
        /// Converts an ADF JSON string to Markdown format.
        /// </summary>
        /// <param name="adfJson">The ADF JSON string to convert.</param>
        /// <returns>The Markdown representation of the ADF JSON string.</returns>
        public string ConvertAdf(string adfJson)
        {
            if (string.IsNullOrWhiteSpace(adfJson))
                return string.Empty;
            return ConvertAdf(AdfJsonConverter.Deserialize(adfJson));
        }

        /// <summary>
        /// Renders a collection of block-level ADF nodes to Markdown, separated by blank lines.
        /// </summary>
        private string RenderBlocks(IEnumerable<AdfNode> nodes)
        {
            IEnumerable<string> rendered = nodes.Select(RenderBlock).Where(s => s.Length > 0);
            return string.Join("\n\n", rendered);
        }

        /// <summary>
        /// Renders a single block-level ADF node to Markdown based on its type.
        /// </summary>
        private string RenderBlock(AdfNode node)
        {
            switch (node.Type)
            {
                case AdfNodeType.Doc:
                    return node.Content == null ? string.Empty : RenderBlocks(node.Content);
                case AdfNodeType.Paragraph:
                    return RenderInlineContent(node.Content);
                case AdfNodeType.Heading:
                    return RenderHeading(node);
                case AdfNodeType.BulletList:
                    return RenderList(node, ordered: false);
                case AdfNodeType.OrderedList:
                    return RenderList(node, ordered: true);
                case AdfNodeType.Blockquote:
                    return RenderBlockquote(node);
                case AdfNodeType.CodeBlock:
                    return RenderCodeBlock(node);
                case AdfNodeType.Rule:
                    return "---";
                case AdfNodeType.Table:
                    return RenderTable(node);
                case AdfNodeType.Panel:
                    return RenderPanel(node);
                case AdfNodeType.Expand:
                case AdfNodeType.NestedExpand:
                    return RenderExpand(node);
                case AdfNodeType.TaskList:
                    return RenderTaskList(node);
                case AdfNodeType.MediaSingle:
                case AdfNodeType.Media:
                case AdfNodeType.MediaGroup:
                case AdfNodeType.SyncBlock:
                    // No Markdown representation: Media Services identifiers and sync-block resource
                    // references aren't renderable content.
                    return string.Empty;
                case AdfNodeType.BodiedSyncBlock:
                case AdfNodeType.MultiBodiedExtension:
                case AdfNodeType.ExtensionFrame:
                    // No Markdown representation either, but the body is ordinary content: rendered
                    // transparently, one-way (it parses back as plain blocks, without the wrapper).
                    return node.Content == null ? string.Empty : RenderBlocks(node.Content);
                default:
                    return node.Content == null ? string.Empty : RenderBlocks(node.Content);
            }
        }

        /// <summary>
        /// Renders a heading node to Markdown format (e.g. <c>## Title</c>).
        /// </summary>
        private string RenderHeading(AdfNode node)
        {
            int level = GetIntAttribute(node.Attrs, "level");
            if (level < 1 || level > 6)
                level = 1;
            return new string('#', level) + " " + RenderInlineContent(node.Content);
        }

        /// <summary>
        /// Renders the inline content of a paragraph or heading (text, hard breaks, inline cards, emoji).
        /// </summary>
        private string RenderInlineContent(IEnumerable<AdfNode>? nodes)
        {
            if (nodes == null)
                return string.Empty;
            StringBuilder builder = new StringBuilder();
            foreach (AdfNode node in nodes)
                builder.Append(RenderInline(node));
            return builder.ToString();
        }

        /// <summary>
        /// Renders a single inline ADF node to Markdown based on its type.
        /// </summary>
        private string RenderInline(AdfNode node)
        {
            switch (node.Type)
            {
                case AdfNodeType.Text:
                    return RenderText(node);
                case AdfNodeType.HardBreak:
                    // A backslash immediately followed by a newline is a CommonMark hard line break that
                    // survives whitespace-trimming editors, unlike the alternative two-trailing-spaces form.
                    return "\\\n";
                case AdfNodeType.InlineCard:
                    return RenderInlineCard(node);
                case AdfNodeType.Emoji:
                    return EscapeText(GetStringAttribute(node.Attrs, "text") ?? GetStringAttribute(node.Attrs, "shortName") ?? string.Empty);
                case AdfNodeType.Mention:
                    // Markdown has no mention syntax - rendered as its plain "@Name" text, one-way only.
                    return EscapeText(GetStringAttribute(node.Attrs, "text") ?? "@" + GetStringAttribute(node.Attrs, "id"));
                case AdfNodeType.Date:
                    // No date syntax either - rendered as yyyy-MM-dd text, one-way only.
                    return EscapeText(FormatTimestamp(GetStringAttribute(node.Attrs, "timestamp") ?? string.Empty));
                case AdfNodeType.Status:
                    // No status-lozenge syntax - rendered as its text, one-way only.
                    return EscapeText(GetStringAttribute(node.Attrs, "text") ?? string.Empty);
                case AdfNodeType.MediaInline:
                    // No Markdown representation (Media Services identifiers), like media.
                    return string.Empty;
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Renders a text node to Markdown format, applying any marks.
        /// </summary>
        private string RenderText(AdfNode node)
        {
            // Text carrying a Code mark is rendered inside a backtick span, where CommonMark treats
            // content literally - escaping it first would leak backslashes into the rendered span.
            bool isCode = node.Marks != null && node.Marks.Any(m => m.Type == AdfMarkType.Code);
            string text = isCode ? node.Text ?? string.Empty : EscapeText(node.Text ?? string.Empty);
            return ApplyMarks(text, node.Marks);
        }

        /// <summary>
        /// CommonMark's largest ordered-list start number (at most nine digits).
        /// </summary>
        private const int MaxListStart = 999_999_999;

        /// <summary>
        /// Renders a bullet or ordered list node to Markdown format. An ordered list is numbered from its
        /// <c>order</c> attribute (1 when absent, or when it's beyond what CommonMark allows).
        /// </summary>
        private string RenderList(AdfNode node, bool ordered)
        {
            if (node.Content == null || node.Content.Count == 0)
                return string.Empty;

            List<string> lines = new List<string>();
            int order = ordered && node.Attrs != null && node.Attrs.ContainsKey("order") ? GetIntAttribute(node.Attrs, "order") : 1;
            long index = order < 0 || order > MaxListStart ? 1 : order;
            foreach (AdfNode item in node.Content)
            {
                string marker = ordered ? $"{index}. " : "- ";
                string body = RenderBlocks(item.Content ?? new List<AdfNode>());
                lines.Add(marker + IndentContinuation(body, marker.Length));
                index++;
            }
            return string.Join("\n", lines);
        }

        /// <summary>
        /// Renders a task list as a GitHub-flavored-Markdown task list (<c>- [ ]</c> / <c>- [x]</c>). A block task
        /// item's paragraphs are indented under its marker. Markdown can only nest a list inside an item, so a
        /// nested task list is indented under the item before it (or rendered at the same level if it comes
        /// first). <c>localId</c>s have no Markdown form and are lost.
        /// </summary>
        private string RenderTaskList(AdfNode node)
        {
            List<string> items = new List<string>();
            foreach (AdfNode child in node.Content ?? new List<AdfNode>())
            {
                if (child.Type == AdfNodeType.TaskList)
                {
                    string nested = RenderTaskList(child);
                    if (nested.Length == 0)
                        continue;
                    if (items.Count > 0)
                        items[items.Count - 1] += "\n  " + IndentContinuation(nested, 2);
                    else
                        items.Add(nested);
                    continue;
                }

                string marker = GetStringAttribute(child.Attrs, "state") == "DONE" ? "- [x] " : "- [ ] ";
                string body = child.Type == AdfNodeType.BlockTaskItem
                    ? RenderBlocks(child.Content ?? new List<AdfNode>())
                    : RenderInlineContent(child.Content);
                // Continuation lines line up with the text after "- ", where the item's content starts.
                items.Add(marker + IndentContinuation(body, 2));
            }
            return string.Join("\n", items);
        }

        /// <summary>
        /// Indents every line after the first by <paramref name="indent"/> spaces, leaving blank lines
        /// untouched, so multi-block list item content lines up under its marker as a Markdown continuation.
        /// </summary>
        private string IndentContinuation(string text, int indent)
        {
            string[] lines = text.Split('\n');
            if (lines.Length <= 1)
                return text;

            string pad = new string(' ', indent);
            for (int i = 1; i < lines.Length; i++)
                lines[i] = lines[i].Length == 0 ? lines[i] : pad + lines[i];
            return string.Join("\n", lines);
        }

        /// <summary>
        /// Renders a blockquote node to Markdown format, prefixing every line with <c>&gt;</c>.
        /// </summary>
        private string RenderBlockquote(AdfNode node)
        {
            return QuoteLines(node.Content == null ? string.Empty : RenderBlocks(node.Content));
        }

        /// <summary>
        /// Renders a panel node as a GitHub-flavored-Markdown alert (<c>&gt; [!NOTE]</c> followed by the
        /// panel's quoted block content), using <see cref="AdfMarkdownStructure.GetAlertKind"/>'s one-to-one
        /// panel type &lt;-&gt; alert kind mapping so it parses back to the same panel type.
        /// </summary>
        private string RenderPanel(AdfNode node)
        {
            string kind = AdfMarkdownStructure.GetAlertKind(GetStringAttribute(node.Attrs, "panelType") ?? "info");
            string body = node.Content == null ? string.Empty : RenderBlocks(node.Content);
            return QuoteLines($"[!{kind}]" + (body.Length == 0 ? string.Empty : "\n" + body));
        }

        /// <summary>
        /// Renders an expand or nested expand node as a raw-HTML <c>&lt;details&gt;</c> block (the GitHub
        /// convention): the opening line with the title in a <c>&lt;summary&gt;</c>, a blank line, the Markdown
        /// content, a blank line, then <c>&lt;/details&gt;</c>. The blank lines end each HTML block, so the content
        /// between them is still parsed as Markdown.
        /// </summary>
        private string RenderExpand(AdfNode node)
        {
            string? title = GetStringAttribute(node.Attrs, "title");
            // A line break in the title would end the HTML block early.
            string open = title == null ? "<details>" : $"<details><summary>{WebUtility.HtmlEncode(title.Replace('\r', ' ').Replace('\n', ' '))}</summary>";
            string body = node.Content == null ? string.Empty : RenderBlocks(node.Content);
            return body.Length == 0 ? open + "\n\n</details>" : $"{open}\n\n{body}\n\n</details>";
        }

        /// <summary>
        /// Renders an expand inside a pipe-table cell, which can't hold an HTML block: the title as bold text,
        /// then the content. One-way only - it parses back as plain paragraphs.
        /// </summary>
        private string RenderExpandInCell(AdfNode node)
        {
            string? title = GetStringAttribute(node.Attrs, "title");
            IEnumerable<string> parts = (node.Content ?? new List<AdfNode>()).Select(RenderBlock);
            if (!string.IsNullOrEmpty(title))
                parts = new[] { $"**{EscapeText(title!)}**" }.Concat(parts);
            return string.Join("<br>", parts.Where(s => s.Length > 0));
        }

        /// <summary>
        /// Prefixes every line of <paramref name="body"/> with <c>&gt;</c>, as blockquote/alert syntax.
        /// </summary>
        private static string QuoteLines(string body)
        {
            string[] lines = body.Split('\n');
            for (int i = 0; i < lines.Length; i++)
                lines[i] = lines[i].Length == 0 ? ">" : "> " + lines[i];
            return string.Join("\n", lines);
        }

        /// <summary>
        /// Renders a code block node to Markdown format as a fenced code block, using a longer backtick
        /// fence than any backtick run already present in the code so the fence can't be broken out of.
        /// </summary>
        private string RenderCodeBlock(AdfNode node)
        {
            string language = GetStringAttribute(node.Attrs, "language") ?? string.Empty;
            string text = node.Content == null ? string.Empty : string.Concat(node.Content.Select(c => c.Text ?? string.Empty));

            int maxBacktickRun = 0;
            int currentRun = 0;
            foreach (char c in text)
            {
                if (c == '`')
                {
                    currentRun++;
                    maxBacktickRun = Math.Max(maxBacktickRun, currentRun);
                }
                else
                {
                    currentRun = 0;
                }
            }

            string fence = new string('`', Math.Max(3, maxBacktickRun + 1));
            return $"{fence}{language}\n{text}\n{fence}";
        }

        /// <summary>
        /// Renders a table node to Markdown format as a GitHub-flavored-Markdown pipe table. A pipe table's
        /// delimiter row must directly follow the first row, so the first row is always treated as the
        /// header row regardless of whether its cells are <c>tableHeader</c> or <c>tableCell</c> - a table
        /// whose first row isn't all headers round-trips with that row reclassified as <c>tableHeader</c>,
        /// a limitation of the pipe-table format itself rather than of this converter.
        /// </summary>
        private string RenderTable(AdfNode node)
        {
            List<AdfNode> rows = node.Content ?? new List<AdfNode>();
            if (rows.Count == 0)
                return string.Empty;

            int columnCount = rows.Max(r => r.Content?.Count ?? 0);
            List<string> lines = new List<string>();

            for (int i = 0; i < rows.Count; i++)
            {
                List<AdfNode> cells = rows[i].Content ?? new List<AdfNode>();
                List<string> cellTexts = cells.Select(RenderTableCellContent).ToList();
                while (cellTexts.Count < columnCount)
                    cellTexts.Add(string.Empty);

                lines.Add("| " + string.Join(" | ", cellTexts) + " |");

                if (i == 0)
                    lines.Add("| " + string.Join(" | ", Enumerable.Repeat("---", columnCount)) + " |");
            }

            return string.Join("\n", lines);
        }

        /// <summary>
        /// Renders a table cell's block content as a single table-row-safe line: block boundaries become
        /// <c>&lt;br&gt;</c> (a pipe table cell cannot contain a literal newline) and any remaining newlines
        /// from multi-line block content collapse to spaces.
        /// </summary>
        private string RenderTableCellContent(AdfNode cell)
        {
            if (cell.Content == null)
                return string.Empty;
            IEnumerable<string> parts = cell.Content
                .Select(n => n.Type == AdfNodeType.NestedExpand || n.Type == AdfNodeType.Expand ? RenderExpandInCell(n) : RenderBlock(n))
                .Where(s => s.Length > 0);
            return string.Join("<br>", parts).Replace("\n", " ");
        }

        /// <summary>
        /// Renders an inline card node to Markdown format as an autolink.
        /// </summary>
        private string RenderInlineCard(AdfNode node)
        {
            string url = GetStringAttribute(node.Attrs, "url") ?? string.Empty;
            return string.IsNullOrWhiteSpace(url) ? string.Empty : $"<{url}>";
        }

        /// <summary>
        /// Applies a list of marks to the given text. Underline, SubSup, TextColor, BackgroundColor and Border
        /// have no Markdown syntax and are intentionally left unrendered (see docs/markdown-converter.md).
        /// </summary>
        private string ApplyMarks(string text, List<AdfMark>? marks)
        {
            if (marks == null || marks.Count == 0)
                return text;

            string content = text;
            foreach (AdfMark mark in marks)
            {
                switch (mark.Type)
                {
                    case AdfMarkType.Strong:
                        content = $"**{content}**";
                        break;
                    case AdfMarkType.Em:
                        content = $"*{content}*";
                        break;
                    case AdfMarkType.Code:
                        content = $"`{content}`";
                        break;
                    case AdfMarkType.Strike:
                        content = $"~~{content}~~";
                        break;
                    case AdfMarkType.Link:
                        content = RenderLink(content, mark);
                        break;
                    default:
                        break;
                }
            }
            return content;
        }

        /// <summary>
        /// Renders a link mark to Markdown format. The destination is wrapped in angle brackets (a
        /// CommonMark-legal link destination form) so hrefs containing spaces or parentheses can't break
        /// the surrounding <c>(...)</c> syntax. A <c>title</c> becomes the link title
        /// (<c>[text](&lt;url&gt; "title")</c>), with backslashes and quotes escaped and line breaks turned into
        /// spaces (a continuation line inside a list item would otherwise gain indentation).
        /// </summary>
        private string RenderLink(string content, AdfMark mark)
        {
            string href = GetStringAttribute(mark.Attrs, "href") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(href))
                return content;

            string? title = GetStringAttribute(mark.Attrs, "title");
            string titlePart = string.IsNullOrEmpty(title)
                ? string.Empty
                : " \"" + title!.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " ") + "\"";
            return $"[{content}](<{href}>{titlePart})";
        }

        /// <summary>
        /// Formats an ADF date timestamp (Unix seconds, or milliseconds for values too large to be seconds)
        /// as <c>yyyy-MM-dd</c> in UTC, or returns it unchanged if it isn't a number.
        /// </summary>
        private static string FormatTimestamp(string timestamp)
        {
            if (!long.TryParse(timestamp, NumberStyles.Integer, CultureInfo.InvariantCulture, out long value))
                return timestamp;

            DateTimeOffset date = Math.Abs(value) >= 100_000_000_000L
                ? DateTimeOffset.FromUnixTimeMilliseconds(value)
                : DateTimeOffset.FromUnixTimeSeconds(value);
            return date.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Backslash-escapes CommonMark-significant punctuation in literal text.
        /// </summary>
        private static string EscapeText(string text) => EscapeRegex.Replace(text, m => "\\" + m.Value);

        /// <summary>
        /// Gets a string attribute from the given dictionary of attributes.
        /// </summary>
        private static string? GetStringAttribute(Dictionary<string, object>? attrs, string key)
        {
            if (attrs == null || !attrs.TryGetValue(key, out object obj) || obj == null)
                return null;
            return System.Convert.ToString(obj, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets an integer attribute from the given dictionary of attributes.
        /// </summary>
        private static int GetIntAttribute(Dictionary<string, object>? attrs, string key)
        {
            if (attrs == null || !attrs.TryGetValue(key, out object obj) || obj == null)
                return 0;

            switch (obj)
            {
                case int i:
                    return i;
                case long l:
                    return System.Convert.ToInt32(l, CultureInfo.InvariantCulture);
                default:
                    return int.TryParse(System.Convert.ToString(obj, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : 0;
            }
        }
    }
}
