// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Enums;
using System.Linq;
using static AdfDotNet.Models.AdfAttrValues;

namespace AdfDotNet.Models
{
    /// <summary>
    /// Describes, for every <see cref="AdfMarkType"/>, its attributes and which other marks it can't be
    /// combined with - the mark-side counterpart of <see cref="AdfNodeSchema"/> (which says which node types
    /// may carry which marks). Every rule comes from the mark's page in the ADF spec
    /// (https://developer.atlassian.com/cloud/jira/platform/apis/document/structure/), except <c>border</c>'s,
    /// whose page returns 404 and whose rules come from the ADF JSON schema instead.
    /// </summary>
    public static class AdfMarkSchema
    {
        private sealed class Entry
        {
            public IReadOnlyList<AdfAttrRule> Attrs { get; set; } = Array.Empty<AdfAttrRule>();

            /// <summary>
            /// Marks this one can't be combined with. When <see cref="OnlyCombinableWith"/> is set, it wins.
            /// </summary>
            public IReadOnlyList<AdfMarkType> IncompatibleWith { get; set; } = Array.Empty<AdfMarkType>();

            /// <summary>
            /// When set, the only marks this one may be combined with (e.g. <c>code</c> + <c>link</c>).
            /// </summary>
            public IReadOnlyList<AdfMarkType>? OnlyCombinableWith { get; set; }
        }

        private static readonly Entry _unconstrained = new Entry();

        private static readonly Dictionary<AdfMarkType, Entry> _schema = new Dictionary<AdfMarkType, Entry>()
        {
            [AdfMarkType.Strong] = _unconstrained,
            [AdfMarkType.Em] = _unconstrained,
            [AdfMarkType.Strike] = _unconstrained,
            [AdfMarkType.Underline] = _unconstrained,
            [AdfMarkType.Code] = new Entry
            {
                OnlyCombinableWith = new[] { AdfMarkType.Link },
            },
            [AdfMarkType.Link] = new Entry
            {
                Attrs = new[]
                {
                    AdfAttrRule.Required("href", "a string", IsString),
                    AdfAttrRule.Optional("title", "a string", IsString),
                    AdfAttrRule.Optional("id", "a string", IsString),
                    AdfAttrRule.Optional("collection", "a string", IsString),
                    AdfAttrRule.Optional("occurrenceKey", "a string", IsString),
                },
            },
            [AdfMarkType.SubSup] = new Entry
            {
                Attrs = new[] { AdfAttrRule.Required("type", "\"sub\" or \"sup\"", OneOf("sub", "sup")) },
            },
            [AdfMarkType.TextColor] = new Entry
            {
                Attrs = new[] { AdfAttrRule.Required("color", "a hex color such as \"#daa520\"", IsHexColor) },
                IncompatibleWith = new[] { AdfMarkType.Code, AdfMarkType.Link },
            },
            [AdfMarkType.BackgroundColor] = new Entry
            {
                Attrs = new[] { AdfAttrRule.Required("color", "a hex color such as \"#fedec8\"", IsHexColor) },
                IncompatibleWith = new[] { AdfMarkType.Code },
            },
            // The border page returns 404; these rules come from the ADF JSON schema's border_mark, which also
            // allows an eight-digit (alpha) hex color. No combination rules are given.
            [AdfMarkType.Border] = new Entry
            {
                Attrs = new[]
                {
                    AdfAttrRule.Required("size", "a number from 1 to 3", Number(1, 3)),
                    AdfAttrRule.Required("color", "a six- or eight-digit hex color such as \"#091e4224\"", IsBorderColor),
                },
            },
        };

        private static Entry Get(AdfMarkType type) => _schema.TryGetValue(type, out Entry? entry) ? entry : _unconstrained;

        /// <summary>
        /// Converts an <see cref="AdfMarkType"/> to the camelCase name used for its ADF JSON "type" field
        /// (e.g. <see cref="AdfMarkType.TextColor"/> -&gt; "textColor"; <see cref="AdfMarkType.SubSup"/> -&gt; "subsup").
        /// </summary>
        public static string GetTypeName(AdfMarkType type) =>
            type == AdfMarkType.SubSup ? "subsup" : AdfNodeSchema.ToCamelCase(type.ToString());

        /// <summary>
        /// Determines whether marks of types <paramref name="first"/> and <paramref name="second"/> may be
        /// applied to the same node. Symmetric: either mark's restriction is enough to forbid the pair.
        /// </summary>
        public static bool CanCombine(AdfMarkType first, AdfMarkType second) =>
            Allows(Get(first), second) && Allows(Get(second), first);

        private static bool Allows(Entry entry, AdfMarkType other) =>
            entry.OnlyCombinableWith != null
                ? entry.OnlyCombinableWith.Contains(other)
                : !entry.IncompatibleWith.Contains(other);

        /// <summary>
        /// Checks one node's marks: each mark's attributes, and every pair of marks for a forbidden combination.
        /// Whether the node's type may carry each mark at all is <see cref="AdfNodeSchema"/>'s job.
        /// </summary>
        internal static void Validate(IReadOnlyList<AdfMark> marks, string path, List<string> errors)
        {
            foreach (AdfMark mark in marks)
                AdfAttrRule.Check(Get(mark.Type).Attrs, mark.Attrs, $"mark '{GetTypeName(mark.Type)}'", path, errors);

            for (int i = 0; i < marks.Count; i++)
            {
                for (int j = i + 1; j < marks.Count; j++)
                {
                    if (!CanCombine(marks[i].Type, marks[j].Type))
                        errors.Add($"{path}: mark '{GetTypeName(marks[i].Type)}' cannot be combined with mark '{GetTypeName(marks[j].Type)}'.");
                }
            }
        }
    }
}
