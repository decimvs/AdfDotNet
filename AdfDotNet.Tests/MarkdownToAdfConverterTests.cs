// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Builders;
using AdfDotNet.DataConverters;
using AdfDotNet.FormatConverters;
using AdfDotNet.Models;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AdfDotNet.Tests;

/// <summary>
/// Markdown -> ADF cases for inline constructs that the round-trip suites only reach one way: autolinks
/// (how <c>inlineCard</c> renders) and the raw <c>&lt;br&gt;</c> that joins the blocks of a pipe-table cell.
/// </summary>
public class MarkdownToAdfConverterTests
{
    [Fact]
    public void UrlAutolink_BecomesLinkedText()
    {
        AssertConverts(
            "see <https://example.com/card> now",
            doc => doc.Paragraph(p => p
                .Text("see ")
                .Text("https://example.com/card", m => m.Link("https://example.com/card"))
                .Text(" now")));
    }

    [Fact]
    public void EmailAutolink_BecomesMailtoLink()
    {
        AssertConverts(
            "<ada@example.com>",
            doc => doc.Paragraph(p => p.Text("ada@example.com", m => m.Link("mailto:ada@example.com"))));
    }

    [Fact]
    public void AutolinkInsideEmphasis_KeepsTheEmphasis()
    {
        AssertConverts(
            "**<https://example.com>**",
            doc => doc.Paragraph(p => p.Text("https://example.com", m => m.Strong().Link("https://example.com"))));
    }

    [Fact]
    public void InlineCard_ComesBackAsLinkedText()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc.Paragraph(p => p.InlineCard("https://example.com/card")));

        AdfDocument parsed = MarkdownToAdfConverter.Convert(AdfToMarkdownConverter.Convert(document));

        AssertSameJson(AdfDocumentBuilder.Build(doc => doc
            .Paragraph(p => p.Text("https://example.com/card", m => m.Link("https://example.com/card")))), parsed);
    }

    [Theory]
    [InlineData("<br>")]
    [InlineData("<br/>")]
    [InlineData("<BR />")]
    public void InlineBrTag_BecomesHardBreak(string br)
    {
        AssertConverts(
            $"| h |\n| --- |\n| one{br}two |",
            doc => doc.Table(t => t
                .Row(r => r.Header(c => c.Paragraph(p => p.Text("h"))))
                .Row(r => r.Cell(c => c.Paragraph(p => p.Text("one").HardBreak().Text("two"))))));
    }

    [Fact]
    public void OtherInlineHtml_IsStillDropped()
    {
        AssertConverts("a <u>b</u> c", doc => doc.Paragraph(p => p.Text("a b c")));
    }

    private static void AssertConverts(string markdown, Action<AdfBlockContentBuilder> expected)
    {
        AdfDocument actual = MarkdownToAdfConverter.Convert(markdown);

        AssertSameJson(AdfDocumentBuilder.Build(expected), actual);

        AdfValidationResult validation = actual.Validate();
        Assert.True(validation.IsValid, string.Join("\n", validation.Errors));
    }

    private static void AssertSameJson(AdfDocument expected, AdfDocument actual)
    {
        JToken expectedJson = JToken.Parse(AdfJsonConverter.Serialize(expected));
        JToken actualJson = JToken.Parse(AdfJsonConverter.Serialize(actual));
        Assert.True(JToken.DeepEquals(expectedJson, actualJson), $"Mismatch.\nExpected: {expectedJson}\nActual:   {actualJson}");
    }
}
