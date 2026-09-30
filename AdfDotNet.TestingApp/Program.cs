// Copyright (c) 2026 Guillermo Espert Carrasquer. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using AdfDotNet.DataConverters;
using AdfDotNet.Models;

var document = AdfNode.CreateDocument(new List<AdfNode>
{
    AdfNode.CreateHeading(1, new List<AdfNode>
    {
        AdfNode.CreateText("Hello World")
    }),
    AdfNode.CreateParagraph(new List<AdfNode>
    {
        AdfNode.CreateText("This is a sample paragraph.")
    })
});

var json = document.ToJson(true);


Console.WriteLine("ADF JSON:");