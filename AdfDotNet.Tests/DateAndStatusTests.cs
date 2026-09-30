// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Builders;
using AdfDotNet.DataConverters;
using AdfDotNet.Enums;
using AdfDotNet.FormatConverters;
using AdfDotNet.Models;
using Xunit;

namespace AdfDotNet.Tests;

/// <summary>
/// Covers the <c>date</c> and <c>status</c> inline node types end to end: document model/builder,
/// <c>Validate()</c>, JSON, HTML (<c>&lt;time data-timestamp&gt;</c>/<c>&lt;span data-status-color&gt;</c>, both
/// round-tripping) and Markdown (plain text, one-way).
/// </summary>
public class DateAndStatusTests
{
    [Fact]
    public void Date_HtmlRoundTrips()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .Paragraph(p => p.Text("Due ").Date("1582152559").Text(".")));

        RoundTripAssert.FullRoundTrip(document);
    }

    [Theory]
    [InlineData("1582152559", "2020-02-19")]
    [InlineData("1582152559000", "2020-02-19")]
    [InlineData("not-a-number", "not-a-number")]
    public void Date_RendersAsIsoDate(string timestamp, string expected)
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc.Paragraph(p => p.Date(timestamp)));

        Assert.Equal($"<p><time data-timestamp=\"{timestamp}\">{expected}</time></p>", AdfToHtmlConverter.Convert(document));
        Assert.Equal(expected.Replace("-", "\\-"), AdfToMarkdownConverter.Convert(document));
    }

    [Fact]
    public void Status_HtmlRoundTrips()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .Heading(2, h => h.Text("Release ").Status("In Progress", "yellow", "abcdef12-abcd-abcd-abcd-abcdef123456"))
            .Paragraph(p => p.Status("Done & dusted", "#123ABC")));

        RoundTripAssert.FullRoundTrip(document);
    }

    [Fact]
    public void Status_RendersAsHtmlSpan()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc.Paragraph(p => p.Status("Blocked", "red")));

        Assert.Equal("<p><span data-status-color=\"red\">Blocked</span></p>", AdfToHtmlConverter.Convert(document));
    }

    [Fact]
    public void Status_RendersAsPlainTextInMarkdown()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .Paragraph(p => p.Text("State: ").Status("In Progress", "yellow")));

        Assert.Equal("State: In Progress", AdfToMarkdownConverter.Convert(document));
    }

    [Fact]
    public void DateAndStatus_DeserializeFromSpecJson()
    {
        string json = """
        {
          "version": 1,
          "type": "doc",
          "content": [
            {
              "type": "paragraph",
              "content": [
                { "type": "date", "attrs": { "timestamp": "1582152559" } },
                { "type": "status", "attrs": { "localId": "abcdef12-abcd-abcd-abcd-abcdef123456", "text": "In Progress", "color": "yellow" } }
              ]
            }
          ]
        }
        """;

        AdfDocument document = AdfJsonConverter.Deserialize(json);

        List<AdfNode> inline = document.Content!.Single().Content!;
        Assert.Equal(AdfNodeType.Date, inline[0].Type);
        Assert.Equal("1582152559", inline[0].Attrs!["timestamp"]);
        Assert.Equal(AdfNodeType.Status, inline[1].Type);
        Assert.Equal("In Progress", inline[1].Attrs!["text"]);
        Assert.Equal("yellow", inline[1].Attrs!["color"]);

        Assert.True(document.Validate().IsValid);
        RoundTripAssert.JsonRoundTrip(document);
    }

    [Fact]
    public void DateAndStatus_AtDocumentLevel_AreInvalid()
    {
        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>
        {
            AdfNode.CreateDate("1582152559"),
            AdfNode.CreateStatus("Done", "green"),
        });

        Assert.Equal(2, document.Validate().Errors.Count);
    }

    [Fact]
    public void Status_OmitsUnsetLocalId()
    {
        AdfNode status = AdfNode.CreateStatus("Done", "green");

        Assert.Equal(new Dictionary<string, object> { ["text"] = "Done", ["color"] = "green" }, status.Attrs);
    }
}
