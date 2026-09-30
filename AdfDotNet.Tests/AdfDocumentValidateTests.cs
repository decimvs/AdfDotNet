// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Enums;
using AdfDotNet.Models;
using Xunit;

namespace AdfDotNet.Tests;

/// <summary>
/// Covers <see cref="AdfDocument.Validate"/>, the schema-driven structural validator.
/// It is opt-in only - no converter or serializer invokes it automatically - so these tests call it directly.
/// </summary>
public class AdfDocumentValidateTests
{
    [Fact]
    public void WellFormedDocument_IsValid()
    {
        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>()
        {
            AdfNode.CreateHeading(1, "Title"),
            AdfNode.CreateParagraph(new List<AdfNode>()
            {
                AdfNode.CreateText("Hello "),
                AdfNode.CreateText("world", new List<AdfMark>() { new AdfMark(AdfMarkType.Strong) }),
                AdfNode.CreateHardBreak()
            }),
            AdfNode.CreateBulletList(new List<AdfNode>()
            {
                AdfNode.CreateListItem(new List<AdfNode>()
                {
                    AdfNode.CreateParagraph(new List<AdfNode>() { AdfNode.CreateText("Item") })
                })
            }),
            AdfNode.CreateRule()
        });

        AdfValidationResult result = document.Validate();

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void EmptyDocument_IsValid()
    {
        AdfDocument document = AdfNode.CreateDocument();

        AdfValidationResult result = document.Validate();

        Assert.True(result.IsValid);
    }

    [Fact]
    public void RootWithWrongType_IsInvalid()
    {
        AdfDocument document = AdfNode.CreateDocument();
        document.Type = AdfNodeType.Paragraph;

        AdfValidationResult result = document.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("must have type 'doc'"));
    }

    [Fact]
    public void IllegalDirectChild_IsInvalid()
    {
        // "text" is not a legal direct child of "doc" - it must be wrapped in a paragraph/heading/etc.
        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>()
        {
            AdfNode.CreateText("Loose text")
        });

        AdfValidationResult result = document.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not a legal child of 'doc'"));
    }

    [Fact]
    public void IllegalNestedChild_IsDetectedRecursively()
    {
        // "bulletList" is not a legal child of "paragraph".
        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>()
        {
            AdfNode.CreateParagraph(new List<AdfNode>()
            {
                AdfNode.CreateBulletList()
            })
        });

        AdfValidationResult result = document.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not a legal child of 'paragraph'"));
    }

    [Fact]
    public void MediaSingle_WithMediaChild_IsValid()
    {
        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>()
        {
            AdfNode.CreateMediaSingle(AdfNode.CreateMedia("abc-123", "file", "content-collection"))
        });

        AdfValidationResult result = document.Validate();

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void InlineCardAndEmoji_InParagraph_AreValid()
    {
        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>()
        {
            AdfNode.CreateParagraph(new List<AdfNode>()
            {
                AdfNode.CreateText("See "),
                AdfNode.CreateInlineCard("https://example.com"),
                AdfNode.CreateEmoji(":grinning:")
            })
        });

        AdfValidationResult result = document.Validate();

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    // The tests below pin legal-child rules taken from the individual node pages of the ADF spec.

    private static AdfNode Para(string text) => AdfNode.CreateParagraph(new List<AdfNode>() { AdfNode.CreateText(text) });

    private static AdfNode SampleMediaSingle() => AdfNode.CreateMediaSingle(AdfNode.CreateMedia("abc-123", "file", "content-collection"));

    [Fact]
    public void ListItem_WithMediaSingle_IsValid()
    {
        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>()
        {
            AdfNode.CreateBulletList(new List<AdfNode>()
            {
                AdfNode.CreateListItem(new List<AdfNode>() { Para("Item"), SampleMediaSingle() })
            })
        });

        Assert.True(document.Validate().IsValid);
    }

    [Fact]
    public void ListItem_WithBlockquote_IsInvalid()
    {
        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>()
        {
            AdfNode.CreateBulletList(new List<AdfNode>()
            {
                AdfNode.CreateListItem(new List<AdfNode>() { AdfNode.CreateBlockquote(new List<AdfNode>() { Para("Quote") }) })
            })
        });

        AdfValidationResult result = document.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("'blockquote' is not a legal child of 'listItem'"));
    }

    [Fact]
    public void Blockquote_WithListsCodeBlockAndMediaSingle_IsValid()
    {
        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>()
        {
            AdfNode.CreateBlockquote(new List<AdfNode>()
            {
                Para("Quote"),
                AdfNode.CreateBulletList(new List<AdfNode>() { AdfNode.CreateListItem(new List<AdfNode>() { Para("a") }) }),
                AdfNode.CreateOrderedList(new List<AdfNode>() { AdfNode.CreateListItem(new List<AdfNode>() { Para("b") }) }),
                AdfNode.CreateCodeBlock("csharp", new List<AdfNode>() { AdfNode.CreateText("var x = 1;") }),
                SampleMediaSingle()
            })
        });

        Assert.True(document.Validate().IsValid);
    }

    [Theory]
    [InlineData("heading")]
    [InlineData("blockquote")]
    public void Blockquote_WithHeadingOrNestedBlockquote_IsInvalid(string childType)
    {
        AdfNode child = childType == "heading"
            ? AdfNode.CreateHeading(2, "Heading")
            : AdfNode.CreateBlockquote(new List<AdfNode>() { Para("Nested") });

        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>()
        {
            AdfNode.CreateBlockquote(new List<AdfNode>() { child })
        });

        AdfValidationResult result = document.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains($"'{childType}' is not a legal child of 'blockquote'"));
    }

    [Fact]
    public void TableCellAndHeader_WithRule_AreValid()
    {
        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>()
        {
            AdfNode.CreateTable(new List<AdfNode>()
            {
                AdfNode.CreateTableRow(new List<AdfNode>()
                {
                    AdfNode.CreateTableHeader(new List<AdfNode>() { Para("Header"), AdfNode.CreateRule() }),
                    AdfNode.CreateTableCell(new List<AdfNode>() { Para("Cell"), AdfNode.CreateRule() })
                })
            })
        });

        Assert.True(document.Validate().IsValid);
    }

    [Fact]
    public void LeafNodeWithContent_IsInvalid()
    {
        AdfNode textWithStrayContent = AdfNode.CreateText("Should not have children");
        textWithStrayContent.AddContent(AdfNode.CreateText("Nested"));

        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>()
        {
            AdfNode.CreateParagraph(new List<AdfNode>() { textWithStrayContent })
        });

        AdfValidationResult result = document.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("cannot have child content"));
    }
}
