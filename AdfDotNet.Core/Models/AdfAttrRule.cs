// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Collections;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace AdfDotNet.Models
{
    /// <summary>
    /// One attribute constraint taken from an ADF spec page: the attribute's name, whether it's required, and
    /// what its value must look like. Used as data by <see cref="AdfNodeSchema"/> and <see cref="AdfMarkSchema"/>
    /// so <see cref="AdfDocument.Validate"/> can check <c>attrs</c> without per-type code.
    /// </summary>
    /// <remarks>
    /// A <see langword="null"/> value counts as absent: it fails a required rule and passes an optional one.
    /// Attributes no rule mentions are never reported - real Jira/Confluence documents carry extra attrs
    /// (e.g. <c>localId</c> on most block nodes) that the spec pages don't list.
    /// </remarks>
    internal sealed class AdfAttrRule
    {
        private AdfAttrRule(string name, bool isRequired, string expectation, Func<object, bool> isValid)
        {
            Name = name;
            IsRequired = isRequired;
            Expectation = expectation;
            IsValid = isValid;
        }

        public string Name { get; }

        public bool IsRequired { get; }

        /// <summary>
        /// Describes a valid value, completing the sentence "must be ..." in an error message.
        /// </summary>
        public string Expectation { get; }

        public Func<object, bool> IsValid { get; }

        public static AdfAttrRule Required(string name, string expectation, Func<object, bool> isValid) =>
            new AdfAttrRule(name, true, expectation, isValid);

        public static AdfAttrRule Optional(string name, string expectation, Func<object, bool> isValid) =>
            new AdfAttrRule(name, false, expectation, isValid);

        /// <summary>
        /// Checks <paramref name="attrs"/> against <paramref name="rules"/>, adding one <c>path: message</c>
        /// error per missing required attribute or invalid value.
        /// </summary>
        public static void Check(IReadOnlyList<AdfAttrRule> rules, Dictionary<string, object>? attrs, string owner, string path, List<string> errors)
        {
            foreach (AdfAttrRule rule in rules)
            {
                object? value = null;
                attrs?.TryGetValue(rule.Name, out value);

                if (value == null)
                {
                    if (rule.IsRequired)
                        errors.Add($"{path}: attribute '{rule.Name}' is required on {owner}.");
                    continue;
                }

                if (!rule.IsValid(value))
                    errors.Add($"{path}: attribute '{rule.Name}' on {owner} must be {rule.Expectation}, but was {Describe(value)}.");
            }
        }

        private static string Describe(object value) => value switch
        {
            string s => $"\"{s}\"",
            bool b => b ? "true" : "false",
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            IDictionary => "an object",
            IEnumerable => "a list",
            _ => value.ToString() ?? string.Empty,
        };
    }

    /// <summary>
    /// Value predicates for <see cref="AdfAttrRule"/>, one per value shape the spec pages use.
    /// </summary>
    internal static class AdfAttrValues
    {
        private static readonly Regex HexColorPattern = new Regex("^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", RegexOptions.Compiled);
        private static readonly Regex BorderColorPattern = new Regex("^#([0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", RegexOptions.Compiled);
        private static readonly Regex DigitsPattern = new Regex("^[0-9]+$", RegexOptions.Compiled);

        public static bool IsString(object value) => value is string;

        public static bool IsNonEmptyString(object value) => value is string s && s.Length > 0;

        public static bool IsBoolean(object value) => value is bool;

        /// <summary>
        /// A <c>#rgb</c> or <c>#rrggbb</c> color. The spec pages ask for "HTML hexadecimal format" and give only
        /// six-digit examples; the three-digit short form is accepted too, since it's the same notation.
        /// </summary>
        public static bool IsHexColor(object value) => value is string s && HexColorPattern.IsMatch(s);

        /// <summary>
        /// A <c>#rrggbb</c> or <c>#rrggbbaa</c> color, per the ADF JSON schema's <c>border_mark</c>.
        /// </summary>
        public static bool IsBorderColor(object value) => value is string s && BorderColorPattern.IsMatch(s);

        public static bool IsDigits(object value) => value is string s && DigitsPattern.IsMatch(s);

        public static Func<object, bool> OneOf(params string[] allowed) =>
            value => value is string s && allowed.Contains(s);

        /// <summary>
        /// A whole number (<see cref="int"/> or <see cref="long"/>, per the Attrs contract) within the range.
        /// </summary>
        public static Func<object, bool> Integer(long min, long max = long.MaxValue) =>
            value => TryGetInteger(value, out long n) && n >= min && n <= max;

        /// <summary>
        /// Any number (<see cref="int"/>, <see cref="long"/> or <see cref="double"/>) within the range.
        /// </summary>
        public static Func<object, bool> Number(double min, double max = double.MaxValue) =>
            value => TryGetNumber(value, out double n) && n >= min && n <= max;

        /// <summary>
        /// Any number, with no range (<c>mediaInline.width</c>/<c>height</c>, typed only as "number" by the JSON schema).
        /// </summary>
        public static bool IsNumber(object value) => TryGetNumber(value, out _);

        /// <summary>
        /// A number greater than zero (sizes in pixels).
        /// </summary>
        public static bool IsPositiveNumber(object value) => TryGetNumber(value, out double n) && n > 0;

        /// <summary>
        /// A list whose every element is a number of zero or more (<c>tableCell.colwidth</c>, where the spec
        /// pages allow 0 for a column with no fixed width).
        /// </summary>
        public static bool NonNegativeNumberList(object value) =>
            value is IEnumerable items && value is not string && value is not IDictionary
                && items.Cast<object?>().All(item => item != null && TryGetNumber(item, out double n) && n >= 0);

        private static bool TryGetInteger(object value, out long result)
        {
            switch (value)
            {
                case int i: result = i; return true;
                case long l: result = l; return true;
                default: result = 0; return false;
            }
        }

        private static bool TryGetNumber(object value, out double result)
        {
            switch (value)
            {
                case int i: result = i; return true;
                case long l: result = l; return true;
                case double d: result = d; return true;
                default: result = 0; return false;
            }
        }
    }
}
