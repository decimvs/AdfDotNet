// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Builders;
using AdfDotNet.DataConverters;
using AdfDotNet.Enums;
using AdfDotNet.FormatConverters;
using AdfDotNet.Models;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AdfDotNet.Tests;

/// <summary>
/// <see cref="AdfNormalizer"/>, and the HTML/Markdown -> ADF output it reshapes: input
/// nesting or marks that ADF doesn't allow must come out as a document that passes <see cref="AdfDocument.Validate"/>.
/// </summary>
public class NormalizationTests
{
    // ---- Nesting: HTML ----

    [Fact]
    public void Html_HeadingInBlockquote_BecomesParagraph()
    {
        AssertHtml(
            "<blockquote><h2>Title</h2><p>Body</p></blockquote>",
            doc => doc.Blockquote(q => q
                .Paragraph(p => p.Text("Title"))
                .Paragraph(p => p.Text("Body"))));
    }

    [Fact]
    public void Html_HeadingInBlockquote_KeepsInlineMarks()
    {
        AssertHtml(
            "<blockquote><h2>A <em>b</em></h2></blockquote>",
            doc => doc.Blockquote(q => q.Paragraph(p => p.Text("A ").Text("b", m => m.Em()))));
    }

    [Fact]
    public void Html_NestedBlockquote_IsFlattenedIntoParent()
    {
        AssertHtml(
            "<blockquote><p>outer</p><blockquote><p>inner</p></blockquote></blockquote>",
            doc => doc.Blockquote(q => q
                .Paragraph(p => p.Text("outer"))
                .Paragraph(p => p.Text("inner"))));
    }

    [Fact]
    public void Html_BlockquoteInListItem_IsUnwrapped()
    {
        AssertHtml(
            "<ul><li><blockquote><p>quoted</p></blockquote></li></ul>",
            doc => doc.BulletList(l => l.Item(i => i.Paragraph(p => p.Text("quoted")))));
    }

    [Fact]
    public void Html_HeadingInListItem_BecomesParagraph()
    {
        AssertHtml(
            "<ul><li><h3>Item</h3></li></ul>",
            doc => doc.BulletList(l => l.Item(i => i.Paragraph(p => p.Text("Item")))));
    }

    [Fact]
    public void Html_RuleInBlockquote_IsDropped()
    {
        AssertHtml(
            "<blockquote><p>a</p><hr><p>b</p></blockquote>",
            doc => doc.Blockquote(q => q
                .Paragraph(p => p.Text("a"))
                .Paragraph(p => p.Text("b"))));
    }

    [Fact]
    public void Html_TableInListItem_IsUnwrappedIntoCellContent()
    {
        AssertHtml(
            "<ul><li><table><tr><td>a</td><td>b</td></tr></table></li></ul>",
            doc => doc.BulletList(l => l.Item(i => i
                .Paragraph(p => p.Text("a"))
                .Paragraph(p => p.Text("b")))));
    }

    [Fact]
    public void Html_CodeBlockInPanel_BecomesCodeMarkedParagraph()
    {
        AssertHtml(
            "<div data-panel-type=\"info\"><pre><code>line1\nline2\n</code></pre></div>",
            doc => doc.Panel("info", b => b.Paragraph(p => p
                .Text("line1", m => m.Code())
                .HardBreak()
                .Text("line2", m => m.Code()))));
    }

    [Fact]
    public void Html_EmptyList_IsDropped()
    {
        AssertHtml(
            "<p>a</p><ul></ul><p>b</p>",
            doc => doc
                .Paragraph(p => p.Text("a"))
                .Paragraph(p => p.Text("b")));
    }

    [Fact]
    public void Html_EmptyTable_IsDropped()
    {
        AssertHtml(
            "<p>a</p><table></table>",
            doc => doc.Paragraph(p => p.Text("a")));
    }

    [Fact]
    public void Html_OnlyAnEmptyList_LeavesOneEmptyParagraph()
    {
        AssertHtml("<ul></ul>", doc => doc.Paragraph(p => { }));
    }

    [Fact]
    public void Html_EmptyCodeBlock_HasNoEmptyTextNode()
    {
        AdfDocument actual = HtmlToAdfConverter.Convert("<pre></pre>");

        AssertValid(actual);
        Assert.Equal(AdfNodeType.CodeBlock, actual.Content![0].Type);
        Assert.Empty(actual.Content[0].Content!);
    }

    [Theory]
    [InlineData("<h1>a<ul><li>b</li></ul></h1>")]
    [InlineData("<p>a<table><tr><td>b</td></tr></table></p>")]
    [InlineData("<blockquote><div data-panel-type=\"info\"><h2>x</h2><pre>code</pre></div></blockquote>")]
    [InlineData("<ul><li><ul></ul></li><li></li></ul>")]
    [InlineData("<table><tr></tr><tr><td><blockquote><blockquote><h1>x</h1></blockquote></blockquote></td></tr></table>")]
    [InlineData("<div data-panel-type=\"note\"><table><tr><th>h</th></tr></table><hr><blockquote>q</blockquote></div>")]
    [InlineData("<ol><li><h1><code><b><a href=\"u\" style=\"color:red\">x</a></b></code></h1></li></ol>")]
    public void Html_MessyNesting_ProducesValidDocument(string html)
    {
        AssertValid(HtmlToAdfConverter.Convert(html));
    }

    [Theory]
    [InlineData("> - > # deep\n>   > > deeper")]
    [InlineData("- a\n\n  > b\n  >\n  > | x |\n  > |---|\n  > | y |")]
    [InlineData("> [!WARNING]\n> ```\n> code\n> ```\n> > quote")]
    [InlineData("***`x`*** ~~`y`~~ [**`z`**](u)")]
    public void Markdown_MessyNesting_ProducesValidDocument(string markdown)
    {
        AssertValid(MarkdownToAdfConverter.Convert(markdown));
    }

    // ---- Nesting: Markdown ----

    [Fact]
    public void Markdown_HeadingInQuote_BecomesParagraph()
    {
        AssertMarkdown(
            "> # Title",
            doc => doc.Blockquote(q => q.Paragraph(p => p.Text("Title"))));
    }

    [Fact]
    public void Markdown_NestedQuote_IsFlattenedIntoParent()
    {
        AssertMarkdown(
            "> outer\n>\n> > inner",
            doc => doc.Blockquote(q => q
                .Paragraph(p => p.Text("outer"))
                .Paragraph(p => p.Text("inner"))));
    }

    [Fact]
    public void Markdown_QuoteInListItem_IsUnwrapped()
    {
        AssertMarkdown(
            "- > quoted",
            doc => doc.BulletList(l => l.Item(i => i.Paragraph(p => p.Text("quoted")))));
    }

    [Fact]
    public void Markdown_BodylessAlert_IsDropped()
    {
        AssertMarkdown(
            "before\n\n> [!NOTE]\n\nafter",
            doc => doc
                .Paragraph(p => p.Text("before"))
                .Paragraph(p => p.Text("after")));
    }

    [Fact]
    public void Markdown_EmptyFencedCodeBlock_HasNoEmptyTextNode()
    {
        AdfDocument actual = MarkdownToAdfConverter.Convert("```\n```");

        AssertValid(actual);
        Assert.Equal(AdfNodeType.CodeBlock, actual.Content![0].Type);
        Assert.Empty(actual.Content[0].Content!);
    }

    // ---- Marks ----

    [Fact]
    public void Markdown_CodeInsideStrong_KeepsOnlyCode()
    {
        AssertMarkdown(
            "**bold `code`**",
            doc => doc.Paragraph(p => p
                .Text("bold ", m => m.Strong())
                .Text("code", m => m.Code())));
    }

    [Fact]
    public void Markdown_CodeInsideEmAndLink_KeepsCodeAndLink()
    {
        AssertMarkdown(
            "[*`code`*](https://example.com)",
            doc => doc.Paragraph(p => p.Text("code", m => m.Link("https://example.com").Code())));
    }

    [Fact]
    public void Html_CodeInsideStrong_KeepsOnlyCode()
    {
        AssertHtml(
            "<p><strong><code>x</code></strong></p>",
            doc => doc.Paragraph(p => p.Text("x", m => m.Code())));
    }

    [Fact]
    public void Html_ColoredLink_KeepsLinkAndDropsTextColor()
    {
        AssertHtml(
            "<p><a href=\"https://example.com\" style=\"color: red\">x</a></p>",
            doc => doc.Paragraph(p => p.Text("x", m => m.Link("https://example.com"))));
    }

    [Fact]
    public void Html_AnchorWithoutHref_DropsLinkMark()
    {
        AssertHtml(
            "<p><a>x</a> <strong><a name=\"n\">y</a></strong></p>",
            doc => doc.Paragraph(p => p.Text("x").Text(" ").Text("y", m => m.Strong())));
    }

    [Fact]
    public void Html_NestedSameTag_DoesNotRepeatMark()
    {
        AssertHtml(
            "<p><b><strong>x</strong></b></p>",
            doc => doc.Paragraph(p => p.Text("x", m => m.Strong())));
    }

    // ---- AdfNormalizer directly ----

    [Fact]
    public void Normalize_LooseInlineUnderDoc_IsWrappedInOneParagraph()
    {
        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>
        {
            AdfNode.CreateText("a"),
            AdfNode.CreateHardBreak(),
            AdfNode.CreateText("b"),
            AdfNode.CreateRule(),
            AdfNode.CreateText("c"),
        });

        AssertEquivalent(
            doc => doc
                .Paragraph(p => p.Text("a").HardBreak().Text("b"))
                .Rule()
                .Paragraph(p => p.Text("c")),
            document.Normalize());
    }

    [Fact]
    public void Normalize_ParagraphDirectlyInList_IsWrappedInListItem()
    {
        AdfNode list = AdfNode.CreateBulletList();
        list.AddContent(AdfNode.CreateParagraph(new List<AdfNode> { AdfNode.CreateText("x") }));
        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode> { list });

        AssertEquivalent(
            doc => doc.BulletList(l => l.Item(i => i.Paragraph(p => p.Text("x")))),
            document.Normalize());
    }

    [Fact]
    public void Normalize_EmptyListItem_GetsEmptyParagraph()
    {
        AdfNode list = AdfNode.CreateBulletList();
        list.AddContent(AdfNode.CreateListItem());
        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode> { list });

        AssertEquivalent(
            doc => doc.BulletList(l => l.Item(i => i.Paragraph(p => { }))),
            document.Normalize());
    }

    [Fact]
    public void Normalize_ValidDocument_IsUnchanged()
    {
        AdfDocument built = AdfDocumentBuilder.Build(doc => doc
            .Heading(1, "Title")
            .Blockquote(q => q.Paragraph(p => p.Text("q", m => m.Strong().Em())))
            .BulletList(l => l.Item(i => i.Paragraph(p => p.Text("i")).CodeBlock("x", "cs")))
            .Panel("info", b => b.Heading(2, "h"))
            .Table(t => t.Row(r => r.Cell(c => c.Blockquote(q => q.Paragraph(p => p.Text("c")))))));
        string before = AdfJsonConverter.Serialize(built);

        Assert.Equal(before, AdfJsonConverter.Serialize(built.Normalize()));
    }

    [Fact]
    public void Normalize_BackgroundColorWithCode_KeepsCode()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .Paragraph(p => p.Text("x", m => m.BackgroundColor("#fedec8").Code())));

        AssertEquivalent(
            doc => doc.Paragraph(p => p.Text("x", m => m.Code())),
            document.Normalize());
    }

    // ---- Helpers ----

    private static void AssertHtml(string html, Action<AdfBlockContentBuilder> expected)
    {
        AdfDocument actual = HtmlToAdfConverter.Convert(html);
        AssertEquivalent(expected, actual, $"HTML -> ADF for: {html}");
    }

    private static void AssertMarkdown(string markdown, Action<AdfBlockContentBuilder> expected)
    {
        AdfDocument actual = MarkdownToAdfConverter.Convert(markdown);
        AssertEquivalent(expected, actual, $"Markdown -> ADF for: {markdown}");
    }

    private static void AssertEquivalent(Action<AdfBlockContentBuilder> expected, AdfDocument actual, string context = "")
    {
        JToken actualJson = JToken.Parse(AdfJsonConverter.Serialize(actual));
        JToken expectedJson = JToken.Parse(AdfJsonConverter.Serialize(AdfDocumentBuilder.Build(expected)));

        Assert.True(
            JToken.DeepEquals(expectedJson, actualJson),
            $"{context}\nExpected: {expectedJson}\nActual:   {actualJson}");

        AssertValid(actual);
    }

    private static void AssertValid(AdfDocument document)
    {
        AdfValidationResult validation = document.Validate();
        Assert.True(validation.IsValid, string.Join("\n", validation.Errors));
    }
}
