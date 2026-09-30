// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Enums;

namespace AdfDotNet.Models
{
    /// <summary>
    /// Represents an ADF (Atlassian Document Format) document, which is the root node of an ADF structure. It contains a version number and inherits from the AdfNode class.
    /// </summary>
    public class AdfDocument : AdfNode
    {
        /// <summary>
        /// Gets or sets the version number of the ADF document.
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdfDocument"/> class.
        /// </summary>
        public AdfDocument()
            : base(AdfNodeType.Doc)
        {
        }

        /// <summary>
        /// Validates this document's tree against the ADF spec, as encoded in <see cref="AdfNodeSchema"/> and
        /// <see cref="AdfMarkSchema"/>: legal child types and child counts, required attributes and their
        /// values, which marks each node may carry, and forbidden mark combinations (e.g. <c>code</c> with
        /// anything but <c>link</c>). Also checks the root's type and <see cref="Version"/>. This does not
        /// depend on how the tree was built (HTML parsing, JSON
        /// deserialization, the fluent builder, or manual construction) - callers decide when to invoke it,
        /// it is never run implicitly by a converter or serializer.
        /// </summary>
        /// <returns>The validation result. Check <see cref="AdfValidationResult.IsValid"/> and, if <c>false</c>, <see cref="AdfValidationResult.Errors"/>.</returns>
        public AdfValidationResult Validate()
        {
            List<string> errors = new List<string>();

            if (Type != AdfNodeType.Doc)
                errors.Add($"root: document node must have type 'doc', but was '{AdfNodeSchema.GetTypeName(Type)}'.");

            if (Version != 1)
                errors.Add($"root: document version must be 1, but was {Version}.");

            errors.AddRange(AdfNodeSchema.Validate(this));

            return AdfValidationResult.FromErrors(errors);
        }

        /// <summary>
        /// Reshapes this document in place so its nesting and marks follow the spec (e.g. a heading inside a
        /// blockquote becomes a paragraph, an empty list is removed, <c>code</c> wins over a conflicting
        /// <c>strong</c>). See <see cref="AdfNormalizer"/> for the full rules. The HTML and Markdown converters
        /// already call this on their output; call it yourself on a tree you've built or edited by hand.
        /// It doesn't fix attribute values, so <see cref="Validate"/> can still report errors afterwards.
        /// </summary>
        /// <returns>This document, for chaining.</returns>
        public AdfDocument Normalize() => AdfNormalizer.Normalize(this);

        /// <summary>
        /// Merges another document into this one by appending <paramref name="other"/>'s top-level content
        /// nodes after this document's own content. This document remains the one returned/mutated (the
        /// "parent" of the merge); <paramref name="other"/>'s nodes become trailing children of it, in
        /// their existing order - <paramref name="other"/> itself is left as an unattached loose document.
        /// Call this repeatedly, or combine with <see cref="Builders.AdfDocumentBuilder.Extend"/>/
        /// <see cref="Builders.AdfDocumentBuilder.Prepend"/>, if you need a specific interleaving of several
        /// sources rather than always appending at the end.
        /// </summary>
        /// <remarks>
        /// The merged-in nodes are the same <see cref="AdfNode"/> instances from <paramref name="other"/>,
        /// not copies - mutating one afterward affects both trees. This is a purely structural splice, not a
        /// validity check; call <see cref="Validate"/> afterward if you need to confirm the combined tree is
        /// still well-formed.
        /// </remarks>
        /// <param name="other">The document whose content is appended to this one.</param>
        /// <returns>This document, for chaining.</returns>
        public AdfDocument Merge(AdfDocument other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (other.Content == null || other.Content.Count == 0)
                return this;

            Content ??= new List<AdfNode>();
            Content.AddRange(other.Content);

            return this;
        }
    }
}
