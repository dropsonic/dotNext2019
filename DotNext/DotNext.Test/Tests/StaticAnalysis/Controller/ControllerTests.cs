using System.Threading.Tasks;
using DotNext.StaticAnalysis;
using DotNext.StaticAnalysis.Controller;
using DotNext.Test.Helpers;
using DotNext.Test.Verification;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace DotNext.Test.Tests.StaticAnalysis.Controller
{
	public class ControllerTests : DiagnosticVerifier
	{
		// Сообщаем unit test framework'у, какой analyzer и code fix мы тестируем
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new ControllerAnalyzer();

		[Theory]
		[EmbeddedFileData("Example.cs")]
		public Task Example_ShouldShowDiagnostic(string actual) => 
			VerifyCSharpDiagnosticAsync(actual, Descriptors.DN1001_ThrowInDispose.CreateFor(line: 9, column: 4));
	}
}
