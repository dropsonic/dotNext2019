using System.Data;
using Dapper;

namespace DapperTest
{
	class Program
	{
		static void Main(string[] args)
		{
			IDbConnection connection = null;
			connection.Query<User>("{QUERY_TEXT}");
		}
	}
}
