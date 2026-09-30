// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.DataConverters;
using AdfDotNet.Enums;
using AdfDotNet.Models;
using System.Globalization;
using System.Net;
using System.Text;

namespace AdfDotNet.FormatConverters
{
    /// <summary>
    /// Converts an ADF (Atlassian Document Format) document to HTML format.
    /// </summary>
    public class AdfToHtmlConverter
    {
        /// <summary>
        /// Creates a new instance of the AdfToHtmlConverter class.
        /// </summary>
        /// <returns>A new instance of the AdfToHtmlConverter class.</returns>
        public static AdfToHtmlConverter Create()
        {
            return new AdfToHtmlConverter();
        }

        /// <summary>
        /// Converts an ADF document to HTML format.
        /// </summary>
        /// <param name="adfDocument">The ADF document to convert.</param>
        /// <returns>The HTML representation of the ADF document.</returns>
        public static string Convert(AdfDocument adfDocument)
        {
            return new AdfToHtmlConverter().ConvertAdf(adfDocument);
        }

        /// <summary>
        /// Converts an ADF JSON string to HTML format.
        /// </summary>
        /// <param name="adfJson">The ADF JSON string to convert.</param>
        /// <returns>The HTML representation of the ADF JSON string.</returns>
        public static string Convert(string adfJson)
        {
            return new AdfToHtmlConverter().ConvertAdf(adfJson);
        }

        /// <summary>
        /// Converts an ADF document to HTML format.
        /// </summary>
        /// <param name="adfDocument">The ADF document to convert.</param>
        /// <returns>The HTML representation of the ADF document.</returns>
        public string ConvertAdf(AdfDocument adfDocument)
        {
            return adfDocument.Content == null || adfDocument.Content.Count == 0 ? "<p></p>" : RenderNodes(adfDocument.Content);
        }

        /// <summary>
        /// Converts an ADF JSON string to HTML format.
        /// </summary>
        /// <param name="adfJson">The ADF JSON string to convert.</param>
        /// <returns>The HTML representation of the ADF JSON string.</returns>
        public string ConvertAdf(string adfJson)
        {
            if (string.IsNullOrWhiteSpace(adfJson))
                return "<p></p>";
            return ConvertAdf(AdfJsonConverter.Deserialize(adfJson));
        }

        /// <summary>
        /// Renders a collection of ADF nodes to HTML format.
        /// </summary>
        /// <param name="nodes">The collection of ADF nodes to render.</param>
        /// <returns>The HTML representation of the ADF nodes.</returns>
        private string RenderNodes(IEnumerable<AdfNode>? nodes)
        {
            if (nodes == null)
                return string.Empty;
            StringBuilder html = new StringBuilder();
            foreach (AdfNode node in nodes)
                html.Append(RenderNode(node));
            return html.ToString();
        }

        /// <summary>
        /// Renders a single ADF node to HTML format based on its type.
        /// </summary>
        /// <param name="node">The ADF node to render.</param>
        /// <returns>The HTML representation of the ADF node.</returns>
        private string RenderNode(AdfNode node)
        {
            return node.Type switch
            {
                AdfNodeType.Doc => RenderNodes(node.Content),
                AdfNodeType.Paragraph => $"<p>{RenderNodes(node.Content)}</p>",
                AdfNodeType.Text => RenderText(node),
                AdfNodeType.Heading => RenderHeading(node),
                AdfNodeType.BulletList => $"<ul>{RenderNodes(node.Content)}</ul>",
                AdfNodeType.OrderedList => RenderOrderedList(node),
                AdfNodeType.ListItem => $"<li>{RenderNodes(node.Content)}</li>",
                AdfNodeType.Blockquote => $"<blockquote>{RenderNodes(node.Content)}</blockquote>",
                AdfNodeType.CodeBlock => RenderCodeBlock(node),
                AdfNodeType.Rule => "<hr/>",
                AdfNodeType.HardBreak => "<br/>",
                AdfNodeType.InlineCard => RenderInlineCard(node),
                AdfNodeType.Table => RenderTable(node),
                AdfNodeType.TableRow => $"<tr>{RenderNodes(node.Content)}</tr>",
                AdfNodeType.TableCell => RenderTableCell(node, "td"),
                AdfNodeType.TableHeader => RenderTableCell(node, "th"),
                AdfNodeType.Emoji => RenderEmoji(node),
                AdfNodeType.Mention => RenderMention(node),
                AdfNodeType.Date => RenderDate(node),
                AdfNodeType.Status => RenderStatus(node),
                AdfNodeType.Panel => RenderPanel(node),
                AdfNodeType.Expand => RenderExpand(node, "expand"),
                AdfNodeType.NestedExpand => RenderExpand(node, "nestedExpand"),
                AdfNodeType.TaskList => RenderTaskList(node),
                AdfNodeType.TaskItem => RenderTaskItem(node, isBlock: false),
                AdfNodeType.BlockTaskItem => RenderTaskItem(node, isBlock: true),
                // No HTML representation: Media Services identifiers and sync-block resource references
                // aren't renderable content, so these (and the border mark, which only decorates media) vanish.
                AdfNodeType.MediaSingle or AdfNodeType.Media or AdfNodeType.MediaGroup or AdfNodeType.MediaInline
                    or AdfNodeType.SyncBlock => string.Empty,
                // No HTML representation either, but the body is ordinary content: rendered transparently,
                // one-way (it parses back as plain blocks, without the wrapper).
                AdfNodeType.BodiedSyncBlock or AdfNodeType.MultiBodiedExtension or AdfNodeType.ExtensionFrame
                    => RenderNodes(node.Content),
                _ => RenderNodes(node.Content),
            };
        }
        
        /// <summary>
        /// Renders a text node to HTML format, applying any marks.
        /// </summary>
        /// <param name="node">The text node to render.</param>
        /// <returns>The HTML representation of the text node.</returns>
        private string RenderText(AdfNode node)
        {
            return ApplyMarks(WebUtility.HtmlEncode(node.Text ?? string.Empty), node.Marks);
        }

        /// <summary>
        /// Renders a heading node to HTML format.
        /// </summary>
        /// <param name="node">The heading node to render.</param>
        /// <returns>The HTML representation of the heading node.</returns>
        private string RenderHeading(AdfNode node)
        {
            int level = GetIntAttribute(node.Attrs, "level");

            if (level < 1 || level > 6)
                level = 1;

            return $"<h{level}>{RenderNodes(node.Content)}</h{level}>";
        }
        
        /// <summary>
        /// Renders an ordered list node to HTML format, with its <c>order</c> attribute (if any) as <c>start</c>.
        /// </summary>
        /// <param name="node">The ordered list node to render.</param>
        /// <returns>The HTML representation of the ordered list node.</returns>
        private string RenderOrderedList(AdfNode node)
        {
            StringBuilder html = new StringBuilder("<ol");
            AppendDataAttribute(html, "start", GetStringAttribute(node.Attrs, "order"));
            html.Append('>').Append(RenderNodes(node.Content)).Append("</ol>");
            return html.ToString();
        }

        /// <summary>
        /// Renders a table node to HTML format. Its attributes have no HTML equivalent, so they're carried as
        /// <c>data-layout</c>, <c>data-width</c>, <c>data-display-mode</c> and <c>data-number-column-enabled</c>,
        /// which <see cref="HtmlToAdfConverter"/> parses back.
        /// </summary>
        /// <param name="node">The table node to render.</param>
        /// <returns>The HTML representation of the table node.</returns>
        private string RenderTable(AdfNode node)
        {
            StringBuilder html = new StringBuilder("<table");
            AppendDataAttribute(html, "data-layout", GetStringAttribute(node.Attrs, "layout"));
            AppendDataAttribute(html, "data-width", GetStringAttribute(node.Attrs, "width"));
            AppendDataAttribute(html, "data-display-mode", GetStringAttribute(node.Attrs, "displayMode"));
            if (node.Attrs != null && node.Attrs.TryGetValue("isNumberColumnEnabled", out object? numbered) && numbered is bool isNumbered)
                AppendDataAttribute(html, "data-number-column-enabled", isNumbered ? "true" : "false");
            html.Append('>').Append(RenderNodes(node.Content)).Append("</table>");
            return html.ToString();
        }

        /// <summary>
        /// Renders a table cell or header node as <c>&lt;td&gt;</c>/<c>&lt;th&gt;</c>, with <c>colspan</c>/<c>rowspan</c>
        /// as the native attributes, <c>background</c> as a <c>background-color</c> style, and <c>colwidth</c> as a
        /// comma-separated <c>data-colwidth</c>.
        /// </summary>
        /// <param name="node">The table cell or header node to render.</param>
        /// <param name="tag">The HTML tag to render (<c>td</c> or <c>th</c>).</param>
        /// <returns>The HTML representation of the node.</returns>
        private string RenderTableCell(AdfNode node, string tag)
        {
            StringBuilder html = new StringBuilder("<").Append(tag);
            AppendDataAttribute(html, "colspan", GetStringAttribute(node.Attrs, "colspan"));
            AppendDataAttribute(html, "rowspan", GetStringAttribute(node.Attrs, "rowspan"));

            string? background = GetStringAttribute(node.Attrs, "background");
            if (!string.IsNullOrWhiteSpace(background))
                AppendDataAttribute(html, "style", "background-color: " + background);

            if (node.Attrs != null && node.Attrs.TryGetValue("colwidth", out object? colwidth) && colwidth is IEnumerable<object> widths)
                AppendDataAttribute(html, "data-colwidth", string.Join(",", widths.Select(w => System.Convert.ToString(w, CultureInfo.InvariantCulture))));

            html.Append('>').Append(RenderNodes(node.Content)).Append("</").Append(tag).Append('>');
            return html.ToString();
        }

        /// <summary>
        /// Renders a code block node to HTML format.
        /// </summary>
        /// <param name="node">The code block node to render.</param>
        /// <returns>The HTML representation of the code block node.</returns>
        private string RenderCodeBlock(AdfNode node)
        {
            string? language = GetStringAttribute(node.Attrs, "language");
            string code = RenderNodes(node.Content);

            if (string.IsNullOrWhiteSpace(language))
                return $"<pre><code>{code}</code></pre>";

            return $"<pre><code class=\"language-{WebUtility.HtmlEncode(language)}\">{code}</code></pre>";
        }

        /// <summary>
        /// Renders an inline card node to HTML format.
        /// </summary>
        /// <param name="node">The inline card node to render.</param>
        /// <returns>The HTML representation of the inline card node.</returns>
        private string RenderInlineCard(AdfNode node)
        {
            string? url = GetStringAttribute(node.Attrs, "url");
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;
            string encodedUrl = WebUtility.HtmlEncode(url);
            return $"<a href=\"{encodedUrl}\">{encodedUrl}</a>";
        }

        /// <summary>
        /// Renders an emoji node to HTML format.
        /// </summary>
        /// <param name="node">The emoji node to render.</param>
        /// <returns>The HTML representation of the emoji node.</returns>
        private string RenderEmoji(AdfNode node)
        {
            return WebUtility.HtmlEncode(GetStringAttribute(node.Attrs, "text") ?? GetStringAttribute(node.Attrs, "shortName") ?? string.Empty);
        }

        /// <summary>
        /// Renders a mention node to HTML format as a <c>&lt;span data-mention-id&gt;</c> (plus
        /// <c>data-access-level</c>/<c>data-user-type</c> when present), which <see cref="HtmlToAdfConverter"/>
        /// parses back into a mention.
        /// </summary>
        /// <param name="node">The mention node to render.</param>
        /// <returns>The HTML representation of the mention node.</returns>
        private string RenderMention(AdfNode node)
        {
            string id = GetStringAttribute(node.Attrs, "id") ?? string.Empty;
            string text = GetStringAttribute(node.Attrs, "text") ?? "@" + id;

            StringBuilder html = new StringBuilder($"<span data-mention-id=\"{WebUtility.HtmlEncode(id)}\"");
            AppendDataAttribute(html, "data-access-level", GetStringAttribute(node.Attrs, "accessLevel"));
            AppendDataAttribute(html, "data-user-type", GetStringAttribute(node.Attrs, "userType"));
            html.Append('>').Append(WebUtility.HtmlEncode(text)).Append("</span>");
            return html.ToString();
        }

        /// <summary>
        /// Renders a date node to HTML format as a <c>&lt;time data-timestamp&gt;</c> showing the date as
        /// <c>yyyy-MM-dd</c> (UTC), which <see cref="HtmlToAdfConverter"/> parses back into a date.
        /// </summary>
        /// <param name="node">The date node to render.</param>
        /// <returns>The HTML representation of the date node.</returns>
        private string RenderDate(AdfNode node)
        {
            string timestamp = GetStringAttribute(node.Attrs, "timestamp") ?? string.Empty;
            return $"<time data-timestamp=\"{WebUtility.HtmlEncode(timestamp)}\">{WebUtility.HtmlEncode(FormatTimestamp(timestamp))}</time>";
        }

        /// <summary>
        /// Renders a status node to HTML format as a <c>&lt;span data-status-color&gt;</c> (plus
        /// <c>data-local-id</c> when present), which <see cref="HtmlToAdfConverter"/> parses back into a status.
        /// </summary>
        /// <param name="node">The status node to render.</param>
        /// <returns>The HTML representation of the status node.</returns>
        private string RenderStatus(AdfNode node)
        {
            string color = GetStringAttribute(node.Attrs, "color") ?? "neutral";

            StringBuilder html = new StringBuilder($"<span data-status-color=\"{WebUtility.HtmlEncode(color)}\"");
            AppendDataAttribute(html, "data-local-id", GetStringAttribute(node.Attrs, "localId"));
            html.Append('>').Append(WebUtility.HtmlEncode(GetStringAttribute(node.Attrs, "text") ?? string.Empty)).Append("</span>");
            return html.ToString();
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
        /// Renders a panel node to HTML format as a <c>&lt;div data-panel-type&gt;</c> wrapping its block
        /// content, which <see cref="HtmlToAdfConverter"/> parses back into a panel.
        /// </summary>
        /// <param name="node">The panel node to render.</param>
        /// <returns>The HTML representation of the panel node.</returns>
        private string RenderPanel(AdfNode node)
        {
            string panelType = GetStringAttribute(node.Attrs, "panelType") ?? "info";
            return $"<div data-panel-type=\"{WebUtility.HtmlEncode(panelType)}\">{RenderNodes(node.Content)}</div>";
        }

        /// <summary>
        /// Renders an expand or nested expand node as <c>&lt;details data-adf-type&gt;</c>, with the title (if any)
        /// in a <c>&lt;summary&gt;</c>, which <see cref="HtmlToAdfConverter"/> parses back into the same node type.
        /// </summary>
        /// <param name="node">The expand or nested expand node to render.</param>
        /// <param name="adfType">The ADF type name written to <c>data-adf-type</c>.</param>
        /// <returns>The HTML representation of the node.</returns>
        private string RenderExpand(AdfNode node, string adfType)
        {
            string? title = GetStringAttribute(node.Attrs, "title");
            string summary = title == null ? string.Empty : $"<summary>{WebUtility.HtmlEncode(title)}</summary>";
            return $"<details data-adf-type=\"{adfType}\">{summary}{RenderNodes(node.Content)}</details>";
        }

        /// <summary>
        /// Renders a task list as <c>&lt;ul data-adf-type="taskList"&gt;</c> (plus <c>data-local-id</c>), which
        /// <see cref="HtmlToAdfConverter"/> parses back into a task list. A nested task list is rendered directly
        /// inside it, the way ADF nests it.
        /// </summary>
        /// <param name="node">The task list node to render.</param>
        /// <returns>The HTML representation of the task list node.</returns>
        private string RenderTaskList(AdfNode node)
        {
            StringBuilder html = new StringBuilder("<ul data-adf-type=\"taskList\"");
            AppendDataAttribute(html, "data-local-id", GetStringAttribute(node.Attrs, "localId"));
            html.Append('>').Append(RenderNodes(node.Content)).Append("</ul>");
            return html.ToString();
        }

        /// <summary>
        /// Renders a task item or block task item as a <c>&lt;li data-task-state&gt;</c> (plus <c>data-local-id</c>)
        /// that starts with a disabled checkbox, checked when the state is <c>DONE</c>. A block task item also
        /// carries <c>data-adf-type="blockTaskItem"</c> and keeps its paragraphs; a task item's inline content
        /// follows the checkbox directly.
        /// </summary>
        /// <param name="node">The task item node to render.</param>
        /// <param name="isBlock">Whether the node is a block task item.</param>
        /// <returns>The HTML representation of the task item node.</returns>
        private string RenderTaskItem(AdfNode node, bool isBlock)
        {
            string state = GetStringAttribute(node.Attrs, "state") ?? "TODO";

            StringBuilder html = new StringBuilder("<li");
            if (isBlock)
                html.Append(" data-adf-type=\"blockTaskItem\"");
            AppendDataAttribute(html, "data-task-state", state);
            AppendDataAttribute(html, "data-local-id", GetStringAttribute(node.Attrs, "localId"));
            html.Append("><input type=\"checkbox\"").Append(state == "DONE" ? " checked" : string.Empty).Append(" disabled/>");
            html.Append(RenderNodes(node.Content)).Append("</li>");
            return html.ToString();
        }

        /// <summary>
        /// Appends <c> name="value"</c> to an opening tag being built, if <paramref name="value"/> is non-empty.
        /// </summary>
        private static void AppendDataAttribute(StringBuilder html, string name, string? value)
        {
            if (!string.IsNullOrEmpty(value))
                html.Append(' ').Append(name).Append("=\"").Append(WebUtility.HtmlEncode(value)).Append('"');
        }

        /// <summary>
        /// Applies a list of marks to the given text.
        /// </summary>
        /// <param name="text">The text to apply marks to.</param>
        /// <param name="marks">The list of marks to apply.</param>
        /// <returns>The HTML representation of the text with marks applied.</returns>
        private string ApplyMarks(string text, List<AdfMark>? marks)
        {
            if (marks == null || marks.Count == 0)
                return text;
            string content = text;
            foreach (AdfMark mark in marks)
            {
                content = mark.Type switch
                {
                    AdfMarkType.Strong => $"<strong>{content}</strong>",
                    AdfMarkType.Em => $"<em>{content}</em>",
                    AdfMarkType.Code => $"<code>{content}</code>",
                    AdfMarkType.Link => RenderLink(content, mark),
                    AdfMarkType.Strike => $"<s>{content}</s>",
                    AdfMarkType.Underline => $"<u>{content}</u>",
                    AdfMarkType.SubSup => RenderSubSup(content, mark),
                    AdfMarkType.TextColor => RenderColorSpan(content, mark, "color"),
                    AdfMarkType.BackgroundColor => RenderColorSpan(content, mark, "background-color"),
                    _ => content,
                };
            }
            return content;
        }

        /// <summary>
        /// Renders a link mark to HTML format, with its <c>title</c> (if any) as the <c>title</c> attribute.
        /// </summary>
        /// <param name="content">The content of the link.</param>
        /// <param name="mark">The link mark to render.</param>
        /// <returns>The HTML representation of the link mark.</returns>
        private string RenderLink(string content, AdfMark mark)
        {
            string? href = GetStringAttribute(mark.Attrs, "href");
            if (string.IsNullOrWhiteSpace(href))
                return content;
            string? title = GetStringAttribute(mark.Attrs, "title");
            string titleAttribute = title == null ? string.Empty : $" title=\"{WebUtility.HtmlEncode(title)}\"";
            return $"<a href=\"{WebUtility.HtmlEncode(href)}\"{titleAttribute}>{content}</a>";
        }

        /// <summary>
        /// Renders a subscript or superscript mark to HTML format.
        /// </summary>
        /// <param name="content">The content of the subscript or superscript.</param>
        /// <param name="mark">The subscript or superscript mark to render.</param>
        /// <returns>The HTML representation of the subscript or superscript mark.</returns>
        private string RenderSubSup(string content, AdfMark mark)
        {
            return GetStringAttribute(mark.Attrs, "type")?.ToLowerInvariant() switch
            {
                "sub" => $"<sub>{content}</sub>",
                "sup" => $"<sup>{content}</sup>",
                _ => content,
            };
        }

        /// <summary>
        /// Renders a color mark (<c>textColor</c> or <c>backgroundColor</c>) to HTML format as a styled span.
        /// </summary>
        /// <param name="content">The content to color.</param>
        /// <param name="mark">The color mark to render.</param>
        /// <param name="cssProperty">The CSS property that carries the mark's color.</param>
        /// <returns>The HTML representation of the color mark.</returns>
        private string RenderColorSpan(string content, AdfMark mark, string cssProperty)
        {
            string? color = GetStringAttribute(mark.Attrs, "color");
            if (string.IsNullOrWhiteSpace(color))
                return content;
            return $"<span style=\"{cssProperty}: {WebUtility.HtmlEncode(color)}\">{content}</span>";
        }

        /// <summary>
        /// Gets a string attribute from the given dictionary of attributes.
        /// </summary>
        /// <param name="attrs">The dictionary of attributes.</param>
        /// <param name="key">The key of the attribute to retrieve.</param>
        /// <returns>The string value of the attribute, or null if not found.</returns>
        private static string? GetStringAttribute(Dictionary<string, object>? attrs, string key)
        {
            if (attrs == null || !attrs.TryGetValue(key, out object obj) || obj == null)
                return null;
            return System.Convert.ToString(obj, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets an integer attribute from the given dictionary of attributes.
        /// </summary>
        /// <param name="attrs">The dictionary of attributes.</param>
        /// <param name="key">The key of the attribute to retrieve.</param>
        /// <returns>The integer value of the attribute, or 0 if not found or invalid.</returns>
        private static int GetIntAttribute(Dictionary<string, object>? attrs, string key)
        {
            if (attrs == null || !attrs.TryGetValue(key, out object obj) || obj == null)
                return 0;

            return obj switch
            {
                int num => num,
                long num => System.Convert.ToInt32(num, CultureInfo.InvariantCulture),
                _ => int.TryParse(System.Convert.ToString(obj, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : 0,
            };
        }
    }

}