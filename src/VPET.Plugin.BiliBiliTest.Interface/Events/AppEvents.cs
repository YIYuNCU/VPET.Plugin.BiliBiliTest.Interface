namespace VPET.Evian.BiliBiliTest.Core.Events
{
    // 插件之间 / 宿主与插件之间的事件

    /// <summary>登录成功、长连接建立后发布</summary>
    public sealed class SessionStartedEvent
    {
        public string AnchorOpenId { get; set; } = "";
        public string GameId { get; set; } = "";
    }

    /// <summary>会话结束（主动关闭或被服务器踢出）后发布</summary>
    public sealed class SessionEndedEvent
    {
    }

    /// <summary>
    /// 礼物/点赞批次：LiveSession 每隔固定时间把期间的礼物与点赞合并后发布一次
    /// </summary>
    public sealed class GiftBatchEvent
    {
        public List<Gift> Gifts { get; set; } = new();
    }

    /// <summary>请桌宠说一句话。由主程序的朗读队列（SpeechService）消费，插件需要说话时发布此事件，不依赖其他插件</summary>
    public sealed class SpeakRequestEvent
    {
        public string Content { get; set; } = "";
        /// <summary>指定动画，空则随机</summary>
        public string Action { get; set; } = "";
        /// <summary>Notice / Guard / Gift / SuperChat / Barrage / Thumb / EnterRoom，决定朗读优先级</summary>
        public string Type { get; set; } = "";
        public GuardType Guard { get; set; } = GuardType.None;
    }

    /// <summary>查询观众资料。好感度插件填充结果，查询方读取</summary>
    public sealed class UserProfileQuery
    {
        public string OpenId { get; set; } = "";
        /// <summary>好感度，查询失败为 -1</summary>
        public long Like { get; set; } = -1;
        public double Money { get; set; }
        public GuardType Guard { get; set; } = GuardType.None;
        /// <summary>是否有插件应答</summary>
        public bool Answered { get; set; }
    }

    /// <summary>一位观众的资料，好感度数据页显示和修改的字段。金钱和累计获得金额使用内部金额单位(1000=1元，见 Currency)</summary>
    public sealed class UserRecord
    {
        public string OpenId { get; set; } = "";
        public string Name { get; set; } = "";
        public double Money { get; set; }
        public double TotalMoney { get; set; }
        public long Thumbs { get; set; }
        public long Barrages { get; set; }
        public long Like { get; set; }
        public GuardType GuardType { get; set; }
        public long TotalCheckIns { get; set; }
        public long ConsecutiveCheckIns { get; set; }
        public DateTime LastInteractionTime { get; set; }
        public DateTime LastCheckInDate { get; set; }

        public UserRecord Clone() => (UserRecord)MemberwiseClone();
    }

    /// <summary>读取所有观众资料。好感度插件填充 Users，查询方读取</summary>
    public sealed class UserRecordsQuery
    {
        public List<UserRecord> Users { get; set; } = new();
        /// <summary>是否有插件应答</summary>
        public bool Answered { get; set; }
    }

    /// <summary>写入观众资料。好感度插件在一个事务里落库，成功置 Saved，失败把原因写进 Error</summary>
    public sealed class UserRecordsSave
    {
        public List<UserRecord> Users { get; set; } = new();
        public bool Saved { get; set; }
        public string Error { get; set; } = "";
    }

    /// <summary>调整观众金钱（正加负减），由好感度插件落库</summary>
    public sealed class UserMoneyAdjustEvent
    {
        public string OpenId { get; set; } = "";
        public double Delta { get; set; }
    }

    /// <summary>请求屏蔽词（弹幕朗读插件将其加入屏蔽列表，避免指令被朗读）</summary>
    public sealed class BlockedWordsRequestEvent
    {
        public List<string> Words { get; set; } = new();
    }

    /// <summary>弹幕朗读插件刚启动，请其他插件把自己的屏蔽词再发一次（BlockedWordsRequestEvent）</summary>
    public sealed class BlockedWordsResendEvent
    {
    }

    /// <summary>观众资料类型</summary>
    public enum GuardType
    {
        [System.ComponentModel.Description("无")]
        None = 0,
        [System.ComponentModel.Description("舰长")]
        captain = 1,
        [System.ComponentModel.Description("提督")]
        Admiral = 2,
        [System.ComponentModel.Description("总督")]
        Viceroy = 3,
    }

    /// <summary>一条（合并后的）礼物或点赞记录</summary>
    public sealed class Gift
    {
        public const string LikeName = "like";

        public string Open_id { get; set; } = "";
        public string UserName { get; set; } = "";
        public long GiftNum { get; set; }
        /// <summary>单价（1000=1元），总价为 Price × GiftNum</summary>
        public long Price { get; set; }
        public string GiftName { get; set; } = "";
        public string GiftIcon { get; set; } = "";

        public bool IsLike => Price == 0 && GiftName == LikeName;

        public Gift(string open_id, string userName, long giftNum, long price, string giftName, string giftIcon)
        {
            Open_id = open_id;
            UserName = userName;
            GiftNum = giftNum;
            Price = price;
            GiftName = giftName;
            GiftIcon = giftIcon;
        }
    }
}
