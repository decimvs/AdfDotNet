// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Models;

namespace AdfDotNet.FormatConverters
{
    /// <summary>
    /// Default, stateless <see cref="IAdfMarkdownConverter"/> implementation. Delegates to the existing
    /// <see cref="AdfToMarkdownConverter"/>/<see cref="MarkdownToAdfConverter"/> static APIs, so it is safe
    /// to register as a DI singleton.
    /// </summary>
    public sealed class AdfMarkdownConverter : IAdfMarkdownConverter
    {
        /// <inheritdoc />
        public string ConvertToMarkdown(AdfDocument adfDocument) => AdfToMarkdownConverter.Convert(adfDocument);

        /// <inheritdoc />
        public string ConvertToMarkdown(string adfJson) => AdfToMarkdownConverter.Convert(adfJson);

        /// <inheritdoc />
        public AdfDocument ConvertToAdf(string markdown) => MarkdownToAdfConverter.Convert(markdown);
    }
}
