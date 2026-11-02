namespace VPET.Evian.BiliBiliTest.Core.Events
{
    /// <summary>
    /// 进程内事件总线。同步派发：处理器在发布者线程上依次执行，按 priority 从小到大。
    /// 单个处理器抛出的异常会被记录并跳过，不影响其他处理器。
    /// 处理器内如需操作控件，请使用 <see cref="Ui.Post"/>；耗时操作自行丢到后台。
    /// </summary>
    public sealed class EventBus
    {
        private sealed class Entry
        {
            public int Priority;
            public Delegate Handler = null!;
        }

        private readonly Log log;
        private readonly object sync = new();
        // 写时复制：订阅（极少发生）加锁重建，发布（高频）无锁读取
        private volatile Dictionary<Type, Entry[]> map = new();

        public EventBus(Log log)
        {
            this.log = log;
        }

        /// <summary>订阅事件，返回的对象 Dispose 后取消订阅</summary>
        public IDisposable Subscribe<T>(Action<T> handler, int priority = 0) where T : class
        {
            var entry = new Entry { Priority = priority, Handler = handler };
            lock (sync)
            {
                var copy = new Dictionary<Type, Entry[]>(map);
                copy.TryGetValue(typeof(T), out var old);
                var list = old == null ? new List<Entry>() : new List<Entry>(old);
                int index = list.FindIndex(x => x.Priority > priority);
                if (index < 0)
                    list.Add(entry);
                else
                    list.Insert(index, entry);
                copy[typeof(T)] = list.ToArray();
                map = copy;
            }
            return new Subscription(this, typeof(T), entry);
        }

        /// <summary>发布事件，T 必须是订阅时使用的事件类型本身</summary>
        public void Publish<T>(T e) where T : class
        {
            if (!map.TryGetValue(typeof(T), out var entries))
                return;
            for (int i = 0; i < entries.Length; i++)
            {
                try
                {
                    ((Action<T>)entries[i].Handler)(e);
                }
                catch (Exception ex)
                {
                    log.AddErrorLog($"事件{typeof(T).Name}处理出错:{ex}");
                }
            }
        }

        private void Unsubscribe(Type type, Entry entry)
        {
            lock (sync)
            {
                if (!map.TryGetValue(type, out var old))
                    return;
                var copy = new Dictionary<Type, Entry[]>(map);
                copy[type] = old.Where(x => x != entry).ToArray();
                map = copy;
            }
        }

        private sealed class Subscription : IDisposable
        {
            private readonly EventBus bus;
            private readonly Type type;
            private readonly Entry entry;

            public Subscription(EventBus bus, Type type, Entry entry)
            {
                this.bus = bus;
                this.type = type;
                this.entry = entry;
            }

            public void Dispose() => bus.Unsubscribe(type, entry);
        }
    }
}
