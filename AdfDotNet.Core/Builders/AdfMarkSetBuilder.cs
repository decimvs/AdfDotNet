// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Enums;
using AdfDotNet.Models;

namespace AdfDotNet.Builders
{
    /// <summary>
    /// Builds the <see cref="AdfMark"/> list applied to a single text run, via nested-lambda composition
    /// (e.g. <c>m => m.Strong().Em()</c>) passed to <see cref="AdfInlineContentBuilder.Text(string, Action{AdfMarkSetBuilder})"/>.
    /// This is the mechanism for combining multiple marks - including marks that carry <see cref="AdfMark.Attrs"/>
    /// such as <see cref="AdfMarkType.Link"/> - on one run, since ADF represents marks as a flat list per node.
    /// </summary>
    public sealed class AdfMarkSetBuilder
    {
        private readonly List<AdfMark> _marks = new List<AdfMark>();

        internal AdfMarkSetBuilder()
        {
        }

        /// <summary>
        /// Adds a strong (bold) mark.
        /// </summary>
        public AdfMarkSetBuilder Strong() => Add(AdfMarkType.Strong);

        /// <summary>
        /// Adds an emphasis (italic) mark.
        /// </summary>
        public AdfMarkSetBuilder Em() => Add(AdfMarkType.Em);

        /// <summary>
        /// Adds a code (inline monospace) mark.
        /// </summary>
        public AdfMarkSetBuilder Code() => Add(AdfMarkType.Code);

        /// <summary>
        /// Adds a strikethrough mark.
        /// </summary>
        public AdfMarkSetBuilder Strike() => Add(AdfMarkType.Strike);

        /// <summary>
        /// Adds an underline mark.
        /// </summary>
        public AdfMarkSetBuilder Underline() => Add(AdfMarkType.Underline);

        /// <summary>
        /// Adds a link mark with the given target URL.
        /// </summary>
        /// <param name="href">The link target URL.</param>
        /// <param name="title">The optional link title (HTML's <c>title</c> attribute).</param>
        public AdfMarkSetBuilder Link(string href, string? title = null)
        {
            Dictionary<string, object> attrs = new Dictionary<string, object> { ["href"] = href };
            if (title != null)
                attrs["title"] = title;
            _marks.Add(new AdfMark(AdfMarkType.Link, attrs));
            return this;
        }

        /// <summary>
        /// Adds a text color mark with the given hex color.
        /// </summary>
        /// <param name="color">A hex color such as <c>"#daa520"</c>. The ADF spec requires hex; a CSS color name
        /// like <c>"red"</c> is stored as given, but fails <c>Validate()</c>.</param>
        public AdfMarkSetBuilder TextColor(string color)
        {
            _marks.Add(new AdfMark(AdfMarkType.TextColor, new Dictionary<string, object> { ["color"] = color }));
            return this;
        }

        /// <summary>
        /// Adds a subscript or superscript mark.
        /// </summary>
        /// <param name="type">Either <c>"sub"</c> or <c>"sup"</c>.</param>
        public AdfMarkSetBuilder SubSup(string type)
        {
            _marks.Add(new AdfMark(AdfMarkType.SubSup, new Dictionary<string, object> { ["type"] = type }));
            return this;
        }

        /// <summary>
        /// Adds a background color (highlight) mark with the given hex color. Legal on text; can't be combined
        /// with <see cref="Code"/>.
        /// </summary>
        /// <param name="color">A hex color such as <c>"#fedec8"</c>.</param>
        public AdfMarkSetBuilder BackgroundColor(string color)
        {
            _marks.Add(new AdfMark(AdfMarkType.BackgroundColor, new Dictionary<string, object> { ["color"] = color }));
            return this;
        }

        /// <summary>
        /// Adds a border mark. Legal only on <c>media</c> and <c>mediaInline</c> - pass it through the
        /// <c>marks</c> lambda of <see cref="AdfBlockContentBuilder.MediaSingle"/>, <see cref="AdfMediaGroupBuilder.Media"/>
        /// or <see cref="AdfInlineContentBuilder.MediaInline"/>; on a text run it fails <c>Validate()</c>.
        /// </summary>
        /// <param name="size">The border width, from 1 to 3.</param>
        /// <param name="color">A six- or eight-digit hex color such as <c>"#091e4224"</c>.</param>
        public AdfMarkSetBuilder Border(int size, string color)
        {
            _marks.Add(new AdfMark(AdfMarkType.Border, new Dictionary<string, object> { ["size"] = size, ["color"] = color }));
            return this;
        }

        private AdfMarkSetBuilder Add(AdfMarkType type)
        {
            _marks.Add(new AdfMark(type));
            return this;
        }

        internal List<AdfMark> Build() => _marks;

        /// <summary>
        /// Runs an optional marks lambda, returning <see langword="null"/> when it's absent or adds nothing.
        /// </summary>
        internal static List<AdfMark>? Build(Action<AdfMarkSetBuilder>? marks)
        {
            if (marks == null)
                return null;

            AdfMarkSetBuilder builder = new AdfMarkSetBuilder();
            marks(builder);
            return builder._marks.Count == 0 ? null : builder._marks;
        }
    }
}
