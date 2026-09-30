// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Builders;
using AdfDotNet.Enums;
using AdfDotNet.FormatConverters;
using AdfDotNet.Models;
using Xunit;

namespace AdfDotNet.Tests;

/// <summary>
/// Covers HTML and Markdown conversion of <c>expand</c>/<c>nestedExpand</c>: both render as
/// <c>&lt;details&gt;</c> with the title in a <c>&lt;summary&gt;</c>, and the normalizer picks the right type
/// for a <c>&lt;details&gt;</c> from its placement.
/// </summary>
public class ExpandConversionTests
{
    private static AdfDocument ExpandDocument(string? title) => AdfDocumentBuilder.Build(doc => doc
        .Expand(title, e => e
            .Paragraph(p => p.Text("Hello ").Text("world", m => m.Strong()))
            .BulletList(l => l.Item(i => i.Paragraph(p => p.Text("item"))))
            .CodeBlock("var x = 1;", "csharp")));

    private static AdfDocument NestedExpandDocument() => AdfDocumentBuilder.Build(doc => doc
        .Table(t => t.Row(r => r.Cell(c => c
            .NestedExpand("More", n => n.Paragraph(p => p.Text("hidden")).Heading(3, h => h.Text("Sub")))))));

    [Theory]
    [InlineData("Details")]
    [InlineData("A <b>&amp; \"quoted\"</b> title")]
    [InlineData("")]
    [InlineData(null)]
    public void Expand_HtmlRoundTrips(string? title)
    {
        RoundTripAssert.FullRoundTrip(ExpandDocument(title));
    }

    [Fact]
    public void Expand_RendersAsDetails()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc.Expand("T & C", e => e.Paragraph(p => p.Text("x"))));

        Assert.Equal("<details data-adf-type=\"expand\"><summary>T &amp; C</summary><p>x</p></details>", AdfToHtmlConverter.Convert(document));
    }

    [Fact]
    public void NestedExpand_HtmlRoundTrips()
    {
        AdfDocument document = NestedExpandDocument();
        Assert.True(document.Validate().IsValid);

        Assert.Contains("<details data-adf-type=\"nestedExpand\"><summary>More</summary>", AdfToHtmlConverter.Convert(document));
        RoundTripAssert.FullRoundTrip(document);
    }

    [Fact]
    public void NestedExpand_InsideExpand_HtmlRoundTrips()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .Expand("outer", e => e.NestedExpand("inner", n => n.Paragraph(p => p.Text("x")))));
        Assert.True(document.Validate().IsValid);

        RoundTripAssert.FullRoundTrip(document);
    }

    [Fact]
    public void PlainDetails_TakesItsTypeFromPlacement()
    {
        AdfDocument document = HtmlToAdfConverter.Convert(
            "<details><summary> Top  level </summary>text<details><summary>Inner</summary><p>deep</p></details></details>" +
            "<table><tr><td><details><summary>Cell</summary><ul><li>a</li></ul></details></td></tr></table>");

        AdfValidationResult result = document.Validate();
        Assert.True(result.IsValid, string.Join("\n", result.Errors));

        AdfNode expand = document.Content![0];
        Assert.Equal(AdfNodeType.Expand, expand.Type);
        Assert.Equal("Top level", expand.Attrs!["title"]);
        Assert.Equal(AdfNodeType.Paragraph, expand.Content![0].Type);
        Assert.Equal(AdfNodeType.NestedExpand, expand.Content[1].Type);
        Assert.Equal("Inner", expand.Content[1].Attrs!["title"]);

        // A list isn't legal in a nestedExpand, so it's unwrapped into its paragraph.
        AdfNode nested = document.Content[1].Content![0].Content![0].Content![0];
        Assert.Equal(AdfNodeType.NestedExpand, nested.Type);
        Assert.Equal("Cell", nested.Attrs!["title"]);
        Assert.Equal("a", Assert.Single(Assert.Single(nested.Content!).Content!).Text);
    }

    [Fact]
    public void NestedExpandMarkedDetails_AtTopLevel_BecomesExpand()
    {
        AdfDocument document = HtmlToAdfConverter.Convert("<details data-adf-type=\"nestedExpand\"><summary>x</summary><p>y</p></details>");

        Assert.Equal(AdfNodeType.Expand, Assert.Single(document.Content!).Type);
        Assert.True(document.Validate().IsValid);
    }

    [Fact]
    public void Details_WithoutSummaryOrContent_IsValid()
    {
        AdfDocument document = HtmlToAdfConverter.Convert("<details></details>");

        AdfNode expand = Assert.Single(document.Content!);
        Assert.Equal(AdfNodeType.Expand, expand.Type);
        Assert.Empty(expand.Attrs!);
        Assert.Equal(AdfNodeType.Paragraph, Assert.Single(expand.Content!).Type);
        Assert.True(document.Validate().IsValid);
    }

    [Fact]
    public void Details_WhereNoExpandIsLegal_KeepsTitleAsBoldParagraph()
    {
        AdfDocument document = HtmlToAdfConverter.Convert("<blockquote><details><summary>Title</summary><p>body</p></details></blockquote>");

        AdfValidationResult result = document.Validate();
        Assert.True(result.IsValid, string.Join("\n", result.Errors));

        AdfNode blockquote = Assert.Single(document.Content!);
        Assert.Equal(2, blockquote.Content!.Count);
        AdfNode title = Assert.Single(blockquote.Content[0].Content!);
        Assert.Equal("Title", title.Text);
        Assert.Equal(AdfMarkType.Strong, Assert.Single(title.Marks!).Type);
        Assert.Equal("body", blockquote.Content[1].Content![0].Text);
    }

    [Theory]
    [InlineData("Details")]
    [InlineData("A <b>&amp; \"quoted\"</b> title")]
    [InlineData("")]
    [InlineData(null)]
    public void Expand_MarkdownRoundTrips(string? title)
    {
        MarkdownRoundTripAssert.FullRoundTrip(ExpandDocument(title));
    }

    [Fact]
    public void Expand_RendersAsMarkdownDetailsBlock()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc.Expand("T & C", e => e.Paragraph(p => p.Text("x"))));

        Assert.Equal("<details><summary>T &amp; C</summary>\n\nx\n\n</details>", AdfToMarkdownConverter.Convert(document));
    }

    [Fact]
    public void NestedExpand_InsideExpand_MarkdownRoundTrips()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .Paragraph(p => p.Text("before"))
            .Expand("outer", e => e
                .Paragraph(p => p.Text("a"))
                .NestedExpand("inner", n => n.Paragraph(p => p.Text("b")))
                .Paragraph(p => p.Text("c")))
            .Paragraph(p => p.Text("after")));

        MarkdownRoundTripAssert.FullRoundTrip(document);
    }

    [Fact]
    public void NestedExpand_InTableCell_RendersOneWayInMarkdown()
    {
        string markdown = AdfToMarkdownConverter.Convert(NestedExpandDocument());

        Assert.Contains("| **More**<br>hidden<br>### Sub |", markdown);
        Assert.DoesNotContain("<details>", markdown);
    }

    [Theory]
    [InlineData("<details>\n<summary>Title</summary>\n\nbody\n\n</details>")]
    [InlineData("<details><summary>Title</summary>\nbody\n</details>")]
    [InlineData("<details open>\n  <summary><b>Title</b></summary>\n\nbody\n\n</details>")]
    public void MarkdownDetails_CommonShapes_Parse(string markdown)
    {
        AdfDocument document = MarkdownToAdfConverter.Convert(markdown);

        AdfNode expand = Assert.Single(document.Content!);
        Assert.Equal(AdfNodeType.Expand, expand.Type);
        Assert.Equal("Title", expand.Attrs!["title"]);
        Assert.Equal("body", Assert.Single(Assert.Single(expand.Content!).Content!).Text);
        Assert.True(document.Validate().IsValid);
    }

    [Fact]
    public void MarkdownDetails_Unclosed_IsIgnored()
    {
        AdfDocument document = MarkdownToAdfConverter.Convert("<details><summary>T</summary>\n\nbody");

        AdfNode paragraph = Assert.Single(document.Content!);
        Assert.Equal(AdfNodeType.Paragraph, paragraph.Type);
        Assert.Equal("body", Assert.Single(paragraph.Content!).Text);
    }
}
