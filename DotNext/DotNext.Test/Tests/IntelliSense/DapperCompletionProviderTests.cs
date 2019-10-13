using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using DotNext.IntelliSense;
using DotNext.Test.Helpers;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Xunit;
using static DotNext.Test.Verification.VerificationHelper;

namespace DotNext.Test.Tests.IntelliSense
{
	public class DapperCompletionProviderTests
    {
		[Theory(Skip = "IntelliSense tests require disables parallelization")]
		[EmbeddedFileData("NoDapper.cs")]
        public async Task NotADapperCall_ShouldNotShowAnyCustomItems(string source)
        {
	        var document = CreateCSharpDocument<DapperCompletionProvider>(References, source);
	        int position = await GetPositionAsync(document, 9, 23);
	        var service = CompletionService.GetService(document);
	        var actual = await service.GetCompletionsAsync(document, position);

	        actual.Should().BeNull("because it is a completion inside non-Dapper call");
        }

	    [Theory(Skip = "IntelliSense tests require disables parallelization")]
	    [IntelliSenseText("")]
	    public async Task EmptyQuery_ShouldShowSelectSuggestion(int position, params string[] sourceFiles)
	    {
		    var document = CreateCSharpDocument<DapperCompletionProvider>(References, sourceFiles);
		    var service = CompletionService.GetService(document);
		    var actual = await service.GetCompletionsAsync(document, position);

		    actual.Should().NotBeNull();
		    actual.Items.Should().HaveCount(1);
	    }

	    [Theory(Skip = "IntelliSense tests require disables parallelization")]
	    [IntelliSenseText("SELECT ")]
	    public async Task AfterSelect_ShouldShowSelectionSuggestion(int position, params string[] sourceFiles)
	    {
		    var document = CreateCSharpDocument<DapperCompletionProvider>(References, sourceFiles);
		    var service = CompletionService.GetService(document);
		    var actual = await service.GetCompletionsAsync(document, position);

		    actual.Should().NotBeNull();
		    actual.Items.Should().HaveCount(1);
	    }


	    private static readonly MetadataReference NetStandard = MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location);
	    private static readonly MetadataReference SystemRuntimeReference = MetadataReference.CreateFromFile(Assembly.Load("System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a").Location);
	    private static readonly MetadataReference SystemDataCommon = MetadataReference.CreateFromFile(Assembly.Load("System.Data.Common, Version=4.2.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a").Location);
	    private static readonly MetadataReference DapperReference = MetadataReference.CreateFromFile(typeof(Dapper.SqlMapper).Assembly.Location);

	    private static readonly MetadataReference[] References = new[]
	    {
		    NetStandard,
			SystemRuntimeReference,
			SystemDataCommon,
			DapperReference,
	    };

	    class IntelliSenseTextAttribute : EmbeddedFileDataAttribute
	    {
		    private const string QueryTextPlaceholder = "{QUERY_TEXT}";
		    private readonly string _text;

		    public IntelliSenseTextAttribute(string text)
			    : base("DapperQuery.cs", "User.cs", "Post.cs")
		    {
			    _text = text;
		    }

		    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
		    {
			    foreach (object[] item in base.GetData(testMethod))
			    {
				    object[] result = new object[item.Length + 1];
				    int position = -1;
				    for (int i = 0; i < item.Length; i++)
				    {
					    string docText = (string) item[i];

					    if (position < 0)
					    {
						    position = docText.IndexOf(QueryTextPlaceholder, StringComparison.Ordinal) + _text.Length;
						    if (position > 0)
						    {
							    docText = docText.Replace(QueryTextPlaceholder, _text);
						    }
					    }

					    result[i + 1] = docText;
				    }

				    result[0] = position;

				    yield return result;
			    }
		    }
	    }
    }
}
