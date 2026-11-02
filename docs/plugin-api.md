# 插件接口参考

本文是 B 站直播互动 mod 外部插件接口的完整说明。接口类型都在程序集 `VPET.Plugin.BiliBiliTest.Interface`（命名空间 `VPET.Evian.BiliBiliTest.Core` 和 `VPET.Evian.BiliBiliTest.Core.Events`）。开发时不需要 mod 的其他源代码。

mod 负责登录、直播间长连接、设置窗口、桌宠朗读队列。观众互动、礼物动画等功能是内置插件。外部插件和内置插件使用同一套接口，只通过事件总线通信。

## 生命周期

宿主按下面的顺序调用，带方括号的是可选接口：

```
构造函数 → Initialize → [LoadSettings] → [LoadData] → Start → 运行
        → [SaveSettings]（点“确定”“应用”和退出时，可多次）→ [SaveData] → Shutdown
```

- 宿主加载 dll 时，对每个符合条件的类调用一次构造函数，创建一个备用实例，所以构造函数里不要做耗时或有副作用的事。
- 用户可以在“基本设置”里随时关闭、再启用插件。关闭时宿主先取消该插件的全部事件订阅，再依次调用 `SaveSettings`、`SaveData`、`Shutdown`，然后丢弃实例。再次启用时用类的无参构造函数创建全新实例，从 `Initialize` 重新开始。
- 启用最多等 10 秒，关闭最多等 5 秒，超时按失败或已关闭处理。启用/关闭可能在非界面线程执行，`Start`/`Shutdown` 里操作控件要经过 `Ui.Dispatcher`。
- `Shutdown` 必须释放自己创建的线程、任务、定时器、挂到主窗口的控件和事件、数据库连接。
- 同一个 `Id` 重复注册时，后注册的被忽略并写一条警告日志。

## 接口

| 类型 | 说明 |
| --- | --- |
| `IBiliPlugin` | 必须实现。`Id`、`DisplayName`、`Order`、`Required`（默认 false），以及 `Initialize(PluginContext)`、`Start()`、`Shutdown()` |
| `BiliPluginBase` | 实现了 `IBiliPlugin` 的基类，推荐继承。提供 `Context`、`Events`、`Log`，子类实现 `OnStart()`，按需重写 `OnShutdown()` |
| `IPluginSettings` | 设置：`LoadSettings(LpsDocument)`、`SaveSettings(LpsDocument)`、`CreateSettingsPages()` |
| `IPluginSaveData` | 存档：`LoadData(string dataDirectory)`、`SaveData()` |
| `IPluginDocs` | 说明文档：`CreateDocs()` 返回若干 `PluginDoc(标题, Markdown 文件完整路径)` |
| `SettingsPage` | 设置窗口的一个页签：`SettingsPage(标题, UserControl, Action? apply)`，可选属性 `Fill`、`ConfirmClose` |
| `PluginContext` | 宿主给插件的能力：`MainWindow`（桌宠主窗口 `IMainWindow`）、`Events`（`PluginEvents`）、`Session`（`ILiveSession`） |
| `ILiveSession` | 只读的直播状态：`Working`、`GameId`、`AnchorOpenId` |

### BiliPluginBase

| 成员 | 默认值 | 说明 |
| --- | --- | --- |
| `Id`（抽象） | 无 | 唯一标识，同时是存档目录名。不要和已有插件重复 |
| `DisplayName` | 同 `Id` | 在基本设置、设置页导航里显示的名字 |
| `Order` | 100 | 加载顺序，数值小的先加载，同时决定设置页签的顺序 |
| `Required` | false | true 表示不能被用户关闭。外部插件一般不需要 |
| `LogTitle`（protected） | 同 `Id` | 日志目录名 |
| `ConfigSection`（protected） | 同 `Id` | 保存 `EnableLog` 的设置节 |
| `OnStart()`（抽象） | 无 | 设置和存档已经读取完毕，在这里订阅事件、开始工作 |
| `OnShutdown()` | 空 | 释放资源 |

`Initialize` 时基类创建 `Log`：设置文件里还没有本插件的设置节（首次运行）时日志默认开启，否则读 `<ConfigSection>.EnableLog`。想让用户能关日志，在 `SaveSettings` 里写 `section[(gbol)"EnableLog"] = Log.Enable;`。

### 设置页 SettingsPage

- `apply`：用户点设置窗口的“确定”或“应用”时调用，把界面上的值写回插件字段。之后宿主调用 `SaveSettings` 写入文件。点“取消”不会调用。
- `Fill = true`：页面自带滚动条（例如表格）时设置。设置窗口默认给每页套一个垂直滚动条，设了 `Fill` 就不套，把可用高度全给页面。
- `ConfirmClose`：设置窗口关闭前调用，返回 false 则取消关闭。有未保存的修改时在这里询问用户。
- 同一个页面控件实例在设置窗口打开期间一直保留。每次打开设置窗口会重新调用 `CreateSettingsPages()`。
- 样式：XAML 里引入 `/VPet-Simulator.Windows.Interface;component/ResourceStyle.xaml`，背景用 `{DynamicResource PrimaryLighter}`，按钮用 `{DynamicResource PrimaryTransA}`，和游戏主题一致。

### 设置读写

- 所有插件的设置保存在同一个文件 `ModData/BiliVpet/Configs/Config_User.lps`，由宿主在启动时读入内存，各插件通过 `LoadSettings(LpsDocument)` 取自己的节：`config["节名"]`。
- 读：`section[(gint)"Key"]`、`(gbol)`、`(gdbe)`、`(gstr)`。键不存在时得到类型默认值（0、false、空字符串），所以读出来要先判断是否有效，无效就保持字段默认值。
- 写：`section[(gint)"Key"] = value;`。只更新自己拥有的键，不要新建文档，否则会丢掉别的插件的设置。
- 首次运行时文件不存在，节是空的（`section.Count == 0`）。默认值写在插件字段上，`LoadSettings` 里先判断 `section.Count == 0` 就直接返回，保持字段默认值。
- 同名多行的设置读取时没有对应行就保留代码里的默认值，保存时先 `RemoveAll` 再重写。
- 设置只在 `LoadSettings` 读，在 `SaveSettings` 写。

### 存档

`LoadData(dataDirectory)` 给的目录是 `ModData/BiliVpet/Data/<Id>`，已创建，由本插件独占。`SaveData()` 保存并关闭文件占用（SQLite 等要在这里关闭连接）。

### 说明文档

`CreateDocs()` 返回的文档会出现在设置窗口的“说明”页，点“打开说明”弹窗显示。`PluginDoc.Path` 是 Markdown 文件的完整路径，外部插件可以把文档放在自己的 dll 旁边，用 `typeof(MyPlugin).Assembly.Location` 取目录。

Markdown 渲染支持标题、段落、列表、表格、引用、代码、粗体斜体、链接、图片。HTML 标签不解析，按原文显示。图片路径相对 md 文件，后面写 `{width=380}` 限制最大宽度，例如 `![](./Pictures/x.png){width=380}`。

## 事件总线

插件通过 `Events`（`PluginEvents`）使用事件总线：

- `Events.Subscribe<T>(Action<T> handler, int priority = 0)`：priority 小的先执行。订阅被自动记录，插件关闭时宿主一次性取消，不要自己保存订阅返回值。
- `Events.Publish<T>(T e)`：同步在调用线程执行所有处理器。某个处理器抛异常只记录日志，不影响其他处理器。
- `T` 必须是订阅时的事件类型本身，总线不做继承匹配（订阅 `DanmakuEvent` 收不到 `UserEvent`）。
- 直播事件（弹幕、礼物等）在 B 站 WebSocket 线程上触发：处理器里不要做耗时的阻塞操作；操作控件用 `Ui.Post` 或 `Ui.Dispatcher`。
- 请求/应答式事件（如 `UserProfileQuery`）：发布者 new 一个事件对象，`Publish` 后读取被应答方填写的字段，先检查 `Answered`。
- 可拦截式事件：`DanmakuEvent.Handled`、`GiftEvent.Ignored`。先执行的处理器置位，后续处理器自己检查并跳过。

### 直播间事件（宿主发布）

| 事件 | 字段 | 说明 |
| --- | --- | --- |
| `DanmakuEvent` | `OpenId`、`UserName`、`Message`、`Handled` | 弹幕。`Handled` 表示已被指令或投喂消费，后续处理器应跳过 |
| `GiftEvent` | `OpenId`、`UserName`、`GiftNum`、`Price`、`GiftName`、`GiftIcon`、`Paid`、`Ignored` | 礼物。`Price` 是单价，总价是 `Price × GiftNum`。`Ignored = true` 后不进入致谢和动画批次 |
| `LikeEvent` | `OpenId`、`UserName`、`Count`、`Timestamp` | 点赞 |
| `GuardEvent` | `OpenId`、`UserName`、`Level`、`Months`、`Price` | 上舰。`Level`：1 总督、2 提督、3 舰长 |
| `SuperChatEvent` | `OpenId`、`UserName`、`Message`、`Rmb` | 充电弹幕。`Rmb` 的单位是元 |
| `EnterRoomEvent` | `OpenId`、`UserName` | 观众进入直播间 |
| `LiveStateEvent` | `OpenId`、`RoomId`、`Started` | 开播状态变化 |
| `SessionStartedEvent` | `AnchorOpenId`、`GameId` | 登录成功、长连接建立后 |
| `SessionEndedEvent` | 无 | 会话结束（主动关闭或被服务器踢出） |
| `GiftBatchEvent` | `Gifts`（`List<Gift>`） | 每 10 秒一批，已按（用户，礼物）合并。点赞也在其中，`Gift.IsLike` 为 true |

`Gift`：`Open_id`、`UserName`、`GiftNum`、`Price`（单价）、`GiftName`、`GiftIcon`、`IsLike`。

### 插件间事件

| 事件 | 发布者 | 处理者 | 说明 |
| --- | --- | --- | --- |
| `SpeakRequestEvent` | 任意插件 | 宿主朗读队列 | 请桌宠说话。`Content`、`Action`（指定动画，空则随机）、`Type`（`Notice` / `Guard` / `Gift` / `SuperChat` / `Barrage` / `Thumb` / `EnterRoom`，决定朗读优先级）、`Guard` |
| `UserProfileQuery` | 任意插件 | 内置好感度插件 | 查询观众资料。设 `OpenId`，发布后读 `Like`（查询失败为 -1）、`Money`、`Guard`，先检查 `Answered` |
| `UserMoneyAdjustEvent` | 任意插件 | 内置好感度插件 | 增减观众金钱：`OpenId`、`Delta`（内部金额单位，正数增加，负数扣除） |
| `BlockedWordsRequestEvent` | 任意插件 | 内置读弹幕插件 | `Words`：请求把指令词加入屏蔽词，避免指令被当成普通弹幕朗读 |
| `BlockedWordsResendEvent` | 内置读弹幕插件 | 任意插件 | 读弹幕插件刚启动，请把自己的屏蔽词再发一次 |
| `UserRecordsQuery` / `UserRecordsSave` | 内置好感度数据插件 | 内置好感度插件 | 读取和写回全部观众资料（`UserRecord`）。外部插件一般不需要 |

写指令类插件的惯例：订阅 `DanmakuEvent`，匹配后置 `Handled = true`，把指令词用 `BlockedWordsRequestEvent` 交给读弹幕插件屏蔽，并订阅 `BlockedWordsResendEvent` 在读弹幕插件重启后重发。

### 处理器优先级

同一事件有多个插件处理时，用优先级固定顺序（数值小先执行）。内置插件使用的优先级如下，外部插件选一个合适的位置：

| 事件 | -100 | 0 | 5 | 10 | 100 | 500 | 1000 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `DanmakuEvent` | 读弹幕：朗读判断 | 好感度：查询指令（设 Handled） | 桌宠动作：动作指令（设 Handled） | 投喂：投喂指令（设 Handled） | 好感度：弹幕计数（Handled 则跳过） | | 读弹幕：刷新最后互动时间 |
| `GiftEvent` | 好感度：记金钱（必要时设 Ignored） | | | | | 宿主：入批次 | 读弹幕：刷新最后互动时间 |
| `LikeEvent` | | 好感度：记点赞 | | | | 宿主：入批次 | 读弹幕：刷新最后互动时间 |
| `GuardEvent` / `SuperChatEvent` | 读弹幕：致谢（要在好感度更新数据前读取） | 好感度：记录 | | | | | 读弹幕：刷新最后互动时间 |
| `EnterRoomEvent` | 读弹幕：欢迎（进房不算互动，不刷新时间） | | | | | | |

要拦截弹幕指令，优先级取 1 到 99 之间，且比 100（好感度弹幕计数）小。要在好感度更新数据之前读数据，用比 -100 更小的值；要读更新后的数据，用 0 以上。

### 内置插件

外部插件的 `Id` 和 `Order` 不要与下表冲突。

| Id | 显示名 | Order | 说明 |
| --- | --- | --- | --- |
| `Barrage` | 读弹幕 | 10 | 读弹幕、礼物、上舰致谢、进房欢迎、屏蔽词 |
| `GiftEffect` | 礼物动画 | 20 | 礼物掉落动画 |
| `Like` | 好感度 | 30 | 好感度、金钱、签到、查询指令。必须加载，不能关闭 |
| `LikeData` | 好感度数据 | 35 | 表格查看和修改观众数据 |
| `Feeding` | 投喂 | 40 | 观众投喂食物或饮品（设置节和日志目录名是 `Pet`） |
| `PetAction` | 桌宠动作 | 50 | 摸头、开始/结束工作等动作指令 |
| `LogTools` | 日志工具 | 90 | 查看、导出、清空日志 |

好感度插件是必须加载的插件，所以 `UserProfileQuery` 和 `UserMoneyAdjustEvent` 总有应答。仍然要检查 `Answered`，应答失败时不要当成 0 好感度处理。

## 工具类

| 类型 | 说明 |
| --- | --- |
| `Log` | 日志。`AddInfoLog`、`AddDebugLog`、`AddWarningLog`、`AddErrorLog`，以及只记一次的 `*_Once` 版本。写入可来自任意线程。`BiliPluginBase.Log` 已经创建好 |
| `Ui` | `Dispatcher`（主界面线程）、`Post(Action)`（投递到界面线程）、`ShowMessage(string)`（弹出提示框） |
| `AppPaths` | MOD 存储目录下的路径：`UserRoot`、`Configs`、`UserConfig`、`Data`、`LogRoot`、`Resources` |
| `Currency` | 金额换算常量：`GoldPerBattery = 100`、`GoldPerYuan = 1000` |
| `ConfigStore` | 设置文件的内存副本。由宿主使用，插件通过 `IPluginSettings` 读写 |

### 文件位置

设置、存档、日志、下载的资源都在 VPet 提供的 MOD 存储目录，即游戏目录下的 `ModData/BiliVpet`，不会被 mod 更新或重装覆盖：

| 内容 | 位置 |
| --- | --- |
| 设置 | `Configs/Config_User.lps`（`AppPaths.UserConfig`） |
| 存档 | `Data/<插件Id>/`（`AppPaths.Data`，`LoadData` 直接给出） |
| 日志 | `Log/<日志目录名>/`（`AppPaths.LogRoot`） |
| 下载的资源 | `Resources/`（`AppPaths.Resources`） |

mod 目录本身（`mod/<mod 名>/`）只放随 mod 发布的只读内容，运行时往里写的文件会被 mod 更新清掉。外部插件 dll 放在 mod 目录的 `Plugins` 文件夹。

### 金额单位

- 内部金额（`GiftEvent.Price`、`Gift.Price`、`GuardEvent.Price`、`UserProfileQuery.Money`、`UserMoneyAdjustEvent.Delta`）的单位是 1/1000 元，`1000 = 1 元`。
- 设置页里填的价格和对观众播报的金额用“电池”，`1 电池 = 100 内部单位 = 0.1 元`。换算用 `Currency`，只在设置和播报这一层做。
- `SuperChatEvent.Rmb` 的单位是元，不要和礼物金额混用。
- 礼物的 `Price` 是单价，总价是 `Price × GiftNum`。

### 日志

`Log.MinLevel` 是所有日志共用的最低记录等级（调试 < 信息 < 警告 < 错误），由“日志工具”页设置。日志先存在内存，保存时一次写出到 `Log/<目录名>`，会话结束和插件关闭时自动保存。

## 与桌宠本体交互

通过 `Context.MainWindow`（`IMainWindow`，来自 NuGet 包 `VPet-Simulator.Windows.Interface`）操作桌宠：

- 直播事件在 WebSocket 线程触发，碰桌宠的代码（存档、统计、`Main.Display`、`StartWork` 等）必须 `Ui.Post` 回界面线程。
- 优先走游戏自己的流程。例如投喂用 `IMainWindow.TakeItem` + `TakeItemHandle` + `DisplayFoodAnimation`，不要自己改存档字段，否则升级突破、Buff、模式切换会出问题。
- 不要写 `Main.PlayingVoice`（游戏的语音播放标志，只读）。说话请发布 `SpeakRequestEvent`，不要强制 `Say`。
- 不要在界面线程做没有超时的网络等待。

## 部署

- 外部插件编译成 dll，放进 mod 目录的 `Plugins` 文件夹。宿主启动时扫描该文件夹里所有 `*.dll`，注册其中所有 `public`、非抽象、有公开无参构造函数且实现了 `IBiliPlugin` 的类。
- 只放自己的 dll。接口程序集、`LinePutScript`、VPet 的 dll 宿主已经带了，不要复制第二份。示例工程用 `<CopyLocalLockFileAssemblies>false</CopyLocalLockFileAssemblies>` 和 `<Private>false</Private>` 保证输出里只有插件自己的 dll。
- 编译时引用的接口程序集版本要和用户安装的 mod 里的接口程序集主版本一致。
- 加载失败（例如缺少依赖、类型不匹配）会在主日志 `ModData/BiliVpet/Log/Main` 里留下“加载外部插件...失败”的错误。
- 添加新的第三方 NuGet 依赖前，先检查包里有没有内嵌资源（`.xshd`、`.cur` 等）：宿主加载 mod 时的代码检测会拦截带无法识别资源的 dll。
