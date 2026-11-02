using LinePutScript;
using LinePutScript.Converter;
using VPET.Evian.BiliBiliTest.Core.Events;

namespace VPET.Evian.BiliBiliTest.Core
{
    /// <summary>
    /// 插件基类：提供上下文与日志。需要设置/存档时，在子类上额外实现 <see cref="IPluginSettings"/> / <see cref="IPluginSaveData"/>
    /// </summary>
    public abstract class BiliPluginBase : IBiliPlugin
    {
        public abstract string Id { get; }
        public virtual string DisplayName => Id;
        public virtual int Order => 100;
        /// <summary>必须加载，不能关闭，见 <see cref="IBiliPlugin.Required"/></summary>
        public virtual bool Required => false;

        /// <summary>日志子目录，默认同 Id</summary>
        protected virtual string LogTitle => Id;
        /// <summary>设置文件中保存 EnableLog 的节，默认同 Id</summary>
        protected virtual string ConfigSection => Id;

        protected PluginContext Context { get; private set; } = null!;
        protected PluginEvents Events => Context.Events;
        public Log Log { get; private set; } = null!;

        public void Initialize(PluginContext context)
        {
            Context = context;
            // 设置节还是空的说明是首次运行，默认开启日志
            var section = context.Config.Document[ConfigSection];
            Log = new Log(LogTitle, section.Count == 0 || section[(gbol)"EnableLog"], DisplayName);
        }

        public void Start()
        {
            OnStart();
            // 会话结束时落盘一次日志
            Events.Subscribe<SessionEndedEvent>(_ => Log.SaveLog());
        }

        /// <summary>设置与存档已读取，订阅事件、开始工作</summary>
        protected abstract void OnStart();

        public void Shutdown()
        {
            OnShutdown();
            Log.SaveLog();
        }

        protected virtual void OnShutdown()
        {
        }
    }
}
