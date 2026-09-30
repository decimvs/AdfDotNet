// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Models;

namespace AdfDotNet.Builders
{
    /// <summary>
    /// Builds the inline content of a <c>paragraph</c> or <c>heading</c> node (text runs and hard breaks),
    /// via nested-lambda composition, e.g. <c>p.Text("hello ").Text("world", m => m.Strong())</c>.
    /// </summary>
    /// <remarks>
    /// Shared between paragraph and heading content rather than split into per-parent-type builders - like
    /// <see cref="AdfBlockContentBuilder"/>, legality of the resulting tree (e.g. <c>hardBreak</c> being
    /// illegal directly under some parent types) is left to <see cref="AdfDocument.Validate"/> rather than
    /// reinvented here.
    /// </remarks>
    public sealed class AdfInlineContentBuilder
    {
        private readonly List<AdfNode> _content;

        internal AdfInlineContentBuilder(List<AdfNode> content) => _content = content;

        /// <summary>
        /// Adds a plain (unmarked) text run.
        /// </summary>
        /// <param name="text">The run's text.</param>
        public AdfInlineContentBuilder Text(string text)
        {
            _content.Add(AdfNode.CreateText(text));
            return this;
        }

        /// <summary>
        /// Adds a text run with marks, built via a nested <see cref="AdfMarkSetBuilder"/> lambda - the
        /// mechanism for combining multiple marks (e.g. bold and italic) on one run.
        /// </summary>
        /// <param name="text">The run's text.</param>
        /// <param name="marks">Builds the marks applied to this run.</param>
        public AdfInlineContentBuilder Text(string text, Action<AdfMarkSetBuilder> marks)
        {
            AdfMarkSetBuilder markSetBuilder = new AdfMarkSetBuilder();
            marks(markSetBuilder);
            _content.Add(AdfNode.CreateText(text, markSetBuilder.Build()));
            return this;
        }

        /// <summary>
        /// Adds a hard line break.
        /// </summary>
        public AdfInlineContentBuilder HardBreak()
        {
            _content.Add(AdfNode.CreateHardBreak());
            return this;
        }

        /// <summary>
        /// Adds an inline card (smart link) for the given URL.
        /// </summary>
        /// <param name="url">The URL the inline card renders a smart link for.</param>
        public AdfInlineContentBuilder InlineCard(string url)
        {
            _content.Add(AdfNode.CreateInlineCard(url));
            return this;
        }

        /// <summary>
        /// Adds an emoji.
        /// </summary>
        /// <param name="shortName">The emoji's short name, e.g. <c>:grinning:</c>.</param>
        /// <param name="id">The optional emoji service ID.</param>
        /// <param name="text">Optional text representation, rendered in place of <paramref name="shortName"/> when present.</param>
        public AdfInlineContentBuilder Emoji(string shortName, string? id = null, string? text = null)
        {
            _content.Add(AdfNode.CreateEmoji(shortName, id, text));
            return this;
        }

        /// <summary>
        /// Adds a user mention.
        /// </summary>
        /// <param name="id">The Atlassian Account ID of the mentioned user.</param>
        /// <param name="text">Optional textual representation, including the leading <c>@</c>.</param>
        /// <param name="accessLevel">Optional access level - <c>"NONE"</c>, <c>"SITE"</c>, <c>"APPLICATION"</c> or <c>"CONTAINER"</c>.</param>
        /// <param name="userType">Optional user type - <c>"DEFAULT"</c>, <c>"SPECIAL"</c> or <c>"APP"</c>.</param>
        public AdfInlineContentBuilder Mention(string id, string? text = null, string? accessLevel = null, string? userType = null)
        {
            _content.Add(AdfNode.CreateMention(id, text, accessLevel, userType));
            return this;
        }

        /// <summary>
        /// Adds a date.
        /// </summary>
        /// <param name="timestamp">The date as a Unix timestamp string.</param>
        public AdfInlineContentBuilder Date(string timestamp)
        {
            _content.Add(AdfNode.CreateDate(timestamp));
            return this;
        }

        /// <summary>
        /// Adds a status lozenge.
        /// </summary>
        /// <param name="text">The status text (e.g. <c>"In Progress"</c>).</param>
        /// <param name="color">The lozenge color - <c>"neutral"</c>, <c>"purple"</c>, <c>"blue"</c>, <c>"red"</c>, <c>"yellow"</c>, <c>"green"</c>, or a hex color.</param>
        /// <param name="localId">An optional unique identifier for this status instance.</param>
        public AdfInlineContentBuilder Status(string text, string color, string? localId = null)
        {
            _content.Add(AdfNode.CreateStatus(text, color, localId));
            return this;
        }

        /// <summary>
        /// Adds an inline media item.
        /// </summary>
        /// <param name="id">The Media Services ID used to query the media's metadata.</param>
        /// <param name="collection">The Media Services collection name the media belongs to.</param>
        /// <param name="type">The optional media type - <c>"file"</c>, <c>"link"</c> or <c>"image"</c>.</param>
        /// <param name="width">The optional display width, in pixels.</param>
        /// <param name="height">The optional display height, in pixels.</param>
        /// <param name="marks">Optionally builds the media's marks - <c>link</c> and <c>border</c> are legal.</param>
        public AdfInlineContentBuilder MediaInline(string id, string collection, string? type = null, int? width = null, int? height = null, Action<AdfMarkSetBuilder>? marks = null)
        {
            AdfNode media = AdfNode.CreateMediaInline(id, collection, type, width, height);
            media.Marks = AdfMarkSetBuilder.Build(marks);
            _content.Add(media);
            return this;
        }
    }
}
