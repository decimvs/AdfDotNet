// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Models;

namespace AdfDotNet.Builders
{
    /// <summary>
    /// Builds the items of a <c>bulletList</c> or <c>orderedList</c> node, via nested-lambda composition.
    /// </summary>
    public sealed class AdfListBuilder
    {
        private readonly List<AdfNode> _content;

        internal AdfListBuilder(List<AdfNode> content) => _content = content;

        /// <summary>
        /// Adds a list item with block content built via a nested <see cref="AdfBlockContentBuilder"/> lambda.
        /// </summary>
        public AdfListBuilder Item(Action<AdfBlockContentBuilder> content)
        {
            AdfNode node = AdfNode.CreateListItem();
            content(new AdfBlockContentBuilder(node.Content!));
            _content.Add(node);
            return this;
        }
    }
}
