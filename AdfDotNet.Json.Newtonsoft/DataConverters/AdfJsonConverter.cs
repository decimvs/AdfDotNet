// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace AdfDotNet.DataConverters
{
    /// <summary>
    /// Serializes and deserializes <see cref="AdfDocument"/> instances to and from ADF JSON using
    /// Newtonsoft.Json. Stateless, so it is safe to use directly or via <see cref="IAdfDocumentSerializer"/>.
    /// </summary>
    public static class AdfJsonConverter
    {
        /// <summary>
        /// Gets the default JSON serializer settings for serializing/deserializing ADF documents.
        /// </summary>
        /// <param name="prettyPrint">Whether to indent the produced JSON.</param>
        public static JsonSerializerSettings GetDefaultSettings(bool prettyPrint = false)
        {
            return new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = new List<JsonConverter>()
                {
                    new CamelCaseStringEnumConverter(),
                    new AdfDocumentConverter()
                }
            };
        }

        /// <summary>
        /// Converts an ADF document to its JSON representation.
        /// </summary>
        /// <param name="document">The ADF document to serialize.</param>
        /// <param name="prettyPrint">Whether to indent the produced JSON.</param>
        /// <returns>A JSON string representing the ADF document.</returns>
        public static string Serialize(AdfDocument document, bool prettyPrint = false)
        {
            return JsonConvert.SerializeObject(document, GetDefaultSettings(prettyPrint));
        }

        /// <summary>
        /// Creates an ADF document from its JSON representation.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>An ADF document represented by the JSON string.</returns>
        public static AdfDocument Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("ADF JSON content cannot be null or whitespace.", nameof(json));
            }

            return JsonConvert.DeserializeObject<AdfDocument>(json, GetDefaultSettings())
                ?? throw new JsonSerializationException("Unable to deserialize the ADF JSON content.");
        }
    }
}
