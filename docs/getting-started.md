# 从零写第一个插件

这篇文章带你写一个最小的插件：观众在直播间发“你好桌宠”，桌宠回一句话。写完之后再按需加上设置页、存档和说明文档。

## 准备

- Windows，[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)，一个能编辑 C# 的环境（Visual Studio、Rider、VS Code 都行）。
- 已安装 VPet 和 [B站直播助手 mod](https://steamcommunity.com/sharedfiles/filedetails/?id=3356069105)，并且 mod 能正常登录直播间。
- 本仓库的副本：`git clone https://github.com/YIYuNCU/VPET.Plugin.BiliBiliTest.Interface.git`。

mod 目录指包含 `info.lps` 和 `plugin` 文件夹的那一层，下文的 `SubPlugins` 文件夹放在这一层。

## 1. 新建工程

最快的办法是复制 `samples/EchoSample` 整个文件夹，改名后用。要自己从头建，按下面做。

新建一个类库工程 `HelloPet`，把 `HelloPet.csproj` 改成：

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <UseWPF>true</UseWPF>
    <ImplicitUsings>enable</ImplicitUsings>
    <!-- 宿主已经带了接口程序集、LinePutScript、VPet 的 dll，输出里只保留插件自己的 dll -->
    <CopyLocalLockFileAssemblies>false</CopyLocalLockFileAssemblies>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\VPET.Plugin.BiliBiliTest.Interface\src\VPET.Plugin.BiliBiliTest.Interface\VPET.Plugin.BiliBiliTest.Interface.csproj">
      <Private>false</Private>
    </ProjectReference>
  </ItemGroup>

</Project>
```

`ProjectReference` 的路径按你放本仓库的位置调整。不想引用源码，也可以先编译接口工程，再引用生成的 dll：

```
dotnet build src/VPET.Plugin.BiliBiliTest.Interface -c Release
```

```xml
<ItemGroup>
  <Reference Include="VPET.Plugin.BiliBiliTest.Interface">
    <HintPath>..\VPET.Plugin.BiliBiliTest.Interface\src\VPET.Plugin.BiliBiliTest.Interface\bin\Release\net8.0-windows\VPET.Plugin.BiliBiliTest.Interface.dll</HintPath>
    <Private>false</Private>
  </Reference>
  <PackageReference Include="LinePutScript" Version="1.11.9" />
  <PackageReference Include="VPet-Simulator.Windows.Interface" Version="1.1.0.71" />
</ItemGroup>
```

用 `Reference` 方式时要自己加后两个 `PackageReference`（`ProjectReference` 方式会自动带上）。

## 2. 写插件

新建 `HelloPlugin.cs`：

```csharp
using VPET.Evian.BiliBiliTest.Core;
using VPET.Evian.BiliBiliTest.Core.Events;

namespace HelloPet
{
    public sealed class HelloPlugin : BiliPluginBase
    {
        public override string Id => "HelloPet";           // 唯一，也是存档目录名、设置节名、日志目录名
        public override string DisplayName => "打招呼";
        public override int Order => 200;                   // 内置插件占用 10 到 90，外部插件用 100 以上

        protected override void OnStart()
        {
            Events.Subscribe<DanmakuEvent>(OnDanmaku, priority: 20);
        }

        private void OnDanmaku(DanmakuEvent e)
        {
            if (e.Handled || e.Message != "你好桌宠")
                return;
            e.Handled = true;                                // 告诉其他插件：这条弹幕我处理了
            Log.AddInfoLog($"{e.UserName} 打了招呼");
            Events.Publish(new SpeakRequestEvent { Content = $"{e.UserName}，你好呀", Type = "Notice" });
        }
    }
}
```

要点：

- 类必须是 `public`、非抽象、有公开无参构造函数（没有写构造函数就是默认的），宿主才会加载它。
- `OnStart` 里订阅事件，不需要在 `OnShutdown` 里取消，宿主在插件关闭时统一取消。
- `priority: 20` 排在好感度指令（0）、桌宠动作（5）、投喂（10）之后，好感度弹幕计数（100）之前。完整的优先级表见 [events.md](events.md#处理器优先级)。
- 说话只发布 `SpeakRequestEvent`，由宿主的朗读队列排队，不要自己操作桌宠的气泡。

## 3. 编译和部署

```
dotnet build -c Release
```

在 mod 目录下新建 `SubPlugins/HelloPet/` 文件夹，把输出目录里的 `HelloPet.dll`（和 `HelloPet.pdb`，报错时能看到行号）复制进去。输出目录里应该只有自己的文件，如果出现了 `VPET.Plugin.BiliBiliTest.Interface.dll`、`LinePutScript.dll` 之类，说明 `Private` 或 `CopyLocalLockFileAssemblies` 没配对，不要复制它们。

```
<mod 目录>/
  info.lps
  plugin/
  SubPlugins/
    HelloPet/
      HelloPet.dll
      HelloPet.pdb
```

目录规则：每个插件一个子文件夹；直接放在 `SubPlugins` 根目录的 dll 不会加载。

## 4. 验证

1. 重启桌宠，打开 mod 的设置窗口。左侧“基本设置”页最上面的插件列表里应该能看到“打招呼”，勾选表示启用。
2. 登录直播间（需要主播码并开播），在直播间发“你好桌宠”，桌宠应该说“<昵称>，你好呀”。
3. 没有出现？打开 `<游戏主目录>/ModData/BiliVpet/Log/Main/` 里最新的日志，找“加载外部插件”开头的错误。常见原因见 [faq.md](faq.md)。

## 5. 进阶

### 加一个设置

在类上额外实现 `IPluginSettings`，把需要保存的值写进设置节：

```csharp
public sealed class HelloPlugin : BiliPluginBase, IPluginSettings
{
    private string keyword = "你好桌宠";                   // 默认值写在字段上

    public void LoadSettings(LpsDocument config)
    {
        var section = config[Id];
        if (section.Count == 0)
            return;                                          // 首次运行，用默认值
        string? saved = section[(gstr)"Keyword"];
        if (!string.IsNullOrEmpty(saved))
            keyword = saved;
    }

    public void SaveSettings(LpsDocument config)
    {
        var section = config[Id];
        section[(gbol)"EnableLog"] = Log.Enable;
        section[(gstr)"Keyword"] = keyword;
    }

    public IEnumerable<SettingsPage> CreateSettingsPages() { yield break; }   // 暂时没有设置页
}
```

需要 `using LinePutScript;` 和 `using LinePutScript.Converter;`。再把 `OnDanmaku` 里的 `"你好桌宠"` 换成 `keyword`。要让用户在设置窗口里改，返回一个 `SettingsPage`，`samples/EchoSample/EchoPlugin.cs` 里有完整写法（用代码建控件，点“确定”或“应用”才写回字段）。读写规则见 [plugin-api.md](plugin-api.md#设置)。

### 保存数据

实现 `IPluginSaveData`：`LoadData(dataDirectory)` 拿到本插件独占的目录，`SaveData()` 里保存并释放文件占用。规则见 [plugin-api.md](plugin-api.md#存档)。

### 写说明文档

实现 `IPluginDocs`，返回 `PluginDoc("打招呼说明", 路径)`，路径指向插件文件夹里的 Markdown 文件。用户在设置窗口的“说明”页能看到。规则见 [plugin-api.md](plugin-api.md#说明文档)。

### 更多事件

弹幕、礼物、点赞、上舰、充电弹幕、进房，以及向好感度插件查询、扣观众金钱的写法，见 [events.md](events.md)。

## 调试

- 日志：`Log.AddInfoLog` 等写入 `<游戏主目录>/ModData/BiliVpet/Log/<插件Id>/`，在设置窗口的“日志工具”页也能实时看。日志会按“日志工具”页设置的最低等级过滤，调试时把等级调到“调试”。
- 不开播测试：把事件处理逻辑写成不依赖宿主的方法，用单元测试里的 `new EventBus(new Log("test", false))` 发布事件来验证。
- 异常：事件处理器里抛出的异常会被记录在主日志里，不会让 mod 崩溃，也不会影响其他插件。
- 调试器：按 .NET 的常规方式把调试器附加到 VPet 进程。

## 分享给别人

把 `SubPlugins/HelloPet/` 整个文件夹打成 zip。使用者解压到自己 mod 目录的 `SubPlugins` 下，重启桌宠即可。发布前：

1. 确认文件夹里没有接口程序集、`LinePutScript`、VPet 的 dll。
2. 在 README 里写清楚依赖的接口版本（见 [CHANGELOG.md](../CHANGELOG.md)）和已知限制。
3. `Id` 起得有辨识度，避免和别人的插件、内置插件冲突。
