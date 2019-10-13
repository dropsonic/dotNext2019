using System.Threading.Tasks;
using DotNext.StaticAnalysis;
using DotNext.StaticAnalysis.WhatTheHeck;
using DotNext.Test.Helpers;
using DotNext.Test.Verification;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace DotNext.Test.Tests.StaticAnalysis.WhatTheHeck
{
	public class WhatTheHeckTests : CodeFixVerifier
	{
		// Сообщаем unit test framework'у, какой analyzer и code fix мы тестируем
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new WhatTheHeckAnalyzer();
		protected override CodeFixProvider GetCSharpCodeFixProvider() => new WhatTheHeckCodeFixProvider();

		// Проверка ложно-положительных срабатываний ("негативный" тест)
		[Theory]
		[EmbeddedFileData("PoliteComment.cs")]
		public Task PoliteComment_ShouldNotShowDiagnostic(string actual) => 
			VerifyCSharpDiagnosticAsync(actual);

		// Проверка срабатываний ("позитивный" тест)
		[Theory]
		[EmbeddedFileData("ImpoliteComment_StandaloneWord.cs")]
		public Task ImpoliteComment_StandaloneWord_ShouldShowDiagnostic(string actual) => 
			VerifyCSharpDiagnosticAsync(actual, Descriptors.DN1000_WhatTheHeckComment.CreateFor(line: 5, column: 3));

		// Проверка срабатываний ("позитивный" тест)
		[Theory]
		[EmbeddedFileData("ImpoliteComment_ComplexWord.cs")]
		public Task ImpoliteComment_ComplexWord_ShouldShowDiagnostic(string actual) =>
			VerifyCSharpDiagnosticAsync(actual, Descriptors.DN1000_WhatTheHeckComment.CreateFor(line: 5, column: 3));

		// Проверка отсутствия срабатывания при наличии исключения в suppression-файле
		[Theory]
		[EmbeddedFileData("ImpoliteComment_StandaloneWord.cs", "WhatTheHeck.suppression")]
		public Task ImpoliteComment_StandaloneWord_ShouldNotShowDiagnostic_BecauseItIsSuppressedInFile(string actual, string suppressionFile) => 
			VerifyCSharpDiagnosticWithSuppressionFileAsync(actual, suppressionFile);

		// Проверка отсутствия срабатывания при наличии suppression-комментария
		[Theory]
		[EmbeddedFileData("ImpoliteComment_StandaloneWord_SuppressedWithComment.cs")]
		public Task ImpoliteComment_StandaloneWord_ShouldNotShowDiagnostic_BecauseItIsSuppressedWithComment(string actual) => 
			VerifyCSharpDiagnosticAsync(actual);

		// Проверка, что code fix корректно исправляет найденную ошибку
		[Theory]
		[EmbeddedFileData("ImpoliteComment_StandaloneWord.cs", "ImpoliteComment_StandaloneWord_Expected.cs")]
		public Task ImpoliteComment_StandaloneWord_ShouldFixIt(string actual, string expected) => 
			VerifyCSharpFixAsync(actual, expected);

		// Проверка, что code fix корректно исправляет найденную ошибку
		[Theory]
		[EmbeddedFileData("ImpoliteComment_ComplexWord.cs", "ImpoliteComment_ComplexWord_Expected.cs")]
		public Task ImpoliteComment_ComplexWord_ShouldFixIt(string actual, string expected) => 
			VerifyCSharpFixAsync(actual, expected);
	}
}
