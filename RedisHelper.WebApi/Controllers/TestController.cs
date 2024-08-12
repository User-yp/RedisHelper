using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RedisHelper.WebApi.TestEntity;
using StackExchange.Redis;
using System.Collections.Concurrent;

namespace RedisHelper.WebApi.Controllers;

[Route("[controller]/[action]")]
[ApiController]
public class TestController : ControllerBase
{
    private readonly IRedisHelper redis;
    private readonly IConfiguration configuration;

    public TestController(IRedisHelper redis,IConfiguration configuration)
    {
        this.redis = redis;
        this.configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> ApolloTest()
    {
        var cfg = configuration.GetSection("Redis");
        return Ok(cfg);
    }
    [HttpPost]
    public async Task <ActionResult> StringTest()
    {
        Book book = new("今日时报");
        Book book2 = new("东方时空");
        Book book3 = new("早间新闻");
        await redis.StringSetAsync(book.Title, book);
        await redis.StringSetAsync(book2.Title, book2,TimeSpan.FromSeconds(300));
        await redis.StringSetAsync(book3.Title, book3);
        var getBook= await redis.StringGetAsync<Book>(book3.Title);
        var res= ReferenceEquals(getBook, book3);
        int increment = 1;
        await redis.StringSetAsync("increment", increment);
        await redis.StringIncrementAsync("increment", 5);
        await redis.StringDecrementAsync("increment", 2);
        await redis.DeleteAllKeyAsync();
        return Ok();
    }
    [HttpPost]
    public async Task<ActionResult> ListTest()
    {
        Book book = new("今日时报");
        Book book2 = new("东方时空");
        Book book3 = new("早间新闻");
        await redis.EnqueueorCreateAsync("book", book);
        await redis.EnqueueAsync("book", book2);
        await redis.EnqueueAsync("book", book3);
        var books= await redis.PeekRangeAsync<Book>("book",1,1);
        var getBook= books.FirstOrDefault();
        var res = ReferenceEquals(getBook, book);
        await redis.DeleteAllKeyAsync();
        return Ok();
    }

    [HttpPost]
    public async Task<ActionResult> SetTest()
    {
        string key = "SetTest";
        for (int i = 0; i < 10; i++)
            await redis.SetAddAsync(key, i+1);
        await redis.SetRemoveAsync(key, new List<int> { 5,6,7,8,9});
        var res= await redis.SetMembersAsync<string>(key);
        var ints=res.ToList();
        var con= await redis.SetContainsAsync(key, 1);
        await redis.DeleteAllKeyAsync();
        return Ok();
    }

    [HttpPost]
    public async Task<ActionResult> HashTest()
    {
        Book book = new("今日时报");
        await redis.HashSetAsync(nameof(Book),new ConcurrentDictionary<string, string>
        {
            [nameof(book.Id)]=book.Id.ToString(),
            [nameof(book.Title)]=book.Title,
            [nameof(book.Created)]=book.Created.ToString()
        });
        await redis.HashDeleteFieldsAsync(nameof(Book), [nameof(book.Created)]);
        await redis.HashSetFieldsAsync(nameof(Book), new ConcurrentDictionary<string, string>
        {
            [nameof(book.Created)] = DateTime.Now.ToString(),
            ["TestField"]= "TestField"
        });
        await redis.HashDeleteAsync(nameof(Book));
        return Ok();
    }
}
