// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Enums;
using AdfDotNet.Models;
using Xunit;

namespace AdfDotNet.Tests;

/// <summary>
/// Ported from the original hand-rolled AdfDotNet.Tests.ValidationTest.TestAdfHtmlRoundTrip smoke test.
/// </summary>
public class AdfHtmlRoundTripTests
{
    [Fact]
    public void ComplexDocument_RoundTripsThroughHtml()
    {
        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>()
        {
            AdfNode.CreateHeading(2, new List<AdfNode>()
            {
                AdfNode.CreateText("Roundtrip")
            }),
            AdfNode.CreateParagraph(new List<AdfNode>()
            {
                AdfNode.CreateText("This is "),
                AdfNode.CreateText("bold", new List<AdfMark>()
                {
                    new AdfMark(AdfMarkType.Strong)
                }),
                AdfNode.CreateText(" and "),
                AdfNode.CreateText("italic", new List<AdfMark>()
                {
                    new AdfMark(AdfMarkType.Em)
                }),
                AdfNode.CreateText(" text.")
            }),
            AdfNode.CreateBulletList(new List<AdfNode>()
            {
                AdfNode.CreateListItem(new List<AdfNode>()
                {
                    AdfNode.CreateParagraph(new List<AdfNode>()
                    {
                        AdfNode.CreateText("Item 1")
                    })
                }),
                AdfNode.CreateListItem(new List<AdfNode>()
                {
                    AdfNode.CreateParagraph(new List<AdfNode>()
                    {
                        AdfNode.CreateText("Item 2")
                    })
                })
            })
        });

        RoundTripAssert.FullRoundTrip(document);
    }
}
