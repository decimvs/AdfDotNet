// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Models;

namespace AdfDotNet.Builders
{
    /// <summary>
    /// Builds the cells and headers of a <c>tableRow</c> node, via nested-lambda composition.
    /// </summary>
    public sealed class AdfTableRowBuilder
    {
        private readonly List<AdfNode> _content;

        internal AdfTableRowBuilder(List<AdfNode> content) => _content = content;

        /// <summary>
        /// Adds a table cell with block content built via a nested <see cref="AdfBlockContentBuilder"/> lambda.
        /// </summary>
        public AdfTableRowBuilder Cell(Action<AdfBlockContentBuilder> content)
        {
            AdfNode node = AdfNode.CreateTableCell();
            content(new AdfBlockContentBuilder(node.Content!));
            _content.Add(node);
            return this;
        }

        /// <summary>
        /// Adds a table header cell with block content built via a nested <see cref="AdfBlockContentBuilder"/> lambda.
        /// </summary>
        public AdfTableRowBuilder Header(Action<AdfBlockContentBuilder> content)
        {
            AdfNode node = AdfNode.CreateTableHeader();
            content(new AdfBlockContentBuilder(node.Content!));
            _content.Add(node);
            return this;
        }
    }
}
