using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNext
{
	[ApiController]
	[RoutePrefix("foo")]
	public class FooController : ControllerBase
	{
		[HttpGet, Route]
		public IEnumerable<Foo> Get() => Enumerable.Empty<Foo>();

		[HttpGet, Route("{id}")]
		public Foo GetById(int id) => new Foo();

		[HttpPost, Route("{name}")]
		public void Add(string name) => Create(name);

		private Foo Create(string name) => new Foo();
	}

	public class Foo { }
}
