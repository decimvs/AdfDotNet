// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.DataConverters;
using AdfDotNet.Enums;
using AdfDotNet.Models;
using Newtonsoft.Json;
using Xunit;

namespace AdfDotNet.Tests;

/// <summary>
/// Covers <see cref="AdfDocumentConverter"/>'s handling of ADF node/mark types outside
/// <see cref="AdfNodeType"/>/<see cref="AdfMarkType"/> - see <see cref="AdfSpecCoverage"/>, which
/// distinguishes a JSON-schema-only type (dropped, not an error) from a type name neither the spec nor the
/// schema defines (still throws).
/// </summary>
public class AdfJsonConverterTests
{
    [Fact]
    public void JsonSchemaOnlyNodeType_IsSkippedWithItsSubtree()
    {
        // "decisionList" exists only in the ADF JSON schema, not on the spec page.
        string json = """
        {
          "version": 1,
          "type": "doc",
          "content": [
            { "type": "paragraph", "content": [ { "type": "text", "text": "kept" } ] },
            {
              "type": "decisionList",
              "attrs": { "localId": "list-1" },
              "content": [
                { "type": "decisionItem", "attrs": { "localId": "item-1", "state": "DECIDED" }, "content": [ { "type": "text", "text": "dropped" } ] }
              ]
            }
          ]
        }
        """;

        AdfDocument document = AdfJsonConverter.Deserialize(json);

        AdfNode kept = Assert.Single(document.Content!);
        Assert.Equal(AdfNodeType.Paragraph, kept.Type);
    }

    [Fact]
    public void JsonSchemaOnlyMarkType_IsSkipped()
    {
        // "alignment" exists only in the ADF JSON schema, not on the spec page.
        string json = """
        {
          "version": 1,
          "type": "doc",
          "content": [
            {
              "type": "paragraph",
              "marks": [ { "type": "alignment", "attrs": { "align": "center" } } ],
              "content": [
                { "type": "text", "text": "hello", "marks": [ { "type": "em" }, { "type": "annotation", "attrs": { "id": "a1", "annotationType": "inlineComment" } } ] }
              ]
            }
          ]
        }
        """;

        AdfDocument document = AdfJsonConverter.Deserialize(json);

        AdfNode paragraph = document.Content!.Single();
        Assert.Empty(paragraph.Marks!);
        AdfMark mark = Assert.Single(paragraph.Content!.Single().Marks!);
        Assert.Equal(AdfMarkType.Em, mark.Type);
    }

    [Fact]
    public void CompletelyUnknownNodeType_StillThrows()
    {
        string json = """
        {
          "version": 1,
          "type": "doc",
          "content": [
            { "type": "notARealAdfType" }
          ]
        }
        """;

        Assert.Throws<JsonSerializationException>(() => AdfJsonConverter.Deserialize(json));
    }

    [Fact]
    public void CompletelyUnknownMarkType_StillThrows()
    {
        string json = """
        {
          "version": 1,
          "type": "doc",
          "content": [
            {
              "type": "paragraph",
              "content": [
                { "type": "text", "text": "hello", "marks": [ { "type": "notARealMarkType" } ] }
              ]
            }
          ]
        }
        """;

        Assert.Throws<JsonSerializationException>(() => AdfJsonConverter.Deserialize(json));
    }
}
