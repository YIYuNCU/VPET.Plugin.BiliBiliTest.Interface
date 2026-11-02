namespace VPET.Evian.BiliBiliTest.Core.Events
{
    /// <summary>
    /// 某个插件使用的事件入口。订阅会被记录，插件被关闭时宿主调用 <see cref="Dispose"/> 一次性取消该插件的全部订阅，
    /// 因此插件不需要自己保存订阅返回值
    /// </summary>
    public sealed class PluginEvents : IDisposable
    {
        private readonly EventBus bus;
        private readonly List<IDisposable> subscriptions = new();

        /// <summary>由宿主创建，插件不需要自己构造</summary>
        public PluginEvents(EventBus bus)
        {
            this.bus = bus;
        }

        /// <summary>订阅事件，插件关闭时自动取消</summary>
        public void Subscribe<T>(Action<T> handler, int priority = 0) where T : class
        {
            subscriptions.Add(bus.Subscribe(handler, priority));
        }

        /// <summary>发布事件，T 必须是订阅时使用的事件类型本身</summary>
        public void Publish<T>(T e) where T : class
        {
            bus.Publish(e);
        }

        public void Dispose()
        {
            foreach (var subscription in subscriptions)
                subscription.Dispose();
            subscriptions.Clear();
        }
    }
}
