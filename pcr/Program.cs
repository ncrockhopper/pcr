// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class Program
{
	private static void Main(string[] args)
	{
		TestFormatterAndClassifierAsync().Wait();
	}

	private static async Task TestFormatterAndClassifierAsync()
	{
		string srccodestr =
@"class C
{
	static void Main()
	{
		WriteLine(""Hello, World!"");
		string str = null;
		TestNullRefCheck(str);
	}

	public void TestNullRefCheck(object obj)
	{
		WriteLine(obj.GetType());
	}
}";
		AdhocWorkspace workspace = new AdhocWorkspace();
		Solution solution = workspace.CurrentSolution;
		Project project = solution.AddProject("projectName", "assemblyName", LanguageNames.CSharp);
		Document document = project.AddDocument("name.cs", srccodestr);
		document = await Formatter.FormatAsync(document);
		SourceText text = await document.GetTextAsync();

		IEnumerable<ClassifiedSpan> classifiedSpans = await Classifier.GetClassifiedSpansAsync(document, TextSpan.FromBounds(0, text.Length));
		Console.BackgroundColor = ConsoleColor.Black;

		var ranges = classifiedSpans.Select(classifiedSpan =>
			new Range(classifiedSpan, text.GetSubText(classifiedSpan.TextSpan).ToString()));

		ranges = FillGaps(text, ranges);

		foreach (Range range in ranges)
		{
			switch (range.ClassificationType)
			{
				case "keyword":
					Console.ForegroundColor = ConsoleColor.DarkCyan;
					break;
				case "class name":
					Console.ForegroundColor = ConsoleColor.Cyan;
					break;
				case "string":
					Console.ForegroundColor = ConsoleColor.DarkYellow;
					break;
				default:
					Console.ForegroundColor = ConsoleColor.White;
					break;
			}

			Console.Write(range.Text);
		}

		Console.ResetColor();
		Console.WriteLine();
		Console.ReadLine();


		SyntaxNode root = await document.GetSyntaxRootAsync().ConfigureAwait(false);
		//SyntaxNode node = root.FindNode(context.Span);
		//ParameterSyntax parameterSyntax = node as ParameterSyntax;

		SyntaxTree tree = CSharpSyntaxTree.ParseText(srccodestr);

		var root2 = (CompilationUnitSyntax)tree.GetRoot();

		var firstMember = root2.Members[0];

		Console.ReadLine();
	}

	private static IEnumerable<Range> FillGaps(SourceText text, IEnumerable<Range> ranges)
	{
		const string WhitespaceClassification = null;
		int current = 0;
		Range previous = null;

		foreach (Range range in ranges)
		{
			int start = range.TextSpan.Start;
			if (start > current)
			{
				yield return new Range(WhitespaceClassification, TextSpan.FromBounds(current, start), text);
			}

			if (previous == null || range.TextSpan != previous.TextSpan)
			{
				yield return range;
			}

			previous = range;
			current = range.TextSpan.End;
		}

		if (current < text.Length)
		{
			yield return new Range(WhitespaceClassification, TextSpan.FromBounds(current, text.Length), text);
		}
	}
}
