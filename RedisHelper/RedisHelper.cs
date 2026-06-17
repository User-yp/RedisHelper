using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Net;
using System.Runtime.CompilerServices;
using Timer = System.Timers.Timer;

namespace RedisHelper;

public class RedisHelper : IRedisHelper
{
    private readonly ConnectionMultiplexer _conn;
    private readonly IDatabase _db;

    public RedisHelper(IOptionsMonitor<RedisHelperOptions> options) : this(options.CurrentValue)
    {
    }

    public RedisHelper(RedisHelperOptions options)
    {
        var connectionString = options.ConnectionString;
        _conn = ConnectionMultiplexer.Connect(connectionString);

        var dbNumber = options.DbNumber;
        _db = _conn.GetDatabase(dbNumber);
    }

    #region String

    public async Task<bool> StringSetAsync<T>(string key, T value)
    {
        return await _db.StringSetAsync(key, value.ToRedisValue());
    }

    public async Task<bool> StringSetAsync<T>(string key, T value, TimeSpan timeSpan)
    {
        return await _db.StringSetAsync(key, value.ToRedisValue(), timeSpan);
    }

    public async Task<bool> StringSetIfNotExistsAsync<T>(string key, T value)
    {
        return await _db.StringSetAsync(key, value.ToRedisValue(), when: When.NotExists);
    }

    public async Task<bool> StringSetIfNotExistsAsync<T>(string key, T value, TimeSpan timeSpan)
    {
        return await _db.StringSetAsync(key, value.ToRedisValue(), timeSpan, when: When.NotExists);
    }

    public async Task<T?> StringGetAsync<T>(string key)
    {
        return (await _db.StringGetAsync(key)).ToObject<T>();
    }

    public async Task<T?> StringGetSetAsync<T>(string key, T value)
    {
        return (await _db.StringGetSetAsync(key, value.ToRedisValue())).ToObject<T>();
    }

    public async Task<Dictionary<string, T?>> StringGetMultipleAsync<T>(IEnumerable<string> keys)
    {
        var keyArray = keys as string[] ?? keys.ToArray();
        var redisKeys = keyArray.Select(k => (RedisKey)k).ToArray();
        var values = await _db.StringGetAsync(redisKeys);
        return keyArray.Zip(values, (key, value) => new { key, value })
            .ToDictionary(x => x.key, x => x.value.ToObject<T>());
    }

    public async Task<bool> StringSetMultipleAsync<T>(IDictionary<string, T> keyValues)
    {
        var pairs = keyValues.Select(kv => new KeyValuePair<RedisKey, RedisValue>(kv.Key, kv.Value.ToRedisValue()))
            .ToArray();
        return await _db.StringSetAsync(pairs);
    }

    public async Task<long> StringLengthAsync(string key)
    {
        return await _db.StringLengthAsync(key);
    }

    public async Task<long> StringAppendAsync(string key, string value)
    {
        return await _db.StringAppendAsync(key, value);
    }

    public async Task<double> StringIncrementAsync(string key, int value = 1)
    {
        return await _db.StringIncrementAsync(key, value);
    }

    public async Task<double> StringDecrementAsync(string key, int value = 1)
    {
        return await _db.StringDecrementAsync(key, value);
    }

    public async Task<double> StringIncrementByDoubleAsync(string key, double value)
    {
        return await _db.StringIncrementAsync(key, value);
    }

    #endregion

    #region List

    public async Task<long> EnqueueAsync<T>(string key, T value)
    {
        return await _db.ListRightPushAsync(key, value.ToRedisValue());
    }

    public async Task<long> ListLeftPushAsync<T>(string key, T value)
    {
        return await _db.ListLeftPushAsync(key, value.ToRedisValue());
    }

    public async Task<T?> DequeueAsync<T>(string key) where T : class =>
        (await _db.ListLeftPopAsync(key)).ToObject<T>();

    public async Task<T?> ListRightPopAsync<T>(string key) where T : class =>
        (await _db.ListRightPopAsync(key)).ToObject<T>();

    public async Task<IEnumerable<T>?> PeekRangeAsync<T>(string key, long start = 0, long stop = -1) where T : class =>
        (await _db.ListRangeAsync(key, start, stop)).ToObjects<T>();

    public async Task<long> ListLengthAsync(string key)
    {
        return await _db.ListLengthAsync(key);
    }

    public async Task<T?> ListGetByIndexAsync<T>(string key, long index) where T : class =>
        (await _db.ListGetByIndexAsync(key, index)).ToObject<T>();

    public async Task ListSetByIndexAsync<T>(string key, long index, T value)
    {
        await _db.ListSetByIndexAsync(key, index, value.ToRedisValue());
    }

    public async Task<long> ListRemoveAsync<T>(string key, T value, long count = 0)
    {
        return await _db.ListRemoveAsync(key, value.ToRedisValue(), count);
    }

    public async Task ListTrimAsync(string key, long start, long stop)
    {
        await _db.ListTrimAsync(key, start, stop);
    }

    #endregion

    #region Set

    public async Task<bool> SetAddAsync<T>(string key, T value)
    {
        return await _db.SetAddAsync(key, value.ToRedisValue());
    }

    public async Task<long> SetAddMultipleAsync<T>(string key, IEnumerable<T> values)
    {
        return await _db.SetAddAsync(key, values.ToRedisValues());
    }

    public async Task<long> SetRemoveAsync<T>(string key, IEnumerable<T> values)
    {
        return await _db.SetRemoveAsync(key, values.ToRedisValues());
    }

    public async Task<IEnumerable<T>?> SetMembersAsync<T>(string key) where T : class
    {
        return (await _db.SetMembersAsync(key)).ToObjects<T>();
    }

    public async Task<bool> SetContainsAsync<T>(string key, T value)
    {
        return await _db.SetContainsAsync(key, value.ToRedisValue());
    }

    public async Task<long> SetLengthAsync(string key)
    {
        return await _db.SetLengthAsync(key);
    }

    public async Task<T?> SetRandomMemberAsync<T>(string key) where T : class =>
        (await _db.SetRandomMemberAsync(key)).ToObject<T>();

    public async Task<IEnumerable<T>?> SetRandomMembersAsync<T>(string key, long count) where T : class =>
        (await _db.SetRandomMembersAsync(key, count)).ToObjects<T>();

    public async Task<T?> SetPopAsync<T>(string key) where T : class =>
        (await _db.SetPopAsync(key)).ToObject<T>();

    public async Task<bool> SetMoveAsync<T>(string sourceKey, string destinationKey, T value)
    {
        return await _db.SetMoveAsync(sourceKey, destinationKey, value.ToRedisValue());
    }

    public async Task<IEnumerable<T>?> SetCombineUnionAsync<T>(params string[] keys)
    {
        var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
        return (await _db.SetCombineAsync(SetOperation.Union, redisKeys)).ToObjects<T>();
    }

    public async Task<IEnumerable<T>?> SetCombineIntersectAsync<T>(params string[] keys)
    {
        var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
        return (await _db.SetCombineAsync(SetOperation.Intersect, redisKeys)).ToObjects<T>();
    }

    public async Task<IEnumerable<T>?> SetCombineDifferenceAsync<T>(params string[] keys)
    {
        var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
        return (await _db.SetCombineAsync(SetOperation.Difference, redisKeys)).ToObjects<T>();
    }

    #endregion

    #region SortedSet

    public async Task<bool> SortedSetAddAsync(string key, string member, double score)
    {
        return await _db.SortedSetAddAsync(key, member, score);
    }

    public async Task<long> SortedSetRemoveAsync(string key, IEnumerable<string> members)
    {
        return await _db.SortedSetRemoveAsync(key, members.ToRedisValues());
    }

    public async Task<double> SortedSetIncrementAsync(string key, string member, double value)
    {
        return await _db.SortedSetIncrementAsync(key, member, value);
    }

    public async Task<double> SortedSetDecrementAsync(string key, string member, double value)
    {
        return await _db.SortedSetDecrementAsync(key, member, value);
    }

    public async Task<Dictionary<string, double>> SortedSetRangeByRankWithScoresAsync(string key,
        long start = 0,
        long stop = -1,
        Order order = Order.Ascending)
    {
        return (await _db.SortedSetRangeByRankWithScoresAsync(key, start, stop, order)).ToSortedSetDictionary();
    }

    public async Task<Dictionary<string, double>> SortedSetRangeByScoreWithScoresAsync(string key,
        double start = double.NegativeInfinity, double stop = double.PositiveInfinity,
        Exclude exclude = Exclude.None, Order order = Order.Ascending, long skip = 0, long take = -1)
    {
        return (await _db.SortedSetRangeByScoreWithScoresAsync(key, start, stop, exclude, order, skip, take))
            .ToSortedSetDictionary();
    }

    public async Task<long> SortedSetLengthAsync(string key)
    {
        return await _db.SortedSetLengthAsync(key);
    }

    public async Task<long> SortedSetLengthByScoreAsync(string key, double min = double.NegativeInfinity,
        double max = double.PositiveInfinity, Exclude exclude = Exclude.None)
    {
        return await _db.SortedSetLengthAsync(key, min, max, exclude);
    }

    public async Task<long?> SortedSetRankAsync(string key, string member, Order order = Order.Ascending)
    {
        return await _db.SortedSetRankAsync(key, member, order);
    }

    public async Task<double?> SortedSetScoreAsync(string key, string member)
    {
        return await _db.SortedSetScoreAsync(key, member);
    }

    public async Task<long> SortedSetRemoveRangeByRankAsync(string key, long start, long stop)
    {
        return await _db.SortedSetRemoveRangeByRankAsync(key, start, stop);
    }

    public async Task<long> SortedSetRemoveRangeByScoreAsync(string key, double start, double stop,
        Exclude exclude = Exclude.None)
    {
        return await _db.SortedSetRemoveRangeByScoreAsync(key, start, stop, exclude);
    }

    public async Task<long> SortedSetAddMultipleAsync(string key, IEnumerable<KeyValuePair<string, double>> members)
    {
        var entries = members.Select(m => new SortedSetEntry(m.Key, m.Value)).ToArray();
        return await _db.SortedSetAddAsync(key, entries);
    }

    #endregion

    #region Hash

    public async Task<Dictionary<string, string>> HashGetAsync(string key)
    {
        return (await _db.HashGetAllAsync(key)).ToHashDictionary();
    }

    public async Task<Dictionary<string, string>?> HashGetFieldsAsync(string key, IEnumerable<string> fields)
    {
        return (await _db.HashGetAsync(key, fields.ToRedisValues())).ToHashDictionary(fields);
    }

    public async Task<string?> HashGetSingleAsync(string key, string field)
    {
        return (await _db.HashGetAsync(key, field)).ToString();
    }

    public async Task HashSetAsync(string key, IDictionary<string, string> entries)
    {
        var val = entries.ToHashEntries();
        if (val != null)
            await _db.HashSetAsync(key, val);
    }

    public async Task HashSetAsync(string key, IDictionary<string, string> entries, TimeSpan timeSpan)
    {
        var val = entries.ToHashEntries();
        if (val != null)
            await _db.HashSetAsync(key, val);
        await _db.KeyExpireAsync(key, timeSpan);
    }

    public async Task<bool> HashSetIfNotExistsAsync(string key, string field, string value)
    {
        return await _db.HashSetAsync(key, field, value, When.NotExists);
    }

    public async Task HashSetFieldsAsync(string key, IDictionary<string, string> fields)
    {
        if (fields == null || fields.Count == 0)
            return;

        var hs = await HashGetAsync(key);
        foreach (var field in fields)
        {
            hs[field.Key] = field.Value;
        }
        await HashSetAsync(key, hs);
    }

    public async Task HashSetFieldsAsync(string key, IDictionary<string, string> fields, TimeSpan timeSpan)
    {
        if (fields == null || fields.Count == 0)
            return;

        var hs = await HashGetAsync(key);
        foreach (var field in fields)
        {
            hs[field.Key] = field.Value;
        }
        await HashSetAsync(key, hs);
        await _db.KeyExpireAsync(key, timeSpan);
    }

    public async Task HashSetOrCreateFieldsAsync(string key, IDictionary<string, string> fields)
    {
        if (!await KeyExistsAsync(key))
        {
            await HashSetAsync(key, fields);
        }
        else
        {
            if (fields == null || fields.Count == 0)
                return;

            var hs = await HashGetAsync(key);
            foreach (var field in fields)
            {
                hs[field.Key] = field.Value;
            }
            await HashSetAsync(key, hs);
        }
    }

    public async Task HashSetOrCreateFieldsAsync(string key, IDictionary<string, string> fields, TimeSpan timeSpan)
    {
        if (!await KeyExistsAsync(key))
        {
            await HashSetAsync(key, fields);
        }
        else
        {
            if (fields == null || fields.Count == 0)
                return;

            var hs = await HashGetAsync(key);
            foreach (var field in fields)
            {
                hs[field.Key] = field.Value;
            }
            await HashSetAsync(key, hs);
            await _db.KeyExpireAsync(key, timeSpan);
        }
    }

    public async Task<bool> HashFieldsExistsAsync(string key, IEnumerable<string> fields)
    {
        if (!await KeyExistsAsync(key))
            return false;

        var dic = await HashGetFieldsAsync(key, fields);
        if (dic == null)
            return false;

        foreach (var field in fields)
        {
            if (!dic.TryGetValue(field, out var value) || value == null)
                return false;
        }
        return true;
    }

    public async Task<bool> HashDeleteAsync(string key)
    {
        return await KeyDeleteAsync(new[] { key }) > 0;
    }

    public async Task<bool> HashDeleteFieldsAsync(string key, IEnumerable<string> fields)
    {
        if (fields == null || !fields.Any())
            return false;

        var success = true;
        foreach (var field in fields)
        {
            if (!await _db.HashDeleteAsync(key, field))
                success = false;
        }
        return success;
    }

    public async Task<long> HashLengthAsync(string key)
    {
        return await _db.HashLengthAsync(key);
    }

    public async Task<IEnumerable<string>> HashKeysAsync(string key)
    {
        return (await _db.HashKeysAsync(key)).Select(k => k.ToString());
    }

    public async Task<IEnumerable<string>> HashValuesAsync(string key)
    {
        return (await _db.HashValuesAsync(key)).Select(v => v.ToString());
    }

    public async Task<long> HashIncrementAsync(string key, string field, long value = 1)
    {
        return await _db.HashIncrementAsync(key, field, value);
    }

    public async Task<double> HashIncrementByDoubleAsync(string key, string field, double value)
    {
        return await _db.HashIncrementAsync(key, field, value);
    }

    #endregion

    #region Key

    public async IAsyncEnumerable<string> GetAllKeysAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var endPoint in _conn.GetEndPoints())
        {
            var server = _conn.GetServer(endPoint);
            await foreach (var key in server.KeysAsync().WithCancellation(cancellationToken))
            {
                yield return key.ToString();
            }
        }
    }

    public async IAsyncEnumerable<string> GetAllKeysAsync(EndPoint endPoint,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var server = _conn.GetServer(endPoint);
        await foreach (var key in server.KeysAsync().WithCancellation(cancellationToken))
        {
            yield return key.ToString();
        }
    }

    public async Task<bool> KeyExistsAsync(string key)
    {
        return await _db.KeyExistsAsync(key);
    }

    public async Task<long> KeyDeleteAsync(IEnumerable<string> keys)
    {
        return await _db.KeyDeleteAsync(keys.Select(k => (RedisKey)k).ToArray());
    }

    public async Task<bool> KeyDeleteAsync(string key)
    {
        return await _db.KeyDeleteAsync(key);
    }

    public async Task<long> KeyDeleteByPatternAsync(string pattern)
    {
        long deleted = 0;
        foreach (var endPoint in _conn.GetEndPoints())
        {
            var server = _conn.GetServer(endPoint);
            await foreach (var key in server.KeysAsync(pattern: pattern))
            {
                if (await _db.KeyDeleteAsync(key))
                    deleted++;
            }
        }
        return deleted;
    }

    public async Task FlushDatabaseAsync()
    {
        foreach (var endPoint in _conn.GetEndPoints())
        {
            var server = _conn.GetServer(endPoint);
            await server.FlushDatabaseAsync(_db.Database);
        }
    }

    public async Task<bool> KeyExpireAsync(string key, TimeSpan? expiry)
    {
        return await _db.KeyExpireAsync(key, expiry);
    }

    public async Task<bool> KeyExpireAsync(string key, DateTime? expiry)
    {
        return await _db.KeyExpireAsync(key, expiry);
    }

    public async Task<TimeSpan?> KeyTimeToLiveAsync(string key)
    {
        return await _db.KeyTimeToLiveAsync(key);
    }

    public async Task<bool> KeyRenameAsync(string oldKey, string newKey)
    {
        return await _db.KeyRenameAsync(oldKey, newKey);
    }

    public async Task<bool> KeyPersistAsync(string key)
    {
        return await _db.KeyPersistAsync(key);
    }

    public async Task<RedisType> KeyTypeAsync(string key)
    {
        return await _db.KeyTypeAsync(key);
    }

    #endregion

    #region Advanced

    public async Task<long> PublishAsync(string channel, string msg)
    {
        return await _conn.GetSubscriber().PublishAsync(RedisChannel.Literal(channel), msg);
    }

    public ChannelMessageQueue Subscribe(string channel, Action<string, string> handler)
    {
        var queue = _conn.GetSubscriber().Subscribe(RedisChannel.Literal(channel));
        queue.OnMessage(cm => handler(cm.Channel.ToString(), cm.Message.ToString()));
        return queue;
    }

    public ChannelMessageQueue SubscribeByPattern(string pattern, Action<string, string> handler)
    {
        var queue = _conn.GetSubscriber().Subscribe(RedisChannel.Pattern(pattern));
        queue.OnMessage(cm => handler(cm.Channel.ToString(), cm.Message.ToString()));
        return queue;
    }

    public Task ExecuteBatchAsync(params Action<IBatch>[] operations)
    {
        var batch = _db.CreateBatch();
        foreach (var operation in operations)
            operation(batch);
        batch.Execute();
        return Task.CompletedTask;
    }

    public async Task<bool> ExecuteTransactionAsync(params Action<ITransaction>[] operations)
    {
        var tran = _db.CreateTransaction();
        foreach (var operation in operations)
            operation(tran);
        return await tran.ExecuteAsync();
    }

    public async Task<bool> LockAcquireAsync(string key, string value, TimeSpan expiry)
    {
        return await _db.LockTakeAsync(key, value, expiry);
    }

    public Task<bool> LockReleaseAsync(string key, string value)
    {
        return _db.LockReleaseAsync(key, value);
    }

    public async Task<(bool, object?)> LockExecuteAsync(string key, string value, Delegate del,
        TimeSpan expiry, params object[] args)
    {
        if (!await _db.LockTakeAsync(key, value, expiry))
            return (false, null);

        try
        {
            return (true, del.DynamicInvoke(args ?? Array.Empty<object>()));
        }
        finally
        {
            await _db.LockReleaseAsync(key, value);
        }
    }

    public bool LockExecute(string key, string value, Delegate del, out object? result, TimeSpan expiry,
        int timeout = 0, params object[] args)
    {
        result = null;
        if (!GetLock(key, value, expiry, timeout))
            return false;

        try
        {
            result = del.DynamicInvoke(args ?? Array.Empty<object>());
            return true;
        }
        finally
        {
            _db.LockRelease(key, value);
        }
    }

    public bool LockExecute(string key, string value, Action action, TimeSpan expiry, int timeout = 0)
    {
        return LockExecute(key, value, action, out var _, expiry, timeout);
    }

    public bool LockExecute<T>(string key, string value, Action<T> action, T arg, TimeSpan expiry, int timeout = 0)
    {
        return LockExecute(key, value, action, out var _, expiry, timeout, arg!);
    }

    public bool LockExecute<T>(string key, string value, Func<T> func, out T? result, TimeSpan expiry,
        int timeout = 0)
    {
        result = default;
        if (!GetLock(key, value, expiry, timeout))
            return false;
        try
        {
            result = func();
            return true;
        }
        finally
        {
            _db.LockRelease(key, value);
        }
    }

    public bool LockExecute<T, TResult>(string key, string value, Func<T, TResult> func, T arg, out TResult? result,
        TimeSpan expiry, int timeout = 0)
    {
        result = default;
        if (!GetLock(key, value, expiry, timeout))
            return false;
        try
        {
            result = func(arg);
            return true;
        }
        finally
        {
            _db.LockRelease(key, value);
        }
    }

    private bool GetLock(string key, string value, TimeSpan expiry, int timeout)
    {
        using var waitHandle = new AutoResetEvent(false);
        Timer? timer = null;
        try
        {
            timer = new Timer(1000);
            timer.Elapsed += (_, _) =>
            {
                try
                {
                    if (!_db.LockTake(key, value, expiry))
                        return;
                    waitHandle.Set();
                    timer.Stop();
                }
                catch
                {
                    // 忽略获取锁过程中的异常，继续重试
                }
            };
            timer.Start();

            if (timeout > 0)
                waitHandle.WaitOne(timeout);
            else
                waitHandle.WaitOne();

            return _db.LockQuery(key) == value;
        }
        finally
        {
            timer?.Stop();
            timer?.Close();
            timer?.Dispose();
        }
    }

    #endregion
}

public static class StackExchangeRedisExtension
{
    public static IEnumerable<string> ToStrings(this IEnumerable<RedisKey> keys)
    {
        var redisKeys = keys as RedisKey[] ?? keys.ToArray();
        return !redisKeys.Any() ? Enumerable.Empty<string>() : redisKeys.Select(k => k.ToString());
    }

    public static RedisValue ToRedisValue<T>(this T? value)
    {
        if (value == null)
            return RedisValue.Null;

        return value switch
        {
            ValueType => value.ToString()!,
            string s => s,
            _ => JsonConvert.SerializeObject(value)
        };
    }

    public static RedisValue[] ToRedisValues<T>(this IEnumerable<T> values)
    {
        var enumerable = values as T[] ?? values.ToArray();
        return !enumerable.Any() ? Array.Empty<RedisValue>() : enumerable.Select(v => v.ToRedisValue()).ToArray();
    }

    public static T? ToObject<T>(this RedisValue value)
    {
        if (!value.HasValue)
            return default;

        if (typeof(T).IsValueType || typeof(T) == typeof(string))
            return (T)Convert.ChangeType(value.ToString(), typeof(T));

        return JsonConvert.DeserializeObject<T>(value.ToString());
    }

    public static IEnumerable<T> ToObjects<T>(this IEnumerable<RedisValue> values)
    {
        var redisValues = values as RedisValue[] ?? values.ToArray();
        if (!redisValues.Any())
            return Enumerable.Empty<T>();
        return redisValues.Select(v => v.ToObject<T>()!);
    }

    public static HashEntry[]? ToHashEntries(this IDictionary<string, string> entries)
    {
        if (entries == null || entries.Count == 0)
            return null;

        var es = new HashEntry[entries.Count];
        var i = 0;
        foreach (var kvp in entries)
        {
            es[i++] = new HashEntry(kvp.Key, kvp.Value);
        }
        return es;
    }

    public static Dictionary<string, string> ToHashDictionary(this IEnumerable<HashEntry> entries)
    {
        var hashEntries = entries as HashEntry[] ?? entries.ToArray();
        var dict = new Dictionary<string, string>(hashEntries.Length);
        foreach (var entry in hashEntries)
            dict[entry.Name!] = entry.Value!;
        return dict;
    }

    public static Dictionary<string, string>? ToHashDictionary(this RedisValue[] hashValues,
        IEnumerable<string> fields)
    {
        var fieldList = fields as string[] ?? fields.ToArray();
        if (hashValues == null || hashValues.Length == 0 || fieldList.Length == 0)
            return null;

        var dict = new Dictionary<string, string>(fieldList.Length);
        var i = 0;
        foreach (var field in fieldList)
        {
            dict[field] = hashValues[i++].ToString();
        }
        return dict;
    }

    public static Dictionary<string, double> ToSortedSetDictionary(this IEnumerable<SortedSetEntry> entries)
    {
        var sortedSetEntries = entries as SortedSetEntry[] ?? entries.ToArray();
        var dict = new Dictionary<string, double>(sortedSetEntries.Length);
        foreach (var entry in sortedSetEntries)
            dict[entry.Element!] = entry.Score;
        return dict;
    }
}
