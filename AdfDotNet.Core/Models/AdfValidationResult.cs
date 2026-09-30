// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace AdfDotNet.Models
{
    /// <summary>
    /// The result of validating an <see cref="AdfDocument"/> tree against <see cref="AdfNodeSchema"/>. See
    /// <see cref="AdfDocument.Validate"/>.
    /// </summary>
    public sealed class AdfValidationResult
    {
        private AdfValidationResult(IReadOnlyList<string> errors) => Errors = errors;

        /// <summary>
        /// Gets the validation error messages. Empty when the document is structurally valid.
        /// </summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>
        /// Gets a value indicating whether the document is structurally valid.
        /// </summary>
        public bool IsValid => Errors.Count == 0;

        internal static readonly AdfValidationResult Success = new AdfValidationResult(Array.Empty<string>());

        internal static AdfValidationResult FromErrors(IReadOnlyList<string> errors) =>
            errors.Count == 0 ? Success : new AdfValidationResult(errors);
    }
}
