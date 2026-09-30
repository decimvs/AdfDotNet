// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Enums;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;

namespace AdfDotNet.Models
{
    /// <summary>
    /// Represents a definition of a mark in the ADF (Atlassian Document Format) model.
    /// </summary>
    public class AdfMarkDefinition
    {
        /// <summary>
        /// Gets or sets the type of the mark.
        /// </summary>
        public AdfMarkType Type { get; set; }

        /// <summary>
        /// Gets or sets a function that extracts attributes from an HTML node.
        /// </summary>
        public Func<HtmlNode, Dictionary<string, object>?>? AttributeExtractor { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdfMarkDefinition"/> class with the specified mark type and an optional attribute extractor function.
        /// </summary>
        /// <param name="type">The type of the mark.</param>
        /// <param name="attributeExtractor">A function that extracts attributes from an HTML node. This is an optional parameter.</param>
        public AdfMarkDefinition(
          AdfMarkType type,
          Func<HtmlNode, Dictionary<string, object>?>? attributeExtractor = null)
        {
            Type = type;
            AttributeExtractor = attributeExtractor;
        }
    }
}
