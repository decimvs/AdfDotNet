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
/// Covers HTML and Markdown conversion of <c>taskList</c>/<c>taskItem</c>/<c>blockTaskItem</c>:
/// HTML renders a <c>&lt;ul data-adf-type="taskList"&gt;</c> of checkbox items and keeps every attr; Markdown uses
/// GFM task lists (<c>- [ ]</c>/<c>- [x]</c>), which can't carry <c>localId</c>, so parsing generates new ones.
/// </summary>
public class TaskListConversionTests
{
    private static AdfDocument TaskListDocument() => AdfDocumentBuilder.Build(doc => doc
        .Paragraph(p => p.Text("before"))
        .TaskList(t => t
            .Item(p => p.Text("Open ").Text("bold", m => m.Strong()), localId: "item-1")
            .Item(p => p.Text("Closed"), done: true, localId: "item-2")
            .List(n => n
                .Item(p => p.Text("Nested"), localId: "item-3")
                .Item(p => p.Text("Nested done"), done: true, localId: "item-4"), localId: "list-2")
            .BlockItem(b => b.Paragraph(p => p.Text("Block")).Paragraph(p => p.Text("Details")), done: true, localId: "item-5"),
            localId: "list-1")
        .Paragraph(p => p.Text("after")));

    [Fact]
    public void TaskList_HtmlRoundTrips()
    {
        AdfDocument document = TaskListDocument();
        Assert.True(document.Validate().IsValid);

        RoundTripAssert.FullRoundTrip(document);
    }

    [Fact]
    public void TaskList_RendersAsCheckboxList()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc.TaskList(t => t
            .Item(p => p.Text("a"), localId: "i1")
            .BlockItem(b => b.Paragraph(p => p.Text("b")), done: true, localId: "i2"), localId: "l1"));

        Assert.Equal(
            "<ul data-adf-type=\"taskList\" data-local-id=\"l1\">" +
            "<li data-task-state=\"TODO\" data-local-id=\"i1\"><input type=\"checkbox\" disabled/>a</li>" +
            "<li data-adf-type=\"blockTaskItem\" data-task-state=\"DONE\" data-local-id=\"i2\"><input type=\"checkbox\" checked disabled/><p>b</p></li>" +
            "</ul>",
            AdfToHtmlConverter.Convert(document));
    }

    [Fact]
    public void TaskList_InListItem_HtmlRoundTrips()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .BulletList(l => l.Item(i => i
                .Paragraph(p => p.Text("parent"))
                .TaskList(t => t.Item(p => p.Text("child"), localId: "i1"), localId: "l1"))));
        Assert.True(document.Validate().IsValid);

        RoundTripAssert.FullRoundTrip(document);
    }

    [Fact]
    public void GfmRenderedHtml_ParsesAsTaskList()
    {
        // The shape Markdig and GitHub render, with a nested task list inside an item.
        AdfDocument document = HtmlToAdfConverter.Convert(
            "<ul class=\"contains-task-list\">\n" +
            "<li class=\"task-list-item\"><input disabled=\"disabled\" type=\"checkbox\" /> Open\n" +
            "<ul class=\"contains-task-list\"><li class=\"task-list-item\"><input disabled=\"disabled\" type=\"checkbox\" checked=\"checked\" /> Inner</li></ul></li>\n" +
            "<li class=\"task-list-item\"><p><input type=\"checkbox\" checked> Loose</p></li>\n" +
            "</ul>");

        AdfValidationResult result = document.Validate();
        Assert.True(result.IsValid, string.Join("\n", result.Errors));

        AdfNode taskList = Assert.Single(document.Content!);
        Assert.Equal(AdfNodeType.TaskList, taskList.Type);
        Assert.True(Guid.TryParse((string)taskList.Attrs!["localId"], out _));
        Assert.Equal(new[] { AdfNodeType.TaskItem, AdfNodeType.TaskList, AdfNodeType.TaskItem }, taskList.Content!.Select(n => n.Type));

        Assert.Equal("TODO", taskList.Content![0].Attrs!["state"]);
        Assert.Equal("Open", Assert.Single(taskList.Content[0].Content!).Text);

        AdfNode inner = Assert.Single(taskList.Content[1].Content!);
        Assert.Equal("DONE", inner.Attrs!["state"]);
        Assert.Equal("Inner", Assert.Single(inner.Content!).Text);

        Assert.Equal("DONE", taskList.Content[2].Attrs!["state"]);
        Assert.Equal("Loose", Assert.Single(taskList.Content[2].Content!).Text);
    }

    [Fact]
    public void HtmlList_WithSomeItemsWithoutCheckbox_StaysBulletList()
    {
        AdfDocument document = HtmlToAdfConverter.Convert("<ul><li><input type=\"checkbox\"> a</li><li>b</li></ul>");

        Assert.Equal(AdfNodeType.BulletList, Assert.Single(document.Content!).Type);
        Assert.True(document.Validate().IsValid);
    }

    [Fact]
    public void HtmlBlockTaskItem_WithTooManyParagraphs_KeepsTwo()
    {
        AdfDocument document = HtmlToAdfConverter.Convert(
            "<ul data-adf-type=\"taskList\"><li data-adf-type=\"blockTaskItem\" data-task-state=\"TODO\"><p>1</p><p>2</p><p>3</p></li></ul>");

        AdfValidationResult result = document.Validate();
        Assert.True(result.IsValid, string.Join("\n", result.Errors));

        AdfNode item = Assert.Single(document.Content![0].Content!);
        Assert.Equal(2, item.Content!.Count);
        Assert.Equal(new[] { AdfNodeType.Text, AdfNodeType.HardBreak, AdfNodeType.Text }, item.Content[1].Content!.Select(n => n.Type));
    }

    [Fact]
    public void EmptyTaskItem_HtmlRoundTrips()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc.TaskList(t => t.Item(p => { }, localId: "i1"), localId: "l1"));
        Assert.True(document.Validate().IsValid);

        RoundTripAssert.FullRoundTrip(document);
    }

    [Fact]
    public void TaskList_MarkdownRoundTrips_ExceptLocalIds()
    {
        MarkdownRoundTripIgnoringLocalIds(TaskListDocument());
    }

    [Fact]
    public void TaskList_RendersAsGfmTaskList()
    {
        string markdown = AdfToMarkdownConverter.Convert(TaskListDocument());

        Assert.Equal(
            "before\n\n" +
            "- [ ] Open **bold**\n" +
            "- [x] Closed\n" +
            "  - [ ] Nested\n" +
            "  - [x] Nested done\n" +
            "- [x] Block\n\n" +
            "  Details\n\n" +
            "after",
            markdown);
    }

    [Fact]
    public void MarkdownTaskList_GetsNewLocalIds()
    {
        AdfDocument document = MarkdownToAdfConverter.Convert("- [ ] a\n- [X] b");

        AdfValidationResult result = document.Validate();
        Assert.True(result.IsValid, string.Join("\n", result.Errors));

        AdfNode taskList = Assert.Single(document.Content!);
        Assert.Equal(new[] { "TODO", "DONE" }, taskList.Content!.Select(n => (string)n.Attrs!["state"]));
        Assert.Equal("a", Assert.Single(taskList.Content![0].Content!).Text);
        Assert.True(Guid.TryParse((string)taskList.Content[0].Attrs!["localId"], out _));
        Assert.NotEqual(taskList.Content[0].Attrs!["localId"], taskList.Content[1].Attrs!["localId"]);
    }

    [Fact]
    public void SingleParagraphBlockTaskItem_ComesBackFromMarkdownAsTaskItem()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc.TaskList(t => t.BlockItem(b => b.Paragraph(p => p.Text("x")))));

        AdfDocument roundTripped = MarkdownToAdfConverter.Convert(AdfToMarkdownConverter.Convert(document));

        AdfNode item = Assert.Single(roundTripped.Content![0].Content!);
        Assert.Equal(AdfNodeType.TaskItem, item.Type);
        Assert.Equal("x", Assert.Single(item.Content!).Text);
    }

    [Fact]
    public void MarkdownTaskItem_WithMoreBlocks_BecomesValidBlockTaskItem()
    {
        AdfDocument document = MarkdownToAdfConverter.Convert("- [x] one\n\n  two\n\n  ```\n  three\n  ```\n\n  four");

        AdfValidationResult result = document.Validate();
        Assert.True(result.IsValid, string.Join("\n", result.Errors));

        AdfNode item = Assert.Single(document.Content![0].Content!);
        Assert.Equal(AdfNodeType.BlockTaskItem, item.Type);
        Assert.Equal("DONE", item.Attrs!["state"]);
        Assert.Equal(2, item.Content!.Count);
        Assert.Equal("one", Assert.Single(item.Content[0].Content!).Text);
    }

    [Fact]
    public void MarkdownList_MixingTaskAndPlainItems_StaysBulletList()
    {
        AdfDocument document = MarkdownToAdfConverter.Convert("- [ ] a\n- b");

        Assert.Equal(AdfNodeType.BulletList, Assert.Single(document.Content!).Type);
        Assert.True(document.Validate().IsValid);
    }

    [Fact]
    public void TaskList_InListItem_MarkdownRoundTrips_ExceptLocalIds()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc
            .BulletList(l => l.Item(i => i
                .Paragraph(p => p.Text("parent"))
                .TaskList(t => t.Item(p => p.Text("child")).Item(p => p.Text("done"), done: true)))));

        MarkdownRoundTripIgnoringLocalIds(document);
    }

    [Fact]
    public void EmptyTaskItem_MarkdownRoundTrips_ExceptLocalIds()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc.TaskList(t => t.Item(p => { }).Item(p => p.Text("x"))));

        MarkdownRoundTripIgnoringLocalIds(document);
    }

    private static void MarkdownRoundTripIgnoringLocalIds(AdfDocument document)
    {
        string markdown = AdfToMarkdownConverter.Convert(document);
        AdfDocument roundTripped = MarkdownToAdfConverter.Convert(markdown);

        AdfValidationResult result = roundTripped.Validate();
        Assert.True(result.IsValid, string.Join("\n", result.Errors));

        JToken original = WithoutLocalIds(document);
        JToken roundTrippedJson = WithoutLocalIds(roundTripped);
        Assert.True(
            JToken.DeepEquals(original, roundTrippedJson),
            $"ADF roundtrip mismatch.\nMarkdown:     {markdown}\nOriginal:     {original}\nRoundtripped: {roundTrippedJson}");
    }

    private static JToken WithoutLocalIds(AdfDocument document)
    {
        JContainer json = (JContainer)JToken.Parse(AdfJsonConverter.Serialize(document));
        foreach (JProperty localId in json.Descendants().OfType<JProperty>().Where(p => p.Name == "localId").ToList())
            localId.Remove();
        return json;
    }
}
