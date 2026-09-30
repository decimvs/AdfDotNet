// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Models;

namespace AdfDotNet.Builders
{
    /// <summary>
    /// Builds the items of a <c>taskList</c> node, via nested-lambda composition.
    /// </summary>
    public sealed class AdfTaskListBuilder
    {
        private readonly List<AdfNode> _content;

        internal AdfTaskListBuilder(List<AdfNode> content) => _content = content;

        /// <summary>
        /// Adds a task item (a checkbox) with inline content built via a nested <see cref="AdfInlineContentBuilder"/> lambda.
        /// </summary>
        /// <param name="content">Builds the item's inline content.</param>
        /// <param name="done">Whether the checkbox is ticked (<c>"DONE"</c>) rather than open (<c>"TODO"</c>).</param>
        /// <param name="localId">A unique identifier for the item. A new GUID when omitted.</param>
        public AdfTaskListBuilder Item(Action<AdfInlineContentBuilder> content, bool done = false, string? localId = null)
        {
            AdfNode node = AdfNode.CreateTaskItem(ToState(done), localId: localId);
            content(new AdfInlineContentBuilder(node.Content!));
            _content.Add(node);
            return this;
        }

        /// <summary>
        /// Adds a block task item (a checkbox whose content is one or two paragraphs) built via a nested
        /// <see cref="AdfBlockContentBuilder"/> lambda.
        /// </summary>
        /// <param name="content">Builds the item's paragraphs.</param>
        /// <param name="done">Whether the checkbox is ticked (<c>"DONE"</c>) rather than open (<c>"TODO"</c>).</param>
        /// <param name="localId">A unique identifier for the item. A new GUID when omitted.</param>
        public AdfTaskListBuilder BlockItem(Action<AdfBlockContentBuilder> content, bool done = false, string? localId = null)
        {
            AdfNode node = AdfNode.CreateBlockTaskItem(ToState(done), localId: localId);
            content(new AdfBlockContentBuilder(node.Content!));
            _content.Add(node);
            return this;
        }

        /// <summary>
        /// Adds a nested task list (an indented level of checkboxes).
        /// </summary>
        /// <param name="content">Builds the nested list's task items.</param>
        /// <param name="localId">A unique identifier for the nested list. A new GUID when omitted.</param>
        public AdfTaskListBuilder List(Action<AdfTaskListBuilder> content, string? localId = null)
        {
            AdfNode node = AdfNode.CreateTaskList(localId: localId);
            content(new AdfTaskListBuilder(node.Content!));
            _content.Add(node);
            return this;
        }

        private static string ToState(bool done) => done ? "DONE" : "TODO";
    }
}
