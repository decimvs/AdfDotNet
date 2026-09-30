// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Models;

namespace AdfDotNet.DataConverters
{
    /// <summary>
    /// Non-DI convenience entry point for serializing <see cref="AdfDocument"/> instances, mirroring the
    /// call syntax of the <c>AdfDocument.ToJson()</c> instance method this package replaces.
    /// </summary>
    public static class AdfDocumentJsonExtensions
    {
        /// <summary>
        /// Converts the ADF document to its JSON representation.
        /// </summary>
        /// <param name="document">The ADF document to serialize.</param>
        /// <param name="prettyPrint">Whether to indent the produced JSON.</param>
        /// <returns>A JSON string representing the ADF document.</returns>
        public static string ToJson(this AdfDocument document, bool prettyPrint = false) =>
            AdfJsonConverter.Serialize(document, prettyPrint);
    }
}
