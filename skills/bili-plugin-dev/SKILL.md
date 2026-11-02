---
name: bili-plugin-dev
description: 为 VPet 的“B站直播互动” mod 开发外部插件时使用：新建插件工程、实现 IBiliPlugin / BiliPluginBase、订阅直播事件（弹幕、礼物、点赞、上舰）、发布朗读请求、设置页、存档、说明文档、部署 dll、调试。只依赖接口程序集 VPET.Plugin.BiliBiliTest.Interface，不需要 mod 源代码。Use when writing an external plugin for the VPet Bilibili live-stream interaction mod.
---

# B站直播互动 mod 外部插件开发

你在为一个已经发布的 mod 写外部插件，手里只有接口仓库（本仓库），没有 mod 的源代码。能依赖的只有接口程序集 `VPET.Plugin.BiliBiliTest.Interface` 里的公开类型，以及 [docs/plugin-api.md](../../docs/plugin-api.md) 记录的行为。

- 接口文档里没写的行为，不要靠猜测写进代码。能通过接口类型的 XML 注释（编译后随程序集提供的 `.xml`）确认的再用；确认不了的，告诉用户“这一点接口文档没有说明，需要在实机上验证”，并在插件 README 里记下来。
- 不要去找或假设内置插件（读弹幕、好感度、投喂等）的内部类型。内置插件只能通过事件交互，事件和字段见接口文档。
- 先读 [docs/plugin-api.md](../../docs/plugin-api.md)（生命周期、基类、设置、存档、文件位置、金额单位）和 [docs/events.md](../../docs/events.md)（事件、字段、优先级），新手可以先跟着 [docs/getting-started.md](../../docs/getting-started.md) 做一遍，再看 [samples/EchoSample](../../samples/EchoSample) 的完整示例，然后动手。

## 新建插件工程

1. 新建 `net8.0-windows` 类库，开启 `UseWPF`、`Nullable`、`ImplicitUsings`。
2. 引用接口程序集，并且不要把宿主已有的 dll 复制进输出：

   ```xml
   <PropertyGroup>
     <TargetFramework>net8.0-windows</TargetFramework>
     <UseWPF>true</UseWPF>
     <Nullable>enable</Nullable>
     <ImplicitUsings>enable</ImplicitUsings>
     <CopyLocalLockFileAssemblies>false</CopyLocalLockFileAssemblies>
   </PropertyGroup>
   <ItemGroup>
     <!-- 二选一：有接口仓库源码时用 ProjectReference，只有编译好的 dll 时用 Reference -->
     <ProjectReference Include="path\to\src\VPET.Plugin.BiliBiliTest.Interface\VPET.Plugin.BiliBiliTest.Interface.csproj">
       <Private>false</Private>
     </ProjectReference>
     <!-- <Reference Include="VPET.Plugin.BiliBiliTest.Interface"><HintPath>path\to\VPET.Plugin.BiliBiliTest.Interface.dll</HintPath><Private>false</Private></Reference> -->
   </ItemGroup>
   ```

   用 `Reference` 方式时还要自己加 `LinePutScript` 1.11.9、`VPet-Simulator.Windows.Interface` 1.1.0.71 的 `PackageReference`（`ProjectReference` 方式会自动带上）。
3. 新建继承 `BiliPluginBase` 的类，类必须 `public`、非抽象、有公开无参构造函数，宿主才会加载它：

   ```csharp
   public sealed class MyPlugin : BiliPluginBase, IPluginSettings
   {
       public override string Id => "MyPlugin";      // 唯一；也是存档目录名、设置节名、日志目录名
       public override string DisplayName => "我的插件";
       public override int Order => 200;              // 内置插件占用 10 到 90，外部插件用 100 以上

       protected override void OnStart()
       {
           Events.Subscribe<DanmakuEvent>(OnDanmaku, priority: 20);
       }

       protected override void OnShutdown() { /* 释放自己创建的一切 */ }
   }
   ```
4. 需要设置、存档、说明文档时，分别实现 `IPluginSettings`、`IPluginSaveData`、`IPluginDocs`。具体约定见接口文档的“接口”和“设置读写”。

## 选事件和优先级

1. 先确定要响应什么：弹幕指令用 `DanmakuEvent`，礼物用 `GiftEvent`（逐个）或 `GiftBatchEvent`（10 秒一批，已合并），上舰用 `GuardEvent`，等等。字段和单位见接口文档“直播间事件”。
2. 要消费弹幕指令：优先级取 1 到 99，匹配后置 `Handled = true`，并在处理前检查 `Handled`。再用 `BlockedWordsRequestEvent` 把指令词交给读弹幕插件屏蔽，订阅 `BlockedWordsResendEvent` 在读弹幕插件重启后重发。
3. 要说话：发布 `SpeakRequestEvent`（设 `Content` 和 `Type`），不要自己操作气泡。
4. 要观众的好感度、金钱、舰长类型：发布 `UserProfileQuery`，读结果前检查 `Answered`。要增减金钱：发布 `UserMoneyAdjustEvent`。
5. 优先级表在 [docs/events.md](../../docs/events.md#处理器优先级)，选值前先看，不要和内置插件抢同一个位置。

## 约定

- 插件之间不引用类型，只通过事件交互。
- 订阅只用 `Events.Subscribe`，宿主会在插件关闭时统一取消；不要自己保存订阅返回值。
- 直播事件在 B 站 WebSocket 线程同步触发，处理器里不做耗时阻塞操作；操作控件、碰桌宠都用 `Ui.Post` 回到界面线程。`OnStart`/`OnShutdown` 也可能在非界面线程执行，操作控件要经过 `Ui.Dispatcher`。
- 事件类型必须与订阅时的类型完全一致，总线不做继承匹配。
- 构造函数里不做耗时或有副作用的事：宿主加载时会先创建一个备用实例。
- 设置只在 `LoadSettings` 读、`SaveSettings` 写，只更新自己拥有的键。首次运行设置节是空的（`section.Count == 0`），默认值写在字段上，空节时 `LoadSettings` 直接返回。
- 存档只在 `LoadData` 读、`SaveData` 写；运行时要写的文件都放在 `LoadData` 给的目录或 `AppPaths` 下，不要写进 mod 目录和自己的 `SubPlugins/<插件名>` 文件夹。
- 金额：事件里的价格是 1/1000 元（`1000 = 1 元`），设置页和播报用电池（`1 电池 = 100` 内部单位），换算用 `Currency`。`GiftEvent.Price` 是单价，总价是 `Price × GiftNum`。`SuperChatEvent.Rmb` 单位是元。
- `OnShutdown` 必须释放自己创建的线程、任务、定时器、挂到主窗口的控件和事件、数据库连接。用户随时可能关闭再启用插件，再次启用是全新实例。
- 碰桌宠（`Context.MainWindow`）：走游戏自己的流程，不要自己改存档字段，不要写 `Main.PlayingVoice`，不要强制 `Say`，界面线程上不做无超时的网络等待。
- 日志用基类的 `Log`。开关保存在 `<ConfigSection>.EnableLog`，想让用户能改就在设置页提供开关，并在 `SaveSettings` 里写回 `Log.Enable`。
- 新增第三方 NuGet 依赖前，先确认包里没有内嵌资源（`.xshd`、`.cur` 等）：宿主加载 mod 时的代码检测会拦截带无法识别资源的 dll。

## 设置页

- `SettingsPage(标题, UserControl, apply)`。控件构造时从插件复制一份值作为绑定属性，`apply` 里才写回插件字段，用户点“取消”就不会改变插件。
- 页面自带滚动条（例如表格）时设 `Fill = true`。有未保存的数据时设 `ConfirmClose`，返回 false 取消关闭。
- XAML 里引入 `/VPet-Simulator.Windows.Interface;component/ResourceStyle.xaml`，背景用 `{DynamicResource PrimaryLighter}`。不想写 XAML 可以像示例那样用代码构建控件。

## 说明文档

- 实现 `IPluginDocs`，返回 `PluginDoc(标题, Markdown 完整路径)`。文档放在自己的 dll 旁边，路径用 `Path.Combine(Path.GetDirectoryName(typeof(MyPlugin).Assembly.Location)!, "Docs", "readme.md")`，并在 csproj 里设置 `CopyToOutputDirectory`。
- 支持标题、段落、列表、表格、引用、代码、粗体斜体、链接、图片。HTML 不解析。强调用 `**粗体**`，提醒用 `> 引用`，图片后加 `{width=380}` 限制宽度。

## 部署和调试

1. `dotnet build -c Release`，把输出里**自己的 dll**（不含接口程序集、LinePutScript、VPet 的 dll）复制到 `<游戏主目录>/ModData/BiliVpet/SubPlugins/<插件名>/`（每个插件一个子文件夹，没有就新建；直接放在 `SubPlugins` 根目录的 dll 不会加载；设置窗口“基本设置”页的“打开子插件文件夹”按钮可以打开 `SubPlugins`）。插件自己的第三方依赖 dll 和说明文档也放在这个子文件夹里。
2. 重启桌宠。设置窗口左侧导航会出现新插件的页签，“基本设置”里能看到它的启用开关。
3. 看日志：`<游戏目录>/ModData/BiliVpet/Log/<LogTitle>/` 是插件自己的日志，`Log/Main/` 是宿主日志，插件没加载出来时看这里的“加载外部插件...失败”。
4. 没有真实直播时，可以在设置窗口里启用插件、检查设置页和存档；事件逻辑要么开播测试，要么在自己的单元测试里直接 `new EventBus(...)` 发布事件验证。

## 完成前检查

1. `dotnet build` 通过，输出目录里只有自己的 dll。
2. 反复启用、关闭插件后，事件不重复触发，文件和数据库连接没有被占用。
3. 修改设置、保存、重启后，值与保存前一致；首次运行（删掉设置节）时用默认值。
4. 没有实机验证的行为、单位假设、遗留限制，写进插件自己的 README，不要当成已验证的事实。

## 本 skill 依赖的接口仓库位置

修改下面任一位置后，必须回来检查本 skill、[docs/plugin-api.md](../../docs/plugin-api.md)、[docs/events.md](../../docs/events.md)、[docs/getting-started.md](../../docs/getting-started.md)、[docs/faq.md](../../docs/faq.md) 是否仍然准确，并在 [CHANGELOG.md](../../CHANGELOG.md) 记录变更：

- `src/VPET.Plugin.BiliBiliTest.Interface/Plugins/`：`IBiliPlugin.cs`、`BiliPluginBase.cs`、`PluginContext.cs`、`ILiveSession.cs`
- `src/VPET.Plugin.BiliBiliTest.Interface/Events/`：`EventBus.cs`、`PluginEvents.cs`、`LiveEvents.cs`、`AppEvents.cs`
- `src/VPET.Plugin.BiliBiliTest.Interface/`：`AppPaths.cs`、`ConfigStore.cs`、`Log.cs`、`Ui.cs`、`Currency.cs`
- `samples/EchoSample/`：示例要能编译，并且演示 skill 里写的做法
