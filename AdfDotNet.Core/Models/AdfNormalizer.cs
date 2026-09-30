// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Enums;
using System.Linq;

namespace AdfDotNet.Models
{
    /// <summary>
    /// Reshapes an ADF tree in place so that its nesting and marks follow <see cref="AdfNodeSchema"/> and
    /// <see cref="AdfMarkSchema"/>. The HTML and Markdown converters run it on their output, since both copy
    /// the input's nesting (<c>&lt;blockquote&gt;&lt;h2&gt;</c>, <c>- &gt; quote</c>, ...) and HTML/Markdown
    /// allow shapes ADF doesn't. It fixes what can be fixed without inventing data; attribute values (e.g. an
    /// unknown <c>panelType</c>) are left alone, so <see cref="AdfDocument.Validate"/> may still report errors.
    /// </summary>
    /// <remarks>
    /// Rules, applied bottom-up:
    /// <list type="bullet">
    /// <item><description>A child that isn't legal in its parent is reshaped: a <c>heading</c> becomes a
    /// <c>paragraph</c> with the same content; a <c>codeBlock</c> becomes a <c>paragraph</c> of
    /// <c>code</c>-marked text, with a <c>hardBreak</c> per line break; anything inside a
    /// <c>bulletList</c>/<c>orderedList</c> is wrapped in a <c>listItem</c>; an <c>expand</c> becomes a
    /// <c>nestedExpand</c> where only that is legal (a table cell, another expand) and vice versa (top level),
    /// and one that fits neither way is unwrapped with its title kept as a bold paragraph; inline nodes are gathered into a
    /// <c>paragraph</c>; any other container (<c>blockquote</c>, <c>panel</c>, <c>table</c>, ...) is replaced by
    /// its own children, each placed the same way; any other leaf (e.g. <c>rule</c>) is dropped.</description></item>
    /// <item><description>A container the schema requires to have children but which ended up empty is
    /// dropped (an empty list, table, row, panel or blockquote), except for a <c>listItem</c>, table cell or
    /// <c>blockTaskItem</c>, which gets an empty <c>paragraph</c> so its list or table keeps its shape.</description></item>
    /// <item><description>A <c>blockTaskItem</c> with more than two paragraphs keeps two: the extra ones are
    /// appended to the second, each after a <c>hardBreak</c>.</description></item>
    /// <item><description>Empty <c>text</c> nodes are dropped.</description></item>
    /// <item><description>Marks: a <c>link</c> without a string <c>href</c> is dropped, as is a mark the node type
    /// can't carry or a repeat of a mark type already on the node. When two marks can't be combined,
    /// <c>code</c> wins over everything, then <c>link</c>; otherwise the earlier mark wins.</description></item>
    /// </list>
    /// </remarks>
    public static class AdfNormalizer
    {
        /// <summary>
        /// Container types that get an empty paragraph instead of being dropped when they end up empty,
        /// because removing them would change the shape of their list or table.
        /// </summary>
        private static readonly AdfNodeType[] FilledWhenEmpty =
        {
            AdfNodeType.ListItem,
            AdfNodeType.TableCell,
            AdfNodeType.TableHeader,
            AdfNodeType.BlockTaskItem,
        };

        /// <summary>
        /// Normalizes <paramref name="node"/>'s marks and whole subtree in place. The node itself is kept even
        /// if it ends up empty; only its descendants are reshaped or dropped.
        /// </summary>
        /// <param name="node">The root of the (sub)tree to normalize, typically an <see cref="AdfDocument"/>.</param>
        /// <returns><paramref name="node"/>, for chaining.</returns>
        public static T Normalize<T>(T node) where T : AdfNode
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            NormalizeNode(node);
            return node;
        }

        private static void NormalizeNode(AdfNode node)
        {
            NormalizeMarks(node);

            if (node.Content == null || !AdfNodeSchema.CanHaveChildren(node.Type))
                return;

            List<AdfNode> placed = new List<AdfNode>();
            AdfNode? pendingInline = null;

            foreach (AdfNode child in node.Content)
                Place(child, node.Type, placed, ref pendingInline);

            FlushInline(node.Type, placed, ref pendingInline);

            if (node.Type == AdfNodeType.BlockTaskItem)
                MergeExtraParagraphs(placed, AdfNodeSchema.GetMaxChildren(node.Type));

            node.Content = placed;
        }

        /// <summary>
        /// Merges the paragraphs past <paramref name="max"/> into the last one allowed, each after a
        /// <c>hardBreak</c>, so a <c>blockTaskItem</c> keeps at most two paragraphs without losing text.
        /// </summary>
        private static void MergeExtraParagraphs(List<AdfNode> paragraphs, int? max)
        {
            if (max == null || max < 1 || paragraphs.Count <= max)
                return;

            AdfNode last = paragraphs[max.Value - 1];
            foreach (AdfNode extra in paragraphs.Skip(max.Value))
            {
                last.AddContent(AdfNode.CreateHardBreak());
                foreach (AdfNode inline in extra.Content ?? new List<AdfNode>())
                    last.AddContent(inline);
            }
            paragraphs.RemoveRange(max.Value, paragraphs.Count - max.Value);
        }

        /// <summary>
        /// Normalizes <paramref name="child"/> and adds it (or whatever it's reshaped into) to
        /// <paramref name="placed"/>, the new content of a node of type <paramref name="parentType"/>.
        /// </summary>
        private static void Place(AdfNode child, AdfNodeType parentType, List<AdfNode> placed, ref AdfNode? pendingInline)
        {
            NormalizeNode(child);

            if (child.Type == AdfNodeType.Text && string.IsNullOrEmpty(child.Text))
                return;

            if (AdfNodeSchema.IsValidChildType(parentType, child.Type))
            {
                FlushInline(parentType, placed, ref pendingInline);
                if (IsEmptyRequiredContainer(child))
                {
                    if (!FilledWhenEmpty.Contains(child.Type))
                        return;
                    child.AddContent(AdfNode.CreateParagraph());
                }
                placed.Add(child);
                return;
            }

            // Inline content loose in a block container: gather consecutive runs into one paragraph, but only
            // where a paragraph can eventually be placed (directly, or wrapped in a listItem).
            if (AdfNodeSchema.IsValidChildType(AdfNodeType.Paragraph, child.Type))
            {
                if (CanHoldParagraph(parentType))
                {
                    pendingInline ??= AdfNode.CreateParagraph();
                    pendingInline.AddContent(child);
                }
                return;
            }

            FlushInline(parentType, placed, ref pendingInline);

            switch (child.Type)
            {
                case AdfNodeType.Heading:
                    Place(AdfNode.CreateParagraph(child.Content), parentType, placed, ref pendingInline);
                    return;

                case AdfNodeType.CodeBlock:
                    Place(CodeBlockToParagraph(child), parentType, placed, ref pendingInline);
                    return;

                case AdfNodeType.ListItem:
                    break;

                case AdfNodeType.Expand:
                case AdfNodeType.NestedExpand:
                    AdfNodeType otherExpand = child.Type == AdfNodeType.Expand ? AdfNodeType.NestedExpand : AdfNodeType.Expand;
                    if (AdfNodeSchema.IsValidChildType(parentType, otherExpand))
                    {
                        Place(new AdfNode(otherExpand) { Attrs = child.Attrs, Content = child.Content, Marks = child.Marks }, parentType, placed, ref pendingInline);
                        return;
                    }
                    if (AdfNodeSchema.IsValidChildType(parentType, AdfNodeType.ListItem))
                    {
                        Place(AdfNode.CreateListItem(new List<AdfNode> { child }), parentType, placed, ref pendingInline);
                        return;
                    }
                    // Unwrapped below; keep the title as a bold paragraph so its text isn't lost.
                    if (child.Attrs != null && child.Attrs.TryGetValue("title", out object? title) && title is string titleText && titleText.Length > 0)
                        Place(AdfNode.CreateParagraph(new List<AdfNode> { AdfNode.CreateText(titleText, new List<AdfMark> { new AdfMark(AdfMarkType.Strong) }) }), parentType, placed, ref pendingInline);
                    break;

                default:
                    if (AdfNodeSchema.IsValidChildType(parentType, AdfNodeType.ListItem))
                    {
                        Place(AdfNode.CreateListItem(new List<AdfNode> { child }), parentType, placed, ref pendingInline);
                        return;
                    }
                    break;
            }

            if (AdfNodeSchema.CanHaveChildren(child.Type) && child.Content != null)
            {
                foreach (AdfNode grandchild in child.Content)
                    Place(grandchild, parentType, placed, ref pendingInline);
            }
        }

        private static void FlushInline(AdfNodeType parentType, List<AdfNode> placed, ref AdfNode? pendingInline)
        {
            if (pendingInline == null)
                return;

            AdfNode paragraph = pendingInline;
            pendingInline = null;
            Place(paragraph, parentType, placed, ref pendingInline);
        }

        private static bool CanHoldParagraph(AdfNodeType parentType) =>
            AdfNodeSchema.IsValidChildType(parentType, AdfNodeType.Paragraph)
            || AdfNodeSchema.IsValidChildType(parentType, AdfNodeType.ListItem);

        private static bool IsEmptyRequiredContainer(AdfNode node) =>
            AdfNodeSchema.CanHaveChildren(node.Type)
            && (node.Content == null || node.Content.Count == 0)
            && AdfNodeSchema.GetMinChildren(node.Type) > 0;

        /// <summary>
        /// Turns a code block that can't stay a code block into a paragraph of <c>code</c>-marked text, one
        /// <c>hardBreak</c> per line break (a trailing line break is dropped).
        /// </summary>
        private static AdfNode CodeBlockToParagraph(AdfNode codeBlock)
        {
            string text = string.Concat((codeBlock.Content ?? new List<AdfNode>()).Select(n => n.Text ?? string.Empty));
            string[] lines = text.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');

            AdfNode paragraph = AdfNode.CreateParagraph();
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                    paragraph.AddContent(AdfNode.CreateHardBreak());
                if (lines[i].Length > 0)
                    paragraph.AddContent(AdfNode.CreateText(lines[i], new List<AdfMark> { new AdfMark(AdfMarkType.Code) }));
            }
            return paragraph;
        }

        private static void NormalizeMarks(AdfNode node)
        {
            if (node.Marks == null)
                return;

            List<AdfMark> candidates = node.Marks
                .Where(m => AdfNodeSchema.IsValidMark(node.Type, m.Type) && (m.Type != AdfMarkType.Link || HasHref(m)))
                .ToList();

            // code first, then link, then the rest in their original order; each is kept only if it combines
            // with every mark kept before it.
            List<AdfMark> kept = new List<AdfMark>();
            foreach (AdfMark mark in candidates.OrderBy(MarkPriority))
            {
                if (kept.All(k => k.Type != mark.Type && AdfMarkSchema.CanCombine(k.Type, mark.Type)))
                    kept.Add(mark);
            }

            List<AdfMark> result = candidates.Where(kept.Contains).ToList();
            node.Marks = result.Count > 0 ? result : null;
        }

        // OrderBy is stable, so marks of equal priority keep their original order.
        private static int MarkPriority(AdfMark mark) => mark.Type switch
        {
            AdfMarkType.Code => 0,
            AdfMarkType.Link => 1,
            _ => 2,
        };

        private static bool HasHref(AdfMark link) =>
            link.Attrs != null && link.Attrs.TryGetValue("href", out object? href) && href is string;
    }
}
