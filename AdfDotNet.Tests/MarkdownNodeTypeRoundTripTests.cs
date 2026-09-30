// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Models;
using Xunit;

namespace AdfDotNet.Tests;

/// <summary>
/// One round-trip case per ADF node type supported by the Markdown converter. Mirrors
/// <see cref="NodeTypeRoundTripTests"/> (the HTML equivalent). <c>inlineCard</c> and <c>emoji</c> render to
/// Markdown but aren't parsed back from it (no Markdown source construct maps to either), so they're not
/// full-round-trip cases here; <c>mediaSingle</c>/<c>media</c> round-trip via JSON only - see the
/// <c>MediaSingle_RoundTrips</c> comment.
/// </summary>
public class MarkdownNodeTypeRoundTripTests
{
    [Fact]
    public void Paragraph_RoundTrips()
    {
        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>()
        {
            AdfNode.CreateParagraph(new List<AdfNode>()
            {
                AdfNode.CreateText("A simple paragraph.")
            })
        });

        MarkdownRoundTripAssert.FullRoundTrip(document);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void Heading_RoundTrips(int level)
    {
        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>()
        {
            AdfNode.CreateHeading(level, $"Heading level {level}")
        });

        MarkdownRoundTripAssert.FullRoundTrip(document);
    }

    [Fact]
    public void BulletList_RoundTrips()
    {
        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>()
        {
            AdfNode.CreateBulletList(new List<AdfNode>()
            {
                AdfNode.CreateListItem(new List<AdfNode>()
                {
                    AdfNode.CreateParagraph(new List<AdfNode>() { AdfNode.CreateText("First") })
                }),
                AdfNode.CreateListItem(new List<AdfNode>()
                {
                    AdfNode.CreateParagraph(new List<AdfNode>() { AdfNode.CreateText("Second") })
                })
            })
        });

        MarkdownRoundTripAssert.FullRoundTrip(document);
    }

    [Fact]
    public void OrderedList_RoundTrips()
    {
        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>()
        {
            AdfNode.CreateOrderedList(new List<AdfNode>()
            {
                AdfNode.CreateListItem(new List<AdfNode>()
                {
                    AdfNode.CreateParagraph(new List<AdfNode>() { AdfNode.CreateText("First") })
                }),
                AdfNode.CreateListItem(new List<AdfNode>()
                {
                    AdfNode.CreateParagraph(new List<AdfNode>() { AdfNode.CreateText("Second") })
                })
            })
        });

        MarkdownRoundTripAssert.FullRoundTrip(document);
    }

    [Fact]
    public void Blockquote_RoundTrips()
    {
        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>()
        {
            AdfNode.CreateBlockquote(new List<AdfNode>()
            {
                AdfNode.CreateParagraph(new List<AdfNode>() { AdfNode.CreateText("Quoted text.") })
            })
        });

        MarkdownRoundTripAssert.FullRoundTrip(document);
    }

    [Fact]
    public void CodeBlock_WithoutLanguage_RoundTrips()
    {
        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>()
        {
            AdfNode.CreateCodeBlock(content: new List<AdfNode>()
            {
                AdfNode.CreateText("var x = 1;")
            })
        });

        MarkdownRoundTripAssert.FullRoundTrip(document);
    }

    [Fact]
    public void CodeBlock_WithLanguage_RoundTrips()
    {
        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>()
        {
            AdfNode.CreateCodeBlock("csharp", new List<AdfNode>()
            {
                AdfNode.CreateText("var x = 1;")
            })
        });

        MarkdownRoundTripAssert.FullRoundTrip(document);
    }

    [Fact]
    public void Rule_RoundTrips()
    {
        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>()
        {
            AdfNode.CreateParagraph(new List<AdfNode>() { AdfNode.CreateText("Before") }),
            AdfNode.CreateRule(),
            AdfNode.CreateParagraph(new List<AdfNode>() { AdfNode.CreateText("After") })
        });

        MarkdownRoundTripAssert.FullRoundTrip(document);
    }

    [Fact]
    public void HardBreak_RoundTrips()
    {
        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>()
        {
            AdfNode.CreateParagraph(new List<AdfNode>()
            {
                AdfNode.CreateText("Line one"),
                AdfNode.CreateHardBreak(),
                AdfNode.CreateText("Line two")
            })
        });

        MarkdownRoundTripAssert.FullRoundTrip(document);
    }

    [Fact]
    public void Table_RoundTrips()
    {
        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>()
        {
            AdfNode.CreateTable(new List<AdfNode>()
            {
                AdfNode.CreateTableRow(new List<AdfNode>()
                {
                    AdfNode.CreateTableHeader(new List<AdfNode>()
                    {
                        AdfNode.CreateParagraph(new List<AdfNode>() { AdfNode.CreateText("Header") })
                    })
                }),
                AdfNode.CreateTableRow(new List<AdfNode>()
                {
                    AdfNode.CreateTableCell(new List<AdfNode>()
                    {
                        AdfNode.CreateParagraph(new List<AdfNode>() { AdfNode.CreateText("Cell") })
                    })
                })
            })
        });

        MarkdownRoundTripAssert.FullRoundTrip(document);
    }

    [Fact]
    public void MediaSingle_RoundTrips()
    {
        // mediaSingle/media carry Jira Media-Services identifiers (id/collection), not a fetchable URL, so
        // the Markdown converter can't render or parse them - see docs/markdown-converter.md. Only the ADF
        // document model + JSON serialization are expected to round-trip this node type.
        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>()
        {
            AdfNode.CreateMediaSingle(AdfNode.CreateMedia("abc-123", "file", "content-collection"), "center")
        });

        RoundTripAssert.JsonRoundTrip(document);
    }

    [Fact]
    public void ListItem_WithMultipleBlocks_RoundTrips()
    {
        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>()
        {
            AdfNode.CreateBulletList(new List<AdfNode>()
            {
                AdfNode.CreateListItem(new List<AdfNode>()
                {
                    AdfNode.CreateParagraph(new List<AdfNode>() { AdfNode.CreateText("First paragraph.") }),
                    AdfNode.CreateParagraph(new List<AdfNode>() { AdfNode.CreateText("Second paragraph.") })
                })
            })
        });

        MarkdownRoundTripAssert.FullRoundTrip(document);
    }

    [Fact]
    public void TextWithMarkdownSpecialCharacters_RoundTrips()
    {
        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>()
        {
            AdfNode.CreateParagraph(new List<AdfNode>()
            {
                AdfNode.CreateText("1. Not a list? [maybe] (a link?) *not em*_not em_ #not-a-heading | pipe ~not strike~")
            })
        });

        MarkdownRoundTripAssert.FullRoundTrip(document);
    }
}
