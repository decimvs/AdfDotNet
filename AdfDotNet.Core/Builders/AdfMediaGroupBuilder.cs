// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Models;

namespace AdfDotNet.Builders
{
    /// <summary>
    /// Builds the media items of a <c>mediaGroup</c> node, via nested-lambda composition.
    /// </summary>
    public sealed class AdfMediaGroupBuilder
    {
        private readonly List<AdfNode> _content;

        internal AdfMediaGroupBuilder(List<AdfNode> content) => _content = content;

        /// <summary>
        /// Adds a media item.
        /// </summary>
        /// <param name="id">The Media Services ID used to query the media's metadata.</param>
        /// <param name="type">The media type - <c>"file"</c> or <c>"link"</c>.</param>
        /// <param name="collection">The Media Services collection name the media belongs to.</param>
        /// <param name="width">The optional display width of the media, in pixels.</param>
        /// <param name="height">The optional display height of the media, in pixels.</param>
        /// <param name="marks">Optionally builds the media's marks - <c>link</c> and <c>border</c> are legal.</param>
        public AdfMediaGroupBuilder Media(string id, string type, string collection, int? width = null, int? height = null, Action<AdfMarkSetBuilder>? marks = null)
        {
            AdfNode media = AdfNode.CreateMedia(id, type, collection, width, height);
            media.Marks = AdfMarkSetBuilder.Build(marks);
            _content.Add(media);
            return this;
        }
    }
}
