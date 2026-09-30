// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Enums;
using AdfDotNet.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace AdfDotNet.DataConverters
{
    /// <summary>
    /// A custom JSON converter for the AdfDocument class, responsible for serializing and deserializing ADF documents to and from JSON format.
    /// </summary>
    public class AdfDocumentConverter : JsonConverter<AdfDocument>
    {
        /// <summary>
        /// Reads the JSON representation of an ADF document and converts it into an AdfDocument object.
        /// </summary>
        /// <param name="reader">The JsonReader to read from.</param>
        /// <param name="objectType">The type of the object to create.</param>
        /// <param name="existingValue">The existing value of the object being read.</param>
        /// <param name="hasExistingValue">Whether there is an existing value.</param>
        /// <param name="serializer">The JsonSerializer to use.</param>
        /// <returns>The deserialized AdfDocument object.</returns>
        /// <exception cref="JsonSerializationException"></exception>
        public override AdfDocument? ReadJson(JsonReader reader, Type objectType, AdfDocument? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            JObject jsonObject = JObject.Load(reader);
            string rootTypeName = jsonObject["type"]?.Value<string>() ?? throw new JsonSerializationException("ADF node type is required.");

            if (!Enum.TryParse(rootTypeName, true, out AdfNodeType type) || type != AdfNodeType.Doc)
            {
                throw new JsonSerializationException("Invalid ADF document format. Root node must have type 'doc'.");
            }

            return ReadDocument(jsonObject);
        }

        /// <summary>
        /// Writes the JSON representation of an AdfDocument object to the specified JsonWriter.
        /// </summary>
        /// <param name="writer">The JsonWriter to write to.</param>
        /// <param name="value">The AdfDocument value to write.</param>
        /// <param name="serializer">The JsonSerializer to use.</param>
        public override void WriteJson(JsonWriter writer, AdfDocument? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();

            writer.WritePropertyName("version");
            writer.WriteValue(value.Version);

            writer.WritePropertyName("type");
            serializer.Serialize(writer, value.Type);

            if (value.Attrs != null)
            {
                writer.WritePropertyName("attrs");
                serializer.Serialize(writer, value.Attrs);
            }

            if (value.Content != null)
            {
                writer.WritePropertyName("content");
                serializer.Serialize(writer, value.Content);
            }

            if (value.Text != null)
            {
                writer.WritePropertyName("text");
                writer.WriteValue(value.Text);
            }

            if (value.Marks != null)
            {
                writer.WritePropertyName("marks");
                serializer.Serialize(writer, value.Marks);
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Reads an ADF document from a JObject and converts it into an AdfDocument object.
        /// </summary>
        /// <param name="jsonObject">The JObject representing the ADF document.</param>
        /// <returns>The deserialized AdfDocument object.</returns>
        private static AdfDocument ReadDocument(JObject jsonObject)
        {
            return new AdfDocument
            {
                Version = jsonObject["version"]?.Value<int>() ?? 1,
                Attrs = ReadAttributes(jsonObject["attrs"]),
                Content = ReadContent(jsonObject["content"]),
                Text = jsonObject["text"]?.Value<string>(),
                Marks = ReadMarks(jsonObject["marks"])
            };
        }
        
        /// <summary>
        /// Reads an ADF node from a JObject and converts it into an AdfNode object, or returns
        /// <see langword="null"/> when the node's type is defined only by the ADF JSON schema
        /// (<see cref="AdfSpecCoverage.IsNonSpecNodeTypeName"/>) - dropped (with its whole subtree) rather than
        /// failing the deserialization, since it's known ADF, not malformed input. A type name neither the spec
        /// nor the schema defines still throws.
        /// </summary>
        /// <param name="nodeObject">The JObject representing the ADF node.</param>
        /// <returns>The deserialized AdfNode object, or <see langword="null"/> if it was skipped.</returns>
        /// <exception cref="JsonSerializationException">Thrown when the node's type is missing or not a recognized ADF type.</exception>
        private static AdfNode? ReadNode(JObject nodeObject)
        {
            string typeName = nodeObject["type"]?.Value<string>() ?? throw new JsonSerializationException("ADF node type is required.");

            if (!Enum.TryParse(typeName, true, out AdfNodeType type))
            {
                if (AdfSpecCoverage.IsNonSpecNodeTypeName(typeName))
                    return null;

                throw new JsonSerializationException($"Unknown ADF node type '{typeName}'.");
            }

            AdfNode node = type == AdfNodeType.Doc ? new AdfDocument() : new AdfNode(type);

            node.Type = type;
            node.Attrs = ReadAttributes(nodeObject["attrs"]);
            node.Content = ReadContent(nodeObject["content"]);
            node.Text = nodeObject["text"]?.Value<string>();
            node.Marks = ReadMarks(nodeObject["marks"]);

            if (node is AdfDocument document)
            {
                document.Version = nodeObject["version"]?.Value<int>() ?? 1;
            }

            return node;
        }
        
        /// <summary>
        /// Reads a list of ADF nodes from a JToken and converts it into a list of AdfNode objects.
        /// </summary>
        /// <param name="token">The JToken representing the ADF content.</param>
        /// <returns>The deserialized list of AdfNode objects.</returns>
        private static List<AdfNode>? ReadContent(JToken? token)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return null;
            }

            if (token is not JArray contentArray)
            {
                throw new JsonSerializationException("ADF content must be a JSON array.");
            }

            List<AdfNode> content = new List<AdfNode>(contentArray.Count);

            foreach (JToken childToken in contentArray)
            {
                if (childToken is not JObject childObject)
                {
                    throw new JsonSerializationException("ADF content items must be JSON objects.");
                }

                AdfNode? child = ReadNode(childObject);
                if (child != null)
                {
                    content.Add(child);
                }
            }

            return content;
        }

        /// <summary>
        /// Reads a list of ADF marks from a JToken and converts it into a list of AdfMark objects.
        /// </summary>
        /// <param name="token">The JToken representing the ADF marks.</param>
        /// <returns>The deserialized list of AdfMark objects.</returns>
        /// <exception cref="JsonSerializationException">Thrown when the token is not a JSON array, contains invalid items, or a mark type the spec doesn't define at all.</exception>
        /// <remarks>
        /// A mark whose type is defined only by the ADF JSON schema
        /// (<see cref="AdfSpecCoverage.IsNonSpecMarkTypeName"/>) is dropped rather than failing the whole
        /// deserialization - the node itself and its other marks are kept.
        /// </remarks>
        private static List<AdfMark>? ReadMarks(JToken? token)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return null;
            }

            if (token is not JArray markArray)
            {
                throw new JsonSerializationException("ADF marks must be a JSON array.");
            }

            List<AdfMark> marks = new List<AdfMark>(markArray.Count);

            foreach (JToken markToken in markArray)
            {
                if (markToken is not JObject markObject)
                {
                    throw new JsonSerializationException("ADF marks must be JSON objects.");
                }

                string typeName = markObject["type"]?.Value<string>() ?? throw new JsonSerializationException("ADF mark type is required.");

                if (!Enum.TryParse(typeName, true, out AdfMarkType markType))
                {
                    if (AdfSpecCoverage.IsNonSpecMarkTypeName(typeName))
                        continue;

                    throw new JsonSerializationException($"Unknown ADF mark type '{typeName}'.");
                }

                marks.Add(new AdfMark(markType, ReadAttributes(markObject["attrs"])));
            }

            return marks;
        }

        /// <summary>
        /// Reads a dictionary of attributes from a JToken and converts it into a <c>Dictionary&lt;string, object&gt;</c>.
        /// </summary>
        /// <param name="token">The JToken representing the ADF attributes.</param>
        /// <returns>The deserialized dictionary of attributes.</returns>
        /// <exception cref="JsonSerializationException">Thrown when the token is not a JSON object.</exception>
        private static Dictionary<string, object>? ReadAttributes(JToken? token)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return null;
            }

            if (token is not JObject attrsObject)
            {
                throw new JsonSerializationException("ADF attrs must be a JSON object.");
            }

            Dictionary<string, object> attrs = new Dictionary<string, object>();

            foreach (JProperty property in attrsObject.Properties())
            {
                attrs[property.Name] = ReadValue(property.Value)!;
            }

            return attrs;
        }

        /// <summary>
        /// Reads a value from a JToken and converts it into an appropriate .NET object.
        /// </summary>
        /// <param name="token">The JToken representing the ADF value.</param>
        /// <returns>The deserialized .NET object.</returns>
        private static object? ReadValue(JToken token)
        {
            return token.Type switch
            {
                JTokenType.Object => ReadAttributes(token),
                JTokenType.Array => ReadArray((JArray)token),
                JTokenType.Integer => token.Value<long>(),
                JTokenType.Float => token.Value<double>(),
                JTokenType.Boolean => token.Value<bool>(),
                JTokenType.String => token.Value<string>(),
                JTokenType.Date => token.Value<DateTime>(),
                JTokenType.Null or JTokenType.Undefined => null,
                _ => ((JValue)token).Value
            };
        }

        /// <summary>
        /// Reads a list of values from a JArray and converts it into a <c>List&lt;object?&gt;</c>.
        /// </summary>
        /// <param name="array">The JArray representing the ADF values.</param>
        /// <returns>The deserialized list of .NET objects.</returns>
        /// <exception cref="JsonSerializationException">Thrown when the array contains invalid items.</exception>
        private static List<object?> ReadArray(JArray array)
        {
            List<object?> items = new List<object?>(array.Count);

            foreach (JToken item in array)
            {
                items.Add(ReadValue(item));
            }

            return items;
        }
    }
}
