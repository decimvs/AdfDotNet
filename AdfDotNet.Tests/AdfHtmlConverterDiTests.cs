// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.DataConverters;
using AdfDotNet.FormatConverters;
using AdfDotNet.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AdfDotNet.Tests;

/// <summary>
/// Verifies that <see cref="IAdfHtmlConverter"/> resolves via <c>AddAdfHtmlConverter()</c> and produces the
/// same output as the underlying static <see cref="AdfToHtmlConverter"/>/<see cref="HtmlToAdfConverter"/> APIs.
/// </summary>
public class AdfHtmlConverterDiTests
{
    [Fact]
    public void AddAdfHtmlConverter_RegistersSingleton()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddAdfHtmlConverter()
            .BuildServiceProvider();

        IAdfHtmlConverter first = provider.GetRequiredService<IAdfHtmlConverter>();
        IAdfHtmlConverter second = provider.GetRequiredService<IAdfHtmlConverter>();

        Assert.Same(first, second);
    }

    [Fact]
    public void ConvertToHtml_MatchesStaticConverter()
    {
        IAdfHtmlConverter converter = new ServiceCollection()
            .AddAdfHtmlConverter()
            .BuildServiceProvider()
            .GetRequiredService<IAdfHtmlConverter>();

        AdfDocument document = AdfNode.CreateDocument(new List<AdfNode>
        {
            AdfNode.CreateParagraph(new List<AdfNode> { AdfNode.CreateText("Hello world") })
        });

        Assert.Equal(AdfToHtmlConverter.Convert(document), converter.ConvertToHtml(document));
    }

    [Fact]
    public void ConvertToAdf_MatchesStaticConverter()
    {
        IAdfHtmlConverter converter = new ServiceCollection()
            .AddAdfHtmlConverter()
            .BuildServiceProvider()
            .GetRequiredService<IAdfHtmlConverter>();

        const string html = "<p>Hello world</p>";

        Assert.Equal(HtmlToAdfConverter.Convert(html).ToJson(), converter.ConvertToAdf(html).ToJson());
    }
}
