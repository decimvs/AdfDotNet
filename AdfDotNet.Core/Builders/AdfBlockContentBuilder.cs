// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Models;

namespace AdfDotNet.Builders
{
    /// <summary>
    /// Builds the block content of a container node (a document, list item, blockquote, table cell, or table
    /// header), via nested-lambda composition, e.g. <c>b.Paragraph(p => p.Text("hello")).Rule()</c>.
    /// </summary>
    /// <remarks>
    /// This one builder type is shared by every block-content parent rather than split per parent type (a
    /// document allows <c>rule</c>/<c>table</c>/<c>mediaSingle</c> that a list item or table cell does not,
    /// per <see cref="AdfNodeSchema"/>) - the fluent builder leans on
    /// <see cref="AdfDocument.Validate"/> to catch illegal nesting rather than reinvent legality enforcement
    /// with a proliferation of narrower builder types. Call <c>Validate()</c> on the built document if you
    /// need to confirm structural legality.
    /// </remarks>
    public sealed class AdfBlockContentBuilder
    {
        private readonly List<AdfNode> _content;

        internal AdfBlockContentBuilder(List<AdfNode> content) => _content = content;

        /// <summary>
        /// Adds a paragraph with inline content built via a nested <see cref="AdfInlineContentBuilder"/> lambda.
        /// </summary>
        public AdfBlockContentBuilder Paragraph(Action<AdfInlineContentBuilder> content)
        {
            AdfNode node = AdfNode.CreateParagraph();
            content(new AdfInlineContentBuilder(node.Content!));
            _content.Add(node);
            return this;
        }

        /// <summary>
        /// Adds a heading with inline content built via a nested <see cref="AdfInlineContentBuilder"/> lambda.
        /// </summary>
        /// <param name="level">The heading level (1-6).</param>
        /// <param name="content">Builds the heading's inline content.</param>
        public AdfBlockContentBuilder Heading(int level, Action<AdfInlineContentBuilder> content)
        {
            AdfNode node = AdfNode.CreateHeading(level);
            content(new AdfInlineContentBuilder(node.Content!));
            _content.Add(node);
            return this;
        }

        /// <summary>
        /// Adds a heading with plain unmarked text content.
        /// </summary>
        /// <param name="level">The heading level (1-6).</param>
        /// <param name="text">The heading's text.</param>
        public AdfBlockContentBuilder Heading(int level, string text)
        {
            _content.Add(AdfNode.CreateHeading(level, text));
            return this;
        }

        /// <summary>
        /// Adds a bullet list with items built via a nested <see cref="AdfListBuilder"/> lambda.
        /// </summary>
        public AdfBlockContentBuilder BulletList(Action<AdfListBuilder> content)
        {
            AdfNode node = AdfNode.CreateBulletList();
            content(new AdfListBuilder(node.Content!));
            _content.Add(node);
            return this;
        }

        /// <summary>
        /// Adds an ordered list with items built via a nested <see cref="AdfListBuilder"/> lambda.
        /// </summary>
        /// <param name="content">Builds the list items.</param>
        /// <param name="order">The optional number of the first item (0 or more). Omitted, the list starts at 1.</param>
        public AdfBlockContentBuilder OrderedList(Action<AdfListBuilder> content, int? order = null)
        {
            AdfNode node = AdfNode.CreateOrderedList(order: order);
            content(new AdfListBuilder(node.Content!));
            _content.Add(node);
            return this;
        }

        /// <summary>
        /// Adds a blockquote with block content built via a nested <see cref="AdfBlockContentBuilder"/> lambda.
        /// </summary>
        public AdfBlockContentBuilder Blockquote(Action<AdfBlockContentBuilder> content)
        {
            AdfNode node = AdfNode.CreateBlockquote();
            content(new AdfBlockContentBuilder(node.Content!));
            _content.Add(node);
            return this;
        }

        /// <summary>
        /// Adds a panel with block content built via a nested <see cref="AdfBlockContentBuilder"/> lambda. Per
        /// the ADF spec a panel may only contain paragraphs, headings and lists - <see cref="AdfDocument.Validate"/>
        /// reports anything else.
        /// </summary>
        /// <param name="panelType">The panel type - <c>"info"</c>, <c>"note"</c>, <c>"warning"</c>, <c>"success"</c> or <c>"error"</c>.</param>
        /// <param name="content">Builds the panel's block content.</param>
        public AdfBlockContentBuilder Panel(string panelType, Action<AdfBlockContentBuilder> content)
        {
            AdfNode node = AdfNode.CreatePanel(panelType);
            content(new AdfBlockContentBuilder(node.Content!));
            _content.Add(node);
            return this;
        }

        /// <summary>
        /// Adds a code block with the given text and optional language.
        /// </summary>
        /// <param name="text">The code block's text.</param>
        /// <param name="language">The optional programming language.</param>
        public AdfBlockContentBuilder CodeBlock(string text, string? language = null)
        {
            AdfNode node = AdfNode.CreateCodeBlock(language);
            node.Content!.Add(AdfNode.CreateText(text));
            _content.Add(node);
            return this;
        }

        /// <summary>
        /// Adds a horizontal rule.
        /// </summary>
        public AdfBlockContentBuilder Rule()
        {
            _content.Add(AdfNode.CreateRule());
            return this;
        }

        /// <summary>
        /// Adds a media single node wrapping a single media item, per the ADF spec.
        /// </summary>
        /// <param name="id">The Media Services ID used to query the media's metadata.</param>
        /// <param name="type">The media type - <c>"file"</c> or <c>"link"</c>.</param>
        /// <param name="collection">The Media Services collection name the media belongs to.</param>
        /// <param name="layout">The layout - e.g. <c>"center"</c>, <c>"wrap-left"</c>, <c>"wide"</c>. Defaults to <c>"center"</c>.</param>
        /// <param name="width">The optional display width of the media, in pixels.</param>
        /// <param name="height">The optional display height of the media, in pixels.</param>
        /// <param name="marks">Optionally builds the inner media's marks - <c>link</c> and <c>border</c> are legal.</param>
        public AdfBlockContentBuilder MediaSingle(string id, string type, string collection, string layout = "center", int? width = null, int? height = null, Action<AdfMarkSetBuilder>? marks = null)
        {
            AdfNode media = AdfNode.CreateMedia(id, type, collection, width, height);
            media.Marks = AdfMarkSetBuilder.Build(marks);
            _content.Add(AdfNode.CreateMediaSingle(media, layout));
            return this;
        }

        /// <summary>
        /// Adds a media group holding several media items, built via a nested <see cref="AdfMediaGroupBuilder"/> lambda.
        /// </summary>
        public AdfBlockContentBuilder MediaGroup(Action<AdfMediaGroupBuilder> content)
        {
            AdfNode node = AdfNode.CreateMediaGroup();
            content(new AdfMediaGroupBuilder(node.Content!));
            _content.Add(node);
            return this;
        }

        /// <summary>
        /// Adds an expand (a collapsible container) with block content built via a nested
        /// <see cref="AdfBlockContentBuilder"/> lambda. Per the ADF spec an expand is only legal at the top level
        /// of a document; inside a table cell or header use <see cref="NestedExpand"/> instead -
        /// <see cref="AdfDocument.Validate"/> reports either misplacement.
        /// </summary>
        /// <param name="title">The optional title shown on the expand.</param>
        /// <param name="content">Builds the expand's block content.</param>
        public AdfBlockContentBuilder Expand(string? title, Action<AdfBlockContentBuilder> content)
        {
            AdfNode node = AdfNode.CreateExpand(title);
            content(new AdfBlockContentBuilder(node.Content!));
            _content.Add(node);
            return this;
        }

        /// <summary>
        /// Adds a nested expand with block content built via a nested <see cref="AdfBlockContentBuilder"/> lambda.
        /// Per the ADF spec a nested expand is only legal inside a table cell or header, and may only contain
        /// paragraphs, headings, media groups and media singles - <see cref="AdfDocument.Validate"/> reports
        /// anything else.
        /// </summary>
        /// <param name="title">The optional title shown on the expand.</param>
        /// <param name="content">Builds the nested expand's block content.</param>
        public AdfBlockContentBuilder NestedExpand(string? title, Action<AdfBlockContentBuilder> content)
        {
            AdfNode node = AdfNode.CreateNestedExpand(title);
            content(new AdfBlockContentBuilder(node.Content!));
            _content.Add(node);
            return this;
        }

        /// <summary>
        /// Adds a task list (a checkbox list) with items built via a nested <see cref="AdfTaskListBuilder"/> lambda.
        /// </summary>
        /// <param name="content">Builds the task items.</param>
        /// <param name="localId">A unique identifier for the list. A new GUID when omitted.</param>
        public AdfBlockContentBuilder TaskList(Action<AdfTaskListBuilder> content, string? localId = null)
        {
            AdfNode node = AdfNode.CreateTaskList(localId: localId);
            content(new AdfTaskListBuilder(node.Content!));
            _content.Add(node);
            return this;
        }

        /// <summary>
        /// Adds a sync block - a reference to synced content, with no content of its own.
        /// </summary>
        /// <param name="resourceId">The identifier of the synced resource.</param>
        /// <param name="localId">A unique identifier for the node. A new GUID when omitted.</param>
        public AdfBlockContentBuilder SyncBlock(string resourceId, string? localId = null)
        {
            _content.Add(AdfNode.CreateSyncBlock(resourceId, localId));
            return this;
        }

        /// <summary>
        /// Adds a bodied sync block - the source of synced content - with block content built via a nested
        /// <see cref="AdfBlockContentBuilder"/> lambda.
        /// </summary>
        /// <param name="resourceId">The identifier of the synced resource.</param>
        /// <param name="content">Builds the synced block's body.</param>
        /// <param name="localId">A unique identifier for the node. A new GUID when omitted.</param>
        public AdfBlockContentBuilder BodiedSyncBlock(string resourceId, Action<AdfBlockContentBuilder> content, string? localId = null)
        {
            AdfNode node = AdfNode.CreateBodiedSyncBlock(resourceId, localId: localId);
            content(new AdfBlockContentBuilder(node.Content!));
            _content.Add(node);
            return this;
        }

        /// <summary>
        /// Adds a multi-bodied extension with frames built via a nested <see cref="AdfExtensionFrameBuilder"/> lambda.
        /// </summary>
        /// <param name="extensionKey">The extension's key (non-empty).</param>
        /// <param name="extensionType">The extension's type (non-empty).</param>
        /// <param name="frames">Builds the extension's frames.</param>
        public AdfBlockContentBuilder MultiBodiedExtension(string extensionKey, string extensionType, Action<AdfExtensionFrameBuilder> frames)
        {
            AdfNode node = AdfNode.CreateMultiBodiedExtension(extensionKey, extensionType);
            frames(new AdfExtensionFrameBuilder(node.Content!));
            _content.Add(node);
            return this;
        }

        /// <summary>
        /// Adds an extension frame with block content built via a nested <see cref="AdfBlockContentBuilder"/>
        /// lambda. Per the ADF spec a frame belongs in a multi-bodied extension (see
        /// <see cref="MultiBodiedExtension"/>) or an expand.
        /// </summary>
        public AdfBlockContentBuilder ExtensionFrame(Action<AdfBlockContentBuilder> content)
        {
            AdfNode node = AdfNode.CreateExtensionFrame();
            content(new AdfBlockContentBuilder(node.Content!));
            _content.Add(node);
            return this;
        }

        /// <summary>
        /// Adds a table with rows built via a nested <see cref="AdfTableBuilder"/> lambda.
        /// </summary>
        public AdfBlockContentBuilder Table(Action<AdfTableBuilder> content)
        {
            AdfNode node = AdfNode.CreateTable();
            content(new AdfTableBuilder(node.Content!));
            _content.Add(node);
            return this;
        }
    }
}
