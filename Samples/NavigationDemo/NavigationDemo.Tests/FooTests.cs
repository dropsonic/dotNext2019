using Xunit;

namespace NavigationDemo.Tests
{
    public class FooTests
    {
		[Fact]
	    public void DoSomething_ShouldDoSomething()
		{
			var foo = new Foo();
			foo.DoSomething();
	    }
    }
}
