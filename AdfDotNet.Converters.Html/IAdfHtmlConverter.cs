// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Models;

namespace AdfDotNet.FormatConverters
{
    /// <summary>
    /// DI-friendly entry point for converting between ADF (Atlassian Document Format) and HTML. Wraps the
    /// existing stateless <see cref="AdfToHtmlConverter"/>/<see cref="HtmlToAdfConverter"/> static APIs so
    /// they can be registered and consumed as a service.
    /// </summary>
    public interface IAdfHtmlConverter
    {
        /// <summary>
        /// Converts an ADF document to HTML format.
        /// </summary>
        /// <param name="adfDocument">The ADF document to convert.</param>
        /// <returns>The HTML representation of the ADF document.</returns>
        string ConvertToHtml(AdfDocument adfDocument);

        /// <summary>
        /// Converts an ADF JSON string to HTML format.
        /// </summary>
        /// <param name="adfJson">The ADF JSON string to convert.</param>
        /// <returns>The HTML representation of the ADF JSON string.</returns>
        string ConvertToHtml(string adfJson);

        /// <summary>
        /// Converts an HTML string to an ADF document.
        /// </summary>
        /// <param name="html">The HTML string to convert.</param>
        /// <returns>An ADF document representing the HTML content.</returns>
        AdfDocument ConvertToAdf(string html);
    }
}
