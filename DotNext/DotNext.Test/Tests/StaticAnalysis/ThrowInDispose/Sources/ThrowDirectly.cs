using System;

namespace DotNext
{
	class Foo : IDisposable
	{
		public void Dispose()
		{
			throw new Exception();
		}
	}

}
