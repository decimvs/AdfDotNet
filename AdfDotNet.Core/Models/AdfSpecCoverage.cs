// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace AdfDotNet.Models
{
    /// <summary>
    /// Registry of ADF node/mark type names that <see cref="AdfDotNet.Enums.AdfNodeType"/>/
    /// <see cref="AdfDotNet.Enums.AdfMarkType"/> deliberately don't model: types that exist only in the
    /// separate ADF JSON schema (<c>@atlaskit/adf-schema</c>) and aren't part of the Atlassian Document Format
    /// spec (https://developer.atlassian.com/cloud/jira/platform/apis/document/structure/). Every type on the
    /// spec page itself is modeled.
    /// </summary>
    /// <remarks>
    /// Exists so a JSON deserializer can tell "a known ADF construct this library doesn't support - drop it and
    /// keep going" apart from "not a recognized ADF type at all - the document is malformed, fail loudly."
    /// Names listed here are dropped; names neither listed here nor in the enums are errors.
    /// </remarks>
    public static class AdfSpecCoverage
    {
        // Node/mark types in the ADF JSON schema (https://unpkg.com/@atlaskit/adf-schema/dist/json-schema/v1/full.json)
        // but not on the spec page. They occur in real Confluence content, but the spec page says JSON-schema-only
        // types "may not be valid in this implementation", so this library never models them - they're dropped
        // so such documents still load, and what's left is valid for Jira. taskList/taskItem are the exception:
        // they're modeled, because the schema's taskList is the only parent of the spec's blockTaskItem.
        private static readonly HashSet<string> _nonSpecNodeTypeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "blockCard",
            "bodiedExtension",
            "caption",
            "decisionItem",
            "decisionList",
            "embedCard",
            "extension",
            "inlineExtension",
            "layoutColumn",
            "layoutSection",
            "placeholder",
        };

        private static readonly HashSet<string> _nonSpecMarkTypeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "alignment",
            "annotation",
            "breakout",
            "dataConsumer",
            "fontSize",
            "fragment",
            "indentation",
        };

        /// <summary>
        /// True when <paramref name="typeName"/> is a node type defined only by the ADF JSON schema, not by the
        /// spec page - known ADF, but deliberately outside what this library models.
        /// </summary>
        public static bool IsNonSpecNodeTypeName(string typeName) => _nonSpecNodeTypeNames.Contains(typeName);

        /// <summary>
        /// True when <paramref name="typeName"/> is a mark type defined only by the ADF JSON schema, not by the
        /// spec page - known ADF, but deliberately outside what this library models.
        /// </summary>
        public static bool IsNonSpecMarkTypeName(string typeName) => _nonSpecMarkTypeNames.Contains(typeName);
    }
}
