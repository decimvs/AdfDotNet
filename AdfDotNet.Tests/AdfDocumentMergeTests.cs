// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Builders;
using AdfDotNet.Enums;
using AdfDotNet.Models;
using Xunit;

namespace AdfDotNet.Tests;

/// <summary>
/// Covers <see cref="AdfDocument.Merge"/>, which splices another document's top-level content onto the
/// end of this one - the caller decides the resulting order by choosing which document to call it on and
/// what to merge in, and in which order to chain multiple merges.
/// </summary>
public class AdfDocumentMergeTests
{
    [Fact]
    public void Merge_AppendsOtherDocumentContentAfterOwnContent()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc.Paragraph(p => p.Text("first")));
        AdfDocument other = AdfDocumentBuilder.Build(doc => doc
            .Paragraph(p => p.Text("second"))
            .Rule());

        AdfDocument result = document.Merge(other);

        Assert.Same(document, result);
        Assert.Equal(3, document.Content!.Count);
        Assert.Equal("first", document.Content[0].Content!.Single().Text);
        Assert.Equal("second", document.Content[1].Content!.Single().Text);
        Assert.Equal(AdfNodeType.Rule, document.Content[2].Type);
    }

    [Fact]
    public void Merge_UsesSameNodeInstances_NotCopies()
    {
        AdfDocument document = AdfNode.CreateDocument();
        AdfDocument other = AdfDocumentBuilder.Build(doc => doc.Paragraph(p => p.Text("shared")));
        AdfNode otherParagraph = other.Content!.Single();

        document.Merge(other);

        Assert.Same(otherParagraph, document.Content!.Single());
    }

    [Fact]
    public void Merge_DocumentWithNullContent_InitializesContent()
    {
        AdfDocument document = AdfNode.CreateDocument();
        document.Content = null;
        AdfDocument other = AdfDocumentBuilder.Build(doc => doc.Rule());

        document.Merge(other);

        Assert.Equal(AdfNodeType.Rule, document.Content!.Single().Type);
    }

    [Fact]
    public void Merge_OtherDocumentWithNullOrEmptyContent_IsNoOp()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc.Rule());
        AdfDocument emptyOther = AdfNode.CreateDocument();
        emptyOther.Content = null;

        document.Merge(emptyOther);

        Assert.Single(document.Content!);
    }

    [Fact]
    public void Merge_NullOther_Throws()
    {
        AdfDocument document = AdfNode.CreateDocument();

        Assert.Throws<ArgumentNullException>(() => document.Merge(null!));
    }

    [Fact]
    public void Merge_CanBeChainedForMultiWaySplice()
    {
        AdfDocument document = AdfDocumentBuilder.Build(doc => doc.Heading(1, "Title"));
        AdfDocument middle = AdfDocumentBuilder.Build(doc => doc.Paragraph(p => p.Text("middle")));
        AdfDocument last = AdfDocumentBuilder.Build(doc => doc.Rule());

        document.Merge(middle).Merge(last);

        Assert.Equal(3, document.Content!.Count);
        Assert.Equal(AdfNodeType.Heading, document.Content[0].Type);
        Assert.Equal("middle", document.Content[1].Content!.Single().Text);
        Assert.Equal(AdfNodeType.Rule, document.Content[2].Type);
    }
}
