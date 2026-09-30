// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Enums;

namespace AdfDotNet.Models
{
    /// <summary>
    /// Represents a mark in the ADF (Atlassian Document Format) model.
    /// </summary>
    public class AdfMark
    {
        /// <summary>
        /// Gets or sets the type of the mark.
        /// </summary>
        public AdfMarkType Type { get; set; }

        /// <summary>
        /// Gets or sets the attributes of the mark. This is an optional property that can hold additional information about the mark.
        /// </summary>
        public Dictionary<string, object>? Attrs { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdfMark"/> class with the specified type and optional attributes.
        /// </summary>
        /// <param name="type">The type of the mark.</param>
        /// <param name="attrs">The attributes of the mark. This is an optional parameter that can hold additional information about the mark.</param>
        public AdfMark(AdfMarkType type, Dictionary<string, object>? attrs = null)
        {
            Type = type;
            Attrs = attrs;
        }
    }
}