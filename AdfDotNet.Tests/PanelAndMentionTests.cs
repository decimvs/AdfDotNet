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
/// Covers the <c>panel</c> and <c>mention</c> node types end to end: document model/builder, <c>Validate()</c>,
/// JSON, HTML (<c>&lt;div data-panel-type&gt;</c>/<c>&lt;span data-mention-id&gt;</c>) and Markdown
/// (GitHub-flavored alerts for panels; mentions render one-way as plain <c>@Name</c> text).
/// </summary>
public class PanelAndMentionTests
{
    [Theory]
    [InlineData("info")]
    [InlineData("note")]
    [InlineData("warning")]
    [InlineData("success")]
    [InlineData("error")]
    public void Panel_HtmlRoundTrips(string panelType)
    {
        RoundTripAssert.FullRoundTrip(PanelDocument(panelType));
    }

    [Theory]
    [InlineData("info")]
    [InlineData("note")]
    [InlineData("warning")]
    [InlineData("success")]
    [InlineData("error")]
    public void Panel_MarkdownRoundTrips(string panelType)
    {
        MarkdownRoundTripAssert.FullRoundTrip(PanelDocument(panelType));
    }

    [Theory]
    [InlineData("info", "NOTE")]
    [InlineData("note", "IMPORTANT")]
    [InlineData("success", "TIP")]
    [InlineData("warning", "WARNING")]
    [InlineData("error", "CAUTION")]
    public void Panel_RendersAsGitHubAlert(string panelType, string alertKind)
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc.Panel(panelType, b => b.Paragraph(p => p.Text("Body"))));

        Assert.Equal($"> [!{alertKind}]\n> Body", AdfToMarkdownConverter.Convert(document));
    }

    [Fact]
    public void Panel_InTableCell_HtmlRoundTrips()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .Table(t => t.Row(r => r.Cell(c => c.Panel("warning", b => b.Paragraph(p => p.Text("careful")))))));

        Assert.True(document.Validate().IsValid);
        RoundTripAssert.FullRoundTrip(document);
    }

    [Fact]
    public void Mention_HtmlRoundTrips()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .Paragraph(p => p
                .Text("Hi ")
                .Mention("5b10ac8d82e05b22cc7d4ef5", "@Jane Doe", accessLevel: "CONTAINER", userType: "DEFAULT")
                .Text(", please review.")));

        RoundTripAssert.FullRoundTrip(document);
    }

    [Fact]
    public void Mention_RendersAsHtmlSpan()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .Paragraph(p => p.Mention("abc", "@Jane & Co", userType: "APP")));

        Assert.Equal(
            "<p><span data-mention-id=\"abc\" data-user-type=\"APP\">@Jane &amp; Co</span></p>",
            AdfToHtmlConverter.Convert(document));
    }

    [Fact]
    public void Mention_RendersAsPlainTextInMarkdown()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .Paragraph(p => p.Text("Hi ").Mention("abc", "@Jane")));

        Assert.Equal("Hi @Jane", AdfToMarkdownConverter.Convert(document));
    }

    [Fact]
    public void Mention_WithoutText_FallsBackToIdInOutput()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc.Paragraph(p => p.Mention("abc")));

        Assert.Equal("@abc", AdfToMarkdownConverter.Convert(document));
        Assert.Equal("<p><span data-mention-id=\"abc\">@abc</span></p>", AdfToHtmlConverter.Convert(document));
    }

    [Fact]
    public void PanelAndMention_DeserializeFromJiraJson()
    {
        string json = """
        {
          "version": 1,
          "type": "doc",
          "content": [
            {
              "type": "panel",
              "attrs": { "panelType": "info" },
              "content": [
                {
                  "type": "paragraph",
                  "content": [
                    { "type": "text", "text": "Ping " },
                    { "type": "mention", "attrs": { "id": "ABCDE-ABCDE", "text": "@Bradley Ayers", "userType": "APP" } }
                  ]
                }
              ]
            }
          ]
        }
        """;

        AdfDocument document = AdfJsonConverter.Deserialize(json);

        AdfNode panel = Assert.Single(document.Content!);
        Assert.Equal(AdfNodeType.Panel, panel.Type);
        Assert.Equal("info", panel.Attrs!["panelType"]);

        AdfNode mention = panel.Content!.Single().Content![1];
        Assert.Equal(AdfNodeType.Mention, mention.Type);
        Assert.Equal("ABCDE-ABCDE", mention.Attrs!["id"]);
        Assert.Equal("@Bradley Ayers", mention.Attrs["text"]);

        Assert.True(document.Validate().IsValid);
        RoundTripAssert.JsonRoundTrip(document);
    }

    [Fact]
    public void Panel_WithCodeBlock_IsInvalid()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc.Panel("info", b => b.CodeBlock("x")));

        AdfValidationResult result = document.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("'codeBlock' is not a legal child of 'panel'"));
    }

    [Fact]
    public void Panel_InListItem_IsInvalid()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .BulletList(l => l.Item(i => i.Panel("info", b => b.Paragraph(p => p.Text("x"))))));

        Assert.False(document.Validate().IsValid);
    }

    [Fact]
    public void Mention_OmitsUnsetOptionalAttrs()
    {
        AdfNode mention = AdfNode.CreateMention("abc");

        Assert.Equal(new Dictionary<string, object> { ["id"] = "abc" }, mention.Attrs);
    }

    private static AdfDocument PanelDocument(string panelType)
    {
        return AdfDocumentBuilder.Build(doc => doc
            .Panel(panelType, b => b
                .Heading(3, "Heads up")
                .Paragraph(p => p.Text("Panel ").Text("body", m => m.Strong()))
                .BulletList(l => l
                    .Item(i => i.Paragraph(p => p.Text("one")))
                    .Item(i => i.Paragraph(p => p.Text("two"))))));
    }
}
