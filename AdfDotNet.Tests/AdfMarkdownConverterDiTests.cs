// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.DataConverters;
using AdfDotNet.FormatConverters;
using AdfDotNet.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AdfDotNet.Tests;

/// <summary>
/// Verifies that <see cref="IAdfMarkdownConverter"/> resolves via <c>AddAdfMarkdownConverter()</c> and
/// produces the same output as the underlying static <see cref="AdfToMarkdownConverter"/>/
/// <see cref="MarkdownToAdfConverter"/> APIs. Mirrors <see cref="AdfHtmlConverterDiTests"/>.
/// </summary>
public class AdfMarkdownConverterDiTests
{
    [Fact]
    public void AddAdfMarkdownConverter_RegistersSingleton()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddAdfMarkdownConverter()
            .BuildServiceProvider();

        IAdfMarkdownConverter first = provider.GetRequiredService<IAdfMarkdownConverter>();
        IAdfMarkdownConverter second = provider.GetRequiredService<IAdfMarkdownConverter>();

        Assert.Same(first, second);
    }

    [Fact]
    public void ConvertToMarkdown_MatchesStaticConverter()
    {
        IAdfMarkdownConverter converter = new ServiceCollection()
            .AddAdfMarkdownConverter()
            .BuildServiceProvider()
            .GetRequiredService<IAdfMarkdownConverter>();

        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>
        {
            AdfNode.CreateParagraph(new List<AdfNode> { AdfNode.CreateText("Hello world") })
        });

        Assert.Equal(AdfToMarkdownConverter.Convert(document), converter.ConvertToMarkdown(document));
    }

    [Fact]
    public void ConvertToAdf_MatchesStaticConverter()
    {
        IAdfMarkdownConverter converter = new ServiceCollection()
            .AddAdfMarkdownConverter()
            .BuildServiceProvider()
            .GetRequiredService<IAdfMarkdownConverter>();

        const string markdown = "Hello world";

        Assert.Equal(MarkdownToAdfConverter.Convert(markdown).ToJson(), converter.ConvertToAdf(markdown).ToJson());
    }
}
