using System.Data;
using System.Data.SqlClient;
using Dapper;

namespace DapperTest
{
    class Program
    {
        static void Main(string[] args)
        {
	        using (IDbConnection connection = new SqlConnection("Data Source=(local);Initial Catalog=dapper;Integrated Security=True;"))
	        {
                connection.Query<User, Post, Post>(
	        }
        }
    }

    /// <summary>
    /// Represents user identity.
    /// </summary>
	class User
	{
		public int Id { get; set; }

        /// <summary>
        /// Username.
        /// </summary>
		public string Name { get; set; }
	}

	class Post
	{
		public int Id { get; set; }

        /// <summary>
        /// Title of the post.
        /// </summary>
		public string Title { get; set; }
		public string Content { get; set; }
		public User Owner { get; set; }
	}
}
