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

    public TestController(IRedisHelper redis, IConfiguration configuration)
    {
        this.redis = redis;
        this.configuration = configuration;
    }

    [HttpGet]
    public IActionResult ApolloTest()
    {
        var cfg = configuration.GetSection("Redis");
        return Ok(cfg);
    }

    [HttpPost]
    public async Task<ActionResult> StringTest()
    {
        // 基本 SET/GET
        Book book = new("今日时报");
        Book book2 = new("东方时空");
        Book book3 = new("早间新闻");
        await redis.StringSetAsync(book.Title, book);
        var getBook = await redis.StringGetAsync<Book>(book.Title);

        // SETNX（仅当 key 不存在时设置）
        var setnx1 = await redis.StringSetIfNotExistsAsync("setnx_test", "value1");          // true
        var setnx2 = await redis.StringSetIfNotExistsAsync("setnx_test", "value2");          // false
        var setnx3 = await redis.StringSetIfNotExistsAsync("setnx_ttl", "val", TimeSpan.FromMinutes(10));

        // GETSET（设置新值返回旧值）
        var oldVal = await redis.StringGetSetAsync("setnx_test", "new_value");               // "value1"

        // MSET / MGET（批量操作）
        await redis.StringSetMultipleAsync(new Dictionary<string, string>
        {
            ["batch_1"] = "a",
            ["batch_2"] = "b",
            ["batch_3"] = "c"
        });
        var batchGet = await redis.StringGetMultipleAsync<string>(new[] { "batch_1", "batch_2", "batch_3" });

        // STRLEN / APPEND
        var len = await redis.StringLengthAsync("batch_1");                                  // 1
        var newLen = await redis.StringAppendAsync("batch_1", "ppend");                       // 6

        // INCR / DECR / INCRBYFLOAT
        await redis.StringSetAsync("counter", 10);
        var incr = await redis.StringIncrementAsync("counter", 5);                           // 15
        var decr = await redis.StringDecrementAsync("counter", 3);                           // 12
        var incrDbl = await redis.StringIncrementByDoubleAsync("counter", 0.5);             // 12.5

        await redis.FlushDatabaseAsync();
        return Ok(new { setnx1, setnx2, setnx3, oldVal, batchGet, len, newLen, incr, decr, incrDbl });
    }

    [HttpPost]
    public async Task<ActionResult> ListTest()
    {
        Book book = new("今日时报");
        Book book2 = new("东方时空");
        Book book3 = new("早间新闻");

        // 右侧入队（RPUSH）
        await redis.EnqueueAsync("book", book);
        // 左侧入队（LPUSH）
        await redis.ListLeftPushAsync("book", book2);
        // 右侧入队
        await redis.EnqueueAsync("book", book3);

        // 现在列表顺序：book2(东方时空) -> book(今日时报) -> book3(早间新闻)

        // LLEN
        var len = await redis.ListLengthAsync("book");                                       // 3

        // LINDEX
        var first = await redis.ListGetByIndexAsync<Book>("book", 0);                         // 东方时空

        // LSET
        await redis.ListSetByIndexAsync("book", 1, new Book("午间新闻"));

        // PeekRange（不弹出）
        var all = await redis.PeekRangeAsync<Book>("book");

        // LREM（移除指定元素）
        var removed = await redis.ListRemoveAsync("book", book2);

        // LTRIM（截取）
        await redis.ListTrimAsync("book", 0, -1);

        // RPOP（右侧弹出）
        var rightPop = await redis.ListRightPopAsync<Book>("book");                         // 早间新闻
        // LPOP（左侧弹出，Dequeue）
        var leftPop = await redis.DequeueAsync<Book>("book");                               // 午间新闻

        await redis.FlushDatabaseAsync();
        return Ok(new { len, first = first?.Title, all, removed, rightPop = rightPop?.Title, leftPop = leftPop?.Title });
    }

    [HttpPost]
    public async Task<ActionResult> SetTest()
    {
        string key = "SetTest";
        string key2 = "SetTest2";

        // SADD（单个）
        for (int i = 0; i < 10; i++)
            await redis.SetAddAsync(key, i + 1);

        // SADD（批量）
        await redis.SetAddMultipleAsync(key2, new[] { 5, 6, 7, 8, 9, 10, 11 });

        // SCARD
        var len1 = await redis.SetLengthAsync(key);                                          // 10

        // SREM
        await redis.SetRemoveAsync(key, new List<int> { 5, 6, 7, 8, 9 });

        // SMEMBERS
        var members = await redis.SetMembersAsync<string>(key);

        // SISMEMBER
        var exists = await redis.SetContainsAsync(key, 1);

        // SRANDMEMBER
        var random1 = await redis.SetRandomMemberAsync<string>(key);
        var randoms = await redis.SetRandomMembersAsync<string>(key, 3);

        // SPOP
        var popped = await redis.SetPopAsync<string>(key);

        // SMOVE
        await redis.SetMoveAsync(key, "dest_set", 2);

        // 集合运算：并集、交集、差集
        var union = await redis.SetCombineUnionAsync<int>(key, key2);
        var intersect = await redis.SetCombineIntersectAsync<int>(key, key2);
        var diff = await redis.SetCombineDifferenceAsync<int>(key, key2);

        await redis.FlushDatabaseAsync();
        return Ok(new { len1, members, exists, random1, randoms, popped, union, intersect, diff });
    }

    [HttpPost]
    public async Task<ActionResult> SortedSetTest()
    {
        string key = "Leaderboard";

        // ZADD（单个）
        await redis.SortedSetAddAsync(key, "player1", 100);
        await redis.SortedSetAddAsync(key, "player2", 200);
        await redis.SortedSetAddAsync(key, "player3", 150);

        // ZADD（批量）
        await redis.SortedSetAddMultipleAsync(key, new Dictionary<string, double>
        {
            ["player4"] = 180,
            ["player5"] = 250
        });

        // ZCARD
        var len = await redis.SortedSetLengthAsync(key);                                     // 5

        // ZCOUNT
        var count = await redis.SortedSetLengthByScoreAsync(key, 100, 200);

        // ZSCORE
        var score = await redis.SortedSetScoreAsync(key, "player1");                        // 100

        // ZRANK
        var rank = await redis.SortedSetRankAsync(key, "player5", Order.Descending);        // 0

        // ZINCRBY / ZDECRBY
        var newScore = await redis.SortedSetIncrementAsync(key, "player1", 50);             // 150
        await redis.SortedSetDecrementAsync(key, "player2", 30);                            // 170

        // ZRANGE WITHSCORES
        var top = await redis.SortedSetRangeByRankWithScoresAsync(key, 0, 2, Order.Descending);
        var byScore = await redis.SortedSetRangeByScoreWithScoresAsync(key, 100, 200);

        // ZREMRANGEBYRANK / ZREMRANGEBYSCORE
        await redis.SortedSetRemoveRangeByRankAsync(key, 0, 1);                              // 删除前2名
        await redis.SortedSetRemoveRangeByScoreAsync(key, 0, 100);

        await redis.FlushDatabaseAsync();
        return Ok(new { len, count, score, rank, newScore, top, byScore });
    }

    [HttpPost]
    public async Task<ActionResult> HashTest()
    {
        Book book = new("今日时报");

        // HSET
        await redis.HashSetAsync(nameof(Book), new Dictionary<string, string>
        {
            [nameof(book.Id)] = book.Id.ToString(),
            [nameof(book.Title)] = book.Title,
            [nameof(book.Created)] = book.Created.ToString()
        });

        // HSETNX
        var setnxOk = await redis.HashSetIfNotExistsAsync(nameof(Book), "status", "active");
        var setnxFail = await redis.HashSetIfNotExistsAsync(nameof(Book), "status", "inactive");

        // HGET (单字段)
        var title = await redis.HashGetSingleAsync(nameof(Book), nameof(book.Title));

        // HGETALL
        var all = await redis.HashGetAsync(nameof(Book));

        // HMGET
        var fields = await redis.HashGetFieldsAsync(nameof(Book),
            new[] { nameof(book.Id), nameof(book.Title) });

        // HEXISTS
        var fieldExists = await redis.HashFieldsExistsAsync(nameof(Book),
            new[] { nameof(book.Title) });

        // HLEN
        var len = await redis.HashLengthAsync(nameof(Book));

        // HKEYS / HVALS
        var keys = await redis.HashKeysAsync(nameof(Book));
        var vals = await redis.HashValuesAsync(nameof(Book));

        // HINCRBY / HINCRBYFLOAT
        await redis.HashSetAsync("stats", new Dictionary<string, string> { ["views"] = "0" });
        var views = await redis.HashIncrementAsync("stats", "views", 10);                   // 10
        var rate = await redis.HashIncrementByDoubleAsync("stats", "rate", 2.5);            // 2.5

        // HDEL (指定字段)
        await redis.HashDeleteFieldsAsync(nameof(Book), new[] { nameof(book.Created) });

        // HDEL (删除整个 Hash)
        await redis.HashDeleteAsync(nameof(Book));

        return Ok(new
        {
            setnxOk,
            setnxFail,
            title,
            all,
            fields,
            fieldExists,
            len,
            keys,
            vals,
            views,
            rate
        });
    }

    [HttpPost]
    public async Task<ActionResult> KeyTest()
    {
        // 设置一些数据
        await redis.StringSetAsync("temp_key", "hello", TimeSpan.FromSeconds(60));

        // EXISTS
        var exists = await redis.KeyExistsAsync("temp_key");                                // true

        // TYPE
        var type = await redis.KeyTypeAsync("temp_key");                                   // String

        // TTL
        var ttl = await redis.KeyTimeToLiveAsync("temp_key");                               // ~60s

        // RENAME
        await redis.KeyRenameAsync("temp_key", "renamed_key");

        // PERSIST（移除过期时间）
        await redis.KeyPersistAsync("renamed_key");

        // SCAN（获取所有 key）
        var allKeys = new List<string>();
        await foreach (var key in redis.GetAllKeysAsync())
        {
            allKeys.Add(key);
        }

        // 按模式删除
        for (int i = 0; i < 5; i++)
            await redis.StringSetAsync($"pattern:test:{i}", i);
        var deleted = await redis.KeyDeleteByPatternAsync("pattern:test:*");                // 5

        // DELETE（批量）
        var batchDeleted = await redis.KeyDeleteAsync(new[] { "renamed_key" });

        return Ok(new { exists, type, ttl, allKeys, deleted, batchDeleted });
    }

    [HttpPost]
    public async Task<ActionResult> AdvancedTest()
    {
        // 分布式锁（Lock/Unlock 分离）
        var lockAcquired = await redis.LockAcquireAsync("resource_key", "client_1", TimeSpan.FromSeconds(30));
        string? result = null;
        if (lockAcquired)
        {
            try
            {
                result = "critical section completed";
            }
            finally
            {
                await redis.LockReleaseAsync("resource_key", "client_1");
            }
        }

        // 事务（MULTI/EXEC）
        var tranSuccess = await redis.ExecuteTransactionAsync(tran =>
        {
            tran.StringSetAsync("tx_key1", "val1");
            tran.StringSetAsync("tx_key2", "val2");
            tran.StringIncrementAsync("tx_counter", 1);
        });

        // Batch 操作（非原子，高性能批量提交）
        await redis.ExecuteBatchAsync(batch =>
        {
            batch.StringSetAsync("batch_a", "1");
            batch.StringSetAsync("batch_b", "2");
        });

        await redis.FlushDatabaseAsync();
        return Ok(new { lockAcquired, result, tranSuccess });
    }
}
