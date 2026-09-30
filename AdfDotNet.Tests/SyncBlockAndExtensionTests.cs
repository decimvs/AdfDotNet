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
/// Covers <c>syncBlock</c>, <c>bodiedSyncBlock</c>, <c>multiBodiedExtension</c> and <c>extensionFrame</c>:
/// document model/builder, <c>Validate()</c> and JSON. Their spec pages return 404, so the shapes follow the
/// ADF JSON schema (the extension types only its stage-0 variant). They carry Jira-only identifiers and have no
/// HTML/Markdown representation.
/// </summary>
public class SyncBlockAndExtensionTests
{
    [Fact]
    public void SyncBlocks_DeserializeFromSchemaShapedJson()
    {
        string json = """
        {
          "version": 1,
          "type": "doc",
          "content": [
            { "type": "syncBlock", "attrs": { "resourceId": "res-1", "localId": "sync-1" } },
            {
              "type": "bodiedSyncBlock",
              "attrs": { "resourceId": "res-2", "localId": "sync-2" },
              "content": [ { "type": "paragraph", "content": [ { "type": "text", "text": "Synced" } ] } ]
            }
          ]
        }
        """;

        AdfDocument document = AdfJsonConverter.Deserialize(json);

        Assert.Equal(new[] { AdfNodeType.SyncBlock, AdfNodeType.BodiedSyncBlock }, document.Content!.Select(n => n.Type));
        Assert.Equal("res-2", document.Content![1].Attrs!["resourceId"]);

        AdfValidationResult result = document.Validate();
        Assert.True(result.IsValid, string.Join("\n", result.Errors));
        RoundTripAssert.JsonRoundTrip(document);
    }

    [Fact]
    public void MultiBodiedExtension_DeserializesFromSchemaShapedJson()
    {
        string json = """
        {
          "version": 1,
          "type": "doc",
          "content": [
            {
              "type": "multiBodiedExtension",
              "attrs": {
                "extensionKey": "tabs",
                "extensionType": "com.example.tabs",
                "parameters": { "activeTab": 0, "labels": [ "One", "Two" ] },
                "layout": "wide"
              },
              "content": [
                { "type": "extensionFrame", "content": [ { "type": "paragraph", "content": [ { "type": "text", "text": "Tab one" } ] } ] },
                { "type": "extensionFrame", "content": [ { "type": "heading", "attrs": { "level": 2 }, "content": [ { "type": "text", "text": "Tab two" } ] } ] }
              ]
            }
          ]
        }
        """;

        AdfDocument document = AdfJsonConverter.Deserialize(json);

        AdfNode extension = Assert.Single(document.Content!);
        Assert.Equal(AdfNodeType.MultiBodiedExtension, extension.Type);
        Assert.All(extension.Content!, frame => Assert.Equal(AdfNodeType.ExtensionFrame, frame.Type));
        Dictionary<string, object> parameters = Assert.IsType<Dictionary<string, object>>(extension.Attrs!["parameters"]);
        Assert.Equal(2, Assert.IsType<List<object>>(parameters["labels"]).Count);

        AdfValidationResult result = document.Validate();
        Assert.True(result.IsValid, string.Join("\n", result.Errors));
        RoundTripAssert.JsonRoundTrip(document);
    }

    [Fact]
    public void Builder_ProducesValidDocument()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .SyncBlock("res-1")
            .BodiedSyncBlock("res-2", b => b
                .Paragraph(p => p.Text("Synced"))
                .Expand("More", e => e.Paragraph(p => p.Text("x"))))
            .MultiBodiedExtension("tabs", "com.example.tabs", f => f
                .Frame(b => b.Paragraph(p => p.Text("One")))
                .Frame(b => b.TaskList(t => t.Item(p => p.Text("Two")))))
            .Expand("Frames", e => e.ExtensionFrame(b => b.Paragraph(p => p.Text("In an expand")))));

        AdfValidationResult result = document.Validate();
        Assert.True(result.IsValid, string.Join("\n", result.Errors));

        Assert.True(Guid.TryParse((string)document.Content![0].Attrs!["localId"], out _));
        Assert.Equal(2, document.Content[2].Content!.Count);

        RoundTripAssert.JsonRoundTrip(document);
    }

    [Fact]
    public void SyncBlocks_WithoutRequiredAttrs_AreInvalid()
    {
        AdfNode syncBlock = AdfNode.CreateSyncBlock("res-1");
        syncBlock.Attrs!.Remove("localId");
        AdfNode bodied = AdfNode.CreateBodiedSyncBlock("res-2", [AdfNode.CreateParagraph()]);
        bodied.Attrs!.Remove("resourceId");
        AdfDocument document = AdfNode.CreateDocument([syncBlock, bodied]);

        IReadOnlyList<string> errors = document.Validate().Errors;
        Assert.Equal(2, errors.Count);
        Assert.Contains(errors, e => e.Contains("'localId' is required on 'syncBlock'"));
        Assert.Contains(errors, e => e.Contains("'resourceId' is required on 'bodiedSyncBlock'"));
    }

    [Fact]
    public void SyncBlock_CannotHaveContent_AndBodiedSyncBlockCannotBeEmpty()
    {
        AdfNode syncBlock = AdfNode.CreateSyncBlock("res-1");
        syncBlock.AddContent(AdfNode.CreateParagraph());
        AdfDocument document = AdfNode.CreateDocument([syncBlock, AdfNode.CreateBodiedSyncBlock("res-2")]);

        IReadOnlyList<string> errors = document.Validate().Errors;
        Assert.Equal(2, errors.Count);
        Assert.Contains(errors, e => e.Contains("'syncBlock' cannot have child content"));
        Assert.Contains(errors, e => e.Contains("'bodiedSyncBlock' must have at least 1 child node(s)"));
    }

    [Theory]
    [InlineData(AdfNodeType.SyncBlock)]
    [InlineData(AdfNodeType.BodiedSyncBlock)]
    [InlineData(AdfNodeType.MultiBodiedExtension)]
    public void TopLevelOnlyTypes_InsideAPanel_AreInvalid(AdfNodeType type)
    {
        Assert.True(AdfNodeSchema.IsValidChildType(AdfNodeType.Doc, type));
        Assert.False(AdfNodeSchema.IsValidChildType(AdfNodeType.Panel, type));
        Assert.False(AdfNodeSchema.IsValidChildType(AdfNodeType.TableCell, type));
    }

    [Fact]
    public void ExtensionFrame_Placement_FollowsSpecAndSchema()
    {
        Assert.True(AdfNodeSchema.IsValidChildType(AdfNodeType.MultiBodiedExtension, AdfNodeType.ExtensionFrame));
        Assert.True(AdfNodeSchema.IsValidChildType(AdfNodeType.Expand, AdfNodeType.ExtensionFrame));
        Assert.True(AdfNodeSchema.IsValidChildType(AdfNodeType.Expand, AdfNodeType.MultiBodiedExtension));
        Assert.False(AdfNodeSchema.IsValidChildType(AdfNodeType.Doc, AdfNodeType.ExtensionFrame));
        Assert.False(AdfNodeSchema.IsValidChildType(AdfNodeType.MultiBodiedExtension, AdfNodeType.Paragraph));
    }

    [Fact]
    public void MultiBodiedExtension_WithBadAttrs_IsInvalid()
    {
        AdfNode extension = AdfNode.CreateMultiBodiedExtension("", "com.example.tabs", layout: "narrow");
        extension.Attrs!.Remove("extensionType");
        AdfDocument document = AdfNode.CreateDocument([extension]);

        IReadOnlyList<string> errors = document.Validate().Errors;
        Assert.Equal(3, errors.Count);
        Assert.Contains(errors, e => e.Contains("'extensionKey'"));
        Assert.Contains(errors, e => e.Contains("'extensionType' is required"));
        Assert.Contains(errors, e => e.Contains("'layout'"));
    }

    [Fact]
    public void EmptyMultiBodiedExtension_IsValid_ButEmptyFrameIsNot()
    {
        AdfDocument empty = AdfNode.CreateDocument([AdfNode.CreateMultiBodiedExtension("tabs", "com.example.tabs")]);
        AdfDocument emptyFrame = AdfNode.CreateDocument(
            [AdfNode.CreateMultiBodiedExtension("tabs", "com.example.tabs", [AdfNode.CreateExtensionFrame()])]);

        Assert.True(empty.Validate().IsValid);
        Assert.Contains("'extensionFrame' must have at least 1 child node(s)", Assert.Single(emptyFrame.Validate().Errors));
    }

    [Fact]
    public void Converters_DoNotThrowOnNewTypes()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .SyncBlock("res-1")
            .BodiedSyncBlock("res-2", b => b.Paragraph(p => p.Text("synced")))
            .MultiBodiedExtension("tabs", "com.example.tabs", f => f.Frame(b => b.Paragraph(p => p.Text("frame")))));

        // No HTML/Markdown representation: bodies render transparently, syncBlock as nothing.
        Assert.Equal("<p>synced</p><p>frame</p>", AdfToHtmlConverter.Convert(document));
        Assert.Equal("synced\n\nframe", AdfToMarkdownConverter.Convert(document));
    }
}
