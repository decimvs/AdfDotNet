// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Builders;
using AdfDotNet.FormatConverters;
using AdfDotNet.Models;
using Xunit;

namespace AdfDotNet.Tests;

/// <summary>
/// Covers the attrs of already-converted node and mark types that HTML and Markdown can carry:
/// <c>orderedList.order</c> (HTML <c>start</c>, Markdown start number), the <c>link</c> mark's <c>title</c> (both
/// formats), and the table/cell attrs (HTML only: native <c>colspan</c>/<c>rowspan</c>, a <c>background-color</c>
/// style, and <c>data-*</c> for the rest).
/// </summary>
public class AttributeFidelityTests
{
    private static AdfDocument OrderedListDocument(int order) => AdfDocumentBuilder.Build(doc => doc
        .OrderedList(l => l
            .Item(i => i.Paragraph(p => p.Text("third")))
            .Item(i => i.Paragraph(p => p.Text("fourth"))), order));

    private static AdfDocument LinkDocument(string title) => AdfDocumentBuilder.Build(doc => doc
        .Paragraph(p => p.Text("Atlassian", m => m.Link("http://atlassian.com", title))));

    private static AdfNode Cell(bool header, Dictionary<string, object>? attrs, string text)
    {
        List<AdfNode> content = new List<AdfNode>() { AdfNode.CreateParagraph(new List<AdfNode>() { AdfNode.CreateText(text) }) };
        AdfNode cell = header ? AdfNode.CreateTableHeader(content) : AdfNode.CreateTableCell(content);
        cell.Attrs = attrs;
        return cell;
    }

    private static AdfDocument TableDocument()
    {
        AdfNode table = AdfNode.CreateTable(new List<AdfNode>()
        {
            AdfNode.CreateTableRow(new List<AdfNode>()
            {
                Cell(true, new Dictionary<string, object>() { ["colspan"] = 2, ["background"] = "#deebff", ["colwidth"] = new List<object>() { 150, 200.5 } }, "Wide header"),
            }),
            AdfNode.CreateTableRow(new List<AdfNode>()
            {
                Cell(false, new Dictionary<string, object>() { ["rowspan"] = 2, ["background"] = "red" }, "Tall"),
                Cell(false, new Dictionary<string, object>() { ["colwidth"] = new List<object>() { 0 } }, "Plain"),
            }),
            AdfNode.CreateTableRow(new List<AdfNode>()
            {
                Cell(false, null, "Last"),
            }),
        });
        table.Attrs = new Dictionary<string, object>()
        {
            ["isNumberColumnEnabled"] = true,
            ["layout"] = "align-start",
            ["width"] = 900,
            ["displayMode"] = "fixed",
        };

        AdfDocument document = AdfNode.CreateDocument();
        document.AddContent(table);
        return document;
    }

    [Theory]
    [InlineData(3)]
    [InlineData(0)]
    [InlineData(1)]
    public void OrderedList_Order_HtmlRoundTrips(int order)
    {
        AdfDocument document = OrderedListDocument(order);
        Assert.True(document.Validate().IsValid);

        Assert.StartsWith($"<ol start=\"{order}\">", AdfToHtmlConverter.Convert(document));
        RoundTripAssert.FullRoundTrip(document);
    }

    [Fact]
    public void OrderedList_WithoutOrder_HasNoStart()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc.OrderedList(l => l.Item(i => i.Paragraph(p => p.Text("a")))));

        Assert.Equal("<ol><li><p>a</p></li></ol>", AdfToHtmlConverter.Convert(document));
        Assert.Null(HtmlToAdfConverter.Convert("<ol><li>a</li></ol>").Content![0].Attrs);
    }

    [Theory]
    [InlineData("<ol start=\"-2\"><li>a</li></ol>")]
    [InlineData("<ol start=\"x\"><li>a</li></ol>")]
    public void OrderedList_StartAdfCantCarry_IsIgnored(string html)
    {
        AdfDocument document = HtmlToAdfConverter.Convert(html);

        Assert.Null(document.Content![0].Attrs);
        Assert.True(document.Validate().IsValid);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(0)]
    public void OrderedList_Order_MarkdownRoundTrips(int order)
    {
        AdfDocument document = OrderedListDocument(order);

        Assert.StartsWith($"{order}. third\n{order + 1}. fourth", AdfToMarkdownConverter.Convert(document));
        MarkdownRoundTripAssert.FullRoundTrip(document);
    }

    [Fact]
    public void OrderedList_StartingAtOne_ParsesFromMarkdownWithoutOrder()
    {
        AdfDocument document = MarkdownToAdfConverter.Convert("1. a\n2. b");

        Assert.Null(document.Content![0].Attrs);
    }

    [Fact]
    public void Link_Title_HtmlRoundTrips()
    {
        AdfDocument document = LinkDocument("Atlassian \"home\" & more");
        Assert.True(document.Validate().IsValid);

        Assert.Equal(
            "<p><a href=\"http://atlassian.com\" title=\"Atlassian &quot;home&quot; &amp; more\">Atlassian</a></p>",
            AdfToHtmlConverter.Convert(document));
        RoundTripAssert.FullRoundTrip(document);
    }

    [Fact]
    public void Link_Title_MarkdownRoundTrips()
    {
        AdfDocument document = LinkDocument("Say \"hi\" \\ bye");

        Assert.Equal("[Atlassian](<http://atlassian.com> \"Say \\\"hi\\\" \\\\ bye\")", AdfToMarkdownConverter.Convert(document));
        MarkdownRoundTripAssert.FullRoundTrip(document);
    }

    [Fact]
    public void Link_Title_ParsesFromHandWrittenMarkdown()
    {
        AdfDocument document = MarkdownToAdfConverter.Convert("[a](http://x.com 'T')");

        AdfMark link = document.Content![0].Content![0].Marks![0];
        Assert.Equal("http://x.com", link.Attrs!["href"]);
        Assert.Equal("T", link.Attrs["title"]);
    }

    [Fact]
    public void Table_Attrs_HtmlRoundTrip()
    {
        AdfDocument document = TableDocument();
        Assert.True(document.Validate().IsValid, string.Join("\n", document.Validate().Errors));

        string html = AdfToHtmlConverter.Convert(document);
        Assert.StartsWith("<table data-layout=\"align-start\" data-width=\"900\" data-display-mode=\"fixed\" data-number-column-enabled=\"true\">", html);
        Assert.Contains("<th colspan=\"2\" style=\"background-color: #deebff\" data-colwidth=\"150,200.5\">", html);
        Assert.Contains("<td rowspan=\"2\" style=\"background-color: red\">", html);
        RoundTripAssert.FullRoundTrip(document);
    }

    [Fact]
    public void TableCell_BackgroundStyle_IsCellAttrNotTextMark()
    {
        AdfDocument document = HtmlToAdfConverter.Convert(
            "<table><tr><td style=\"background: #fff\">a</td><td bgcolor=\"yellow\"><span style=\"background-color: #ff0000\">b</span></td></tr></table>");

        AdfNode row = document.Content![0].Content![0];
        Assert.Equal("#fff", row.Content![0].Attrs!["background"]);
        Assert.Null(row.Content[0].Content![0].Content![0].Marks);
        Assert.Equal("yellow", row.Content[1].Attrs!["background"]);
        // A highlight inside the cell is still a mark on its text.
        Assert.Equal("#ff0000", row.Content[1].Content![0].Content![0].Marks![0].Attrs!["color"]);
        Assert.True(document.Validate().IsValid);
    }

    [Theory]
    [InlineData("<table><tr><td colspan=\"0\" rowspan=\"-1\" style=\"background-color: rgb(1, 2, 3)\" data-colwidth=\"a,1\">a</td></tr></table>")]
    [InlineData("<table data-width=\"-5\" data-number-column-enabled=\"maybe\"><tr><td style=\"background: transparent\">a</td></tr></table>")]
    public void TableAttrsAdfCantCarry_AreIgnored(string html)
    {
        AdfDocument document = HtmlToAdfConverter.Convert(html);

        Assert.Null(document.Content![0].Attrs);
        Assert.Null(document.Content[0].Content![0].Content![0].Attrs);
        Assert.True(document.Validate().IsValid);
    }

    [Fact]
    public void Table_Attrs_AreDroppedByMarkdown()
    {
        // Pipe tables have no spans, colors or widths: the text survives, the attrs don't.
        AdfDocument document = MarkdownToAdfConverter.Convert(AdfToMarkdownConverter.Convert(TableDocument()));

        AdfNode table = document.Content![0];
        Assert.Null(table.Attrs);
        Assert.All(table.Content!.SelectMany(row => row.Content!), cell => Assert.Null(cell.Attrs));
        Assert.True(document.Validate().IsValid);
    }

    [Fact]
    public void ColwidthOfZero_IsValid()
    {
        AdfDocument document = TableDocument();

        Assert.True(document.Validate().IsValid);
        document.Content![0].Content![1].Content![1].Attrs!["colwidth"] = new List<object>() { -1 };
        Assert.False(document.Validate().IsValid);
    }
}
