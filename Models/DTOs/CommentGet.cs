namespace AnthemAPI.Models;

public class CommentGet
{
    public required Comment Comment { get; set; }
    public required UserCard UserCard { get; set; }
}
