// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Builders;
using AdfDotNet.Enums;
using AdfDotNet.Models;
using Xunit;

namespace AdfDotNet.Tests;

/// <summary>
/// Covers <see cref="AdfDocumentBuilder"/> and the nested-lambda builder types in
/// <see cref="AdfDotNet.Builders"/>.
/// </summary>
public class AdfDocumentBuilderTests
{
    [Fact]
    public void Build_ComposesExpectedTree()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .Heading(1, "Title")
            .Paragraph(p => p
                .Text("Hello ")
                .Text("world", m => m.Strong())
                .HardBreak())
            .Rule());

        Assert.Equal(AdfNodeType.Doc, document.Type);
        Assert.Equal(3, document.Content!.Count);

        AdfNode heading = document.Content[0];
        Assert.Equal(AdfNodeType.Heading, heading.Type);
        Assert.Equal(1, heading.Attrs!["level"]);
        Assert.Equal("Title", heading.Content!.Single().Text);

        AdfNode paragraph = document.Content[1];
        Assert.Equal(AdfNodeType.Paragraph, paragraph.Type);
        Assert.Equal(3, paragraph.Content!.Count);
        Assert.Equal("Hello ", paragraph.Content[0].Text);
        Assert.Null(paragraph.Content[0].Marks);
        Assert.Equal("world", paragraph.Content[1].Text);
        Assert.Equal(AdfMarkType.Strong, paragraph.Content[1].Marks!.Single().Type);
        Assert.Equal(AdfNodeType.HardBreak, paragraph.Content[2].Type);

        Assert.Equal(AdfNodeType.Rule, document.Content[2].Type);
    }

    [Fact]
    public void Build_WellFormedDocument_PassesValidate()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .Heading(1, "Title")
            .Paragraph(p => p.Text("Hello world"))
            .BulletList(list => list
                .Item(li => li.Paragraph(p => p.Text("one")))
                .Item(li => li.Paragraph(p => p.Text("two"))))
            .OrderedList(list => list
                .Item(li => li.Paragraph(p => p.Text("first"))))
            .Blockquote(bq => bq.Paragraph(p => p.Text("quoted")))
            .CodeBlock("var x = 1;", "csharp")
            .Table(t => t
                .Row(r => r
                    .Header(h => h.Paragraph(p => p.Text("Col")))
                    .Cell(c => c.Paragraph(p => p.Text("Val")))))
            .MediaSingle("abc-123", "file", "content-collection"));

        AdfValidationResult result = document.Validate();

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void MediaSingle_WrapsSingleMediaChild()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .MediaSingle("abc-123", "file", "content-collection", layout: "wide", width: 200, height: 100));

        AdfNode mediaSingle = document.Content!.Single();
        Assert.Equal(AdfNodeType.MediaSingle, mediaSingle.Type);
        Assert.Equal("wide", mediaSingle.Attrs!["layout"]);

        AdfNode media = mediaSingle.Content!.Single();
        Assert.Equal(AdfNodeType.Media, media.Type);
        Assert.Equal("abc-123", media.Attrs!["id"]);
        Assert.Equal("file", media.Attrs!["type"]);
        Assert.Equal("content-collection", media.Attrs!["collection"]);
        Assert.Equal(200, media.Attrs!["width"]);
        Assert.Equal(100, media.Attrs!["height"]);
    }

    [Fact]
    public void InlineCardAndEmoji_AddToParagraphContent()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .Paragraph(p => p
                .Text("See ")
                .InlineCard("https://example.com")
                .Emoji(":grinning:", text: "\U0001F600")));

        AdfNode paragraph = document.Content!.Single();
        Assert.Equal(3, paragraph.Content!.Count);

        AdfNode inlineCard = paragraph.Content[1];
        Assert.Equal(AdfNodeType.InlineCard, inlineCard.Type);
        Assert.Equal("https://example.com", inlineCard.Attrs!["url"]);

        AdfNode emoji = paragraph.Content[2];
        Assert.Equal(AdfNodeType.Emoji, emoji.Type);
        Assert.Equal(":grinning:", emoji.Attrs!["shortName"]);
        Assert.Equal("\U0001F600", emoji.Attrs!["text"]);

        AdfValidationResult result = document.Validate();
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Text_WithMultipleMarks_CombinesThemOnOneRun()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .Paragraph(p => p.Text("styled", m => m.Strong().Em().Strike().Underline())));

        AdfNode text = document.Content!.Single().Content!.Single();

        Assert.Equal(
            new[] { AdfMarkType.Strong, AdfMarkType.Em, AdfMarkType.Strike, AdfMarkType.Underline },
            text.Marks!.Select(mark => mark.Type));
    }

    [Fact]
    public void Text_WithAttributeBearingMarks_SetsExpectedAttrs()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .Paragraph(p => p.Text("link", m => m
                .Link("https://example.com")
                .TextColor("#ff0000")
                .SubSup("sup"))));

        List<AdfMark> marks = document.Content!.Single().Content!.Single().Marks!;

        AdfMark link = marks.Single(m => m.Type == AdfMarkType.Link);
        Assert.Equal("https://example.com", link.Attrs!["href"]);

        AdfMark color = marks.Single(m => m.Type == AdfMarkType.TextColor);
        Assert.Equal("#ff0000", color.Attrs!["color"]);

        AdfMark subSup = marks.Single(m => m.Type == AdfMarkType.SubSup);
        Assert.Equal("sup", subSup.Attrs!["type"]);
    }

    [Fact]
    public void CodeBlock_WithoutLanguage_OmitsAttrs()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc.CodeBlock("plain"));

        AdfNode codeBlock = document.Content!.Single();

        Assert.Null(codeBlock.Attrs);
        Assert.Equal("plain", codeBlock.Content!.Single().Text);
    }

    [Fact]
    public void Extend_AppendsToExistingDocumentContent()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc.Paragraph(p => p.Text("first")));

        AdfDocument result = AdfDocumentBuilder.Extend(document, doc => doc
            .Paragraph(p => p.Text("second"))
            .Rule());

        Assert.Same(document, result);
        Assert.Equal(3, document.Content!.Count);
        Assert.Equal("first", document.Content[0].Content!.Single().Text);
        Assert.Equal("second", document.Content[1].Content!.Single().Text);
        Assert.Equal(AdfNodeType.Rule, document.Content[2].Type);
    }

    [Fact]
    public void Extend_DocumentWithNullContent_InitializesContent()
    {
        AdfDocument document = AdfNode.CreateDocument();
        document.Content = null;

        AdfDocumentBuilder.Extend(document, doc => doc.Rule());

        Assert.Equal(AdfNodeType.Rule, document.Content!.Single().Type);
    }

    [Fact]
    public void Prepend_InsertsBeforeExistingDocumentContentInBuiltOrder()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc.Paragraph(p => p.Text("last")));

        AdfDocument result = AdfDocumentBuilder.Prepend(document, doc => doc
            .Heading(1, "Title")
            .Rule());

        Assert.Same(document, result);
        Assert.Equal(3, document.Content!.Count);
        Assert.Equal(AdfNodeType.Heading, document.Content[0].Type);
        Assert.Equal(AdfNodeType.Rule, document.Content[1].Type);
        Assert.Equal("last", document.Content[2].Content!.Single().Text);
    }

    [Fact]
    public void Build_IllegalNesting_IsCaughtByValidateNotByTheBuilder()
    {
        // "table" is only a legal child of "doc" per AdfNodeSchema, not of "listItem" - the builder itself
        // does not stop this (AdfBlockContentBuilder is shared across every block-content parent), so the
        // check happens via Validate() instead.
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .BulletList(list => list
                .Item(li => li.Table(t => t.Row(r => r.Cell(c => c.Paragraph(p => p.Text("x"))))))));

        AdfValidationResult result = document.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not a legal child of 'listItem'"));
    }
}
