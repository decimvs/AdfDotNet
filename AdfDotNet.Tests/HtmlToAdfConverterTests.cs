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
/// HTML -> ADF cases for hand-written HTML shapes that <see cref="AdfToHtmlConverter"/> never emits itself
/// (inline CSS, &lt;thead&gt;/&lt;tbody&gt;, &lt;div&gt;, alternate code-block class names, ...), so the
/// round-trip suites can't reach them.
/// </summary>
public class HtmlToAdfConverterTests
{
    [Theory]
    [InlineData("font-weight: bold")]
    [InlineData("FONT-WEIGHT:bold;")]
    public void CssFontWeightBold_BecomesStrong(string style)
    {
        AssertConverts(
            $"<p><span style=\"{style}\">text</span></p>",
            doc => doc.Paragraph(p => p.Text("text", m => m.Strong())));
    }

    [Fact]
    public void CssFontStyleItalic_BecomesEm()
    {
        AssertConverts(
            "<p><span style=\"font-style: italic\">text</span></p>",
            doc => doc.Paragraph(p => p.Text("text", m => m.Em())));
    }

    [Fact]
    public void CssTextDecorationUnderline_BecomesUnderline()
    {
        AssertConverts(
            "<p><span style=\"text-decoration: underline\">text</span></p>",
            doc => doc.Paragraph(p => p.Text("text", m => m.Underline())));
    }

    [Fact]
    public void CssTextDecorationLineThrough_BecomesStrike()
    {
        AssertConverts(
            "<p><span style=\"text-decoration: line-through\">text</span></p>",
            doc => doc.Paragraph(p => p.Text("text", m => m.Strike())));
    }

    [Theory]
    [InlineData("color: red", "#FF0000")]
    [InlineData("color: #00ff00", "#00ff00")]
    [InlineData("color: abc", "#abc")]
    public void CssColor_BecomesTextColor(string style, string expectedColor)
    {
        AssertConverts(
            $"<p><span style=\"{style}\">text</span></p>",
            doc => doc.Paragraph(p => p.Text("text", m => m.TextColor(expectedColor))));
    }

    [Fact]
    public void CssColor_Unrecognized_IsIgnored()
    {
        AssertConverts(
            "<p><span style=\"color: rgb(1, 2, 3)\">text</span></p>",
            doc => doc.Paragraph(p => p.Text("text")));
    }

    [Fact]
    public void CssUnrelatedProperties_AreIgnored()
    {
        AssertConverts(
            "<p><span style=\"margin: 0; font-weight: normal\">text</span></p>",
            doc => doc.Paragraph(p => p.Text("text")));
    }

    [Fact]
    public void CodeMark_StripsMarksOtherThanLink()
    {
        AssertConverts(
            "<p><a href=\"https://example.com/\"><strong><code>text</code></strong></a></p>",
            doc => doc.Paragraph(p => p.Text("text", m => m.Code().Link("https://example.com/"))));
    }

    [Fact]
    public void NestedTags_CollectMarksInnermostFirst()
    {
        AssertConverts(
            "<p><strong><em>text</em></strong></p>",
            doc => doc.Paragraph(p => p.Text("text", m => m.Em().Strong())));
    }

    [Theory]
    [InlineData("language-csharp")]
    [InlineData("lang-csharp")]
    [InlineData("hljs language-csharp")]
    public void CodeBlock_ReadsLanguageFromCodeClass(string cssClass)
    {
        AssertConverts(
            $"<pre><code class=\"{cssClass}\">var x = 1;</code></pre>",
            doc => doc.CodeBlock("var x = 1;", "csharp"));
    }

    [Fact]
    public void CodeBlock_WithoutCodeElement_UsesPreText()
    {
        AssertConverts(
            "<pre>var x = 1;</pre>",
            doc => doc.CodeBlock("var x = 1;"));
    }

    [Fact]
    public void Table_RowsInsideTheadTbodyTfoot_AreFlattened()
    {
        AssertConverts(
            "<table>" +
                "<thead><tr><th>H</th></tr></thead>" +
                "<tbody><tr><td>B</td></tr></tbody>" +
                "<tfoot><tr><td>F</td></tr></tfoot>" +
            "</table>",
            doc => doc.Table(t => t
                .Row(r => r.Header(c => c.Paragraph(p => p.Text("H"))))
                .Row(r => r.Cell(c => c.Paragraph(p => p.Text("B"))))
                .Row(r => r.Cell(c => c.Paragraph(p => p.Text("F"))))));
    }

    [Fact]
    public void Table_EmptyCell_GetsEmptyParagraph()
    {
        AssertConverts(
            "<table><tr><td>   </td></tr></table>",
            doc => doc.Table(t => t.Row(r => r.Cell(c => c.Paragraph(p => { })))));
    }

    [Fact]
    public void Div_WithInlineContent_BecomesParagraph()
    {
        AssertConverts(
            "<div>plain <strong>bold</strong></div>",
            doc => doc.Paragraph(p => p.Text("plain ").Text("bold", m => m.Strong())));
    }

    [Fact]
    public void Div_WithBlockChildren_IsFlattenedNotDropped()
    {
        AssertConverts(
            "<div><h2>Title</h2><p>Body</p><ul><li>item</li></ul></div>",
            doc => doc
                .Heading(2, "Title")
                .Paragraph(p => p.Text("Body"))
                .BulletList(l => l.Item(i => i.Paragraph(p => p.Text("item")))));
    }

    [Fact]
    public void NestedWrappers_AreFlattened()
    {
        AssertConverts(
            "<main><section><article><div><div><p>deep</p></div></div></article></section></main>",
            doc => doc.Paragraph(p => p.Text("deep")));
    }

    [Fact]
    public void Div_WithMixedInlineAndBlockContent_SplitsIntoParagraphs()
    {
        AssertConverts(
            "<div>before <strong>bold</strong><p>middle</p>after</div>",
            doc => doc
                .Paragraph(p => p.Text("before ").Text("bold", m => m.Strong()))
                .Paragraph(p => p.Text("middle"))
                .Paragraph(p => p.Text("after")));
    }

    [Fact]
    public void Div_WithHr_SplitsAroundRule()
    {
        AssertConverts(
            "<div>one<hr>two</div>",
            doc => doc
                .Paragraph(p => p.Text("one"))
                .Rule()
                .Paragraph(p => p.Text("two")));
    }

    [Fact]
    public void LooseInlineContentInBody_IsGroupedIntoOneParagraph()
    {
        AssertConverts(
            "<body>plain <em>italic</em> <a href=\"https://example.com/\">link</a></body>",
            doc => doc.Paragraph(p => p
                .Text("plain ")
                .Text("italic", m => m.Em())
                .Text(" ")
                .Text("link", m => m.Link("https://example.com/"))));
    }

    [Fact]
    public void ListItem_WithInlineTextAndNestedList_WrapsTextInParagraph()
    {
        AssertConverts(
            "<ul><li><strong>Parent</strong> item<ul><li>child</li></ul></li></ul>",
            doc => doc.BulletList(l => l.Item(i => i
                .Paragraph(p => p.Text("Parent", m => m.Strong()).Text(" item"))
                .BulletList(inner => inner.Item(c => c.Paragraph(p => p.Text("child")))))));
    }

    [Fact]
    public void Blockquote_WithDivWrapper_KeepsContent()
    {
        AssertConverts(
            "<blockquote><div><p>quoted</p></div></blockquote>",
            doc => doc.Blockquote(b => b.Paragraph(p => p.Text("quoted"))));
    }

    [Fact]
    public void EmptyListItem_GetsEmptyParagraph()
    {
        AssertConverts(
            "<ul><li></li></ul>",
            doc => doc.BulletList(l => l.Item(i => i.Paragraph(p => { }))));
    }

    [Fact]
    public void WhitespaceBetweenInlineElements_IsKeptAsWordSeparator()
    {
        AssertConverts(
            "<p>\n  <em>one</em> <strong>two</strong>\n</p>",
            doc => doc.Paragraph(p => p.Text("one", m => m.Em()).Text(" ").Text("two", m => m.Strong())));
    }

    [Fact]
    public void Paragraph_WithBr_BecomesHardBreak()
    {
        AssertConverts(
            "<p>one<br>two</p>",
            doc => doc.Paragraph(p => p.Text("one").HardBreak().Text("two")));
    }

    [Theory]
    [InlineData("<hr>")]
    [InlineData("<hr/>")]
    [InlineData("<HR />")]
    public void Hr_BecomesRule(string hr)
    {
        AssertConverts(hr, doc => doc.Rule());
    }

    [Fact]
    public void HeadAndScript_AreDropped()
    {
        AssertConverts(
            "<html><head><title>T</title><style>p{}</style></head><body><script>x()</script><p>text</p></body></html>",
            doc => doc.Paragraph(p => p.Text("text")));
    }

    [Fact]
    public void EmptyInput_YieldsSingleEmptyParagraph()
    {
        AssertConverts("   ", doc => doc.Paragraph(p => { }));
    }

    [Theory]
    [InlineData("&#128512;")]
    [InlineData("&#x1F600;")]
    [InlineData("😀")]
    public void CharacterOutsideTheBmp_Decodes(string encoded)
    {
        // HtmlAgilityPack's DeEntitize turned &#128512; into "&##128512;"; WebUtility.HtmlDecode doesn't.
        AssertConverts($"<p>smile {encoded} &amp; &lt;done&gt;&nbsp;!</p>", doc => doc.Paragraph(p => p.Text("smile 😀 & <done> !")));
    }

    [Fact]
    public void CharacterOutsideTheBmp_RoundTrips()
    {
        // WebUtility.HtmlEncode writes 😀 as &#128512; on .NET Core.
        RoundTripAssert.FullRoundTrip(AdfDocumentBuilder.Build(doc => doc
            .Paragraph(p => p.Text("smile 😀", m => m.Strong()))));
    }

    private static void AssertConverts(string html, Action<AdfBlockContentBuilder> expected)
    {
        AdfDocument actual = HtmlToAdfConverter.Convert(html);
        JToken actualJson = JToken.Parse(AdfJsonConverter.Serialize(actual));
        JToken expectedJson = JToken.Parse(AdfJsonConverter.Serialize(AdfDocumentBuilder.Build(expected)));

        Assert.True(
            JToken.DeepEquals(expectedJson, actualJson),
            $"HTML -> ADF mismatch for: {html}\nExpected: {expectedJson}\nActual:   {actualJson}");

        AdfValidationResult validation = actual.Validate();
        Assert.True(validation.IsValid, string.Join("\n", validation.Errors));
    }
}
