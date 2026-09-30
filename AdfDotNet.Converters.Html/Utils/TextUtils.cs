// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Text.RegularExpressions;

namespace AdfDotNet.Utils
{
    /// <summary>
    /// Provides utility methods for text processing, including whitespace normalization and color extraction from style strings.
    /// </summary>
    public static class TextUtils
    {
        /// <summary>
        /// A regular expression to match leading and trailing whitespace in a string.
        /// </summary>
        private static readonly Regex WhitespaceRegex = new Regex("^\\s+|\\s+$", RegexOptions.Compiled);

        /// <summary>
        /// A regular expression to match CSS color declarations in a style string.
        /// </summary>
        private static readonly Regex ColorRegex = new Regex("color:\\s*([^;]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// A regular expression to match valid hexadecimal color codes, with or without a leading '#' character.
        /// </summary>
        private static readonly Regex HexColorRegex = new Regex("^#?([a-fA-F0-9]{6}|[a-fA-F0-9]{3})$", RegexOptions.Compiled);
        
        /// <summary>
        /// Normalizes whitespace in a string by replacing leading and trailing whitespace with a single space.
        /// </summary>
        /// <param name="input">The input string to normalize.</param>
        /// <returns>The normalized string with leading and trailing whitespace replaced by a single space.</returns>
        public static string NormalizeWhitespace(string input)
        {
            return string.IsNullOrEmpty(input) ? string.Empty : WhitespaceRegex.Replace(input, " ");
        }
        
        /// <summary>
        /// Extracts a hexadecimal color code from a CSS style string.
        /// </summary>
        /// <param name="style">The CSS style string containing a color declaration.</param>
        /// <returns>The extracted hexadecimal color code, or <c>null</c> if no valid color is found.</returns>
        public static string? ExtractHexColor(string style)
        {
            if (string.IsNullOrEmpty(style))
                return null;
            Match match = ColorRegex.Match(style);
            if (!match.Success)
                return null;
            string colorValue = match.Groups[1].Value.Trim();
            string input = colorValue.ToLowerInvariant() switch
            {
                "red" => "#FF0000",
                "green" => "#008000",
                "blue" => "#0000FF",
                "black" => "#000000",
                "white" => "#FFFFFF",
                _ => colorValue,
            };
            if (!HexColorRegex.IsMatch(input))
                return null;
            return !input.StartsWith("#") ? "#" + input : input;
        }
    }
}