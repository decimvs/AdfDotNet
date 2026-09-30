// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Enums;
using System.Collections.Generic;

namespace AdfDotNet.Models
{
    /// <summary>
    /// Represents a node in the Atlassian Document Format (ADF) structure.
    /// </summary>
    public class AdfNode
    {
        /// <summary>
        /// Gets or sets the version of the ADF structure.
        /// </summary>
        public AdfNodeType Type { get; set; }

        /// <summary>
        /// Gets or sets the attributes of the node, represented as a dictionary of key-value pairs.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The Attrs value contract: regardless of which serializer, converter, or builder produced this
        /// dictionary (JSON deserialization, HTML or Markdown parsing, or manual
        /// construction), every value must be one of the following CLR types, or <see langword="null"/>:
        /// </para>
        /// <list type="bullet">
        /// <item><description><see cref="string"/></description></item>
        /// <item><description><see cref="int"/> or <see cref="long"/> for whole numbers - both are accepted, callers should tolerate either</description></item>
        /// <item><description><see cref="double"/></description></item>
        /// <item><description><see cref="bool"/></description></item>
        /// <item><description><see cref="System.DateTime"/></description></item>
        /// <item><description>a nested <see cref="Dictionary{TKey, TValue}"/> of <see cref="string"/> to <see cref="object"/>, following this same contract recursively</description></item>
        /// <item><description>a <see cref="List{T}"/> of <see cref="object"/>, whose elements follow this same contract</description></item>
        /// </list>
        /// <para>
        /// Values must never be a serializer-specific token/wrapper type (e.g. Newtonsoft's
        /// <c>JValue</c>/<c>JObject</c>/<c>JArray</c>, or System.Text.Json's <c>JsonElement</c>). A serializer
        /// package is responsible for fully unwrapping tokens to the plain CLR types above before assigning
        /// them here - <c>AdfDotNet.Json.Newtonsoft</c>'s <c>AdfDocumentConverter.ReadValue</c> is the
        /// reference implementation, and any future serializer (e.g. a System.Text.Json package) must
        /// normalize to this same contract. Code that bypasses a sanctioned converter (e.g. calling
        /// <c>JsonConvert.DeserializeObject&lt;AdfNode&gt;</c> directly instead of going through
        /// <c>AdfJsonConverter</c>/<c>IAdfDocumentSerializer</c>) is outside this contract.
        /// </para>
        /// </remarks>
        public Dictionary<string, object>? Attrs { get; set; }

        /// <summary>
        /// Gets or sets the content of the node, which is a list of child nodes.
        /// </summary>
        public List<AdfNode>? Content { get; set; }

        /// <summary>
        /// Gets or sets the text content of the node, applicable for text nodes.
        /// </summary>
        public string? Text { get; set; }

        /// <summary>
        /// Gets or sets the marks applied to the node, which is a list of formatting marks.
        /// </summary>
        public List<AdfMark>? Marks { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdfNode"/> class with the specified node type.
        /// </summary>
        /// <param name="type">The type of the node.</param>
        public AdfNode(AdfNodeType type) => Type = type;

        /// <summary>
        /// Creates a new ADF document with the specified content.
        /// </summary>
        /// <param name="content">The content of the document, represented as a list of ADF nodes.</param>
        /// <returns>A new instance of <see cref="AdfDocument"/> with the specified content.</returns>
        public static AdfDocument CreateDocument(List<AdfNode>? content = null)
        {
            AdfDocument document = new AdfDocument
            {
                Version = 1,
                Type = AdfNodeType.Doc,
                Content = content ?? new List<AdfNode>()
            };

            return document;
        }

        /// <summary>
        /// Creates a new paragraph node with the specified content.
        /// </summary>
        /// <param name="content">The content of the paragraph, represented as a list of ADF nodes.</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing a paragraph with the specified content.</returns>
        public static AdfNode CreateParagraph(List<AdfNode>? content = null)
        {
            return new AdfNode(AdfNodeType.Paragraph)
            {
                Content = content ?? []
            };
        }

        /// <summary>
        /// Creates a new text node with the specified text and optional formatting marks.
        /// </summary>
        /// <param name="text">The text content of the text node.</param>
        /// <param name="marks">The formatting marks to apply to the text node.</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing a text node with the specified text and formatting marks.</returns>
        public static AdfNode CreateText(string text, List<AdfMark>? marks = null)
        {
            return new AdfNode(AdfNodeType.Text)
            {
                Text = text,
                Marks = marks
            };
        }
        
        /// <summary>
        /// Creates a new heading node with the specified level and content.
        /// </summary>
        /// <param name="level">The level of the heading.</param>
        /// <param name="content">The content of the heading, represented as a list of ADF nodes.</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing a heading with the specified level and content.</returns>
        public static AdfNode CreateHeading(int level, List<AdfNode>? content = null)
        {
            return new AdfNode(AdfNodeType.Heading)
            {
                Attrs = new Dictionary<string, object>() { { nameof(level), level } },
                Content = content ?? []
            };
        }

        /// <summary>
        /// Creates a new heading node with the specified level and text content.
        /// </summary>
        /// <param name="level">The level of the heading.</param>
        /// <param name="text">The text content of the heading.</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing a heading with the specified level and text content.</returns>
        public static AdfNode CreateHeading(int level, string text)
        {
            return CreateHeading(level, [CreateText(text)]);
        }

        /// <summary>
        /// Creates a new bullet list node with the specified content.
        /// </summary>
        /// <param name="content">The content of the bullet list, represented as a list of ADF nodes.</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing a bullet list with the specified content.</returns>   
        public static AdfNode CreateBulletList(List<AdfNode>? content = null)
        {
            return new AdfNode(AdfNodeType.BulletList)
            {
                Content = content ?? []
            };
        }
        
        /// <summary>
        /// Creates a new ordered list node with the specified content.
        /// </summary>
        /// <param name="content">The content of the ordered list, represented as a list of ADF nodes.</param>
        /// <param name="order">The optional number of the first item (0 or more). Omitted, the list starts at 1.</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing an ordered list with the specified content.</returns>
        public static AdfNode CreateOrderedList(List<AdfNode>? content = null, int? order = null)
        {
            return new AdfNode(AdfNodeType.OrderedList)
            {
                Attrs = order == null ? null : new Dictionary<string, object>() { { nameof(order), order.Value } },
                Content = content ?? []
            };
        }

        /// <summary>
        /// Creates a new list item node with the specified content.
        /// </summary>
        /// <param name="content">The content of the list item, represented as a list of ADF nodes.</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing a list item with the specified content.</returns>
        public static AdfNode CreateListItem(List<AdfNode>? content = null)
        {
            return new AdfNode(AdfNodeType.ListItem)
            {
                Content = content ?? []
            };
        }
        
        /// <summary>
        /// Creates a new blockquote node with the specified content.
        /// </summary>
        /// <param name="content">The content of the blockquote, represented as a list of ADF nodes.</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing a blockquote with the specified content.</returns>    
        public static AdfNode CreateBlockquote(List<AdfNode>? content = null)
        {
            return new AdfNode(AdfNodeType.Blockquote)
            {
                Content = content ?? []
            };
        }
        
        /// <summary>
        /// Creates a new code block node with the specified language and content.
        /// </summary>
        /// <param name="language">The programming language of the code block.</param>
        /// <param name="content">The content of the code block, represented as a list of ADF nodes.</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing a code block with the specified language and content.</returns>
        public static AdfNode CreateCodeBlock(string? language = null, List<AdfNode>? content = null)
        {
            AdfNode codeBlock = new AdfNode(AdfNodeType.CodeBlock)
            {
                Content = content ?? []
            };

            if (!string.IsNullOrEmpty(language))
                codeBlock.Attrs = new Dictionary<string, object>() { { nameof(language), language! } };

            return codeBlock;
        }
        
        /// <summary>
        /// Creates a new horizontal rule node.
        /// </summary>
        /// <returns>A new instance of <see cref="AdfNode"/> representing a horizontal rule.</returns>
        public static AdfNode CreateRule() => new AdfNode(AdfNodeType.Rule);
        
        /// <summary>
        /// Creates a new hard break node.
        /// </summary>
        /// <returns>A new instance of <see cref="AdfNode"/> representing a hard break.</returns>
        public static AdfNode CreateHardBreak() => new AdfNode(AdfNodeType.HardBreak);
        
        /// <summary>
        /// Creates a new media node with the specified Media Services identifiers.
        /// </summary>
        /// <param name="id">The Media Services ID used to query the media's metadata.</param>
        /// <param name="type">The media type - <c>"file"</c> or <c>"link"</c>, per the ADF spec.</param>
        /// <param name="collection">The Media Services collection name the media belongs to.</param>
        /// <param name="width">The optional display width, in pixels.</param>
        /// <param name="height">The optional display height, in pixels.</param>
        /// <param name="occurrenceKey">An optional non-empty key enabling deletion of the file from the collection.</param>
        /// <param name="alt">Optional alternative text.</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing a media node.</returns>
        public static AdfNode CreateMedia(string id, string type, string collection, int? width = null, int? height = null, string? occurrenceKey = null, string? alt = null)
        {
            Dictionary<string, object> attrs = new Dictionary<string, object>()
            {
                { nameof(id), id },
                { nameof(type), type },
                { nameof(collection), collection }
            };

            if (width.HasValue)
                attrs[nameof(width)] = width.Value;

            if (height.HasValue)
                attrs[nameof(height)] = height.Value;

            if (!string.IsNullOrEmpty(occurrenceKey))
                attrs[nameof(occurrenceKey)] = occurrenceKey!;

            if (!string.IsNullOrEmpty(alt))
                attrs[nameof(alt)] = alt!;

            return new AdfNode(AdfNodeType.Media) { Attrs = attrs };
        }

        /// <summary>
        /// Creates a new media single node wrapping the given media node, per the ADF spec (<c>mediaSingle</c>'s
        /// content must be exactly one <c>media</c> node).
        /// </summary>
        /// <param name="media">The media node to wrap. Should be created via <see cref="CreateMedia"/>.</param>
        /// <param name="layout">The layout - e.g. <c>"center"</c>, <c>"wrap-left"</c>, <c>"wide"</c>. Defaults to <c>"center"</c>.</param>
        /// <param name="width">The optional display width, as a percentage (0-100) or in pixels per <paramref name="widthType"/>.</param>
        /// <param name="widthType">Either <c>"percentage"</c> (default) or <c>"pixel"</c>.</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing a media single with the specified content.</returns>
        public static AdfNode CreateMediaSingle(AdfNode media, string layout = "center", double? width = null, string? widthType = null)
        {
            Dictionary<string, object> attrs = new Dictionary<string, object>()
            {
                { nameof(layout), layout }
            };

            if (width.HasValue)
                attrs[nameof(width)] = width.Value;

            if (!string.IsNullOrEmpty(widthType))
                attrs[nameof(widthType)] = widthType!;

            return new AdfNode(AdfNodeType.MediaSingle)
            {
                Attrs = attrs,
                Content = new List<AdfNode>() { media }
            };
        }

        /// <summary>
        /// Creates a new inline card node for the given URL.
        /// </summary>
        /// <param name="url">The URL the inline card renders a smart link for.</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing an inline card.</returns>
        public static AdfNode CreateInlineCard(string url)
        {
            return new AdfNode(AdfNodeType.InlineCard)
            {
                Attrs = new Dictionary<string, object>() { { nameof(url), url } }
            };
        }

        /// <summary>
        /// Creates a new emoji node.
        /// </summary>
        /// <param name="shortName">The emoji's short name, e.g. <c>:grinning:</c>.</param>
        /// <param name="id">The optional emoji service ID (format varies by emoji kind).</param>
        /// <param name="text">Optional text representation, rendered in place of <paramref name="shortName"/> when present.</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing an emoji.</returns>
        public static AdfNode CreateEmoji(string shortName, string? id = null, string? text = null)
        {
            Dictionary<string, object> attrs = new Dictionary<string, object>()
            {
                { nameof(shortName), shortName }
            };

            if (!string.IsNullOrEmpty(id))
                attrs[nameof(id)] = id!;

            if (!string.IsNullOrEmpty(text))
                attrs[nameof(text)] = text!;

            return new AdfNode(AdfNodeType.Emoji) { Attrs = attrs };
        }

        /// <summary>
        /// Creates a new mention node referencing a user.
        /// </summary>
        /// <param name="id">The Atlassian Account ID (or collection name) of the mentioned user.</param>
        /// <param name="text">Optional textual representation, including the leading <c>@</c> (e.g. <c>"@Jane Doe"</c>).</param>
        /// <param name="accessLevel">Optional access level - <c>"NONE"</c>, <c>"SITE"</c>, <c>"APPLICATION"</c> or <c>"CONTAINER"</c>, per the ADF spec.</param>
        /// <param name="userType">Optional user type - <c>"DEFAULT"</c>, <c>"SPECIAL"</c> or <c>"APP"</c>, per the ADF spec.</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing a mention.</returns>
        public static AdfNode CreateMention(string id, string? text = null, string? accessLevel = null, string? userType = null)
        {
            Dictionary<string, object> attrs = new Dictionary<string, object>()
            {
                { nameof(id), id }
            };

            if (!string.IsNullOrEmpty(text))
                attrs[nameof(text)] = text!;

            if (!string.IsNullOrEmpty(accessLevel))
                attrs[nameof(accessLevel)] = accessLevel!;

            if (!string.IsNullOrEmpty(userType))
                attrs[nameof(userType)] = userType!;

            return new AdfNode(AdfNodeType.Mention) { Attrs = attrs };
        }

        /// <summary>
        /// Creates a new date node.
        /// </summary>
        /// <param name="timestamp">The date as a Unix timestamp string, per the ADF spec (e.g. <c>"1582152559"</c>).</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing a date.</returns>
        public static AdfNode CreateDate(string timestamp)
        {
            return new AdfNode(AdfNodeType.Date)
            {
                Attrs = new Dictionary<string, object>() { { nameof(timestamp), timestamp } }
            };
        }

        /// <summary>
        /// Creates a new status lozenge node.
        /// </summary>
        /// <param name="text">The status text (e.g. <c>"In Progress"</c>).</param>
        /// <param name="color">The lozenge color - <c>"neutral"</c>, <c>"purple"</c>, <c>"blue"</c>, <c>"red"</c>, <c>"yellow"</c>, <c>"green"</c>, or a hex color, per the ADF spec.</param>
        /// <param name="localId">An optional unique identifier for this status instance.</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing a status.</returns>
        public static AdfNode CreateStatus(string text, string color, string? localId = null)
        {
            Dictionary<string, object> attrs = new Dictionary<string, object>()
            {
                { nameof(text), text },
                { nameof(color), color }
            };

            if (!string.IsNullOrEmpty(localId))
                attrs[nameof(localId)] = localId!;

            return new AdfNode(AdfNodeType.Status) { Attrs = attrs };
        }

        /// <summary>
        /// Creates a new panel node with the specified panel type and content.
        /// </summary>
        /// <param name="panelType">The panel type - <c>"info"</c>, <c>"note"</c>, <c>"warning"</c>, <c>"success"</c> or <c>"error"</c>, per the ADF spec.</param>
        /// <param name="content">The content of the panel (paragraphs, headings and lists), represented as a list of ADF nodes.</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing a panel with the specified type and content.</returns>
        public static AdfNode CreatePanel(string panelType, List<AdfNode>? content = null)
        {
            return new AdfNode(AdfNodeType.Panel)
            {
                Attrs = new Dictionary<string, object>() { { nameof(panelType), panelType } },
                Content = content ?? []
            };
        }

        /// <summary>
        /// Creates a new expand node - a top-level container whose content can be hidden or shown. Inside a table
        /// cell or header, use <see cref="CreateNestedExpand"/> instead.
        /// </summary>
        /// <param name="title">The optional title shown on the expand.</param>
        /// <param name="content">The content of the expand, represented as a list of ADF nodes.</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing an expand. Its <c>attrs</c> object is always
        /// present (the spec requires it), and empty when no title is given.</returns>
        public static AdfNode CreateExpand(string? title = null, List<AdfNode>? content = null)
        {
            return new AdfNode(AdfNodeType.Expand)
            {
                Attrs = CreateExpandAttrs(title),
                Content = content ?? []
            };
        }

        /// <summary>
        /// Creates a new nested expand node - the variant of an expand that is only legal inside a table cell or
        /// table header, and may only contain paragraphs, headings, <c>mediaGroup</c> and <c>mediaSingle</c>.
        /// </summary>
        /// <param name="title">The optional title shown on the expand.</param>
        /// <param name="content">The content of the nested expand, represented as a list of ADF nodes.</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing a nested expand. Its <c>attrs</c> object
        /// is always present (the spec requires it), and empty when no title is given.</returns>
        public static AdfNode CreateNestedExpand(string? title = null, List<AdfNode>? content = null)
        {
            return new AdfNode(AdfNodeType.NestedExpand)
            {
                Attrs = CreateExpandAttrs(title),
                Content = content ?? []
            };
        }

        private static Dictionary<string, object> CreateExpandAttrs(string? title)
        {
            Dictionary<string, object> attrs = new Dictionary<string, object>();

            if (title != null)
                attrs[nameof(title)] = title;

            return attrs;
        }

        /// <summary>
        /// Creates a new media group node - a container for several media items.
        /// </summary>
        /// <param name="content">The media nodes of the group. Should be created via <see cref="CreateMedia"/>.</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing a media group with the specified content.</returns>
        public static AdfNode CreateMediaGroup(List<AdfNode>? content = null)
        {
            return new AdfNode(AdfNodeType.MediaGroup)
            {
                Content = content ?? []
            };
        }

        /// <summary>
        /// Creates a new inline media node with the specified Media Services identifiers.
        /// </summary>
        /// <param name="id">The Media Services ID used to query the media's metadata.</param>
        /// <param name="collection">The Media Services collection name the media belongs to.</param>
        /// <param name="type">The optional media type - <c>"file"</c>, <c>"link"</c> or <c>"image"</c>.</param>
        /// <param name="width">The optional display width, in pixels.</param>
        /// <param name="height">The optional display height, in pixels.</param>
        /// <param name="alt">Optional alternative text.</param>
        /// <param name="occurrenceKey">An optional non-empty key enabling deletion of the file from the collection.</param>
        /// <param name="localId">An optional unique identifier for this node within the document.</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing an inline media node.</returns>
        public static AdfNode CreateMediaInline(string id, string collection, string? type = null, int? width = null, int? height = null, string? alt = null, string? occurrenceKey = null, string? localId = null)
        {
            Dictionary<string, object> attrs = new Dictionary<string, object>()
            {
                { nameof(id), id },
                { nameof(collection), collection }
            };

            if (!string.IsNullOrEmpty(type))
                attrs[nameof(type)] = type!;

            if (width.HasValue)
                attrs[nameof(width)] = width.Value;

            if (height.HasValue)
                attrs[nameof(height)] = height.Value;

            if (!string.IsNullOrEmpty(alt))
                attrs[nameof(alt)] = alt!;

            if (!string.IsNullOrEmpty(occurrenceKey))
                attrs[nameof(occurrenceKey)] = occurrenceKey!;

            if (!string.IsNullOrEmpty(localId))
                attrs[nameof(localId)] = localId!;

            return new AdfNode(AdfNodeType.MediaInline) { Attrs = attrs };
        }

        /// <summary>
        /// Creates a new sync block node - a reference to synced content. It has no content of its own.
        /// </summary>
        /// <param name="resourceId">The identifier of the synced resource.</param>
        /// <param name="localId">A unique identifier for this node within the document. A new GUID when omitted.</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing a sync block.</returns>
        public static AdfNode CreateSyncBlock(string resourceId, string? localId = null)
        {
            return new AdfNode(AdfNodeType.SyncBlock) { Attrs = CreateSyncBlockAttrs(resourceId, localId) };
        }

        /// <summary>
        /// Creates a new bodied sync block node - the source of synced content, which it carries as children.
        /// </summary>
        /// <param name="resourceId">The identifier of the synced resource.</param>
        /// <param name="content">The synced block content, represented as a list of ADF nodes.</param>
        /// <param name="localId">A unique identifier for this node within the document. A new GUID when omitted.</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing a bodied sync block.</returns>
        public static AdfNode CreateBodiedSyncBlock(string resourceId, List<AdfNode>? content = null, string? localId = null)
        {
            return new AdfNode(AdfNodeType.BodiedSyncBlock)
            {
                Attrs = CreateSyncBlockAttrs(resourceId, localId),
                Content = content ?? []
            };
        }

        private static Dictionary<string, object> CreateSyncBlockAttrs(string resourceId, string? localId)
        {
            return new Dictionary<string, object>()
            {
                { nameof(resourceId), resourceId },
                { nameof(localId), localId ?? NewLocalId() }
            };
        }

        /// <summary>
        /// Creates a new multi-bodied extension node - an app extension whose bodies are
        /// <see cref="CreateExtensionFrame">extension frames</see>.
        /// </summary>
        /// <param name="extensionKey">The extension's key (non-empty).</param>
        /// <param name="extensionType">The extension's type (non-empty).</param>
        /// <param name="content">The extension frames, represented as a list of ADF nodes.</param>
        /// <param name="parameters">Optional extension parameters, following the <see cref="Attrs"/> value contract.</param>
        /// <param name="text">Optional text representation.</param>
        /// <param name="layout">Optional layout - <c>"default"</c>, <c>"wide"</c> or <c>"full-width"</c>.</param>
        /// <param name="localId">An optional unique identifier for this node within the document.</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing a multi-bodied extension.</returns>
        public static AdfNode CreateMultiBodiedExtension(string extensionKey, string extensionType, List<AdfNode>? content = null, Dictionary<string, object>? parameters = null, string? text = null, string? layout = null, string? localId = null)
        {
            Dictionary<string, object> attrs = new Dictionary<string, object>()
            {
                { nameof(extensionKey), extensionKey },
                { nameof(extensionType), extensionType }
            };

            if (parameters != null)
                attrs[nameof(parameters)] = parameters;

            if (text != null)
                attrs[nameof(text)] = text;

            if (!string.IsNullOrEmpty(layout))
                attrs[nameof(layout)] = layout!;

            if (!string.IsNullOrEmpty(localId))
                attrs[nameof(localId)] = localId!;

            return new AdfNode(AdfNodeType.MultiBodiedExtension)
            {
                Attrs = attrs,
                Content = content ?? []
            };
        }

        /// <summary>
        /// Creates a new extension frame node - one body of a multi-bodied extension.
        /// </summary>
        /// <param name="content">The frame's block content, represented as a list of ADF nodes.</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing an extension frame.</returns>
        public static AdfNode CreateExtensionFrame(List<AdfNode>? content = null)
        {
            return new AdfNode(AdfNodeType.ExtensionFrame)
            {
                Content = content ?? []
            };
        }

        /// <summary>
        /// Creates a new task list node (a checkbox list) holding task items, block task items or nested task lists.
        /// </summary>
        /// <param name="content">The items of the list, represented as a list of ADF nodes.</param>
        /// <param name="localId">A unique identifier for this node within the document. A new GUID when omitted.</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing a task list.</returns>
        public static AdfNode CreateTaskList(List<AdfNode>? content = null, string? localId = null)
        {
            return new AdfNode(AdfNodeType.TaskList)
            {
                Attrs = new Dictionary<string, object>() { { nameof(localId), localId ?? NewLocalId() } },
                Content = content ?? []
            };
        }

        /// <summary>
        /// Creates a new task item node - a checkbox with inline content.
        /// </summary>
        /// <param name="state">The checkbox state - <c>"TODO"</c> or <c>"DONE"</c>.</param>
        /// <param name="content">The item's inline content, represented as a list of ADF nodes.</param>
        /// <param name="localId">A unique identifier for this node within the document. A new GUID when omitted.</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing a task item.</returns>
        public static AdfNode CreateTaskItem(string state = "TODO", List<AdfNode>? content = null, string? localId = null)
        {
            return new AdfNode(AdfNodeType.TaskItem)
            {
                Attrs = CreateTaskItemAttrs(state, localId),
                Content = content ?? []
            };
        }

        /// <summary>
        /// Creates a new block task item node - a checkbox whose content is one or two paragraphs.
        /// </summary>
        /// <param name="state">The checkbox state - <c>"TODO"</c> or <c>"DONE"</c>.</param>
        /// <param name="content">The item's paragraphs, represented as a list of ADF nodes.</param>
        /// <param name="localId">A unique identifier for this node within the document. A new GUID when omitted.</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing a block task item.</returns>
        public static AdfNode CreateBlockTaskItem(string state = "TODO", List<AdfNode>? content = null, string? localId = null)
        {
            return new AdfNode(AdfNodeType.BlockTaskItem)
            {
                Attrs = CreateTaskItemAttrs(state, localId),
                Content = content ?? []
            };
        }

        private static Dictionary<string, object> CreateTaskItemAttrs(string state, string? localId)
        {
            return new Dictionary<string, object>()
            {
                { nameof(localId), localId ?? NewLocalId() },
                { nameof(state), state }
            };
        }

        private static string NewLocalId() => Guid.NewGuid().ToString();

        /// <summary>
        /// Creates a new table node with the specified content.
        /// </summary>
        /// <param name="content">The content of the table, represented as a list of ADF nodes.</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing a table with the specified content.</returns>
        public static AdfNode CreateTable(List<AdfNode>? content = null)
        {
            return new AdfNode(AdfNodeType.Table)
            {
                Content = content ?? []
            };
        }

        /// <summary>
        /// Creates a new table row node with the specified content.
        /// </summary>
        /// <param name="content">The content of the table row, represented as a list of ADF nodes.</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing a table row with the specified content.</returns>
        public static AdfNode CreateTableRow(List<AdfNode>? content = null)
        {
            return new AdfNode(AdfNodeType.TableRow)
            {
                Content = content ?? []
            };
        }

        /// <summary>
        /// Creates a new table cell node with the specified content.
        /// </summary>
        /// <param name="content">The content of the table cell, represented as a list of ADF nodes.</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing a table cell with the specified content.</returns>
        public static AdfNode CreateTableCell(List<AdfNode>? content = null)
        {
            return new AdfNode(AdfNodeType.TableCell)
            {
                Content = content ?? []
            };
        }
        
        /// <summary>
        /// Creates a new table header node with the specified content.
        /// </summary>
        /// <param name="content">The content of the table header, represented as a list of ADF nodes.</param>
        /// <returns>A new instance of <see cref="AdfNode"/> representing a table header with the specified content.</returns>
        public static AdfNode CreateTableHeader(List<AdfNode>? content = null)
        {
            return new AdfNode(AdfNodeType.TableHeader)
            {
                Content = content ?? []
            };
        }
        
        /// <summary>
        /// Adds a new child node to the current node's content.
        /// </summary>
        /// <param name="node">The child node to add.</param>
        public void AddContent(AdfNode node)
        {
            Content ??= [];
            Content.Add(node);
        }
        
        /// <summary>
        /// Determines whether the current node can have child nodes.
        /// </summary>
        /// <returns><c>true</c> if the current node can have child nodes; otherwise, <c>false</c>.</returns>
        public bool CanHaveChildren() => AdfNodeSchema.CanHaveChildren(Type);

        /// <summary>
        /// Determines whether the current node is a leaf node (i.e., it cannot have child nodes).
        /// </summary>
        /// <returns><c>true</c> if the current node is a leaf node; otherwise, <c>false</c>.</returns>
        public bool IsLeafNode() => AdfNodeSchema.IsLeaf(Type);
    }
}