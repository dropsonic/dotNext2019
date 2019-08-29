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
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new WhatTheHeckAnalyzer();
		protected override CodeFixProvider GetCSharpCodeFixProvider() => new WhatTheHeckCodeFixProvider();

		[Theory]
		[EmbeddedFileData("PoliteComment.cs")]
		public Task PoliteComment_ShouldNotShowDiagnostic(string actual) => 
			VerifyCSharpDiagnosticAsync(actual);

		[Theory]
		[EmbeddedFileData("ImpoliteComment_StandaloneWord.cs")]
		public Task ImpoliteComment_StandaloneWord_ShouldShowDiagnostic(string actual) => 
			VerifyCSharpDiagnosticAsync(actual, Descriptors.DN1000_WhatTheHeckComment.CreateFor(line: 5, column: 3));

		[Theory]
		[EmbeddedFileData("ImpoliteComment_ComplexWord.cs")]
		public Task ImpoliteComment_ComplexWord_ShouldShowDiagnostic(string actual) =>
			VerifyCSharpDiagnosticAsync(actual, Descriptors.DN1000_WhatTheHeckComment.CreateFor(line: 5, column: 3));


		[Theory]
		[EmbeddedFileData("ImpoliteComment_StandaloneWord.cs", "ImpoliteComment_StandaloneWord_Expected.cs")]
		public Task ImpoliteComment_StandaloneWord_ShouldFixIt(string actual, string expected) => 
			VerifyCSharpFixAsync(actual, expected);

		[Theory]
		[EmbeddedFileData("ImpoliteComment_ComplexWord.cs", "ImpoliteComment_ComplexWord_Expected.cs")]
		public Task ImpoliteComment_ComplexWord_ShouldFixIt(string actual, string expected) => 
			VerifyCSharpFixAsync(actual, expected);
	}
}
