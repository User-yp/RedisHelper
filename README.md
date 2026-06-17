# RedisHelper

一个基于 [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis) 的 .NET Redis 辅助库，提供常用 Redis 数据类型的简化操作接口。

## 支持的功能

| 数据类型 | 支持的操作 |
|---------|-----------|
| **String** | Set、SetIfNotExists（SETNX）、Get、GetSet（GETSET）、GetMultiple（MGET）、SetMultiple（MSET）、Length、Append、Increment、Decrement、IncrementByDouble |
| **List** | Enqueue（RPUSH）、LeftPush（LPUSH）、Dequeue（LPOP）、RightPop（RPOP）、PeekRange（LRANGE）、Length、GetByIndex、SetByIndex、Remove、Trim |
| **Set** | Add、AddMultiple、Remove、Members、Contains、Length、RandomMember、RandomMembers、Pop、Move、CombineUnion、CombineIntersect、CombineDifference |
| **Sorted Set** | Add、AddMultiple、Remove、Increment、Decrement、RangeByRank、RangeByScore、Length、LengthByScore、Rank、Score、RemoveRangeByRank、RemoveRangeByScore |
| **Hash** | Get（HGETALL）、GetSingle（HGET）、GetFields（HMGET）、Set（HSET）、SetIfNotExists（HSETNX）、SetFields、SetOrCreateFields、FieldsExists、Delete、DeleteFields、Length、Keys、Values、Increment、IncrementByDouble |
| **Key** | Exists、Delete（单个/批量）、DeleteByPattern、Expire、TimeToLive、Rename、Persist、Type、GetAllKeys（SCAN）、FlushDatabase |
| **Advanced** | Publish、Subscribe、SubscribeByPattern、Batch、Transaction（MULTI/EXEC）、LockAcquire/Release、LockExecute（阻塞/非阻塞） |

## 快速开始

### 安装

```bash
dotnet add package RedisHelper
```

### 注册服务

**方式一：通过配置文件**

```json
// appsettings.json
{
  "Redis": {
    "ConnectionString": "127.0.0.1:6379",
    "DbNumber": 0
  }
}
```

```csharp
builder.Services.AddRedisHelper(builder.Configuration.GetSection("Redis"));
```

**方式二：通过代码传参**

```csharp
builder.Services.AddRedisHelper("127.0.0.1:6379", dbNumber: 0);
```

### 使用示例

```csharp
public class MyService
{
    private readonly IRedisHelper _redis;

    public MyService(IRedisHelper redis) => _redis = redis;

    public async Task ExamplesAsync()
    {
        // ===== String 操作 =====
        await _redis.StringSetAsync("key", "value", TimeSpan.FromMinutes(10));
        var value = await _redis.StringGetAsync<string>("key");

        // SETNX（分布式锁简单场景）
        var acquired = await _redis.StringSetIfNotExistsAsync("lock:order:123", "owner1", TimeSpan.FromSeconds(30));

        // GETSET
        var old = await _redis.StringGetSetAsync("key", "new_value");

        // MGET / MSET
        var dict = await _redis.StringGetMultipleAsync<string>(new[] { "k1", "k2", "k3" });
        await _redis.StringSetMultipleAsync(new Dictionary<string, string> { ["k1"] = "a", ["k2"] = "b" });

        // INCR / INCRBYFLOAT
        await _redis.StringIncrementAsync("views", 1);
        await _redis.StringIncrementByDoubleAsync("price", 0.5);

        // ===== List 操作 =====
        await _redis.EnqueueAsync("queue", "item1");              // RPUSH
        await _redis.ListLeftPushAsync("queue", "urgent");        // LPUSH
        var item = await _redis.DequeueAsync<string>("queue");    // LPOP
        var tail = await _redis.ListRightPopAsync<string>("queue"); // RPOP
        var all = await _redis.PeekRangeAsync<MyObj>("queue", 0, -1);
        await _redis.ListTrimAsync("queue", 0, 99);               // 保留前 100 个

        // ===== Set 操作 =====
        await _redis.SetAddAsync("tags", "redis");
        await _redis.SetAddMultipleAsync("tags", new[] { "csharp", "dotnet" });
        var members = await _redis.SetMembersAsync<string>("tags");
        var isMember = await _redis.SetContainsAsync("tags", "redis");
        var random = await _redis.SetRandomMemberAsync<string>("tags");

        // 集合运算
        var union = await _redis.SetCombineUnionAsync<string>("set1", "set2");
        var inter = await _redis.SetCombineIntersectAsync<string>("set1", "set2");
        var diff = await _redis.SetCombineDifferenceAsync<string>("set1", "set2");

        // ===== Sorted Set 操作 =====
        await _redis.SortedSetAddAsync("leaderboard", "player1", 100);
        await _redis.SortedSetAddMultipleAsync("leaderboard", new Dictionary<string, double>
        {
            ["player2"] = 200, ["player3"] = 150
        });
        var rank = await _redis.SortedSetRankAsync("leaderboard", "player1", Order.Descending);
        var score = await _redis.SortedSetScoreAsync("leaderboard", "player1");
        var top10 = await _redis.SortedSetRangeByRankWithScoresAsync("leaderboard", 0, 9, Order.Descending);

        // ===== Hash 操作 =====
        await _redis.HashSetAsync("user:1", new Dictionary<string, string>
        {
            ["name"] = "张三", ["email"] = "zhangsan@example.com"
        });
        var name = await _redis.HashGetSingleAsync("user:1", "name");
        var user = await _redis.HashGetAsync("user:1");
        await _redis.HashIncrementAsync("user:1", "login_count", 1);

        // ===== Key 操作 =====
        var exists = await _redis.KeyExistsAsync("key");
        var ttl = await _redis.KeyTimeToLiveAsync("key");
        await _redis.KeyExpireAsync("key", TimeSpan.FromHours(1));
        await _redis.KeyRenameAsync("old_key", "new_key");
        await _redis.KeyPersistAsync("key");                      // 移除过期时间
        var deleted = await _redis.KeyDeleteByPatternAsync("temp:*"); // 按模式删除

        // ===== 分布式锁 =====
        // 方式一：Lock/Unlock 分离
        if (await _redis.LockAcquireAsync("lock:resource", "client1", TimeSpan.FromSeconds(30)))
        {
            try { /* 临界区代码 */ }
            finally { await _redis.LockReleaseAsync("lock:resource", "client1"); }
        }

        // 方式二：LockExecute 自动管理（非阻塞）
        var (success, result) = await _redis.LockExecuteAsync(
            "lock:resource", "client1",
            () => DoWork(),
            TimeSpan.FromSeconds(30));

        // 方式三：LockExecute 阻塞等待
        _redis.LockExecute("lock:resource", "client1",
            () => Console.WriteLine("critical section"),
            expiry: TimeSpan.FromSeconds(30),
            timeout: 5000);

        // ===== 事务 =====
        await _redis.ExecuteTransactionAsync(tran =>
        {
            tran.StringSetAsync("k1", "v1");
            tran.StringSetAsync("k2", "v2");
        });

        // ===== 批处理 =====
        await _redis.ExecuteBatchAsync(batch =>
        {
            batch.StringSetAsync("k1", "v1");
            batch.StringSetAsync("k2", "v2");
        });

        // ===== 发布订阅 =====
        var queue = _redis.Subscribe("channel", (ch, msg) => Console.WriteLine($"{ch}: {msg}"));
        var patternQueue = _redis.SubscribeByPattern("user:*", (ch, msg) => Console.WriteLine($"pattern: {msg}"));
        await _redis.PublishAsync("channel", "hello");

        // 取消订阅
        await queue.UnsubscribeAsync();
        await patternQueue.UnsubscribeAsync();
    }
}
```

## RedisHelperOptions

| 属性 | 说明 | 默认值 |
|------|------|--------|
| `ConnectionString` | Redis 连接字符串（必填） | - |
| `DbNumber` | 数据库编号（0-15） | 0 |

## 支持的 .NET 版本

- .NET 6.0
- .NET 7.0
- .NET 8.0

## License

MIT
