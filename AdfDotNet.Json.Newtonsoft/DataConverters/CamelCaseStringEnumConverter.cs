// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Enums;
using AdfDotNet.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace AdfDotNet.DataConverters
{
    /// <summary>
    /// A custom JSON converter that serializes and deserializes enum values as camelCase strings.
    /// </summary>
    /// <remarks>
    /// <see cref="AdfNodeType"/>/<see cref="AdfMarkType"/> values are written with their ADF spec names from
    /// <see cref="AdfNodeSchema.GetTypeName"/>/<see cref="AdfMarkSchema.GetTypeName"/> rather than plain
    /// camelCase, since the two differ for <see cref="AdfMarkType.SubSup"/> (<c>"subsup"</c>, not
    /// <c>"subSup"</c>).
    /// </remarks>
    public class CamelCaseStringEnumConverter : StringEnumConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CamelCaseStringEnumConverter"/> class.
        /// </summary>
        public CamelCaseStringEnumConverter()
          : base(typeof(CamelCaseNamingStrategy))
        {
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            switch (value)
            {
                case AdfNodeType nodeType:
                    writer.WriteValue(AdfNodeSchema.GetTypeName(nodeType));
                    break;
                case AdfMarkType markType:
                    writer.WriteValue(AdfMarkSchema.GetTypeName(markType));
                    break;
                default:
                    base.WriteJson(writer, value, serializer);
                    break;
            }
        }
    }
}
