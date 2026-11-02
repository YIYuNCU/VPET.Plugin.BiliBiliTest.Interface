namespace VPET.Evian.BiliBiliTest.Core.Events
{
    // 直播间事件：由宿主把 B 站 SDK 数据转换后发布。插件只依赖这些类型，不直接依赖 SDK。

    /// <summary>与某个观众有关的事件</summary>
    public abstract class UserEvent
    {
        public string OpenId { get; set; } = "";
        public string UserName { get; set; } = "";
    }

    /// <summary>收到弹幕</summary>
    public sealed class DanmakuEvent : UserEvent
    {
        public string Message { get; set; } = "";
        /// <summary>已被某个插件作为指令/投喂处理。后续处理器应跳过</summary>
        public bool Handled { get; set; }
    }

    /// <summary>收到礼物</summary>
    public sealed class GiftEvent : UserEvent
    {
        public long GiftNum { get; set; }
        /// <summary>礼物单价（1000=1元），总价为 Price × GiftNum</summary>
        public long Price { get; set; }
        public string GiftName { get; set; } = "";
        public string GiftIcon { get; set; } = "";
        public bool Paid { get; set; }
        /// <summary>置为 true 后，该礼物不进入致谢/动画批次</summary>
        public bool Ignored { get; set; }
    }

    /// <summary>收到点赞</summary>
    public sealed class LikeEvent : UserEvent
    {
        public long Count { get; set; }
        public long Timestamp { get; set; }
    }

    /// <summary>上舰</summary>
    public sealed class GuardEvent : UserEvent
    {
        /// <summary>1总督 2提督 3舰长</summary>
        public long Level { get; set; }
        /// <summary>月数</summary>
        public long Months { get; set; }
        /// <summary>这次上舰的金额（1000=1元）</summary>
        public long Price { get; set; }
    }

    /// <summary>充电弹幕(SuperChat)</summary>
    public sealed class SuperChatEvent : UserEvent
    {
        public string Message { get; set; } = "";
        /// <summary>支付金额，单位是元（注意礼物金额的单位是 1/1000 元）</summary>
        public long Rmb { get; set; }
    }

    /// <summary>观众进入直播间</summary>
    public sealed class EnterRoomEvent : UserEvent
    {
    }

    /// <summary>开播状态变化</summary>
    public sealed class LiveStateEvent
    {
        public string OpenId { get; set; } = "";
        public long RoomId { get; set; }
        public bool Started { get; set; }
    }
}
