namespace VPET.Evian.BiliBiliTest.Core
{
    /// <summary>
    /// 直播会话的只读状态，供插件通过 <see cref="PluginContext.Session"/> 读取。
    /// 登录、开播、关闭由宿主负责，插件不能操作；状态变化可以订阅 SessionStartedEvent / SessionEndedEvent
    /// </summary>
    public interface ILiveSession
    {
        /// <summary>是否已登录并正在接收直播间事件</summary>
        bool Working { get; }
        /// <summary>本次开播的游戏 ID，未登录为空</summary>
        string GameId { get; }
        /// <summary>主播的 OpenId，未登录为空</summary>
        string AnchorOpenId { get; }
    }
}
