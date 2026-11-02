using VPet_Simulator.Windows.Interface;
using VPET.Evian.BiliBiliTest.Core.Events;

namespace VPET.Evian.BiliBiliTest.Core
{
    /// <summary>宿主提供给插件的全部能力</summary>
    public sealed class PluginContext
    {
        /// <summary>桌宠主窗口</summary>
        public IMainWindow MainWindow { get; }
        /// <summary>事件总线</summary>
        public PluginEvents Events { get; }
        /// <summary>设置文件（基类内部使用，插件通过 IPluginSettings 读写设置）</summary>
        internal ConfigStore Config { get; }
        /// <summary>直播会话（登录状态、主播 ID 等）</summary>
        public ILiveSession Session { get; }

        /// <summary>由宿主创建，插件不需要自己构造</summary>
        public PluginContext(IMainWindow mainWindow, PluginEvents events, ConfigStore config, ILiveSession session)
        {
            MainWindow = mainWindow;
            Events = events;
            Config = config;
            Session = session;
        }
    }
}
