// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using HtmlAgilityPack;

namespace AdfDotNet.Models
{
    /// <summary>
    /// Represents a content type in the ADF (Atlassian Document Format) model, including its name, associated marks, and an optional attribute extractor function.
    /// </summary>
    public class AdfContentType
    {
        /// <summary>
        /// Gets or sets the name of the content type.
        /// </summary>
        public string TypeName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of marks associated with the content type.
        /// </summary>
        public List<AdfMarkDefinition> Marks { get; set; } = new List<AdfMarkDefinition>();

        /// <summary>
        /// Gets or sets a function that extracts attributes from an HtmlNode and returns them as a dictionary. This function can be used to customize the extraction of attributes for the content type.
        /// </summary>
        public Func<HtmlNode, Dictionary<string, object>>? AttributeExtractor { get; set; }

        /// <summary>
        /// Creates a new AdfContentType instance with the specified type name.
        /// </summary>
        /// <param name="typeName">The name of the content type.</param>
        /// <returns>A new AdfContentType instance.</returns>
        public static AdfContentType FromName(string typeName)
        {
            return new AdfContentType() { TypeName = typeName };
        }
        
        /// <summary>
        /// Creates a new AdfContentType instance with the specified type name and marks.
        /// </summary>
        /// <param name="typeName">The name of the content type.</param>
        /// <param name="marks">The marks associated with the content type.</param>
        /// <returns>A new AdfContentType instance.</returns>
        public static AdfContentType FromNameAndMarks(string typeName, params AdfMarkDefinition[] marks)
        {
            return new AdfContentType()
            {
                TypeName = typeName,
                Marks = marks.ToList()
            };
        }
        
        /// <summary>
        /// Creates a new AdfContentType instance with the specified type name and attribute extractor function.
        /// </summary>
        /// <param name="typeName">The name of the content type.</param>
        /// <param name="attributeExtractor">A function that extracts attributes from an HtmlNode and returns them as a dictionary.</param>
        /// <returns>A new AdfContentType instance.</returns>
        public static AdfContentType FromNameAndAttributes(
            string typeName,
            Func<HtmlNode, Dictionary<string, object>> attributeExtractor)
        {
            return new AdfContentType()
            {
                TypeName = typeName,
                AttributeExtractor = attributeExtractor
            };
        }
    }
}
