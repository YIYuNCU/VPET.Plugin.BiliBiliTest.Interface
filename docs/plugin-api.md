# 插件接口参考

本文说明接口程序集 `VPET.Plugin.BiliBiliTest.Interface` 里的插件接口、基类和工具类。事件、字段、优先级见 [events.md](events.md)，从零开始写第一个插件见 [getting-started.md](getting-started.md)。

接口类型所在的命名空间是 `VPET.Evian.BiliBiliTest.Core`（事件在 `VPET.Evian.BiliBiliTest.Core.Events`）。mod 本体闭源，本文和程序集自带的 XML 注释就是全部契约；文档没有写的行为不要靠猜测依赖。

## 生命周期

宿主按下面的顺序调用，带方括号的是可选接口：

```
构造函数 → Initialize → [LoadSettings] → [LoadData] → Start → 运行
        → [SaveSettings]（点“确定”“应用”和退出时，可多次）→ [SaveData] → Shutdown
```

- 宿主加载 dll 时，对每个符合条件的类调用一次构造函数，创建一个备用实例。构造函数里不要做耗时或有副作用的事。
- 用户可以在“基本设置”里随时关闭、再启用插件。关闭时宿主先取消该插件的全部事件订阅，再依次调用 `SaveSettings`、`SaveData`、`Shutdown`，然后丢弃实例。再次启用时创建全新实例，从 `Initialize` 重新开始。
- 启用最多等 10 秒，关闭最多等 5 秒，超时按失败或已关闭处理。启用和关闭可能在非界面线程执行，`OnStart`、`OnShutdown` 里操作控件要经过 `Ui.Dispatcher`。
- `Shutdown` 必须释放自己创建的线程、任务、定时器、挂到主窗口的控件和事件、数据库连接。
- 同一个 `Id` 重复注册时，后注册的被忽略，主日志里有一条警告。

## 插件接口

| 类型 | 说明 |
| --- | --- |
| `IBiliPlugin` | 必须实现：`Id`、`DisplayName`、`Order`、`Required`（默认 false）、`Initialize(PluginContext)`、`Start()`、`Shutdown()` |
| `BiliPluginBase` | 实现了 `IBiliPlugin` 的基类，推荐继承 |
| `IPluginSettings` | 设置：`LoadSettings(LpsDocument)`、`SaveSettings(LpsDocument)`、`CreateSettingsPages()` |
| `IPluginSaveData` | 存档：`LoadData(string dataDirectory)`、`SaveData()` |
| `IPluginDocs` | 说明文档：`CreateDocs()` 返回若干 `PluginDoc(标题, Markdown 文件完整路径)` |
| `SettingsPage` | 设置窗口的一个页签 |
| `PluginContext` | 宿主给插件的能力 |
| `ILiveSession` | 只读的直播状态 |

宿主加载的条件：类是 `public`、非抽象、有公开无参构造函数，并且实现了 `IBiliPlugin`。

### BiliPluginBase

| 成员 | 默认值 | 说明 |
| --- | --- | --- |
| `Id`（抽象） | 无 | 唯一标识。同时是存档目录名，也是默认的设置节名和日志目录名。不要与 [内置插件](events.md#内置插件) 冲突 |
| `DisplayName` | 同 `Id` | 在基本设置、设置页导航里显示的名字 |
| `Order` | 100 | 加载顺序，数值小的先加载，同时决定设置页签的顺序。内置插件占用 10 到 90，外部插件用 100 以上 |
| `Required` | false | true 表示用户不能关闭它。外部插件一般不需要 |
| `LogTitle`（protected） | 同 `Id` | 日志目录名 |
| `ConfigSection`（protected） | 同 `Id` | 保存 `EnableLog` 的设置节 |
| `Context`（protected） | | 宿主上下文 |
| `Events`（protected） | | 事件入口，等于 `Context.Events` |
| `Log` | | 本插件的日志 |
| `OnStart()`（抽象） | | 设置和存档已经读取完毕，在这里订阅事件、开始工作 |
| `OnShutdown()` | 空 | 释放资源 |

`Initialize` 时基类创建 `Log`：设置文件里还没有本插件的设置节（首次运行）时日志默认开启，否则读 `<ConfigSection>.EnableLog`。想让用户能关日志，在 `SaveSettings` 里写 `section[(gbol)"EnableLog"] = Log.Enable;`，并在设置页提供开关。会话结束（`SessionEndedEvent`）和插件关闭时，基类会自动保存日志。

不继承基类也可以：直接实现 `IBiliPlugin`，自己处理 `Context`、日志等。

### PluginContext 和 ILiveSession

| 成员 | 说明 |
| --- | --- |
| `Context.MainWindow` | 桌宠主窗口，类型 `IMainWindow`（NuGet 包 `VPet-Simulator.Windows.Interface`） |
| `Context.Events` | 事件入口 `PluginEvents` |
| `Context.Session` | `ILiveSession`：`Working`（是否已登录并接收直播间事件）、`GameId`（本场直播 ID，未登录为空）、`AnchorOpenId`（主播的 open_id，未登录为空） |

登录、开播、关闭由宿主负责，插件不能操作。状态变化订阅 `SessionStartedEvent` / `SessionEndedEvent`。

## 设置

所有插件的设置保存在同一个文件 `ModData/BiliVpet/Configs/Config_User.lps`，宿主启动时读入内存，各插件在 `LoadSettings` 里取自己的节，在 `SaveSettings` 里写回。

```csharp
public void LoadSettings(LpsDocument config)
{
    var section = config[Id];
    if (section.Count == 0)
        return;                                  // 首次运行：保持字段上的默认值
    int saved = section[(gint)"MaxLength"];
    if (saved >= 1)
        maxLength = saved;
}

public void SaveSettings(LpsDocument config)
{
    var section = config[Id];
    section[(gbol)"EnableLog"] = Log.Enable;
    section[(gint)"MaxLength"] = maxLength;
}
```

- 读：`section[(gint)"Key"]`、`(gbol)`、`(gdbe)`、`(gstr)`（需要 `using LinePutScript.Converter;`）。键不存在时得到类型默认值（0、false、空字符串，`(gstr)` 也可能是 null），所以要先判断是否有效，无效就保持字段默认值。
- 写：`section[(gint)"Key"] = value;`。只更新自己拥有的键，不要新建文档，否则会丢掉别的插件的设置。
- 一个设置节下有多行同名数据时，保存前先 `config.RemoveAll("节名")` 再逐行写入，否则旧数据会残留。
- 默认值写在插件字段上。首次运行时设置文件不存在，宿主从空文档开始，点“确定”或“应用”后才会写出文件。
- 设置只在 `LoadSettings` 读、在 `SaveSettings` 写。

### 设置页 SettingsPage

`CreateSettingsPages()` 每次打开设置窗口时调用，返回的每个 `SettingsPage` 是左侧导航里的一个页签：

```csharp
new SettingsPage("页签标题", userControl, apply)
{
    Fill = false,                 // 可选
    ConfirmClose = () => true,    // 可选
}
```

| 参数 | 说明 |
| --- | --- |
| `header` | 页签标题 |
| `content` | 页面内容，一个 `UserControl` |
| `apply` | 用户点设置窗口的“确定”或“应用”时调用，把界面上的值写回插件字段，之后宿主调用 `SaveSettings`。点“取消”不会调用 |
| `Fill` | 页面自带滚动条（例如表格）时设为 true。设置窗口默认给每页套一个垂直滚动条，设了 `Fill` 就不套，把可用高度全给页面 |
| `ConfirmClose` | 设置窗口关闭前调用，返回 false 取消关闭。有未保存的修改时在这里询问用户 |

建议的写法：控件构造时从插件复制一份值作为界面绑定的属性，`apply` 里才写回插件。这样点“取消”不会改变插件。

样式：XAML 里引入 `/VPet-Simulator.Windows.Interface;component/ResourceStyle.xaml`，背景用 `{DynamicResource PrimaryLighter}`，和游戏主题保持一致。不想写 XAML 可以用代码构建控件，示例插件就是这样做的。

## 存档

实现 `IPluginSaveData`。`LoadData(dataDirectory)` 给的目录是 `ModData/BiliVpet/Data/<Id>`，已创建，由本插件独占。`SaveData()` 保存并释放文件占用，SQLite 之类的连接要在这里关闭。

不需要持久化数据的插件不用实现这个接口。

## 说明文档

实现 `IPluginDocs`，`CreateDocs()` 返回的文档会出现在设置窗口的“说明”页，点“打开说明”弹窗显示。

- `PluginDoc.Path` 是 Markdown 文件的完整路径。文档放在自己的子文件夹里，用 `Path.Combine(Path.GetDirectoryName(typeof(MyPlugin).Assembly.Location)!, "readme.md")` 取路径。
- 支持标题、段落、列表、表格、引用、代码、粗体斜体、链接、图片。HTML 标签不解析，按原文显示。
- 图片路径相对 md 文件，后面写 `{width=380}` 限制最大宽度，例如 `![](./Pictures/x.png){width=380}`。
- 如果文档放在 csproj 里，记得设置 `CopyToOutputDirectory`，并且把文档文件一起复制到插件文件夹。

## 工具类

| 类型 | 说明 |
| --- | --- |
| `Log` | 日志，见下 |
| `Ui` | `Dispatcher`（界面线程）、`Post(Action)`（投递到界面线程异步执行）、`ShowMessage(string)`（弹出消息框）。`Ui.Init()` 由宿主调用，插件不要调用 |
| `AppPaths` | MOD 存储目录下的路径，见“文件位置” |
| `Currency` | 金额换算常量：`GoldPerBattery = 100`、`GoldPerYuan = 1000` |
| `ConfigStore` | 设置文件的内存副本，由宿主使用。插件通过 `IPluginSettings` 读写，不直接使用 |
| `EventBus` | 进程内事件总线，由宿主创建。插件使用 `Events`（`PluginEvents`）。写单元测试时可以直接 `new EventBus(new Log(...))` |

### Log

| 成员 | 说明 |
| --- | --- |
| `AddInfoLog` / `AddDebugLog` / `AddWarningLog` / `AddErrorLog` | 按等级记录一行 |
| `AddInfoLog_Once` 等 | 同一内容只记录一次，日志保存后重新计算 |
| `Enable` | 是否记录。关闭时上面的方法什么也不做 |
| `Log.MinLevel`（静态） | 所有日志共用的最低记录等级（`Debug` < `Info` < `Warning` < `Error`），由“日志工具”页设置 |
| `SaveLog()` | 把尚未写出的内容写入文件 |

日志可以从任意线程写入，先存在内存，保存时一次性写到 `ModData/BiliVpet/Log/<日志目录名>/`。`BiliPluginBase.Log` 已经创建好。

## 文件位置

设置、存档、日志、下载的资源都在 VPet 提供的 MOD 存储目录，即游戏主目录下的 `ModData/BiliVpet`，不会被 mod 更新或重装覆盖：

| 内容 | 位置 |
| --- | --- |
| 设置 | `Configs/Config_User.lps`（`AppPaths.UserConfig`） |
| 存档 | `Data/<插件Id>/`（`AppPaths.Data`，`LoadData` 直接给出） |
| 日志 | `Log/<日志目录名>/`（`AppPaths.LogRoot`） |
| 下载的资源 | `Resources/`（`AppPaths.Resources`） |

mod 目录（包含 `info.lps` 和 `plugin` 文件夹的那一层）只放随 mod 发布的只读内容，运行时往里写的文件可能被 mod 更新清掉。外部插件放在 mod 目录的 `SubPlugins/<插件名>/`，见“部署”。

## 金额单位

- 内部金额的单位是 1/1000 元，`1000 = 1 元`（B 站称金瓜子）。包括 `GiftEvent.Price`、`Gift.Price`、`GuardEvent.Price`、`UserProfileQuery.Money`、`UserMoneyAdjustEvent.Delta`。
- 设置页里填的价格和对观众播报的金额用“电池”，`1 电池 = 100 内部单位 = 0.1 元`。换算用 `Currency`，只在设置和播报这一层做，内部计算全部用内部单位。
- `SuperChatEvent.Rmb` 的单位是元，不要和礼物金额混用。
- 礼物的 `Price` 是单价，总价是 `Price × GiftNum`。

## 与桌宠本体交互

通过 `Context.MainWindow`（`IMainWindow`）操作桌宠：

- 直播事件在 WebSocket 线程触发，碰桌宠的代码（存档、统计、`Main.Display`、`StartWork` 等）必须经 `Ui.Post` 回到界面线程。
- 优先走游戏自己的流程。例如投喂用 `IMainWindow.TakeItem` + `TakeItemHandle` + `DisplayFoodAnimation`，不要自己改存档字段，否则升级突破、Buff、模式切换会出问题。
- 不要写 `Main.PlayingVoice`（游戏的语音播放标志，只读）。让桌宠说话请发布 `SpeakRequestEvent`，不要强制 `Say`。
- 界面线程上不要做没有超时的网络等待。

## 部署

- 每个外部插件一个文件夹：`<mod 目录>/SubPlugins/<插件名>/`，把编译出的插件 dll 放在里面。宿主启动时扫描 `SubPlugins` 下每个子文件夹里的所有 `*.dll`，注册其中符合条件的类。直接放在 `SubPlugins` 根目录的 dll 不会加载；非托管 dll 会被跳过。
- 插件自己的第三方依赖 dll、说明文档、图片放在同一个子文件夹里。
- 子文件夹里不要放接口程序集、`LinePutScript` 和 VPet 的 dll：宿主已经带了，复制进去没有用处，版本和宿主不同时还可能引起加载错误。示例工程用 `<CopyLocalLockFileAssemblies>false</CopyLocalLockFileAssemblies>` 和 `<Private>false</Private>` 保证输出里只有插件自己的 dll。
- 加载失败（缺少依赖、类型不匹配等）会在主日志 `ModData/BiliVpet/Log/Main/` 里留下“加载外部插件...失败”的错误。排查方法见 [faq.md](faq.md)。
- 添加新的第三方 NuGet 依赖前，先检查包里有没有内嵌资源（`.xshd`、`.cur` 等）：宿主加载 mod 时的代码检测会拦截带无法识别资源的 dll。

## 版本

接口程序集的版本号在 [CHANGELOG.md](../CHANGELOG.md) 里记录。外部插件编译时引用的接口版本，要和用户安装的 mod 里的接口程序集主版本一致。
