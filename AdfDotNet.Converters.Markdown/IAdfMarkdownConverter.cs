// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Models;

namespace AdfDotNet.FormatConverters
{
    /// <summary>
    /// DI-friendly entry point for converting between ADF (Atlassian Document Format) and Markdown. Wraps
    /// the existing stateless <see cref="AdfToMarkdownConverter"/>/<see cref="MarkdownToAdfConverter"/>
    /// static APIs so they can be registered and consumed as a service.
    /// </summary>
    public interface IAdfMarkdownConverter
    {
        /// <summary>
        /// Converts an ADF document to Markdown format.
        /// </summary>
        /// <param name="adfDocument">The ADF document to convert.</param>
        /// <returns>The Markdown representation of the ADF document.</returns>
        string ConvertToMarkdown(AdfDocument adfDocument);

        /// <summary>
        /// Converts an ADF JSON string to Markdown format.
        /// </summary>
        /// <param name="adfJson">The ADF JSON string to convert.</param>
        /// <returns>The Markdown representation of the ADF JSON string.</returns>
        string ConvertToMarkdown(string adfJson);

        /// <summary>
        /// Converts a Markdown string to an ADF document.
        /// </summary>
        /// <param name="markdown">The Markdown string to convert.</param>
        /// <returns>An ADF document representing the Markdown content.</returns>
        AdfDocument ConvertToAdf(string markdown);
    }
}
