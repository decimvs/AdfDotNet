// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Models;

namespace AdfDotNet.Builders
{
    /// <summary>
    /// Builds the frames of a <c>multiBodiedExtension</c> node, via nested-lambda composition.
    /// </summary>
    public sealed class AdfExtensionFrameBuilder
    {
        private readonly List<AdfNode> _content;

        internal AdfExtensionFrameBuilder(List<AdfNode> content) => _content = content;

        /// <summary>
        /// Adds an extension frame with block content built via a nested <see cref="AdfBlockContentBuilder"/> lambda.
        /// </summary>
        public AdfExtensionFrameBuilder Frame(Action<AdfBlockContentBuilder> content)
        {
            AdfNode node = AdfNode.CreateExtensionFrame();
            content(new AdfBlockContentBuilder(node.Content!));
            _content.Add(node);
            return this;
        }
    }
}
