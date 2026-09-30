// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.DataConverters;
using AdfDotNet.Enums;
using AdfDotNet.Models;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AdfDotNet.Tests;

/// <summary>
/// Covers the spec-level checks <see cref="AdfDocument.Validate"/> performs beyond
/// legal-child nesting (see <see cref="AdfDocumentValidateTests"/>): required attributes and their values,
/// child counts, which marks each node type may carry, and mark combinations (<see cref="AdfMarkSchema"/>).
/// Every rule comes from the node's or mark's page in the ADF spec.
/// </summary>
public class AdfSpecValidationTests
{
    private static AdfNode Para(params AdfNode[] content) => AdfNode.CreateParagraph(content.ToList());

    private static AdfNode Text(string text, params AdfMark[] marks) =>
        AdfNode.CreateText(text, marks.Length == 0 ? null : marks.ToList());

    private static AdfMark Mark(AdfMarkType type, string? attrName = null, object? attrValue = null) =>
        new AdfMark(type, attrName == null ? null : new Dictionary<string, object> { [attrName] = attrValue! });

    private static AdfNode SampleMedia() => AdfNode.CreateMedia("abc-123", "file", "content-collection");

    private static AdfValidationResult ValidateBlocks(params AdfNode[] blocks) =>
        AdfNode.CreateDocument(blocks.ToList()).Validate();

    private static AdfValidationResult ValidateInline(params AdfNode[] inline) => ValidateBlocks(Para(inline));

    private static void AssertInvalid(AdfValidationResult result, string expectedFragment)
    {
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains(expectedFragment));
    }

    // ----- Attributes -----

    [Fact]
    public void Heading_WithoutLevel_IsInvalid()
    {
        AdfNode heading = AdfNode.CreateHeading(1, "Title");
        heading.Attrs = null;

        AssertInvalid(ValidateBlocks(heading), "attribute 'level' is required on 'heading'");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void Heading_WithLevelOutOfRange_IsInvalid(int level)
    {
        AssertInvalid(ValidateBlocks(AdfNode.CreateHeading(level, "Title")), $"must be a whole number from 1 to 6, but was {level}");
    }

    [Fact]
    public void Heading_WithStringLevel_IsInvalid()
    {
        AdfNode heading = AdfNode.CreateHeading(1, "Title");
        heading.Attrs!["level"] = "1";

        AssertInvalid(ValidateBlocks(heading), "but was \"1\"");
    }

    [Fact]
    public void Panel_WithUnknownPanelType_IsInvalid()
    {
        AdfNode panel = AdfNode.CreatePanel("tip", [Para(Text("x"))]);

        AssertInvalid(ValidateBlocks(panel), "attribute 'panelType' on 'panel' must be");
    }

    [Theory]
    [InlineData("neutral", true)]
    [InlineData("green", true)]
    [InlineData("#123ABC", true)]
    [InlineData("orange", false)]
    [InlineData("#abc", false)]
    public void Status_Color_FollowsSpec(string color, bool isValid)
    {
        Assert.Equal(isValid, ValidateInline(AdfNode.CreateStatus("Done", color)).IsValid);
    }

    [Theory]
    [InlineData("1582152559", true)]
    [InlineData("2020-02-19", false)]
    public void Date_Timestamp_MustBeDigits(string timestamp, bool isValid)
    {
        Assert.Equal(isValid, ValidateInline(AdfNode.CreateDate(timestamp)).IsValid);
    }

    [Fact]
    public void Mention_WithUnknownAccessLevel_IsInvalid()
    {
        AssertInvalid(ValidateInline(AdfNode.CreateMention("id-1", accessLevel: "EVERYONE")), "attribute 'accessLevel' on 'mention'");
    }

    [Fact]
    public void Media_WithoutCollectionOrWithUnknownType_IsInvalid()
    {
        AdfNode media = SampleMedia();
        media.Attrs!.Remove("collection");
        media.Attrs["type"] = "image";

        AdfValidationResult result = ValidateBlocks(AdfNode.CreateMediaSingle(media));

        AssertInvalid(result, "attribute 'collection' is required on 'media'");
        AssertInvalid(result, "attribute 'type' on 'media' must be \"file\" or \"link\"");
    }

    [Fact]
    public void MediaSingle_WithUnknownLayout_IsInvalid()
    {
        AssertInvalid(ValidateBlocks(AdfNode.CreateMediaSingle(SampleMedia(), layout: "left")), "attribute 'layout' on 'mediaSingle'");
    }

    [Fact]
    public void MediaSingle_Width_IsAPercentageUnlessWidthTypeIsPixel()
    {
        AssertInvalid(ValidateBlocks(AdfNode.CreateMediaSingle(SampleMedia(), width: 150)), "must be a percentage from 0 to 100");
        Assert.True(ValidateBlocks(AdfNode.CreateMediaSingle(SampleMedia(), width: 150, widthType: "pixel")).IsValid);
        Assert.True(ValidateBlocks(AdfNode.CreateMediaSingle(SampleMedia(), width: 50)).IsValid);
    }

    [Fact]
    public void InlineCard_NeedsExactlyOneOfUrlAndData()
    {
        Assert.True(ValidateInline(AdfNode.CreateInlineCard("https://example.com")).IsValid);

        AdfNode both = AdfNode.CreateInlineCard("https://example.com");
        both.Attrs!["data"] = new Dictionary<string, object> { ["@type"] = "Document" };
        AssertInvalid(ValidateInline(both), "exactly one of the attributes 'url' and 'data'");

        AdfNode neither = AdfNode.CreateInlineCard("https://example.com");
        neither.Attrs!.Clear();
        AssertInvalid(ValidateInline(neither), "exactly one of the attributes 'url' and 'data'");
    }

    [Fact]
    public void Text_MustNotBeEmpty()
    {
        AssertInvalid(ValidateInline(Text("")), "text node must have non-empty text");
    }

    [Fact]
    public void UnlistedAttributes_AreNotReported()
    {
        // Real Jira documents carry attrs the spec pages don't list (e.g. localId on most block nodes).
        AdfNode table = AdfNode.CreateTable([AdfNode.CreateTableRow([AdfNode.CreateTableCell([Para(Text("x"))])])]);
        table.Attrs = new Dictionary<string, object> { ["localId"] = "abc", ["someFutureAttr"] = 1 };

        Assert.True(ValidateBlocks(table).IsValid);
    }

    [Fact]
    public void DocumentVersionOtherThanOne_IsInvalid()
    {
        AdfDocument document = AdfNode.CreateDocument();
        document.Version = 2;

        AssertInvalid(document.Validate(), "document version must be 1, but was 2");
    }

    [Fact]
    public void AttrsDeserializedFromJson_AreValid()
    {
        // JSON whole numbers arrive as long, decimals as double - both must satisfy the integer/number rules.
        string json = """
        {
          "version": 1,
          "type": "doc",
          "content": [
            { "type": "heading", "attrs": { "level": 2 }, "content": [ { "type": "text", "text": "Title" } ] },
            { "type": "orderedList", "attrs": { "order": 3 }, "content": [
              { "type": "listItem", "content": [ { "type": "paragraph", "content": [ { "type": "text", "text": "Item" } ] } ] }
            ] },
            { "type": "table", "attrs": { "isNumberColumnEnabled": false, "layout": "center", "width": 760.0 }, "content": [
              { "type": "tableRow", "content": [
                { "type": "tableHeader", "attrs": { "colspan": 1, "rowspan": 1, "colwidth": [ 150, 200.5 ] },
                  "content": [ { "type": "paragraph" } ] }
              ] }
            ] },
            { "type": "mediaSingle", "attrs": { "layout": "center", "width": 66.67 }, "content": [
              { "type": "media", "attrs": { "id": "abc", "type": "file", "collection": "c", "width": 800, "height": 600 } }
            ] },
            { "type": "codeBlock", "attrs": { "language": "csharp", "wrap": null } }
          ]
        }
        """;

        AdfValidationResult result = AdfJsonConverter.Deserialize(json).Validate();

        Assert.True(result.IsValid, string.Join("\n", result.Errors));
    }

    // ----- Child counts -----

    [Fact]
    public void ContainersThatNeedContent_AreInvalidWhenEmpty()
    {
        AdfValidationResult result = ValidateBlocks(
            AdfNode.CreateBulletList(),
            AdfNode.CreateOrderedList(),
            AdfNode.CreateBlockquote(),
            AdfNode.CreatePanel("info"),
            AdfNode.CreateTable(),
            AdfNode.CreateTable([AdfNode.CreateTableRow()]),
            AdfNode.CreateTable([AdfNode.CreateTableRow([AdfNode.CreateTableCell()])]),
            AdfNode.CreateBulletList([AdfNode.CreateListItem()]));

        foreach (string type in new[] { "bulletList", "orderedList", "blockquote", "panel", "table", "tableRow", "tableCell", "listItem" })
            AssertInvalid(result, $"node of type '{type}' must have at least 1 child node(s), but has 0");
    }

    [Fact]
    public void EmptyParagraphHeadingAndCodeBlock_AreValid()
    {
        Assert.True(ValidateBlocks(AdfNode.CreateParagraph(), AdfNode.CreateHeading(1), AdfNode.CreateCodeBlock()).IsValid);
    }

    [Fact]
    public void MediaSingle_MustHaveExactlyOneMedia()
    {
        AdfNode twoMedia = AdfNode.CreateMediaSingle(SampleMedia());
        twoMedia.AddContent(SampleMedia());
        AssertInvalid(ValidateBlocks(twoMedia), "must have at most 1 child node(s), but has 2");

        AdfNode noMedia = AdfNode.CreateMediaSingle(SampleMedia());
        noMedia.Content!.Clear();
        AssertInvalid(ValidateBlocks(noMedia), "must have at least 1 child node(s), but has 0");
    }

    // ----- Marks per node type -----

    [Fact]
    public void Paragraph_CannotCarryMarks()
    {
        AdfNode paragraph = Para(Text("x"));
        paragraph.Marks = [Mark(AdfMarkType.Strong)];

        AssertInvalid(ValidateBlocks(paragraph), "mark 'strong' is not allowed on 'paragraph'");
    }

    [Fact]
    public void Media_CanCarryLinkButNotStrong()
    {
        AdfNode linked = SampleMedia();
        linked.Marks = [Mark(AdfMarkType.Link, "href", "https://example.com")];
        Assert.True(ValidateBlocks(AdfNode.CreateMediaSingle(linked)).IsValid);

        AdfNode bold = SampleMedia();
        bold.Marks = [Mark(AdfMarkType.Strong)];
        AssertInvalid(ValidateBlocks(AdfNode.CreateMediaSingle(bold)), "mark 'strong' is not allowed on 'media'");
    }

    [Fact]
    public void CodeBlockText_CannotCarryMarks()
    {
        AdfNode codeBlock = AdfNode.CreateCodeBlock(content: [Text("var x = 1;", Mark(AdfMarkType.Strong))]);

        AssertInvalid(ValidateBlocks(codeBlock), "a 'text' node inside 'codeBlock' cannot have marks");
    }

    [Fact]
    public void Status_CannotCarryMarks()
    {
        AdfNode status = AdfNode.CreateStatus("Done", "green");
        status.Marks = [Mark(AdfMarkType.Em)];

        AssertInvalid(ValidateInline(status), "mark 'em' is not allowed on 'status'");
    }

    // ----- Mark attributes and combinations -----

    [Fact]
    public void Link_WithoutHref_IsInvalid()
    {
        AssertInvalid(ValidateInline(Text("x", Mark(AdfMarkType.Link))), "attribute 'href' is required on mark 'link'");
    }

    [Fact]
    public void SubSup_WithUnknownType_IsInvalid()
    {
        AssertInvalid(ValidateInline(Text("x", Mark(AdfMarkType.SubSup, "type", "super"))), "attribute 'type' on mark 'subsup' must be \"sub\" or \"sup\"");
    }

    [Theory]
    [InlineData("#ff0000", true)]
    [InlineData("#f00", true)]
    [InlineData("red", false)]
    public void TextColor_MustBeHex(string color, bool isValid)
    {
        Assert.Equal(isValid, ValidateInline(Text("x", Mark(AdfMarkType.TextColor, "color", color))).IsValid);
    }

    [Theory]
    [InlineData(AdfMarkType.Link, true)]
    [InlineData(AdfMarkType.Strong, false)]
    [InlineData(AdfMarkType.Em, false)]
    [InlineData(AdfMarkType.TextColor, false)]
    public void Code_CombinesOnlyWithLink(AdfMarkType other, bool isValid)
    {
        AdfMark otherMark = other switch
        {
            AdfMarkType.Link => Mark(AdfMarkType.Link, "href", "https://example.com"),
            AdfMarkType.TextColor => Mark(AdfMarkType.TextColor, "color", "#ff0000"),
            _ => Mark(other),
        };

        AdfValidationResult result = ValidateInline(Text("x", Mark(AdfMarkType.Code), otherMark));

        Assert.Equal(isValid, result.IsValid);
        Assert.Equal(isValid, AdfMarkSchema.CanCombine(AdfMarkType.Code, other));
        Assert.Equal(isValid, AdfMarkSchema.CanCombine(other, AdfMarkType.Code));
    }

    [Fact]
    public void TextColor_CannotCombineWithLink()
    {
        AdfNode text = Text("x", Mark(AdfMarkType.TextColor, "color", "#ff0000"), Mark(AdfMarkType.Link, "href", "https://example.com"));

        AssertInvalid(ValidateInline(text), "mark 'textColor' cannot be combined with mark 'link'");
    }

    [Fact]
    public void StrongEmUnderlineStrikeAndTextColor_CombineFreely()
    {
        AdfNode text = Text("x",
            Mark(AdfMarkType.Strong), Mark(AdfMarkType.Em), Mark(AdfMarkType.Underline),
            Mark(AdfMarkType.Strike), Mark(AdfMarkType.TextColor, "color", "#ff0000"));

        Assert.True(ValidateInline(text).IsValid);
    }

    [Fact]
    public void IsValidMark_ReflectsTheTable()
    {
        Assert.True(AdfNodeSchema.IsValidMark(AdfNodeType.Text, AdfMarkType.Code));
        Assert.True(AdfNodeSchema.IsValidMark(AdfNodeType.Media, AdfMarkType.Link));
        Assert.False(AdfNodeSchema.IsValidMark(AdfNodeType.Media, AdfMarkType.Strong));
        Assert.False(AdfNodeSchema.IsValidMark(AdfNodeType.Paragraph, AdfMarkType.Strong));
    }

    // ----- Serialization of spec type names -----

    [Fact]
    public void SubSupMark_SerializesWithSpecName()
    {
        AdfDocument document = AdfNode.CreateDocument([Para(Text("2", Mark(AdfMarkType.SubSup, "type", "sup")))]);

        JToken json = JToken.Parse(AdfJsonConverter.Serialize(document));

        Assert.Equal("subsup", (string?)json.SelectToken("content[0].content[0].marks[0].type"));
        Assert.Equal("textColor", AdfMarkSchema.GetTypeName(AdfMarkType.TextColor));
    }
}
