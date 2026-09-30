// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.Enums;
using AdfDotNet.Models;
using Xunit;

namespace AdfDotNet.Tests;

/// <summary>
/// Covers <see cref="AdfNodeSchema"/> - the unified, <see cref="AdfNodeType"/>-keyed schema table that
/// replaced <see cref="AdfNode"/>'s CanHaveChildren()/IsLeafNode() switch statements and
/// <see cref="AdfStructure"/>'s separate string-keyed legal-child dictionary - plus the
/// <see cref="AdfStructure"/> string-based wrapper methods still backed by it.
/// </summary>
public class AdfNodeSchemaTests
{
    [Theory]
    [InlineData(AdfNodeType.Doc)]
    [InlineData(AdfNodeType.Paragraph)]
    [InlineData(AdfNodeType.Heading)]
    [InlineData(AdfNodeType.BulletList)]
    [InlineData(AdfNodeType.OrderedList)]
    [InlineData(AdfNodeType.ListItem)]
    [InlineData(AdfNodeType.Blockquote)]
    [InlineData(AdfNodeType.CodeBlock)]
    [InlineData(AdfNodeType.Table)]
    [InlineData(AdfNodeType.TableRow)]
    [InlineData(AdfNodeType.TableCell)]
    [InlineData(AdfNodeType.TableHeader)]
    [InlineData(AdfNodeType.MediaSingle)]
    public void CanHaveChildren_TrueForContainerTypes(AdfNodeType type)
    {
        Assert.True(AdfNodeSchema.CanHaveChildren(type));
        Assert.False(AdfNodeSchema.IsLeaf(type));
    }

    [Theory]
    [InlineData(AdfNodeType.Text)]
    [InlineData(AdfNodeType.Rule)]
    [InlineData(AdfNodeType.HardBreak)]
    [InlineData(AdfNodeType.Media)]
    [InlineData(AdfNodeType.Emoji)]
    public void IsLeaf_TrueForLeafTypes(AdfNodeType type)
    {
        Assert.True(AdfNodeSchema.IsLeaf(type));
        Assert.False(AdfNodeSchema.CanHaveChildren(type));
    }

    [Theory]
    [InlineData(AdfNodeType.InlineCard)]
    public void UnclassifiedTypes_AreNeitherContainerNorLeaf(AdfNodeType type)
    {
        Assert.False(AdfNodeSchema.CanHaveChildren(type));
        Assert.False(AdfNodeSchema.IsLeaf(type));
        Assert.Empty(AdfNodeSchema.GetAllowedChildTypes(type));
    }

    [Fact]
    public void IsValidChildType_TrueForLegalNesting()
    {
        Assert.True(AdfNodeSchema.IsValidChildType(AdfNodeType.Doc, AdfNodeType.Paragraph));
        Assert.True(AdfNodeSchema.IsValidChildType(AdfNodeType.Paragraph, AdfNodeType.Text));
        Assert.True(AdfNodeSchema.IsValidChildType(AdfNodeType.BulletList, AdfNodeType.ListItem));
        Assert.True(AdfNodeSchema.IsValidChildType(AdfNodeType.Paragraph, AdfNodeType.InlineCard));
        Assert.True(AdfNodeSchema.IsValidChildType(AdfNodeType.Paragraph, AdfNodeType.Emoji));
        Assert.True(AdfNodeSchema.IsValidChildType(AdfNodeType.Heading, AdfNodeType.InlineCard));
        Assert.True(AdfNodeSchema.IsValidChildType(AdfNodeType.Heading, AdfNodeType.Emoji));
        Assert.True(AdfNodeSchema.IsValidChildType(AdfNodeType.MediaSingle, AdfNodeType.Media));
    }

    [Fact]
    public void IsValidChildType_FalseForIllegalNesting()
    {
        Assert.False(AdfNodeSchema.IsValidChildType(AdfNodeType.Doc, AdfNodeType.Text));
        Assert.False(AdfNodeSchema.IsValidChildType(AdfNodeType.BulletList, AdfNodeType.Paragraph));
        Assert.False(AdfNodeSchema.IsValidChildType(AdfNodeType.Text, AdfNodeType.Text));
        Assert.False(AdfNodeSchema.IsValidChildType(AdfNodeType.Doc, AdfNodeType.Media));
        Assert.False(AdfNodeSchema.IsValidChildType(AdfNodeType.Paragraph, AdfNodeType.Media));
    }

    [Theory]
    [InlineData(AdfNodeType.BulletList, "bulletList")]
    [InlineData(AdfNodeType.CodeBlock, "codeBlock")]
    [InlineData(AdfNodeType.Doc, "doc")]
    [InlineData(AdfNodeType.HardBreak, "hardBreak")]
    public void GetTypeName_ProducesCamelCase(AdfNodeType type, string expected)
    {
        Assert.Equal(expected, AdfNodeSchema.GetTypeName(type));
    }

    [Theory]
    [InlineData("bulletList", AdfNodeType.BulletList)]
    [InlineData("BULLETLIST", AdfNodeType.BulletList)]
    [InlineData("doc", AdfNodeType.Doc)]
    public void TryParseTypeName_RoundTripsGetTypeName(string typeName, AdfNodeType expected)
    {
        Assert.True(AdfNodeSchema.TryParseTypeName(typeName, out AdfNodeType parsed));
        Assert.Equal(expected, parsed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("notAType")]
    public void TryParseTypeName_FalseForUnrecognizedNames(string? typeName)
    {
        Assert.False(AdfNodeSchema.TryParseTypeName(typeName, out _));
    }

    [Fact]
    public void AdfStructure_IsValidChildType_DelegatesToSchema()
    {
        Assert.True(AdfStructure.IsValidChildType("paragraph", "text"));
        Assert.False(AdfStructure.IsValidChildType("bulletList", "paragraph"));
        Assert.False(AdfStructure.IsValidChildType("notAType", "text"));
    }

    [Fact]
    public void AdfStructure_GetAllowedChildTypes_DelegatesToSchema()
    {
        List<string> allowed = AdfStructure.GetAllowedChildTypes("bulletList");
        Assert.Equal(new[] { "listItem" }, allowed);

        Assert.Empty(AdfStructure.GetAllowedChildTypes("notAType"));
    }
}
