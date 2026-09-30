// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Models;

namespace AdfDotNet.Builders
{
    /// <summary>
    /// Builds the rows of a <c>table</c> node, via nested-lambda composition.
    /// </summary>
    public sealed class AdfTableBuilder
    {
        private readonly List<AdfNode> _content;

        internal AdfTableBuilder(List<AdfNode> content) => _content = content;

        /// <summary>
        /// Adds a table row with cells/headers built via a nested <see cref="AdfTableRowBuilder"/> lambda.
        /// </summary>
        public AdfTableBuilder Row(Action<AdfTableRowBuilder> content)
        {
            AdfNode node = AdfNode.CreateTableRow();
            content(new AdfTableRowBuilder(node.Content!));
            _content.Add(node);
            return this;
        }
    }
}
