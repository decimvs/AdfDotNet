// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Builders;
using AdfDotNet.FormatConverters;
using AdfDotNet.Models;
using Xunit;

namespace AdfDotNet.Tests;

/// <summary>
/// Covers the types with no HTML/Markdown representation:<c>media</c>, <c>mediaSingle</c>,
/// <c>mediaGroup</c>, <c>mediaInline</c> and the <c>border</c> mark (Media Services identifiers) and
/// <c>syncBlock</c> (a resource reference) render as nothing; <c>bodiedSyncBlock</c>,
/// <c>multiBodiedExtension</c> and <c>extensionFrame</c> render their body transparently, one-way. Each test
/// compares against the same document with those nodes removed or unwrapped by hand, so the output must be
/// exactly what the rest of the document gives on its own, in every legal placement.
/// </summary>
public class UnrepresentableTypesTests
{
    [Fact]
    public void MediaNodes_RenderAsNothing()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .Paragraph(p => p.Text("a").MediaInline("m-1", "c", "file", marks: m => m.Border(2, "#172b4d")).Text("b"))
            .MediaSingle("m-2", "file", "c", marks: m => m.Border(1, "#172b4d"))
            .MediaGroup(g => g.Media("m-3", "file", "c").Media("m-4", "file", "c"))
            .Heading(2, h => h.MediaInline("m-5", "c").Text("title")));
        AdfDocument expected = AdfDocumentBuilder.Build(doc => doc
            .Paragraph(p => p.Text("a").Text("b"))
            .Heading(2, h => h.Text("title")));

        AssertRendersLike(expected, document);
        Assert.Equal("<p>ab</p><h2>title</h2>", AdfToHtmlConverter.Convert(document));
        Assert.Equal("ab\n\n## title", AdfToMarkdownConverter.Convert(document));
    }

    [Fact]
    public void MediaNodes_InsideContainers_RenderAsNothing()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .Blockquote(q => q.MediaGroup(g => g.Media("m-1", "file", "c")).Paragraph(p => p.Text("quote")))
            .BulletList(l => l.Item(i => i.Paragraph(p => p.Text("item")).MediaSingle("m-2", "file", "c")))
            .Table(t => t.Row(r => r
                .Cell(c => c.MediaGroup(g => g.Media("m-3", "file", "c")).Paragraph(p => p.Text("cell")))
                .Cell(c => c.NestedExpand("more", n => n.MediaSingle("m-4", "file", "c").Paragraph(p => p.Text("nested"))))))
            .Expand("title", e => e.MediaSingle("m-5", "file", "c").Paragraph(p => p.Text("expand"))));
        AdfDocument expected = AdfDocumentBuilder.Build(doc => doc
            .Blockquote(q => q.Paragraph(p => p.Text("quote")))
            .BulletList(l => l.Item(i => i.Paragraph(p => p.Text("item"))))
            .Table(t => t.Row(r => r
                .Cell(c => c.Paragraph(p => p.Text("cell")))
                .Cell(c => c.NestedExpand("more", n => n.Paragraph(p => p.Text("nested"))))))
            .Expand("title", e => e.Paragraph(p => p.Text("expand"))));

        AssertRendersLike(expected, document);
    }

    [Fact]
    public void SyncBlock_RendersAsNothing_AndBodiedSyncBlockRendersItsBody()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .Paragraph(p => p.Text("before"))
            .SyncBlock("res-1")
            .BodiedSyncBlock("res-2", b => b
                .Heading(2, h => h.Text("Synced"))
                .BulletList(l => l.Item(i => i.Paragraph(p => p.Text("item"))))
                .TaskList(t => t.Item(p => p.Text("task"), localId: "task-1"), localId: "list-1"))
            .Paragraph(p => p.Text("after")));
        AdfDocument expected = AdfDocumentBuilder.Build(doc => doc
            .Paragraph(p => p.Text("before"))
            .Heading(2, h => h.Text("Synced"))
            .BulletList(l => l.Item(i => i.Paragraph(p => p.Text("item"))))
            .TaskList(t => t.Item(p => p.Text("task"), localId: "task-1"), localId: "list-1")
            .Paragraph(p => p.Text("after")));

        AssertRendersLike(expected, document);
    }

    [Fact]
    public void Extensions_RenderEveryFrameBody()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .MultiBodiedExtension("tabs", "com.example.tabs", f => f
                .Frame(b => b.Paragraph(p => p.Text("one")))
                .Frame(b => b.CodeBlock("two", "csharp")))
            .Expand("title", e => e.ExtensionFrame(b => b.Paragraph(p => p.Text("framed")))));
        AdfDocument expected = AdfDocumentBuilder.Build(doc => doc
            .Paragraph(p => p.Text("one"))
            .CodeBlock("two", "csharp")
            .Expand("title", e => e.Paragraph(p => p.Text("framed"))));

        AssertRendersLike(expected, document);
    }

    [Fact]
    public void EmptyMultiBodiedExtension_RendersAsNothing()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .Paragraph(p => p.Text("x"))
            .MultiBodiedExtension("tabs", "com.example.tabs", _ => { }));

        Assert.Equal("<p>x</p>", AdfToHtmlConverter.Convert(document));
        Assert.Equal("x", AdfToMarkdownConverter.Convert(document));
    }

    /// <summary>
    /// Asserts that <paramref name="document"/> is valid and renders to exactly the HTML and Markdown of
    /// <paramref name="expected"/>, and that both outputs parse back to a valid document.
    /// </summary>
    private static void AssertRendersLike(AdfDocument expected, AdfDocument document)
    {
        AdfValidationResult result = document.Validate();
        Assert.True(result.IsValid, string.Join("\n", result.Errors));

        string html = AdfToHtmlConverter.Convert(document);
        string markdown = AdfToMarkdownConverter.Convert(document);
        Assert.Equal(AdfToHtmlConverter.Convert(expected), html);
        Assert.Equal(AdfToMarkdownConverter.Convert(expected), markdown);

        AdfValidationResult fromHtml = HtmlToAdfConverter.Convert(html).Validate();
        Assert.True(fromHtml.IsValid, string.Join("\n", fromHtml.Errors));
        AdfValidationResult fromMarkdown = MarkdownToAdfConverter.Convert(markdown).Validate();
        Assert.True(fromMarkdown.IsValid, string.Join("\n", fromMarkdown.Errors));
    }
}
