// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Enums;
using AdfDotNet.Models;
using Xunit;

namespace AdfDotNet.Tests;

/// <summary>
/// One round-trip case per ADF mark type that participates in HTML&lt;-&gt;ADF conversion
/// (i.e. every mark currently registered in <see cref="AdfStructure"/>'s node map or CSS mark extraction).
/// </summary>
public class MarkRoundTripTests
{
    [Fact]
    public void Strong_RoundTrips()
    {
        RoundTripAssert.FullRoundTrip(SingleMarkDocument(AdfMarkType.Strong));
    }

    [Fact]
    public void Em_RoundTrips()
    {
        RoundTripAssert.FullRoundTrip(SingleMarkDocument(AdfMarkType.Em));
    }

    [Fact]
    public void Underline_RoundTrips()
    {
        RoundTripAssert.FullRoundTrip(SingleMarkDocument(AdfMarkType.Underline));
    }

    [Fact]
    public void Strike_RoundTrips()
    {
        RoundTripAssert.FullRoundTrip(SingleMarkDocument(AdfMarkType.Strike));
    }

    [Fact]
    public void Code_RoundTrips()
    {
        RoundTripAssert.FullRoundTrip(SingleMarkDocument(AdfMarkType.Code));
    }

    [Fact]
    public void Link_RoundTrips()
    {
        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>()
        {
            AdfNode.CreateParagraph(new List<AdfNode>()
            {
                AdfNode.CreateText("marked text", new List<AdfMark>()
                {
                    new AdfMark(AdfMarkType.Link, new Dictionary<string, object>()
                    {
                        { "href", "https://example.com/" }
                    })
                })
            })
        });

        RoundTripAssert.FullRoundTrip(document);
    }

    [Theory]
    [InlineData("sub")]
    [InlineData("sup")]
    public void SubSup_RoundTrips(string subSupType)
    {
        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>()
        {
            AdfNode.CreateParagraph(new List<AdfNode>()
            {
                AdfNode.CreateText("marked text", new List<AdfMark>()
                {
                    new AdfMark(AdfMarkType.SubSup, new Dictionary<string, object>()
                    {
                        { "type", subSupType }
                    })
                })
            })
        });

        RoundTripAssert.FullRoundTrip(document);
    }

    [Fact]
    public void TextColor_RoundTrips()
    {
        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>()
        {
            AdfNode.CreateParagraph(new List<AdfNode>()
            {
                AdfNode.CreateText("marked text", new List<AdfMark>()
                {
                    new AdfMark(AdfMarkType.TextColor, new Dictionary<string, object>()
                    {
                        { "color", "#ff0000" }
                    })
                })
            })
        });

        RoundTripAssert.FullRoundTrip(document);
    }

    private static AdfDocument SingleMarkDocument(AdfMarkType markType)
    {
        return AdfNode.CreateDocument(new List<AdfNode>()
        {
            AdfNode.CreateParagraph(new List<AdfNode>()
            {
                AdfNode.CreateText("marked text", new List<AdfMark>()
                {
                    new AdfMark(markType)
                })
            })
        });
    }
}
