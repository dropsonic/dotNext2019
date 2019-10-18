using Microsoft.AspNetCore.Mvc;

namespace DotNext
{
	[ApiController]
	[Route("foo")]
	public class FooController : ControllerBase
	{
		[HttpPost, Route("{name}")]
		public void Add(string name) { }

		[HttpPost, Route("{name}")]
		public void Remove(string name) { }
	}
}
