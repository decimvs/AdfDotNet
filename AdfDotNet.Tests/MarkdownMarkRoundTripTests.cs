// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Enums;
using AdfDotNet.Models;
using Xunit;

namespace AdfDotNet.Tests;

/// <summary>
/// One round-trip case per ADF mark type supported by the Markdown converter. Mirrors
/// <see cref="MarkRoundTripTests"/> (the HTML equivalent); Underline, SubSup and TextColor are excluded -
/// Markdown has no native syntax for them, and the converter deliberately doesn't render them as raw inline
/// HTML.
/// </summary>
public class MarkdownMarkRoundTripTests
{
    [Fact]
    public void Strong_RoundTrips()
    {
        MarkdownRoundTripAssert.FullRoundTrip(SingleMarkDocument(AdfMarkType.Strong));
    }

    [Fact]
    public void Em_RoundTrips()
    {
        MarkdownRoundTripAssert.FullRoundTrip(SingleMarkDocument(AdfMarkType.Em));
    }

    [Fact]
    public void Strike_RoundTrips()
    {
        MarkdownRoundTripAssert.FullRoundTrip(SingleMarkDocument(AdfMarkType.Strike));
    }

    [Fact]
    public void Code_RoundTrips()
    {
        MarkdownRoundTripAssert.FullRoundTrip(SingleMarkDocument(AdfMarkType.Code));
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

        MarkdownRoundTripAssert.FullRoundTrip(document);
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
