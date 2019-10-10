using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using WhatTheHeck.StaticAnalysis;
using WhatTheHeck.Test.Helpers;
using WhatTheHeck.Test.Verification;
using Xunit;

namespace WhatTheHeck.Test.Tests.StaticAnalysis
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
