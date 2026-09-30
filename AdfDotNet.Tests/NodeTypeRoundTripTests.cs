// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Models;
using Xunit;

namespace AdfDotNet.Tests;

/// <summary>
/// One round-trip case per ADF node type that participates in HTML&lt;-&gt;ADF conversion
/// (i.e. every type currently registered in <see cref="AdfStructure"/>'s node map / legal-child table).
/// </summary>
public class NodeTypeRoundTripTests
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

        RoundTripAssert.FullRoundTrip(document);
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

        RoundTripAssert.FullRoundTrip(document);
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

        RoundTripAssert.FullRoundTrip(document);
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

        RoundTripAssert.FullRoundTrip(document);
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

        RoundTripAssert.FullRoundTrip(document);
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

        RoundTripAssert.FullRoundTrip(document);
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

        RoundTripAssert.FullRoundTrip(document);
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

        RoundTripAssert.FullRoundTrip(document);
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

        RoundTripAssert.FullRoundTrip(document);
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

        RoundTripAssert.FullRoundTrip(document);
    }

    [Fact]
    public void MediaSingle_RoundTrips()
    {
        // mediaSingle/media carry Jira Media-Services identifiers (id/collection), not a fetchable URL, so
        // the HTML converter can't render or parse them - see docs/html-converter.md. Only the ADF document
        // model + JSON serialization are expected to round-trip this node type.
        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>()
        {
            AdfNode.CreateMediaSingle(AdfNode.CreateMedia("abc-123", "file", "content-collection"), "center")
        });

        RoundTripAssert.JsonRoundTrip(document);
    }
}
