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
/// Covers the <c>backgroundColor</c> and <c>border</c> marks: builder, <c>Validate()</c>, JSON and conversion.
/// <c>backgroundColor</c>'s rules come from its spec page; <c>border</c>'s page returns 404, so its rules come
/// from the ADF JSON schema. <c>backgroundColor</c> round-trips through HTML as a <c>background-color</c> span;
/// Markdown drops it, and <c>border</c> has no HTML/Markdown form.
/// </summary>
public class BackgroundColorAndBorderTests
{
    private const string MediaId = "6e7c7f2c-dd7a-499c-bceb-6f32bfbf30b5";
    private const string Collection = "ae730abd-a389-46a7-90eb-c03e75a45bf6";

    private static AdfNode Text(string text, params AdfMark[] marks) => AdfNode.CreateText(text, marks.ToList());

    private static AdfMark Background(string color) =>
        new AdfMark(AdfMarkType.BackgroundColor, new Dictionary<string, object> { ["color"] = color });

    private static AdfMark Border(object size, string color) =>
        new AdfMark(AdfMarkType.Border, new Dictionary<string, object> { ["size"] = size, ["color"] = color });

    private static AdfValidationResult ValidateInline(params AdfNode[] inline) =>
        AdfNode.CreateDocument([AdfNode.CreateParagraph(inline.ToList())]).Validate();

    private static AdfValidationResult ValidateMedia(params AdfMark[] marks)
    {
        AdfNode media = AdfNode.CreateMedia(MediaId, "file", Collection);
        media.Marks = marks.ToList();
        return AdfNode.CreateDocument([AdfNode.CreateMediaSingle(media, "center")]).Validate();
    }

    [Fact]
    public void BackgroundColor_DeserializesFromSpecJson()
    {
        string json = """
        {
          "version": 1,
          "type": "doc",
          "content": [
            {
              "type": "paragraph",
              "content": [
                { "type": "text", "text": "Hello world", "marks": [ { "type": "backgroundColor", "attrs": { "color": "#fedec8" } } ] }
              ]
            }
          ]
        }
        """;

        AdfDocument document = AdfJsonConverter.Deserialize(json);

        AdfMark mark = Assert.Single(document.Content![0].Content![0].Marks!);
        Assert.Equal(AdfMarkType.BackgroundColor, mark.Type);
        Assert.Equal("#fedec8", mark.Attrs!["color"]);

        Assert.True(document.Validate().IsValid);
        RoundTripAssert.JsonRoundTrip(document);
        Assert.Contains("\"type\":\"backgroundColor\"", AdfJsonConverter.Serialize(document));
    }

    [Fact]
    public void Border_OnMediaAndMediaInline_DeserializesFromSchemaJson()
    {
        string json = $$"""
        {
          "version": 1,
          "type": "doc",
          "content": [
            {
              "type": "mediaSingle",
              "attrs": { "layout": "center" },
              "content": [
                {
                  "type": "media",
                  "attrs": { "id": "{{MediaId}}", "type": "file", "collection": "{{Collection}}" },
                  "marks": [ { "type": "border", "attrs": { "size": 2, "color": "#091e4224" } } ]
                }
              ]
            },
            {
              "type": "paragraph",
              "content": [
                {
                  "type": "mediaInline",
                  "attrs": { "id": "{{MediaId}}", "collection": "{{Collection}}" },
                  "marks": [ { "type": "border", "attrs": { "size": 1, "color": "#ff0000" } } ]
                }
              ]
            }
          ]
        }
        """;

        AdfDocument document = AdfJsonConverter.Deserialize(json);

        AdfMark mediaBorder = Assert.Single(document.Content![0].Content![0].Marks!);
        Assert.Equal(AdfMarkType.Border, mediaBorder.Type);
        Assert.Equal("#091e4224", mediaBorder.Attrs!["color"]);
        Assert.Equal(AdfMarkType.Border, Assert.Single(document.Content![1].Content![0].Marks!).Type);

        Assert.True(document.Validate().IsValid);
        RoundTripAssert.JsonRoundTrip(document);
    }

    // ----- backgroundColor -----

    [Theory]
    [InlineData("#fedec8", true)]
    [InlineData("#fed", true)]
    [InlineData("yellow", false)]
    public void BackgroundColor_MustBeHex(string color, bool isValid)
    {
        Assert.Equal(isValid, ValidateInline(Text("x", Background(color))).IsValid);
    }

    [Fact]
    public void BackgroundColor_WithoutColor_IsInvalid()
    {
        AdfValidationResult result = ValidateInline(Text("x", new AdfMark(AdfMarkType.BackgroundColor)));

        Assert.Contains(result.Errors, e => e.Contains("attribute 'color' is required on mark 'backgroundColor'"));
    }

    [Fact]
    public void BackgroundColor_CannotCombineWithCode()
    {
        AdfValidationResult result = ValidateInline(Text("x", Background("#fedec8"), new AdfMark(AdfMarkType.Code)));

        Assert.Contains(result.Errors, e => e.Contains("mark 'backgroundColor' cannot be combined with mark 'code'"));
        Assert.False(AdfMarkSchema.CanCombine(AdfMarkType.Code, AdfMarkType.BackgroundColor));
    }

    [Fact]
    public void BackgroundColor_CombinesWithTextColorLinkAndStrong()
    {
        AdfNode text = Text("x",
            Background("#fedec8"),
            new AdfMark(AdfMarkType.Strong),
            new AdfMark(AdfMarkType.Link, new Dictionary<string, object> { ["href"] = "https://example.com" }));
        AdfNode colored = Text("y", Background("#fedec8"), new AdfMark(AdfMarkType.TextColor, new Dictionary<string, object> { ["color"] = "#97a0af" }));

        Assert.True(ValidateInline(text, colored).IsValid);
    }

    [Fact]
    public void BackgroundColor_IsNotAllowedOnMedia()
    {
        Assert.Contains(ValidateMedia(Background("#fedec8")).Errors, e => e.Contains("mark 'backgroundColor' is not allowed on 'media'"));
    }

    // ----- border -----

    [Theory]
    [InlineData(1, true)]
    [InlineData(3, true)]
    [InlineData(2.5, true)]
    [InlineData(0, false)]
    [InlineData(4, false)]
    public void Border_SizeMustBeFromOneToThree(object size, bool isValid)
    {
        Assert.Equal(isValid, ValidateMedia(Border(size, "#ff0000")).IsValid);
    }

    [Theory]
    [InlineData("#ff0000", true)]
    [InlineData("#091e4224", true)]
    [InlineData("#f00", false)]
    [InlineData("red", false)]
    public void Border_ColorMustBeSixOrEightDigitHex(string color, bool isValid)
    {
        Assert.Equal(isValid, ValidateMedia(Border(2, color)).IsValid);
    }

    [Fact]
    public void Border_RequiresSizeAndColor()
    {
        AdfValidationResult result = ValidateMedia(new AdfMark(AdfMarkType.Border));

        Assert.Contains(result.Errors, e => e.Contains("attribute 'size' is required on mark 'border'"));
        Assert.Contains(result.Errors, e => e.Contains("attribute 'color' is required on mark 'border'"));
    }

    [Fact]
    public void Border_CombinesWithLinkOnMedia()
    {
        AdfMark link = new AdfMark(AdfMarkType.Link, new Dictionary<string, object> { ["href"] = "https://example.com" });

        Assert.True(ValidateMedia(Border(2, "#ff0000"), link).IsValid);
    }

    [Fact]
    public void Border_IsNotAllowedOnText()
    {
        Assert.Contains(ValidateInline(Text("x", Border(2, "#ff0000"))).Errors, e => e.Contains("mark 'border' is not allowed on 'text'"));
    }

    [Fact]
    public void IsValidMark_ReflectsTheNewMarks()
    {
        Assert.True(AdfNodeSchema.IsValidMark(AdfNodeType.Text, AdfMarkType.BackgroundColor));
        Assert.True(AdfNodeSchema.IsValidMark(AdfNodeType.Media, AdfMarkType.Border));
        Assert.True(AdfNodeSchema.IsValidMark(AdfNodeType.MediaInline, AdfMarkType.Border));
        Assert.False(AdfNodeSchema.IsValidMark(AdfNodeType.Text, AdfMarkType.Border));
        Assert.False(AdfNodeSchema.IsValidMark(AdfNodeType.MediaInline, AdfMarkType.BackgroundColor));
    }

    // ----- Builder -----

    [Fact]
    public void Builder_AddsBothMarks()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .Paragraph(p => p
                .Text("highlighted", m => m.BackgroundColor("#fedec8").Strong())
                .MediaInline(MediaId, Collection, marks: m => m.Border(1, "#ff0000")))
            .MediaSingle(MediaId, "file", Collection, marks: m => m.Border(2, "#091e4224").Link("https://example.com"))
            .MediaGroup(g => g
                .Media(MediaId, "file", Collection, marks: m => m.Border(3, "#ff0000"))
                .Media(MediaId, "file", Collection)));

        AdfNode paragraph = document.Content![0];
        AdfMark background = paragraph.Content![0].Marks![0];
        Assert.Equal(AdfMarkType.BackgroundColor, background.Type);
        Assert.Equal("#fedec8", background.Attrs!["color"]);

        AdfMark inlineBorder = Assert.Single(paragraph.Content![1].Marks!);
        Assert.Equal(AdfMarkType.Border, inlineBorder.Type);
        Assert.Equal(1, inlineBorder.Attrs!["size"]);

        AdfNode media = document.Content![1].Content![0];
        Assert.Equal([AdfMarkType.Border, AdfMarkType.Link], media.Marks!.Select(m => m.Type));

        AdfNode group = document.Content![2];
        Assert.Equal(AdfMarkType.Border, Assert.Single(group.Content![0].Marks!).Type);
        Assert.Null(group.Content![1].Marks);

        Assert.True(document.Validate().IsValid);
        RoundTripAssert.JsonRoundTrip(document);
    }

    [Fact]
    public void Builder_EmptyMarksLambda_LeavesMarksNull()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc.MediaSingle(MediaId, "file", Collection, marks: _ => { }));

        Assert.Null(document.Content![0].Content![0].Marks);
    }

    // ----- Converters -----

    [Fact]
    public void BackgroundColor_RendersAsBackgroundColorSpan()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .Paragraph(p => p.Text("highlighted", m => m.BackgroundColor("#fedec8"))));

        Assert.Equal("<p><span style=\"background-color: #fedec8\">highlighted</span></p>", AdfToHtmlConverter.Convert(document));
    }

    [Fact]
    public void BackgroundColor_RoundTripsThroughHtml()
    {
        RoundTripAssert.FullRoundTrip(AdfDocumentBuilder.Build(doc => doc
            .Paragraph(p => p.Text("highlighted", m => m.BackgroundColor("#fedec8")))));
    }

    [Fact]
    public void BackgroundColor_WithOtherMarks_RoundTripsThroughHtml()
    {
        RoundTripAssert.FullRoundTrip(AdfDocumentBuilder.Build(doc => doc
            .Paragraph(p => p
                .Text("bold", m => m.Strong().BackgroundColor("#fedec8"))
                .Text(" and ")
                .Text("both colors", m => m.TextColor("#ff5630").BackgroundColor("#c6edfb"))
                .Text(" and ")
                .Text("linked", m => m.Link("https://example.com/").BackgroundColor("#d3f1a7")))));
    }

    [Theory]
    [InlineData("background-color: #fedec8", "#fedec8")]
    [InlineData("BACKGROUND-COLOR:#FEDEC8;", "#FEDEC8")]
    [InlineData("background-color: red", "#FF0000")]
    [InlineData("background: #abc", "#abc")]
    public void Html_CssBackgroundColor_BecomesBackgroundColor(string style, string expectedColor)
    {
        AssertHtmlConverts(
            $"<p><span style=\"{style}\">text</span></p>",
            doc => doc.Paragraph(p => p.Text("text", m => m.BackgroundColor(expectedColor))));
    }

    [Theory]
    [InlineData("background-color: transparent")]
    [InlineData("background-color: rgb(1, 2, 3)")]
    [InlineData("background: url(x.png) #fff")]
    public void Html_UnresolvableBackground_IsIgnored(string style)
    {
        AssertHtmlConverts(
            $"<p><span style=\"{style}\">text</span></p>",
            doc => doc.Paragraph(p => p.Text("text")));
    }

    [Fact]
    public void Html_ColorAndBackgroundColorInOneStyle_GiveBothMarks()
    {
        AssertHtmlConverts(
            "<p><span style=\"color: #ff5630; background-color: #fedec8\">text</span></p>",
            doc => doc.Paragraph(p => p.Text("text", m => m.TextColor("#ff5630").BackgroundColor("#fedec8"))));
    }

    [Fact]
    public void Html_MarkElement_GetsTheDefaultHighlightColor()
    {
        AssertHtmlConverts(
            "<p>a <mark>highlighted</mark> word</p>",
            doc => doc.Paragraph(p => p
                .Text("a ")
                .Text("highlighted", m => m.BackgroundColor(AdfStructure.DefaultHighlightColor))
                .Text(" word")));
    }

    [Fact]
    public void Html_MarkElementWithOwnBackground_KeepsItsColor()
    {
        AssertHtmlConverts(
            "<p><mark style=\"background-color: #fdd0ec\">text</mark></p>",
            doc => doc.Paragraph(p => p.Text("text", m => m.BackgroundColor("#fdd0ec"))));
    }

    [Fact]
    public void Html_NestedBackgrounds_InnermostWins()
    {
        AssertHtmlConverts(
            "<p><span style=\"background-color: #fedec8\"><mark>text</mark></span></p>",
            doc => doc.Paragraph(p => p.Text("text", m => m.BackgroundColor(AdfStructure.DefaultHighlightColor))));
    }

    [Fact]
    public void Html_BackgroundColorOnCode_KeepsOnlyCode()
    {
        AssertHtmlConverts(
            "<p><mark><code>x</code></mark></p>",
            doc => doc.Paragraph(p => p.Text("x", m => m.Code())));
    }

    [Fact]
    public void Markdown_DropsBackgroundColorButKeepsTheText()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .Paragraph(p => p.Text("highlighted", m => m.BackgroundColor("#fedec8").Strong())));

        Assert.Equal("**highlighted**", AdfToMarkdownConverter.Convert(document));
    }

    [Fact]
    public void Converters_DropBorderButKeepTheRest()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .Paragraph(p => p
                .Text("text")
                .MediaInline(MediaId, Collection, marks: m => m.Border(1, "#ff0000"))));

        // mediaInline and border have no HTML/Markdown representation (Media Services identifiers).
        Assert.Equal("<p>text</p>", AdfToHtmlConverter.Convert(document));
        Assert.Equal("text", AdfToMarkdownConverter.Convert(document));
    }

    private static void AssertHtmlConverts(string html, Action<AdfBlockContentBuilder> expected)
    {
        AdfDocument actual = HtmlToAdfConverter.Convert(html);
        string actualJson = AdfJsonConverter.Serialize(actual);
        string expectedJson = AdfJsonConverter.Serialize(AdfDocumentBuilder.Build(expected));

        Assert.Equal(expectedJson, actualJson);
        AdfValidationResult validation = actual.Validate();
        Assert.True(validation.IsValid, string.Join("\n", validation.Errors));
    }
}
