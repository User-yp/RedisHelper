using StackExchange.Redis;
using System.Net;

namespace RedisHelper;

public interface IRedisHelper
{
    #region String

    Task<bool> StringSetAsync<T>(string key, T value);
    Task<bool> StringSetAsync<T>(string key, T value, TimeSpan timeSpan);
    /// <summary>
    /// 仅当 key 不存在时设置值（SETNX）
    /// </summary>
    Task<bool> StringSetIfNotExistsAsync<T>(string key, T value);
    /// <summary>
    /// 仅当 key 不存在时设置值，并指定过期时间
    /// </summary>
    Task<bool> StringSetIfNotExistsAsync<T>(string key, T value, TimeSpan timeSpan);
    Task<T?> StringGetAsync<T>(string key);
    /// <summary>
    /// 设置新值并返回旧值（GETSET）
    /// </summary>
    Task<T?> StringGetSetAsync<T>(string key, T value);
    /// <summary>
    /// 批量获取多个 key 的值
    /// </summary>
    Task<Dictionary<string, T?>> StringGetMultipleAsync<T>(IEnumerable<string> keys);
    /// <summary>
    /// 批量设置多个 key-value
    /// </summary>
    Task<bool> StringSetMultipleAsync<T>(IDictionary<string, T> keyValues);
    /// <summary>
    /// 获取字符串值的长度
    /// </summary>
    Task<long> StringLengthAsync(string key);
    /// <summary>
    /// 追加字符串值
    /// </summary>
    Task<long> StringAppendAsync(string key, string value);
    Task<double> StringIncrementAsync(string key, int value = 1);
    Task<double> StringDecrementAsync(string key, int value = 1);
    /// <summary>
    /// 浮点数自增
    /// </summary>
    Task<double> StringIncrementByDoubleAsync(string key, double value);

    #endregion


    /// <summary>
    /// 入队（右侧推入）。如果 key 不存在则自动创建
    /// </summary>
    Task<long> EnqueueAsync<T>(string key, T value);
    /// <summary>
    /// 左侧推入（LPUSH）
    /// </summary>
    Task<long> ListLeftPushAsync<T>(string key, T value);
    /// <summary>
    /// 从队列左侧取出数据（出队）
    /// </summary>
    Task<T?> DequeueAsync<T>(string key) where T : class;
    /// <summary>
    /// 从队列右侧取出数据（RPOP）
    /// </summary>
    Task<T?> ListRightPopAsync<T>(string key) where T : class;
    /// <summary>
    /// 从队列中读取数据而不出队
    /// </summary>
    /// <param name="start">起始位置</param>
    /// <param name="stop">结束位置，-1 表示到末尾</param>
    Task<IEnumerable<T>?> PeekRangeAsync<T>(string key, long start = 0, long stop = -1) where T : class;
    /// <summary>
    /// 获取 List 长度
    /// </summary>
    Task<long> ListLengthAsync(string key);
    /// <summary>
    /// 根据索引获取元素
    /// </summary>
    Task<T?> ListGetByIndexAsync<T>(string key, long index) where T : class;
    /// <summary>
    /// 根据索引设置元素
    /// </summary>
    Task ListSetByIndexAsync<T>(string key, long index, T value);
    /// <summary>
    /// 移除列表中指定数量的匹配元素（count > 0 从头部开始，count < 0 从尾部开始，count = 0 移除所有）
    /// </summary>
    Task<long> ListRemoveAsync<T>(string key, T value, long count = 0);
    /// <summary>
    /// 截取列表，只保留指定范围内的元素
    /// </summary>
    Task ListTrimAsync(string key, long start, long stop);

    #endregion


    Task<bool> SetAddAsync<T>(string key, T value);
    /// <summary>
    /// 批量添加元素到 Set
    /// </summary>
    Task<long> SetAddMultipleAsync<T>(string key, IEnumerable<T> values);
    Task<long> SetRemoveAsync<T>(string key, IEnumerable<T> values);
    Task<IEnumerable<T>?> SetMembersAsync<T>(string key) where T : class;
    Task<bool> SetContainsAsync<T>(string key, T value);
    /// <summary>
    /// 获取 Set 的元素数量
    /// </summary>
    Task<long> SetLengthAsync(string key);
    /// <summary>
    /// 随机获取一个元素（不移除）
    /// </summary>
    Task<T?> SetRandomMemberAsync<T>(string key) where T : class;
    /// <summary>
    /// 随机获取多个元素（不移除）
    /// </summary>
    Task<IEnumerable<T>?> SetRandomMembersAsync<T>(string key, long count) where T : class;
    /// <summary>
    /// 随机弹出一个元素
    /// </summary>
    Task<T?> SetPopAsync<T>(string key) where T : class;
    /// <summary>
    /// 将元素从源 Set 移动到目标 Set
    /// </summary>
    Task<bool> SetMoveAsync<T>(string sourceKey, string destinationKey, T value);
    /// <summary>
    /// 返回多个 Set 的并集
    /// </summary>
    Task<IEnumerable<T>?> SetCombineUnionAsync<T>(params string[] keys);
    /// <summary>
    /// 返回多个 Set 的交集
    /// </summary>
    Task<IEnumerable<T>?> SetCombineIntersectAsync<T>(params string[] keys);
    /// <summary>
    /// 返回多个 Set 的差集（第一个 key 减去其余 key）
    /// </summary>
    Task<IEnumerable<T>?> SetCombineDifferenceAsync<T>(params string[] keys);

    #endregion

    #region SortedSet

    Task<bool> SortedSetAddAsync(string key, string member, double score);
    Task<long> SortedSetRemoveAsync(string key, IEnumerable<string> members);
    Task<double> SortedSetIncrementAsync(string key, string member, double value);
    Task<double> SortedSetDecrementAsync(string key, string member, double value);
    /// <summary>
    /// 按排名返回指定范围的元素及分数
    /// </summary>
    Task<Dictionary<string, double>> SortedSetRangeByRankWithScoresAsync(string key,
        long start = 0,
        long stop = -1,
        Order order = Order.Ascending);
    /// <summary>
    /// 按分数返回指定范围的元素及分数
    /// </summary>
    Task<Dictionary<string, double>> SortedSetRangeByScoreWithScoresAsync(string key,
        double start = double.NegativeInfinity, double stop = double.PositiveInfinity,
        Exclude exclude = Exclude.None, Order order = Order.Ascending, long skip = 0, long take = -1);
    /// <summary>
    /// 获取 SortedSet 的元素数量
    /// </summary>
    Task<long> SortedSetLengthAsync(string key);
    /// <summary>
    /// 获取分数在指定范围内的元素数量
    /// </summary>
    Task<long> SortedSetLengthByScoreAsync(string key, double min = double.NegativeInfinity, double max = double.PositiveInfinity,
        Exclude exclude = Exclude.None);
    /// <summary>
    /// 获取成员的排名（升序，从 0 开始）
    /// </summary>
    Task<long?> SortedSetRankAsync(string key, string member, Order order = Order.Ascending);
    /// <summary>
    /// 获取成员的分数
    /// </summary>
    Task<double?> SortedSetScoreAsync(string key, string member);
    /// <summary>
    /// 移除指定排名范围内的元素
    /// </summary>
    Task<long> SortedSetRemoveRangeByRankAsync(string key, long start, long stop);
    /// <summary>
    /// 移除指定分数范围内的元素
    /// </summary>
    Task<long> SortedSetRemoveRangeByScoreAsync(string key, double start, double stop,
        Exclude exclude = Exclude.None);
    /// <summary>
    /// 批量添加成员到 SortedSet
    /// </summary>
    Task<long> SortedSetAddMultipleAsync(string key, IEnumerable<KeyValuePair<string, double>> members);



    Task<Dictionary<string, string>> HashGetAsync(string key);
    Task<Dictionary<string, string>?> HashGetFieldsAsync(string key, IEnumerable<string> fields);
    /// <summary>
    /// 获取单个字段的值
    /// </summary>
    Task<string?> HashGetSingleAsync(string key, string field);
    Task HashSetAsync(string key, IDictionary<string, string> entries);
    Task HashSetAsync(string key, IDictionary<string, string> entries, TimeSpan timeSpan);
    /// <summary>
    /// 设置单个字段。仅当字段不存在时设置（HSETNX）
    /// </summary>
    Task<bool> HashSetIfNotExistsAsync(string key, string field, string value);
    /// <summary>
    /// 设置 Hash 的指定字段。已存在的字段会被覆盖，不存在的字段会新增
    /// </summary>
    Task HashSetFieldsAsync(string key, IDictionary<string, string> fields);
    Task HashSetFieldsAsync(string key, IDictionary<string, string> fields, TimeSpan timeSpan);
    /// <summary>
    /// 如果 key 不存在则创建 Hash，否则更新指定字段
    /// </summary>
    Task HashSetOrCreateFieldsAsync(string key, IDictionary<string, string> fields);
    Task HashSetOrCreateFieldsAsync(string key, IDictionary<string, string> fields, TimeSpan timeSpan);
    Task<bool> HashFieldsExistsAsync(string key, IEnumerable<string> fields);
    Task<bool> HashDeleteAsync(string key);
    /// <summary>
    /// 删除 Hash 的指定字段
    /// </summary>
    Task<bool> HashDeleteFieldsAsync(string key, IEnumerable<string> fields);
    /// <summary>
    /// 获取 Hash 的字段数量
    /// </summary>
    Task<long> HashLengthAsync(string key);
    /// <summary>
    /// 获取 Hash 的所有字段名
    /// </summary>
    Task<IEnumerable<string>> HashKeysAsync(string key);
    /// <summary>
    /// 获取 Hash 的所有值
    /// </summary>
    Task<IEnumerable<string>> HashValuesAsync(string key);
    /// <summary>
    /// 对 Hash 的指定字段进行整数自增
    /// </summary>
    Task<long> HashIncrementAsync(string key, string field, long value = 1);
    /// <summary>
    /// 对 Hash 的指定字段进行浮点数自增
    /// </summary>
    Task<double> HashIncrementByDoubleAsync(string key, string field, double value);

    #endregion



    /// <summary>
    /// 获取所有 Key（使用 SCAN，不会阻塞 Redis）
    /// </summary>
    IAsyncEnumerable<string> GetAllKeysAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// 获取指定节点的所有 Key（使用 SCAN，不会阻塞 Redis）
    /// </summary>
    IAsyncEnumerable<string> GetAllKeysAsync(EndPoint endPoint, CancellationToken cancellationToken = default);
    Task<bool> KeyExistsAsync(string key);
    /// <summary>
    /// 删除给定 Key
    /// </summary>
    /// <param name="keys">待删除的 key 集合</param>
    /// <returns>删除 key 的数量</returns>
    Task<long> KeyDeleteAsync(IEnumerable<string> keys);
    /// <summary>
    /// 删除给定 Key
    /// </summary>
    /// <param name="key">待删除的 key</param>
    /// <returns>是否成功删除</returns>
    Task<bool> KeyDeleteAsync(string key);
    /// <summary>
    /// 根据模式匹配删除 Key（使用 SCAN 安全遍历）
    /// </summary>
    /// <param name="pattern">匹配模式，如 "user:*"</param>
    /// <returns>删除的 key 数量</returns>
    Task<long> KeyDeleteByPatternAsync(string pattern);
    /// <summary>
    /// 清空当前数据库的所有 Key（等效于 FLUSHDB）
    /// </summary>
    Task FlushDatabaseAsync();
    /// <summary>
    /// 设置指定 key 过期时间
    /// </summary>
    Task<bool> KeyExpireAsync(string key, TimeSpan? expiry);
    Task<bool> KeyExpireAsync(string key, DateTime? expiry);
    /// <summary>
    /// 获取 key 的剩余生存时间
    /// </summary>
    Task<TimeSpan?> KeyTimeToLiveAsync(string key);
    /// <summary>
    /// 重命名 key
    /// </summary>
    Task<bool> KeyRenameAsync(string oldKey, string newKey);
    /// <summary>
    /// 移除 key 的过期时间，使其永久有效
    /// </summary>
    Task<bool> KeyPersistAsync(string key);
    /// <summary>
    /// 获取 key 的类型
    /// </summary>
    Task<RedisType> KeyTypeAsync(string key);

    #endregion


    Task<long> PublishAsync(string channel, string msg);
    /// <summary>
    /// 订阅频道。返回可用来取消订阅的 ChannelMessageQueue
    /// </summary>
    ChannelMessageQueue Subscribe(string channel, Action<string, string> handler);
    /// <summary>
    /// 模式订阅（PSUBSCRIBE）。返回可用来取消订阅的 ChannelMessageQueue
    /// </summary>
    ChannelMessageQueue SubscribeByPattern(string pattern, Action<string, string> handler);
    /// <summary>
    /// 批量执行 Redis 操作
    /// </summary>
    /// <param name="operations">每个操作接收 IBatch 对象，调用其上方法即可加入批处理</param>
    Task ExecuteBatchAsync(params Action<IBatch>[] operations);
    /// <summary>
    /// 执行事务（MULTI/EXEC）。所有操作原子执行
    /// </summary>
    /// <param name="operations">事务内的操作，接收 ITransaction 对象</param>
    Task<bool> ExecuteTransactionAsync(params Action<ITransaction>[] operations);

    /// <summary>
    /// 获取分布式锁（非阻塞）
    /// </summary>
    /// <param name="key">锁的 key</param>
    /// <param name="value">锁的值（用于解锁时校验）</param>
    /// <param name="expiry">持锁超时时间</param>
    /// <returns>是否获取成功</returns>
    Task<bool> LockAcquireAsync(string key, string value, TimeSpan expiry);
    /// <summary>
    /// 释放分布式锁
    /// </summary>
    /// <param name="key">锁的 key</param>
    /// <param name="value">锁的值（必须与加锁时一致才能释放）</param>
    Task<bool> LockReleaseAsync(string key, string value);

    /// <summary>
    /// 获取分布式锁并执行（非阻塞。加锁失败直接返回 (false, null)）
    /// </summary>
    /// <param name="key">要锁定的 key</param>
    /// <param name="value">锁定的 value，加锁时赋值 value，在解锁时必须是同一个 value 的客户端才能解锁</param>
    /// <param name="del">加锁成功时执行的业务方法</param>
    /// <param name="expiry">持锁超时时间。超时后锁自动释放</param>
    /// <param name="args">业务方法参数</param>
    /// <returns>(success, return value of the del)</returns>
    Task<(bool, object?)> LockExecuteAsync(string key, string value, Delegate del, TimeSpan expiry, params object[] args);

    /// <summary>
    /// 获取分布式锁并执行（阻塞。直到成功加锁或超时）
    /// </summary>
    /// <param name="key">要锁定的 key</param>
    /// <param name="value">锁定的 value，加锁时赋值 value，在解锁时必须是同一个 value 的客户端才能解锁</param>
    /// <param name="del">加锁成功时执行的业务方法</param>
    /// <param name="result">del 返回值</param>
    /// <param name="expiry">持锁超时时间。超时后锁自动释放</param>
    /// <param name="timeout">加锁超时时间(ms)。0 表示永不超时</param>
    /// <param name="args">业务方法参数</param>
    /// <returns>success</returns>
    bool LockExecute(string key, string value, Delegate del, out object? result, TimeSpan expiry, int timeout = 0, params object[] args);

    bool LockExecute(string key, string value, Action action, TimeSpan expiry, int timeout = 0);

    bool LockExecute<T>(string key, string value, Action<T> action, T arg, TimeSpan expiry, int timeout = 0);

    bool LockExecute<T>(string key, string value, Func<T> func, out T? result, TimeSpan expiry, int timeout = 0);

    bool LockExecute<T, TResult>(string key, string value, Func<T, TResult> func, T arg, out TResult? result, TimeSpan expiry, int timeout = 0);

}
