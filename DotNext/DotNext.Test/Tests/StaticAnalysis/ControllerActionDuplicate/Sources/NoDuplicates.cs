using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace DotNext
{
	[ApiController]
	[Route("foo")]
	public class FooController : ControllerBase
	{
		[HttpGet]
		public IEnumerable<Foo> Get() => Enumerable.Empty<Foo>();

		[HttpGet, Route("{id}")]
		public Foo GetById(int id) => new Foo();

		[HttpPost, Route("{name}")]
		public void Add(string name) => Create(name);

		private Foo Create(string name) => new Foo();
	}

	public class Foo { }
}
