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
/// One document that uses every <see cref="AdfNodeType"/> and <see cref="AdfMarkType"/>, built via the fluent
/// builder: it must pass <c>Validate()</c>, round-trip losslessly through JSON, and come back from HTML and
/// Markdown as exactly the document each format can carry. Adding an enum member without
/// adding it here fails <see cref="KitchenSink_UsesEveryNodeAndMarkType"/>, and without classifying it for each
/// format fails <see cref="Formats_ClassifyEveryNodeAndMarkType"/>.
/// </summary>
public class KitchenSinkTests
{
    /// <summary>
    /// How a node or mark type fares in a round trip through one format, as it appears in the kitchen sink.
    /// </summary>
    public enum Fate
    {
        /// <summary>Comes back as itself.</summary>
        RoundTrips,
        /// <summary>Renders as something else (text, a link, its body), which is what comes back.</summary>
        OneWay,
        /// <summary>Renders as nothing.</summary>
        Dropped,
    }

    private static readonly Dictionary<AdfNodeType, Fate> HtmlNodeFates = new()
    {
        [AdfNodeType.Doc] = Fate.RoundTrips,
        [AdfNodeType.Paragraph] = Fate.RoundTrips,
        [AdfNodeType.Text] = Fate.RoundTrips,
        [AdfNodeType.Heading] = Fate.RoundTrips,
        [AdfNodeType.BulletList] = Fate.RoundTrips,
        [AdfNodeType.OrderedList] = Fate.RoundTrips,
        [AdfNodeType.ListItem] = Fate.RoundTrips,
        [AdfNodeType.Blockquote] = Fate.RoundTrips,
        [AdfNodeType.CodeBlock] = Fate.RoundTrips,
        [AdfNodeType.Rule] = Fate.RoundTrips,
        [AdfNodeType.HardBreak] = Fate.RoundTrips,
        [AdfNodeType.Table] = Fate.RoundTrips,
        [AdfNodeType.TableRow] = Fate.RoundTrips,
        [AdfNodeType.TableCell] = Fate.RoundTrips,
        [AdfNodeType.TableHeader] = Fate.RoundTrips,
        [AdfNodeType.Mention] = Fate.RoundTrips,
        [AdfNodeType.Date] = Fate.RoundTrips,
        [AdfNodeType.Status] = Fate.RoundTrips,
        [AdfNodeType.Panel] = Fate.RoundTrips,
        [AdfNodeType.Expand] = Fate.RoundTrips,
        [AdfNodeType.NestedExpand] = Fate.RoundTrips,
        [AdfNodeType.TaskList] = Fate.RoundTrips,
        [AdfNodeType.TaskItem] = Fate.RoundTrips,
        [AdfNodeType.BlockTaskItem] = Fate.RoundTrips,
        [AdfNodeType.InlineCard] = Fate.OneWay,
        [AdfNodeType.Emoji] = Fate.OneWay,
        [AdfNodeType.BodiedSyncBlock] = Fate.OneWay,
        [AdfNodeType.MultiBodiedExtension] = Fate.OneWay,
        [AdfNodeType.ExtensionFrame] = Fate.OneWay,
        [AdfNodeType.MediaSingle] = Fate.Dropped,
        [AdfNodeType.Media] = Fate.Dropped,
        [AdfNodeType.MediaGroup] = Fate.Dropped,
        [AdfNodeType.MediaInline] = Fate.Dropped,
        [AdfNodeType.SyncBlock] = Fate.Dropped,
    };

    private static readonly Dictionary<AdfMarkType, Fate> HtmlMarkFates = new()
    {
        [AdfMarkType.Strong] = Fate.RoundTrips,
        [AdfMarkType.Em] = Fate.RoundTrips,
        [AdfMarkType.Code] = Fate.RoundTrips,
        [AdfMarkType.Link] = Fate.RoundTrips,
        [AdfMarkType.Strike] = Fate.RoundTrips,
        [AdfMarkType.Underline] = Fate.RoundTrips,
        [AdfMarkType.SubSup] = Fate.RoundTrips,
        [AdfMarkType.TextColor] = Fate.RoundTrips,
        [AdfMarkType.BackgroundColor] = Fate.RoundTrips,
        [AdfMarkType.Border] = Fate.Dropped,
    };

    private static readonly Dictionary<AdfNodeType, Fate> MarkdownNodeFates = new(HtmlNodeFates)
    {
        // Plain text in Markdown.
        [AdfNodeType.Mention] = Fate.OneWay,
        [AdfNodeType.Date] = Fate.OneWay,
        [AdfNodeType.Status] = Fate.OneWay,
        // A bold title plus content in a pipe-table cell (a nestedExpand inside an expand does round-trip).
        [AdfNodeType.NestedExpand] = Fate.OneWay,
        // A one-paragraph blockTaskItem comes back as a taskItem.
        [AdfNodeType.BlockTaskItem] = Fate.OneWay,
    };

    private static readonly Dictionary<AdfMarkType, Fate> MarkdownMarkFates = new(HtmlMarkFates)
    {
        [AdfMarkType.Underline] = Fate.Dropped,
        [AdfMarkType.SubSup] = Fate.Dropped,
        [AdfMarkType.TextColor] = Fate.Dropped,
        [AdfMarkType.BackgroundColor] = Fate.Dropped,
    };

    private static AdfDocument BuildKitchenSink() => AdfDocumentBuilder.Build(doc => PlainBlocks(doc
        .Heading(1, "Kitchen sink")
        .Paragraph(p => p
            .Text("bold", m => m.Strong())
            .Text(" ")
            .Text("italic", m => m.Em())
            .Text(" ")
            .Text("code link", m => m.Code().Link("https://example.com"))
            .Text(" ")
            .Text("struck", m => m.Strike().Underline())
            .Text(" ")
            .Text("2", m => m.SubSup("sup"))
            .Text(" ")
            .Text("colored", m => m.TextColor("#ff5630").BackgroundColor("#fedec8"))
            .HardBreak()
            .InlineCard("https://example.com/card")
            .Emoji(":grinning:", "1f600", "😀")
            .Mention("account-1", "@Ada", "CONTAINER", "DEFAULT")
            .Date("1582152559")
            .Status("In Progress", "blue", "status-1")
            .MediaInline("inline-1", "collection-1", "file", 20, 20, m => m.Border(2, "#091e4224"))))
        .MediaSingle("media-1", "file", "collection-1", "center", 640, 480, m => m.Link("https://example.com/media").Border(1, "#091e4224"))
        .MediaGroup(g => g.Media("media-2", "file", "collection-1").Media("media-3", "link", "collection-1"))
        .Table(t => t
            .Row(r => r.Header(b => b.Paragraph(p => p.Text("header"))))
            .Row(r => r.Cell(b => b.NestedExpand("Nested", n => n.Paragraph(p => p.Text("nested expand"))))))
        .Expand("Expand", b => b
            .Paragraph(p => p.Text("expanded"))
            .ExtensionFrame(f => f.Paragraph(p => p.Text("frame in expand"))))
        .TaskList(t => t
            .Item(p => p.Text("todo"), localId: "task-1")
            .BlockItem(b => b.Paragraph(p => p.Text("done")), done: true, localId: "task-2")
            .List(n => n.Item(p => p.Text("nested task"), localId: "task-3"), localId: "tasks-2"), localId: "tasks-1")
        .SyncBlock("resource-1", "sync-1")
        .BodiedSyncBlock("resource-2", b => b.Paragraph(p => p.Text("synced")), "sync-2")
        .MultiBodiedExtension("tabs", "com.example.tabs", f => f
            .Frame(b => b.Paragraph(p => p.Text("tab one")))
            .Frame(b => b.Paragraph(p => p.Text("tab two")))));

    /// <summary>
    /// What the kitchen sink comes back as from HTML: media and <c>syncBlock</c> gone, the inline card as
    /// linked text, the emoji as its text, and sync-block/extension bodies unwrapped.
    /// </summary>
    private static AdfDocument ExpectedFromHtml() => AdfDocumentBuilder.Build(doc => PlainBlocks(doc
        .Heading(1, "Kitchen sink")
        .Paragraph(p => p
            .Text("bold", m => m.Strong())
            .Text(" ")
            .Text("italic", m => m.Em())
            .Text(" ")
            .Text("code link", m => m.Code().Link("https://example.com"))
            .Text(" ")
            .Text("struck", m => m.Strike().Underline())
            .Text(" ")
            .Text("2", m => m.SubSup("sup"))
            .Text(" ")
            .Text("colored", m => m.TextColor("#ff5630").BackgroundColor("#fedec8"))
            .HardBreak()
            .Text("https://example.com/card", m => m.Link("https://example.com/card"))
            .Text("😀")
            .Mention("account-1", "@Ada", "CONTAINER", "DEFAULT")
            .Date("1582152559")
            .Status("In Progress", "blue", "status-1")))
        .Table(t => t
            .Row(r => r.Header(b => b.Paragraph(p => p.Text("header"))))
            .Row(r => r.Cell(b => b.NestedExpand("Nested", n => n.Paragraph(p => p.Text("nested expand"))))))
        .Expand("Expand", b => b
            .Paragraph(p => p.Text("expanded"))
            .Paragraph(p => p.Text("frame in expand")))
        .TaskList(t => t
            .Item(p => p.Text("todo"), localId: "task-1")
            .BlockItem(b => b.Paragraph(p => p.Text("done")), done: true, localId: "task-2")
            .List(n => n.Item(p => p.Text("nested task"), localId: "task-3"), localId: "tasks-2"), localId: "tasks-1")
        .Paragraph(p => p.Text("synced"))
        .Paragraph(p => p.Text("tab one"))
        .Paragraph(p => p.Text("tab two")));

    /// <summary>
    /// What the kitchen sink comes back as from Markdown: as from HTML, plus underline/subsup/color marks gone,
    /// mention/date/status as text, the nested expand as a bold title line, and the one-paragraph
    /// <c>blockTaskItem</c> as a <c>taskItem</c>. Markdig nests the link outside the code span, so that text's
    /// marks come back in the other order. <c>localId</c>s are regenerated, so they aren't compared.
    /// </summary>
    private static AdfDocument ExpectedFromMarkdown() => AdfDocumentBuilder.Build(doc => PlainBlocks(doc
        .Heading(1, "Kitchen sink")
        .Paragraph(p => p
            .Text("bold", m => m.Strong())
            .Text(" ")
            .Text("italic", m => m.Em())
            .Text(" ")
            .Text("code link", m => m.Link("https://example.com").Code())
            .Text(" ")
            .Text("struck", m => m.Strike())
            .Text(" 2 colored")
            .HardBreak()
            .Text("https://example.com/card", m => m.Link("https://example.com/card"))
            .Text("😀@Ada2020-02-19In Progress")))
        .Table(t => t
            .Row(r => r.Header(b => b.Paragraph(p => p.Text("header"))))
            .Row(r => r.Cell(b => b.Paragraph(p => p.Text("Nested", m => m.Strong()).HardBreak().Text("nested expand")))))
        .Expand("Expand", b => b
            .Paragraph(p => p.Text("expanded"))
            .Paragraph(p => p.Text("frame in expand")))
        .TaskList(t => t
            .Item(p => p.Text("todo"))
            .Item(p => p.Text("done"), done: true)
            .List(n => n.Item(p => p.Text("nested task"))))
        .Paragraph(p => p.Text("synced"))
        .Paragraph(p => p.Text("tab one"))
        .Paragraph(p => p.Text("tab two")));

    /// <summary>
    /// The blocks every format carries unchanged, shared by the kitchen sink and its expected round trips.
    /// </summary>
    private static AdfBlockContentBuilder PlainBlocks(AdfBlockContentBuilder doc) => doc
        .BulletList(l => l
            .Item(b => b.Paragraph(p => p.Text("bullet")))
            .Item(b => b
                .Paragraph(p => p.Text("nested"))
                .OrderedList(o => o.Item(i => i.Paragraph(p => p.Text("ordered"))))))
        .Blockquote(b => b.Paragraph(p => p.Text("quoted")))
        .Panel("info", b => b.Paragraph(p => p.Text("panel")))
        .CodeBlock("var x = 1;", "csharp")
        .Rule();

    [Fact]
    public void KitchenSink_UsesEveryNodeAndMarkType()
    {
        AdfDocument document = BuildKitchenSink();

        Assert.Superset(AllNodeTypes(), NodeTypes(document));
        Assert.Superset(AllMarkTypes(), MarkTypes(document));
    }

    [Fact]
    public void KitchenSink_IsValid()
    {
        AdfValidationResult result = BuildKitchenSink().Validate();

        Assert.True(result.IsValid, string.Join("\n", result.Errors));
    }

    [Fact]
    public void KitchenSink_JsonRoundTripsLosslessly()
    {
        AdfDocument document = BuildKitchenSink();

        RoundTripAssert.JsonRoundTrip(document);

        AdfValidationResult result = AdfJsonConverter.Deserialize(AdfJsonConverter.Serialize(document)).Validate();
        Assert.True(result.IsValid, string.Join("\n", result.Errors));
    }

    [Fact]
    public void Formats_ClassifyEveryNodeAndMarkType()
    {
        Assert.Equal(AllNodeTypes(), HtmlNodeFates.Keys.ToHashSet());
        Assert.Equal(AllMarkTypes(), HtmlMarkFates.Keys.ToHashSet());
        Assert.Equal(AllNodeTypes(), MarkdownNodeFates.Keys.ToHashSet());
        Assert.Equal(AllMarkTypes(), MarkdownMarkFates.Keys.ToHashSet());
    }

    [Fact]
    public void KitchenSink_HtmlRoundTrip_KeepsExactlyWhatHtmlCarries()
    {
        AdfDocument roundTripped = HtmlToAdfConverter.Convert(AdfToHtmlConverter.Convert(BuildKitchenSink()));

        AssertFates(roundTripped, HtmlNodeFates, HtmlMarkFates);
        AssertSameJson(ExpectedFromHtml(), roundTripped, ignoreLocalIds: false);
    }

    [Fact]
    public void KitchenSink_MarkdownRoundTrip_KeepsExactlyWhatMarkdownCarries()
    {
        AdfDocument roundTripped = MarkdownToAdfConverter.Convert(AdfToMarkdownConverter.Convert(BuildKitchenSink()));

        AssertFates(roundTripped, MarkdownNodeFates, MarkdownMarkFates);
        AssertSameJson(ExpectedFromMarkdown(), roundTripped, ignoreLocalIds: true);
    }

    /// <summary>
    /// Asserts that a round-tripped kitchen sink is valid, contains every type classified as round-tripping,
    /// and none classified as one-way or dropped.
    /// </summary>
    private static void AssertFates(AdfDocument roundTripped, Dictionary<AdfNodeType, Fate> nodeFates, Dictionary<AdfMarkType, Fate> markFates)
    {
        AdfValidationResult result = roundTripped.Validate();
        Assert.True(result.IsValid, string.Join("\n", result.Errors));

        Assert.Equal(TypesWith(nodeFates, Fate.RoundTrips), NodeTypes(roundTripped));
        Assert.Equal(TypesWith(markFates, Fate.RoundTrips), MarkTypes(roundTripped));
    }

    private static void AssertSameJson(AdfDocument expected, AdfDocument actual, bool ignoreLocalIds)
    {
        JObject expectedJson = JObject.Parse(AdfJsonConverter.Serialize(expected));
        JObject actualJson = JObject.Parse(AdfJsonConverter.Serialize(actual));
        if (ignoreLocalIds)
        {
            RemoveLocalIds(expectedJson);
            RemoveLocalIds(actualJson);
        }
        Assert.True(JToken.DeepEquals(expectedJson, actualJson), $"Round trip mismatch.\nExpected: {expectedJson}\nActual:   {actualJson}");
    }

    private static void RemoveLocalIds(JContainer json)
    {
        foreach (JProperty property in json.Descendants().OfType<JProperty>().Where(p => p.Name == "localId").ToList())
            property.Remove();
    }

    private static HashSet<T> TypesWith<T>(Dictionary<T, Fate> fates, Fate fate) where T : notnull =>
        fates.Where(f => f.Value == fate).Select(f => f.Key).ToHashSet();

    private static HashSet<AdfNodeType> AllNodeTypes() => Enum.GetValues(typeof(AdfNodeType)).Cast<AdfNodeType>().ToHashSet();

    private static HashSet<AdfMarkType> AllMarkTypes() => Enum.GetValues(typeof(AdfMarkType)).Cast<AdfMarkType>().ToHashSet();

    private static HashSet<AdfNodeType> NodeTypes(AdfNode document) => Descendants(document).Select(n => n.Type).ToHashSet();

    private static HashSet<AdfMarkType> MarkTypes(AdfNode document) =>
        Descendants(document).SelectMany(n => n.Marks ?? []).Select(m => m.Type).ToHashSet();

    private static IEnumerable<AdfNode> Descendants(AdfNode node)
    {
        yield return node;

        foreach (AdfNode child in node.Content ?? [])
        {
            foreach (AdfNode descendant in Descendants(child))
                yield return descendant;
        }
    }
}
