using System.Threading.Tasks;
using DotNext.StaticAnalysis;
using DotNext.StaticAnalysis.Controller;
using DotNext.Test.Helpers;
using DotNext.Test.Verification;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace DotNext.Test.Tests.StaticAnalysis.ControllerActionDuplicate
{
	public class ControllerActionDuplicateTests : DiagnosticVerifier
	{
		// Сообщаем unit test framework'у, какой analyzer мы тестируем
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new ControllerAnalyzer(
			// Передаём только один sub-analyzer для гранулярности тестирования
			new ControllerActionDuplicateAnalyzer());

		[Theory]
		[EmbeddedFileData("NoDuplicates.cs")]
		public Task NoDuplicates_ShouldNotShowDiagnostic(string actual) => 
			VerifyCSharpDiagnosticAsync(actual);

		[Theory]
		[EmbeddedFileData("Duplicates.cs")]
		public Task Duplicates_ShouldShowDiagnostic(string actual) => 
			VerifyCSharpDiagnosticAsync(actual, 
				Descriptors.DN1002_DuplicateControllerAction.CreateFor(line: 10, column: 15),
				Descriptors.DN1002_DuplicateControllerAction.CreateFor(line: 13, column: 15));
	}
}
