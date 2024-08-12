namespace RedisHelper.WebApi.TestEntity;

public class Book
{
    public Guid Id { get; init; }
    public string Title { get;private set; }
    public DateTime Created { get; set; }
    public Book(string Title)
    {
        Id= Guid.NewGuid();
        this.Title = Title;
        Created= DateTime.Now;
    }
}
