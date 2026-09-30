// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace AdfDotNet.Enums
{
    /// <summary>
    /// Represents the different types of nodes in an ADF (Atlassian Document Format) document.
    /// </summary>
    public enum AdfNodeType
    {
        /// <summary>
        /// Represents the root document node in an ADF document.
        /// </summary>
        Doc,

        /// <summary>
        /// Represents a paragraph node in an ADF document.
        /// </summary>
        Paragraph,

        /// <summary>
        /// Represents a text node in an ADF document.
        /// </summary>
        Text,

        /// <summary>
        /// Represents a heading node in an ADF document.
        /// </summary>
        Heading,

        /// <summary>
        /// Represents a bullet list node in an ADF document.
        /// </summary>
        BulletList,

        /// <summary>
        /// Represents an ordered list node in an ADF document.
        /// </summary>
        OrderedList,

        /// <summary>
        /// Represents a list item node in an ADF document.
        /// </summary>
        ListItem,

        /// <summary>
        /// Represents a blockquote node in an ADF document.
        /// </summary>
        Blockquote,

        /// <summary>
        /// Represents a code block node in an ADF document.
        /// </summary>
        CodeBlock,

        /// <summary>
        /// Represents a horizontal rule node in an ADF document.
        /// </summary>
        Rule,

        /// <summary>
        /// Represents a hard break node in an ADF document.
        /// </summary>
        HardBreak,

        /// <summary>
        /// Represents an inline card node in an ADF document.
        /// </summary>
        InlineCard,

        /// <summary>
        /// Represents a table node in an ADF document.
        /// </summary>
        Table,

        /// <summary>
        /// Represents a table row node in an ADF document.
        /// </summary>
        TableRow,

        /// <summary>
        /// Represents a table cell node in an ADF document.
        /// </summary>
        TableCell,

        /// <summary>
        /// Represents a table header node in an ADF document.
        /// </summary>
        TableHeader,

        /// <summary>
        /// Represents a media single node in an ADF document.
        /// </summary>
        MediaSingle,

        /// <summary>
        /// Represents a media node in an ADF document.
        /// </summary>
        Media,

        /// <summary>
        /// Represents an emoji node in an ADF document.
        /// </summary>
        Emoji,

        /// <summary>
        /// Represents a panel node in an ADF document - a container that highlights its content.
        /// </summary>
        Panel,

        /// <summary>
        /// Represents a user mention node in an ADF document.
        /// </summary>
        Mention,

        /// <summary>
        /// Represents a date node in an ADF document, displayed in the reader's locale.
        /// </summary>
        Date,

        /// <summary>
        /// Represents a status lozenge node in an ADF document (e.g. "In Progress").
        /// </summary>
        Status,

        /// <summary>
        /// Represents an expand node in an ADF document - a top-level container whose content can be hidden or
        /// shown, like an accordion. Use <see cref="NestedExpand"/> inside a table cell or header.
        /// </summary>
        Expand,

        /// <summary>
        /// Represents a nested expand node in an ADF document - the variant of <see cref="Expand"/> that is only
        /// legal inside a table cell or table header.
        /// </summary>
        NestedExpand,

        /// <summary>
        /// Represents a media group node in an ADF document - a container for several media items.
        /// </summary>
        MediaGroup,

        /// <summary>
        /// Represents an inline media node in an ADF document (a Media Services file shown inline with text).
        /// </summary>
        MediaInline,

        /// <summary>
        /// Represents a sync block node in an ADF document - a reference to content synced from elsewhere,
        /// identified by <c>resourceId</c>. It has no content of its own.
        /// </summary>
        SyncBlock,

        /// <summary>
        /// Represents a bodied sync block node in an ADF document - the source of synced content, carrying
        /// that content as its children.
        /// </summary>
        BodiedSyncBlock,

        /// <summary>
        /// Represents a multi-bodied extension node in an ADF document - an app extension with several
        /// <see cref="ExtensionFrame"/> bodies.
        /// </summary>
        MultiBodiedExtension,

        /// <summary>
        /// Represents an extension frame node in an ADF document - one body of a <see cref="MultiBodiedExtension"/>.
        /// </summary>
        ExtensionFrame,

        /// <summary>
        /// Represents a task list node in an ADF document (a checkbox list). Not on the ADF spec page, which
        /// lists <see cref="BlockTaskItem"/> without a parent; modeled from the ADF JSON schema.
        /// </summary>
        TaskList,

        /// <summary>
        /// Represents a task item node in an ADF document - one checkbox of a <see cref="TaskList"/>, with inline
        /// content. Not on the ADF spec page; modeled from the ADF JSON schema.
        /// </summary>
        TaskItem,

        /// <summary>
        /// Represents a block task item node in an ADF document - a checkbox of a <see cref="TaskList"/> whose
        /// content is paragraphs rather than inline nodes.
        /// </summary>
        BlockTaskItem,
    }
}
