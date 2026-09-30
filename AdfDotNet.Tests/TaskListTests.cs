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
/// Covers <c>taskList</c>, <c>taskItem</c> and <c>blockTaskItem</c>: document model/builder, <c>Validate()</c>
/// and JSON. <c>blockTaskItem</c> is on the spec page; <c>taskList</c>/<c>taskItem</c> come from the ADF JSON
/// schema, as <c>blockTaskItem</c>'s only parent. HTML/Markdown conversion is in <see cref="TaskListConversionTests"/>.
/// </summary>
public class TaskListTests
{
    [Fact]
    public void TaskList_DeserializesFromSchemaShapedJson()
    {
        string json = """
        {
          "version": 1,
          "type": "doc",
          "content": [
            {
              "type": "taskList",
              "attrs": { "localId": "list-1" },
              "content": [
                { "type": "taskItem", "attrs": { "localId": "item-1", "state": "TODO" }, "content": [ { "type": "text", "text": "Write tests" } ] },
                { "type": "taskItem", "attrs": { "localId": "item-2", "state": "DONE" } },
                {
                  "type": "blockTaskItem",
                  "attrs": { "localId": "item-3", "state": "DONE" },
                  "content": [ { "type": "paragraph", "content": [ { "type": "text", "text": "Ship it" } ] } ]
                },
                {
                  "type": "taskList",
                  "attrs": { "localId": "list-2" },
                  "content": [ { "type": "taskItem", "attrs": { "localId": "item-4", "state": "TODO" }, "content": [ { "type": "text", "text": "Nested" } ] } ]
                }
              ]
            }
          ]
        }
        """;

        AdfDocument document = AdfJsonConverter.Deserialize(json);

        AdfNode taskList = Assert.Single(document.Content!);
        Assert.Equal(AdfNodeType.TaskList, taskList.Type);
        Assert.Equal(
            new[] { AdfNodeType.TaskItem, AdfNodeType.TaskItem, AdfNodeType.BlockTaskItem, AdfNodeType.TaskList },
            taskList.Content!.Select(n => n.Type));
        Assert.Equal("DONE", taskList.Content![2].Attrs!["state"]);

        AdfValidationResult result = document.Validate();
        Assert.True(result.IsValid, string.Join("\n", result.Errors));
        RoundTripAssert.JsonRoundTrip(document);
    }

    [Fact]
    public void Builder_ProducesValidDocument()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .TaskList(t => t
                .Item(p => p.Text("Open"))
                .Item(p => p.Text("Closed"), done: true, localId: "fixed-id")
                .BlockItem(b => b.Paragraph(p => p.Text("Block")).Paragraph(p => p.Text("Details")))
                .List(n => n.Item(p => p.Text("Nested")))));

        AdfValidationResult result = document.Validate();
        Assert.True(result.IsValid, string.Join("\n", result.Errors));

        List<AdfNode> items = document.Content![0].Content!;
        Assert.Equal("TODO", items[0].Attrs!["state"]);
        Assert.Equal("DONE", items[1].Attrs!["state"]);
        Assert.Equal("fixed-id", items[1].Attrs!["localId"]);
        Assert.True(Guid.TryParse((string)items[0].Attrs!["localId"], out _));
        Assert.NotEqual(items[0].Attrs!["localId"], items[2].Attrs!["localId"]);
        Assert.Equal(AdfNodeType.BlockTaskItem, items[2].Type);
        Assert.Equal(AdfNodeType.TaskList, items[3].Type);

        RoundTripAssert.JsonRoundTrip(document);
    }

    [Theory]
    [InlineData(AdfNodeType.Doc, true)]
    [InlineData(AdfNodeType.ListItem, true)]
    [InlineData(AdfNodeType.Panel, true)]
    [InlineData(AdfNodeType.TableCell, true)]
    [InlineData(AdfNodeType.TableHeader, true)]
    [InlineData(AdfNodeType.Expand, true)]
    [InlineData(AdfNodeType.NestedExpand, true)]
    [InlineData(AdfNodeType.BodiedSyncBlock, true)]
    [InlineData(AdfNodeType.ExtensionFrame, true)]
    [InlineData(AdfNodeType.TaskList, true)]
    [InlineData(AdfNodeType.Blockquote, false)]
    [InlineData(AdfNodeType.Paragraph, false)]
    public void TaskList_Placement_FollowsJsonSchema(AdfNodeType parent, bool isLegal)
    {
        Assert.Equal(isLegal, AdfNodeSchema.IsValidChildType(parent, AdfNodeType.TaskList));
    }

    [Theory]
    [InlineData(AdfNodeType.TaskItem)]
    [InlineData(AdfNodeType.BlockTaskItem)]
    public void TaskItems_OutsideTaskList_AreInvalid(AdfNodeType type)
    {
        AdfNode item = type == AdfNodeType.TaskItem
            ? AdfNode.CreateTaskItem(content: [AdfNode.CreateText("x")])
            : AdfNode.CreateBlockTaskItem(content: [AdfNode.CreateParagraph()]);
        AdfDocument document = AdfNode.CreateDocument([item]);

        string error = Assert.Single(document.Validate().Errors);
        Assert.Contains("is not a legal child of 'doc'", error);
    }

    [Fact]
    public void TaskItem_WithBadAttrs_IsInvalid()
    {
        AdfNode item = AdfNode.CreateTaskItem("done");
        item.Attrs!.Remove("localId");
        AdfDocument document = AdfNode.CreateDocument([AdfNode.CreateTaskList([item])]);

        IReadOnlyList<string> errors = document.Validate().Errors;
        Assert.Equal(2, errors.Count);
        Assert.Contains(errors, e => e.Contains("'localId' is required on 'taskItem'"));
        Assert.Contains(errors, e => e.Contains("'state' on 'taskItem' must be \"TODO\" or \"DONE\""));
    }

    [Fact]
    public void TaskList_WithoutLocalIdOrItems_IsInvalid()
    {
        AdfNode taskList = AdfNode.CreateTaskList();
        taskList.Attrs!.Remove("localId");
        AdfDocument document = AdfNode.CreateDocument([taskList]);

        IReadOnlyList<string> errors = document.Validate().Errors;
        Assert.Equal(2, errors.Count);
        Assert.Contains(errors, e => e.Contains("'localId' is required on 'taskList'"));
        Assert.Contains(errors, e => e.Contains("must have at least 1 child node(s)"));
    }

    [Fact]
    public void BlockTaskItem_TakesOneOrTwoParagraphsOnly()
    {
        AdfDocument tooMany = AdfDocumentBuilder.Build(doc => doc.TaskList(t => t
            .BlockItem(b => b.Paragraph(p => p.Text("1")).Paragraph(p => p.Text("2")).Paragraph(p => p.Text("3")))));
        AdfDocument wrongChild = AdfDocumentBuilder.Build(doc => doc.TaskList(t => t
            .BlockItem(b => b.Heading(1, "x"))));

        Assert.Contains("must have at most 2 child node(s)", Assert.Single(tooMany.Validate().Errors));
        Assert.Contains("'heading' is not a legal child of 'blockTaskItem'", Assert.Single(wrongChild.Validate().Errors));
    }

    [Fact]
    public void TaskItem_TakesInlineContentOnly()
    {
        AdfNode item = AdfNode.CreateTaskItem(content: [AdfNode.CreateParagraph()]);
        AdfDocument document = AdfNode.CreateDocument([AdfNode.CreateTaskList([item])]);

        Assert.Contains("'paragraph' is not a legal child of 'taskItem'", Assert.Single(document.Validate().Errors));
    }
}
