# 事件参考

插件之间、插件和宿主之间只通过事件总线通信。本文列出全部事件、字段、处理器优先级和常见用法。事件类型都在命名空间 `VPET.Evian.BiliBiliTest.Core.Events`。

## 事件总线的规则

插件通过基类的 `Events`（`PluginEvents`）使用事件总线：

| 方法 | 说明 |
| --- | --- |
| `Events.Subscribe<T>(Action<T> handler, int priority = 0)` | 订阅事件。priority 小的先执行，相同时按订阅先后。订阅被自动记录，插件关闭时宿主一次性取消，不需要自己保存 |
| `Events.Publish<T>(T e)` | 发布事件，在**调用线程上同步**执行所有处理器后返回 |

规则：

- `T` 必须是订阅时的事件类型本身，总线不做继承匹配。订阅 `DanmakuEvent` 收不到别的事件，订阅基类 `UserEvent` 什么也收不到。
- 某个处理器抛异常时，总线把异常写入主日志后继续执行后面的处理器，不会让发布者失败。
- 直播事件（弹幕、礼物、点赞等）在 B 站 WebSocket 线程上触发。处理器里不要做耗时的阻塞操作；要操作控件或桌宠，用 `Ui.Post` 回到界面线程。
- 请求/应答式事件（`UserProfileQuery` 等）：发布者创建事件对象，`Publish` 返回后读取被应答方填写的字段，先检查 `Answered`。因为是同步派发，返回时应答已经完成。
- 可拦截式事件：`DanmakuEvent.Handled`、`GiftEvent.Ignored`。先执行的处理器置位，后续处理器自己检查并跳过。总线不会因为置位而停止派发。
- 没有直播会话（没登录）时，不会有任何直播事件。开发时可以自己 `Publish` 事件来测试处理器。

## 直播间事件

由宿主把 B 站开放平台推送的数据转换后发布。除 `LiveStateEvent` 外都继承 `UserEvent`，带有 `OpenId`（B 站开放平台的观众 open_id，用它区分观众）和 `UserName`（昵称）。

| 事件 | 字段 | 说明 |
| --- | --- | --- |
| `DanmakuEvent` | `Message`(string)、`Handled`(bool) | 弹幕。`Handled` 为 true 表示已被某个插件当作指令或投喂处理，后续处理器应跳过 |
| `GiftEvent` | `GiftNum`(long)、`Price`(long)、`GiftName`(string)、`GiftIcon`(string)、`Paid`(bool)、`Ignored`(bool) | 礼物。`Price` 是**单价**（1000 = 1 元），总价是 `Price × GiftNum`。`Paid` 对应 B 站推送的是否付费礼物。`Ignored` 置 true 后，该礼物不进入致谢和动画批次 |
| `LikeEvent` | `Count`(long)、`Timestamp`(long) | 点赞。两个字段是 B 站推送值的原样转发，含义以 B 站开放平台的点赞事件说明为准 |
| `GuardEvent` | `Level`(long)、`Months`(long)、`Price`(long) | 上舰。`Level`：1 总督、2 提督、3 舰长。`Price` 是这次上舰的金额（1000 = 1 元） |
| `SuperChatEvent` | `Message`(string)、`Rmb`(long) | 充电弹幕。`Rmb` 的单位是**元**，礼物金额的单位是 1/1000 元，两者不要混用 |
| `EnterRoomEvent` | 无 | 观众进入直播间 |
| `LiveStateEvent` | `OpenId`(string)、`RoomId`(long)、`Started`(bool) | 开播状态变化，不是 `UserEvent` |
| `SessionStartedEvent` | `AnchorOpenId`(string)、`GameId`(string) | 登录成功、长连接建立之后 |
| `SessionEndedEvent` | 无 | 会话结束（用户关闭，或被服务器踢出） |
| `GiftBatchEvent` | `Gifts`(`List<Gift>`) | 宿主每 10 秒发布一批，已按（观众，礼物）合并。点赞也在其中，用 `Gift.IsLike` 区分 |

`Gift` 的字段：`Open_id`、`UserName`、`GiftNum`、`Price`（单价，1000 = 1 元）、`GiftName`、`GiftIcon`、`IsLike`（只读，点赞为 true）。

`GiftEvent` 是逐个礼物，`GiftBatchEvent` 是 10 秒一批。做礼物动画、统计这类不要求实时的功能用批次；要在礼物到达时立即响应用 `GiftEvent`。

## 插件间事件

| 事件 | 谁发布 | 谁处理 | 字段与说明 |
| --- | --- | --- | --- |
| `SpeakRequestEvent` | 任意插件 | 宿主朗读队列 | 请桌宠说一句话。`Content`(string)；`Action`(string，指定动画，空则随机)；`Type`(string)：`Notice` / `Guard` / `Gift` / `SuperChat` / `Barrage` / `Thumb` / `EnterRoom`，决定朗读优先级；`Guard`(`GuardType`，观众的舰长类型，可不填) |
| `UserProfileQuery` | 任意插件 | 内置好感度插件 | 查询观众资料。填 `OpenId`，发布后读 `Like`(long，查询失败为 -1)、`Money`(double，内部金额单位)、`Guard`(`GuardType`)，先检查 `Answered` |
| `UserMoneyAdjustEvent` | 任意插件 | 内置好感度插件 | 增减观众金钱。`OpenId`、`Delta`(double，内部金额单位，正数增加，负数扣除)。金钱不会被扣到 0 以下。事件没有应答字段，要判断够不够扣，先用 `UserProfileQuery` 查余额 |
| `BlockedWordsRequestEvent` | 任意插件 | 内置读弹幕插件 | `Words`(`List<string>`)：请求把指令词加入屏蔽词，避免指令被当成普通弹幕朗读 |
| `BlockedWordsResendEvent` | 内置读弹幕插件 | 任意插件 | 读弹幕插件刚启动，请再发一次自己的屏蔽词 |
| `UserRecordsQuery` / `UserRecordsSave` | 内置好感度数据插件 | 内置好感度插件 | 读取、写回全部观众资料（`UserRecord`）。外部插件一般不用 |

`GuardType`：`None`(0)、`captain`(1，舰长)、`Admiral`(2，提督)、`Viceroy`(3，总督)。它的数值和 `GuardEvent.Level`（1 总督、2 提督、3 舰长）正好相反，从 `Level` 转换用 `(GuardType)(4 - level)`，内置好感度插件就是这样转换的。

## 处理器优先级

同一事件有多个插件处理时，用优先级固定顺序（数值小先执行）。内置插件用到的位置如下，外部插件选一个合适的位置：

| 事件 | -100 | 0 | 5 | 10 | 100 | 500 | 1000 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `DanmakuEvent` | 读弹幕：朗读判断 | 好感度：查询指令（设 Handled） | 桌宠动作：动作指令（设 Handled） | 投喂：投喂指令（设 Handled） | 好感度：弹幕计数（Handled 则跳过） | | 读弹幕：刷新最后互动时间 |
| `GiftEvent` | 好感度：记金钱（必要时设 Ignored） | | | | | 宿主：入批次 | 读弹幕：刷新最后互动时间 |
| `LikeEvent` | | 好感度：记点赞 | | | | 宿主：入批次 | 读弹幕：刷新最后互动时间 |
| `GuardEvent` / `SuperChatEvent` | 读弹幕：致谢（要在好感度更新数据前读取） | 好感度：记录 | | | | | 读弹幕：刷新最后互动时间 |
| `EnterRoomEvent` | 读弹幕：欢迎（进房不算互动，不刷新时间） | | | | | | |

选值的建议：

- 要拦截弹幕指令：取 1 到 99，比 100（好感度弹幕计数）小，匹配后置 `Handled = true`。处理前先检查 `Handled`，避免和别的指令冲突。
- 要在好感度更新数据**之前**读取观众数据：取比 -100 更小的值。要读更新**之后**的数据：取 0 以上。
- 不要依赖和内置插件相同优先级时的执行先后，换一个值。

## 内置插件

外部插件的 `Id`、`Order`、设置节名和日志目录名不要与下面的冲突。

| Id | 显示名 | Order | 说明 |
| --- | --- | --- | --- |
| `Barrage` | 读弹幕 | 10 | 读弹幕、礼物、上舰致谢、进房欢迎、屏蔽词 |
| `GiftEffect` | 礼物动画 | 20 | 礼物掉落动画 |
| `Like` | 好感度 | 30 | 好感度、金钱、签到、查询指令。必须加载，不能关闭 |
| `LikeData` | 好感度数据 | 35 | 表格查看和修改观众数据 |
| `Feeding` | 投喂 | 40 | 观众投喂食物或饮品 |
| `PetAction` | 桌宠动作 | 50 | 摸头、开始/结束工作等动作指令 |
| `LogTools` | 日志工具 | 90 | 查看、导出、清空日志 |

宿主和内置插件已占用的设置节名：`Basic`、`DisabledPlugins`、`Log`、`Like`、`LikeData`、`LogTools`、`BarrageRead`、`Blocked`、`Pet`、`GiftInteraction`、`PetAction`、`PetActionRule`。
已占用的日志目录名：`Main`、`TCP`、`BarrageRead`、`Pet`、`Interaction`、`Like`、`LikeData`、`PetAction`。
设置节名和日志目录名默认等于插件 `Id`，所以把 `Id` 起得有辨识度（例如带上自己的名字）就不会冲突。

好感度插件是必须加载的插件，用户不能关闭它，所以 `UserProfileQuery` 和 `UserMoneyAdjustEvent` 总有应答。仍然要检查 `Answered`，失败时不要当成 0 好感度处理。

## 常见用法

### 弹幕指令

```csharp
protected override void OnStart()
{
    Events.Subscribe<DanmakuEvent>(OnDanmaku, priority: 20);
    Events.Subscribe<BlockedWordsResendEvent>(_ => PublishBlockedWords());
    PublishBlockedWords();
}

private void OnDanmaku(DanmakuEvent e)
{
    if (e.Handled || e.Message != "我的指令")
        return;
    e.Handled = true;                 // 告诉后面的处理器：这条弹幕已经被处理
    Events.Publish(new SpeakRequestEvent { Content = $"{e.UserName}你好", Type = "Notice" });
}

private void PublishBlockedWords() =>
    Events.Publish(new BlockedWordsRequestEvent { Words = { "我的指令" } });
```

### 查询好感度后再回应

```csharp
var query = new UserProfileQuery { OpenId = e.OpenId };
Events.Publish(query);
if (query.Answered && query.Like >= 0)
    Events.Publish(new SpeakRequestEvent { Content = $"{e.UserName}的好感度是{query.Like}", Type = "Notice" });
```

### 扣观众金钱

```csharp
const int Cost = 5 * Currency.GoldPerBattery;     // 5 电池
var query = new UserProfileQuery { OpenId = openId };
Events.Publish(query);
if (query.Answered && query.Money >= Cost)
    Events.Publish(new UserMoneyAdjustEvent { OpenId = openId, Delta = -Cost });
```

### 按批次统计礼物

```csharp
Events.Subscribe<GiftBatchEvent>(batch =>
{
    long total = batch.Gifts.Where(g => !g.IsLike).Sum(g => g.Price * g.GiftNum);   // 内部金额单位，1000 = 1 元
    Log.AddInfoLog($"这 10 秒收到礼物 {total / (double)Currency.GoldPerYuan} 元");
});
```
