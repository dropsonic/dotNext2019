using System;

namespace DotNext
{
	class Foo : IDisposable
	{
		private bool _initialized;

		public void Dispose()
		{
			if (this.Ready)
			{
				// do something
			}
		}

		public bool Ready
		{
			get
			{
				if (!_initialized)
					throw new Exception();
				
				return true;
			}
		}
	}
}
