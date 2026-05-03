using System;
using System.Collections.Generic;
using System.Text;

namespace Riverside.Elapsed.App.Models.Timelapses;

public class Comment
{
	public string CommentId;
	public string Content;
	public User.User Author;
	public DateTimeOffset CreatedAt;
}
