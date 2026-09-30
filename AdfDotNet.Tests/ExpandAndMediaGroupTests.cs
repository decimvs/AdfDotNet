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
/// Covers the <c>expand</c>, <c>nestedExpand</c>, <c>mediaGroup</c> and <c>mediaInline</c> node types: document
/// model/builder, <c>Validate()</c> and JSON. The media types have no HTML/Markdown representation; the
/// converters only need to not throw on them. Expand conversion is in <see cref="ExpandConversionTests"/>.
/// </summary>
public class ExpandAndMediaGroupTests
{
    private const string MediaId = "6e7c7f2c-dd7a-499c-bceb-6f32bfbf30b5";
    private const string Collection = "ae730abd-a389-46a7-90eb-c03e75a45bf6";

    [Fact]
    public void Expand_DeserializesFromSpecJson()
    {
        string json = """
        {
          "version": 1,
          "type": "doc",
          "content": [
            {
              "type": "expand",
              "attrs": { "title": "Hello world" },
              "content": [ { "type": "paragraph", "content": [ { "type": "text", "text": "Hello world" } ] } ]
            }
          ]
        }
        """;

        AdfDocument document = AdfJsonConverter.Deserialize(json);

        AdfNode expand = Assert.Single(document.Content!);
        Assert.Equal(AdfNodeType.Expand, expand.Type);
        Assert.Equal("Hello world", expand.Attrs!["title"]);
        Assert.Equal(AdfNodeType.Paragraph, Assert.Single(expand.Content!).Type);

        Assert.True(document.Validate().IsValid);
        RoundTripAssert.JsonRoundTrip(document);
    }

    [Fact]
    public void NestedExpand_DeserializesFromSpecJson()
    {
        // The spec example, placed in a table cell - the only place a nestedExpand is legal.
        string json = """
        {
          "version": 1,
          "type": "doc",
          "content": [
            {
              "type": "table",
              "content": [
                {
                  "type": "tableRow",
                  "content": [
                    {
                      "type": "tableCell",
                      "content": [
                        {
                          "type": "nestedExpand",
                          "attrs": { "title": "Hello world" },
                          "content": [ { "type": "paragraph", "content": [ { "type": "text", "text": "Hello world" } ] } ]
                        }
                      ]
                    }
                  ]
                }
              ]
            }
          ]
        }
        """;

        AdfDocument document = AdfJsonConverter.Deserialize(json);

        AdfNode nestedExpand = document.Content![0].Content![0].Content![0].Content![0];
        Assert.Equal(AdfNodeType.NestedExpand, nestedExpand.Type);
        Assert.Equal("Hello world", nestedExpand.Attrs!["title"]);

        Assert.True(document.Validate().IsValid);
        RoundTripAssert.JsonRoundTrip(document);
    }

    [Fact]
    public void MediaGroup_DeserializesFromSpecJson()
    {
        string json = $$"""
        {
          "version": 1,
          "type": "doc",
          "content": [
            {
              "type": "mediaGroup",
              "content": [
                { "type": "media", "attrs": { "type": "file", "id": "{{MediaId}}", "collection": "{{Collection}}" } },
                { "type": "media", "attrs": { "type": "file", "id": "{{MediaId}}", "collection": "{{Collection}}" } }
              ]
            }
          ]
        }
        """;

        AdfDocument document = AdfJsonConverter.Deserialize(json);

        AdfNode mediaGroup = Assert.Single(document.Content!);
        Assert.Equal(AdfNodeType.MediaGroup, mediaGroup.Type);
        Assert.All(mediaGroup.Content!, media => Assert.Equal(AdfNodeType.Media, media.Type));
        Assert.Equal(2, mediaGroup.Content!.Count);

        Assert.True(document.Validate().IsValid);
        RoundTripAssert.JsonRoundTrip(document);
    }

    [Fact]
    public void MediaInline_DeserializesFromSchemaShapedJson()
    {
        // The mediaInline spec page returns 404; this shape follows the ADF JSON schema's mediaInline_node.
        string json = $$"""
        {
          "version": 1,
          "type": "doc",
          "content": [
            {
              "type": "paragraph",
              "content": [
                { "type": "text", "text": "See " },
                {
                  "type": "mediaInline",
                  "attrs": { "id": "{{MediaId}}", "collection": "{{Collection}}", "type": "file", "width": 200, "height": 100 },
                  "marks": [ { "type": "link", "attrs": { "href": "https://example.com" } } ]
                }
              ]
            }
          ]
        }
        """;

        AdfDocument document = AdfJsonConverter.Deserialize(json);

        AdfNode mediaInline = document.Content![0].Content![1];
        Assert.Equal(AdfNodeType.MediaInline, mediaInline.Type);
        Assert.Equal(MediaId, mediaInline.Attrs!["id"]);
        Assert.Equal(AdfMarkType.Link, Assert.Single(mediaInline.Marks!).Type);

        Assert.True(document.Validate().IsValid);
        RoundTripAssert.JsonRoundTrip(document);
    }

    [Fact]
    public void Builder_ProducesValidDocument()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .Expand("Details", e => e
                .Paragraph(p => p.Text("Inline ").MediaInline(MediaId, Collection, "file"))
                .MediaGroup(g => g.Media(MediaId, "file", Collection).Media(MediaId, "link", Collection))
                .Table(t => t.Row(r => r.Cell(c => c
                    .NestedExpand("More", n => n.Paragraph(p => p.Text("hidden")))))))
            .MediaGroup(g => g.Media(MediaId, "file", Collection)));

        AdfValidationResult result = document.Validate();
        Assert.True(result.IsValid, string.Join("\n", result.Errors));

        AdfNode expand = document.Content![0];
        Assert.Equal(AdfNodeType.Expand, expand.Type);
        Assert.Equal("Details", expand.Attrs!["title"]);
        Assert.Equal(AdfNodeType.MediaInline, expand.Content![0].Content![1].Type);
        Assert.Equal(2, expand.Content[1].Content!.Count);
        Assert.Equal(AdfNodeType.NestedExpand, expand.Content[2].Content![0].Content![0].Content![0].Type);

        RoundTripAssert.JsonRoundTrip(document);
    }

    [Fact]
    public void Expand_WithoutTitle_HasEmptyAttrsAndIsValid()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc.Expand(null, e => e.Paragraph(p => p.Text("x"))));

        Assert.Empty(document.Content![0].Attrs!);
        Assert.True(document.Validate().IsValid);
        RoundTripAssert.JsonRoundTrip(document);
    }

    [Theory]
    [InlineData(AdfNodeType.Expand)]
    [InlineData(AdfNodeType.NestedExpand)]
    public void Expand_WithoutAttrs_IsInvalid(AdfNodeType type)
    {
        AdfNode expand = new AdfNode(type) { Content = [AdfNode.CreateParagraph()] };
        AdfDocument document = type == AdfNodeType.Expand
            ? AdfNode.CreateDocument([expand])
            : AdfNode.CreateDocument([AdfNode.CreateTable([AdfNode.CreateTableRow([AdfNode.CreateTableCell([expand])])])]);

        string error = Assert.Single(document.Validate().Errors);
        Assert.Contains("must have an 'attrs' object", error);
    }

    [Fact]
    public void Expand_InsideTableCell_IsInvalid()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .Table(t => t.Row(r => r.Cell(c => c.Expand("x", e => e.Paragraph(p => p.Text("x")))))));

        string error = Assert.Single(document.Validate().Errors);
        Assert.Contains("'expand' is not a legal child of 'tableCell'", error);
    }

    [Fact]
    public void Expand_InsideExpand_IsInvalid()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .Expand("outer", e => e.Expand("inner", i => i.Paragraph(p => p.Text("x")))));

        string error = Assert.Single(document.Validate().Errors);
        Assert.Contains("'expand' is not a legal child of 'expand'", error);
    }

    [Fact]
    public void NestedExpand_AtDocumentLevel_IsInvalid()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .NestedExpand("x", n => n.Paragraph(p => p.Text("x"))));

        string error = Assert.Single(document.Validate().Errors);
        Assert.Contains("'nestedExpand' is not a legal child of 'doc'", error);
    }

    [Fact]
    public void NestedExpand_WithIllegalChild_IsInvalid()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .Table(t => t.Row(r => r.Header(c => c
                .NestedExpand("x", n => n.BulletList(l => l.Item(i => i.Paragraph(p => p.Text("x")))))))));

        string error = Assert.Single(document.Validate().Errors);
        Assert.Contains("'bulletList' is not a legal child of 'nestedExpand'", error);
    }

    [Fact]
    public void EmptyExpandAndMediaGroup_AreInvalid()
    {
        AdfDocument document = AdfNode.CreateDocument([AdfNode.CreateExpand("x"), AdfNode.CreateMediaGroup()]);

        IReadOnlyList<string> errors = document.Validate().Errors;
        Assert.Equal(2, errors.Count);
        Assert.All(errors, error => Assert.Contains("must have at least 1 child node(s)", error));
    }

    [Theory]
    [InlineData(AdfNodeType.Blockquote, true)]
    [InlineData(AdfNodeType.TableCell, true)]
    [InlineData(AdfNodeType.TableHeader, true)]
    [InlineData(AdfNodeType.NestedExpand, true)]
    [InlineData(AdfNodeType.Expand, true)]
    [InlineData(AdfNodeType.Doc, true)]
    [InlineData(AdfNodeType.ListItem, false)]
    [InlineData(AdfNodeType.Panel, false)]
    public void MediaGroup_Placement_FollowsSpecPages(AdfNodeType parent, bool isLegal)
    {
        Assert.Equal(isLegal, AdfNodeSchema.IsValidChildType(parent, AdfNodeType.MediaGroup));
    }

    [Fact]
    public void MediaGroup_WithNonMediaChild_IsInvalid()
    {
        AdfDocument document = AdfNode.CreateDocument([AdfNode.CreateMediaGroup([AdfNode.CreateParagraph()])]);

        string error = Assert.Single(document.Validate().Errors);
        Assert.Contains("'paragraph' is not a legal child of 'mediaGroup'", error);
    }

    [Fact]
    public void MediaInline_WithBadAttrsOrMarks_IsInvalid()
    {
        AdfNode mediaInline = AdfNode.CreateMediaInline("", Collection, "video");
        mediaInline.Attrs!.Remove("collection");
        mediaInline.Marks = [new AdfMark(AdfMarkType.Strong)];
        AdfDocument document = AdfNode.CreateDocument([AdfNode.CreateParagraph([mediaInline])]);

        IReadOnlyList<string> errors = document.Validate().Errors;
        Assert.Equal(4, errors.Count);
        Assert.Contains(errors, e => e.Contains("'id'"));
        Assert.Contains(errors, e => e.Contains("'collection' is required"));
        Assert.Contains(errors, e => e.Contains("'type'"));
        Assert.Contains(errors, e => e.Contains("mark 'strong' is not allowed on 'mediaInline'"));
    }

    [Fact]
    public void MediaInline_AtDocumentLevel_IsInvalid()
    {
        AdfDocument document = AdfNode.CreateDocument([AdfNode.CreateMediaInline(MediaId, Collection)]);

        string error = Assert.Single(document.Validate().Errors);
        Assert.Contains("'mediaInline' is not a legal child of 'doc'", error);
    }

    [Fact]
    public void Converters_DoNotThrowOnNewTypes()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .Paragraph(p => p.Text("inside").MediaInline(MediaId, Collection))
            .MediaGroup(g => g.Media(MediaId, "file", Collection)));

        // Media nodes have no HTML/Markdown representation: they render as nothing. (Expand conversion is
        // covered by ExpandConversionTests.)
        Assert.Equal("<p>inside</p>", AdfToHtmlConverter.Convert(document));
        Assert.Equal("inside", AdfToMarkdownConverter.Convert(document));
    }
}
