// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Models;

namespace AdfDotNet.FormatConverters
{
    /// <summary>
    /// Default, stateless <see cref="IAdfHtmlConverter"/> implementation. Delegates to the existing
    /// <see cref="AdfToHtmlConverter"/>/<see cref="HtmlToAdfConverter"/> static APIs, so it is safe to
    /// register as a DI singleton.
    /// </summary>
    public sealed class AdfHtmlConverter : IAdfHtmlConverter
    {
        /// <inheritdoc />
        public string ConvertToHtml(AdfDocument adfDocument) => AdfToHtmlConverter.Convert(adfDocument);

        /// <inheritdoc />
        public string ConvertToHtml(string adfJson) => AdfToHtmlConverter.Convert(adfJson);

        /// <inheritdoc />
        public AdfDocument ConvertToAdf(string html) => HtmlToAdfConverter.Convert(html);
    }
}
