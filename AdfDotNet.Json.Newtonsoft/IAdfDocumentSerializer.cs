// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Models;

namespace AdfDotNet.DataConverters
{
    /// <summary>
    /// DI-friendly entry point for serializing and deserializing ADF documents to and from JSON. Wraps the
    /// existing stateless <see cref="AdfJsonConverter"/> static API so it can be registered and consumed as
    /// a service.
    /// </summary>
    public interface IAdfDocumentSerializer
    {
        /// <summary>
        /// Converts an ADF document to its JSON representation.
        /// </summary>
        /// <param name="document">The ADF document to serialize.</param>
        /// <param name="prettyPrint">Whether to indent the produced JSON.</param>
        /// <returns>A JSON string representing the ADF document.</returns>
        string Serialize(AdfDocument document, bool prettyPrint = false);

        /// <summary>
        /// Creates an ADF document from its JSON representation.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>An ADF document represented by the JSON string.</returns>
        AdfDocument Deserialize(string json);
    }
}
