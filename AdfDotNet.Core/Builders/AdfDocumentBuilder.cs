// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Models;

namespace AdfDotNet.Builders
{
    /// <summary>
    /// Entry point for fluent, nested-lambda composition of an <see cref="AdfDocument"/>, e.g.:
    /// <code>
    /// AdfDocument document = AdfDocumentBuilder.Build(doc => doc
    ///     .Heading(1, "Title")
    ///     .Paragraph(p => p.Text("Hello ").Text("world", m => m.Strong().Em()).Text("!")));
    /// </code>
    /// Structural legality (which child types are allowed where) is not enforced while building - call
    /// <see cref="AdfDocument.Validate"/> on the result if you need to confirm it. Validity is defined once, in
    /// <see cref="AdfNodeSchema"/>, rather than re-implemented across every builder method.
    /// </summary>
    public static class AdfDocumentBuilder
    {
        /// <summary>
        /// Builds a new <see cref="AdfDocument"/> whose top-level content is composed via a nested
        /// <see cref="AdfBlockContentBuilder"/> lambda.
        /// </summary>
        public static AdfDocument Build(Action<AdfBlockContentBuilder> content)
        {
            AdfDocument document = AdfNode.CreateDocument();
            content(new AdfBlockContentBuilder(document.Content!));
            return document;
        }

        /// <summary>
        /// Appends more top-level content to an existing <see cref="AdfDocument"/> (e.g. one just
        /// deserialized from stored ADF JSON) via a nested <see cref="AdfBlockContentBuilder"/> lambda. The
        /// new nodes are added after <paramref name="document"/>'s current content.
        /// </summary>
        /// <param name="document">The document to extend. Mutated in place and returned for chaining.</param>
        /// <param name="content">Builds the block content to append.</param>
        public static AdfDocument Extend(AdfDocument document, Action<AdfBlockContentBuilder> content)
        {
            document.Content ??= new List<AdfNode>();
            content(new AdfBlockContentBuilder(document.Content));
            return document;
        }

        /// <summary>
        /// Inserts more top-level content at the start of an existing <see cref="AdfDocument"/> (e.g. one
        /// just deserialized from stored ADF JSON) via a nested <see cref="AdfBlockContentBuilder"/> lambda.
        /// The new nodes are added before <paramref name="document"/>'s current content, in the order they
        /// were built.
        /// </summary>
        /// <param name="document">The document to extend. Mutated in place and returned for chaining.</param>
        /// <param name="content">Builds the block content to insert.</param>
        public static AdfDocument Prepend(AdfDocument document, Action<AdfBlockContentBuilder> content)
        {
            document.Content ??= new List<AdfNode>();
            List<AdfNode> newContent = new List<AdfNode>();
            content(new AdfBlockContentBuilder(newContent));
            document.Content.InsertRange(0, newContent);
            return document;
        }
    }
}
