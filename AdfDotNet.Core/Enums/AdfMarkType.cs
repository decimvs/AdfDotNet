// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace AdfDotNet.Enums
{
    /// <summary>
    /// Represents the types of marks that can be applied to text in an ADF document.
    /// </summary>
    public enum AdfMarkType
    {
        /// <summary>
        /// Represents a strong mark, typically used for bold text.
        /// </summary>
        Strong,

        /// <summary>
        /// Represents an emphasis mark, typically used for italic text.
        /// </summary>
        Em,

        /// <summary>
        /// Represents a code mark, typically used for inline code or monospaced text.
        /// </summary>
        Code,

        /// <summary>
        /// Represents a link mark, typically used for hyperlinks.
        /// </summary>
        Link,

        /// <summary>
        /// Represents a strike mark, typically used for strikethrough text.
        /// </summary>
        Strike,

        /// <summary>
        /// Represents an underline mark, typically used for underlined text.
        /// </summary>
        Underline,

        /// <summary>
        /// Represents a subscript or superscript mark, typically used for subscript or superscript text.
        /// </summary>
        SubSup,

        /// <summary>
        /// Represents a text color mark, typically used for changing the color of the text.
        /// </summary>
        TextColor,

        /// <summary>
        /// Represents a background color mark, typically used for highlighting text.
        /// </summary>
        BackgroundColor,

        /// <summary>
        /// Represents a border mark, drawn around a <c>media</c> or <c>mediaInline</c> node.
        /// </summary>
        Border,
    }
}