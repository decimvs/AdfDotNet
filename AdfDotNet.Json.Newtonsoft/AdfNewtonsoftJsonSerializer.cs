// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Models;

namespace AdfDotNet.DataConverters
{
    /// <summary>
    /// Default, stateless <see cref="IAdfDocumentSerializer"/> implementation. Delegates to the existing
    /// <see cref="AdfJsonConverter"/> static API, so it is safe to register as a DI singleton.
    /// </summary>
    public sealed class AdfNewtonsoftJsonSerializer : IAdfDocumentSerializer
    {
        /// <inheritdoc />
        public string Serialize(AdfDocument document, bool prettyPrint = false) => AdfJsonConverter.Serialize(document, prettyPrint);

        /// <inheritdoc />
        public AdfDocument Deserialize(string json) => AdfJsonConverter.Deserialize(json);
    }
}
