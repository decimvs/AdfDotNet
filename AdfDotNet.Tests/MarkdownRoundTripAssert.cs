// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.DataConverters;
using AdfDotNet.FormatConverters;
using AdfDotNet.Models;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AdfDotNet.Tests;

/// <summary>
/// Shared assertions for verifying that an <see cref="AdfDocument"/> survives an ADF -> Markdown -> ADF
/// round trip with no structural drift. Mirrors <see cref="RoundTripAssert"/>, but Markdown text isn't
/// normalized the way <see cref="RoundTripAssert.NormalizeHtml"/> normalizes HTML - whitespace is
/// significant to Markdown (indentation, blank lines), so only the resulting ADF tree is compared.
/// </summary>
internal static class MarkdownRoundTripAssert
{
    private static readonly AdfToMarkdownConverter ToMarkdownConverter = new();
    private static readonly MarkdownToAdfConverter ToAdfConverter = new();

    public static void FullRoundTrip(AdfDocument document)
    {
        string markdown = ToMarkdownConverter.ConvertAdf(document);
        AdfDocument roundTripped = ToAdfConverter.ConvertMarkdown(markdown);

        Assert.False(string.IsNullOrWhiteSpace(markdown), "ADF -> Markdown conversion produced empty output.");

        JToken original = JToken.Parse(AdfJsonConverter.Serialize(document));
        JToken roundTrippedJson = JToken.Parse(AdfJsonConverter.Serialize(roundTripped));
        Assert.True(
            JToken.DeepEquals(original, roundTrippedJson),
            $"ADF roundtrip mismatch.\nMarkdown:     {markdown}\nOriginal:     {original}\nRoundtripped: {roundTrippedJson}");
    }
}
