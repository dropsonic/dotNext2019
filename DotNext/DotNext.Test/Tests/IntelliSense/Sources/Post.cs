namespace DapperTest
{
	public class Post
	{
		public int Id { get; set; }
		public string Title { get; set; }
		public string Content { get; set; }
		public User Owner { get; set; }
	}
}
