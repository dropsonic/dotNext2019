using System.Threading.Tasks;
using DotNext.StaticAnalysis;
using DotNext.StaticAnalysis.ThrowInDispose;
using DotNext.Test.Helpers;
using DotNext.Test.Verification;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace DotNext.Test.Tests.StaticAnalysis.ThrowInDispose
{
	public class ThrowInDisposeTests : DiagnosticVerifier
	{
		// Сообщаем unit test framework'у, какой analyzer и code fix мы тестируем
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new ThrowInDisposeAnalyzer();

		[Theory]
		[EmbeddedFileData("ThrowDirectly.cs")]
		public Task ThrowDirectly_ShouldShowDiagnostic(string actual) => 
			VerifyCSharpDiagnosticAsync(actual, Descriptors.DN1001_ThrowInDispose.CreateFor(line: 9, column: 4));

		[Theory]
		[EmbeddedFileData("ThrowIndirectly.cs")]
		public Task ThrowIndirectly_ShouldShowDiagnostic(string actual) => 
			VerifyCSharpDiagnosticAsync(actual, Descriptors.DN1001_ThrowInDispose.CreateFor(line: 11, column: 8));
	}
}
