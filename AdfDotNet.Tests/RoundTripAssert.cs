// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.DataConverters;
using AdfDotNet.FormatConverters;
using AdfDotNet.Models;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace AdfDotNet.Tests;

/// <summary>
/// Shared assertions for verifying that an <see cref="AdfDocument"/> survives an ADF -> HTML -> ADF round trip
/// with no structural or rendering drift.
/// </summary>
internal static class RoundTripAssert
{
    private static readonly AdfToHtmlConverter ToHtmlConverter = new();
    private static readonly HtmlToAdfConverter ToAdfConverter = new();
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex BrTagRegex = new("<br>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex HrTagRegex = new("<hr>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static void FullRoundTrip(AdfDocument document)
    {
        string html1 = ToHtmlConverter.ConvertAdf(document);
        AdfDocument roundTripped = ToAdfConverter.ConvertHtml(html1);
        string html2 = ToHtmlConverter.ConvertAdf(roundTripped);

        Assert.False(string.IsNullOrWhiteSpace(html1), "ADF -> HTML conversion produced empty output.");

        Assert.Equal(NormalizeHtml(html1), NormalizeHtml(html2));

        JToken original = JToken.Parse(ToJson(document));
        JToken roundTrippedJson = JToken.Parse(ToJson(roundTripped));
        Assert.True(
            JToken.DeepEquals(original, roundTrippedJson),
            $"ADF roundtrip mismatch.\nOriginal:     {original}\nRoundtripped: {roundTrippedJson}");
    }

    /// <summary>
    /// Verifies an ADF -> JSON -> ADF round trip, with no HTML involved. Used for node types (e.g.
    /// <c>mediaSingle</c>/<c>media</c>) that the HTML converter doesn't render/parse at all - the ADF
    /// document model and JSON serialization are still expected to round-trip them per the ADF spec, even
    /// though this library's HTML/Markdown helpers can't meaningfully represent them (they carry Jira
    /// Media-Services identifiers, not a fetchable URL).
    /// </summary>
    public static void JsonRoundTrip(AdfDocument document)
    {
        string json1 = ToJson(document);
        AdfDocument roundTripped = AdfJsonConverter.Deserialize(json1);
        string json2 = ToJson(roundTripped);

        JToken original = JToken.Parse(json1);
        JToken roundTrippedJson = JToken.Parse(json2);
        Assert.True(
            JToken.DeepEquals(original, roundTrippedJson),
            $"ADF JSON roundtrip mismatch.\nOriginal:     {original}\nRoundtripped: {roundTrippedJson}");
    }

    private static string ToJson(AdfDocument document)
    {
        return AdfJsonConverter.Serialize(document);
    }

    private static string NormalizeHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        string normalized = WhitespaceRegex.Replace(html, " ").Trim();
        normalized = BrTagRegex.Replace(normalized, "<br/>");
        normalized = HrTagRegex.Replace(normalized, "<hr/>");
        return normalized;
    }
}
