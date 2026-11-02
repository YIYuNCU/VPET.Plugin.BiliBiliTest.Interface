# EchoSample

最小的外部插件示例。观众发的弹幕以指令词（默认“回声”）开头时，桌宠把后面的内容复述一遍。

## 演示了什么

| 位置 | 内容 |
| --- | --- |
| `EchoPlugin` 类声明 | 继承 `BiliPluginBase`，实现 `IPluginSettings` |
| `Id`、`DisplayName`、`Order` | 插件身份。`Id` 同时是设置节名和日志目录名 |
| `OnStart` | 用优先级 20 订阅 `DanmakuEvent`；订阅 `BlockedWordsResendEvent`，读弹幕插件重启后重发屏蔽词 |
| `OnDanmaku` | 检查 `Handled`，匹配后置 `Handled = true` 拦截弹幕，用 `Log` 记日志，发布 `SpeakRequestEvent` 让桌宠说话 |
| `PublishBlockedWords` | 发布 `BlockedWordsRequestEvent`，让读弹幕插件不要把指令词当成普通弹幕朗读 |
| `LoadSettings` / `SaveSettings` | 读写自己的设置节；首次运行保持字段默认值；写入 `EnableLog` |
| `CreateSettingsPages` | 用代码构建设置页，点“确定”或“应用”时才把值写回字段 |

## 使用

```
dotnet build -c Release
```

把输出目录里的 `EchoSample.dll` 复制到 `<游戏主目录>/ModData/BiliVpet/SubPlugins/EchoSample/`（设置窗口“基本设置”页的“打开子插件文件夹”按钮可以打开 `SubPlugins`），重启桌宠。

输出目录里只有 `EchoSample.dll`，这是 `EchoSample.csproj` 里 `CopyLocalLockFileAssemblies=false` 和 `Private=false` 的效果：接口程序集、`LinePutScript`、VPet 的 dll 由 mod 提供，不需要复制。

## 改成自己的插件

1. 复制整个文件夹，改文件夹名、`.csproj` 名、命名空间和类名。
2. 修改 `Id`、`DisplayName`，`Id` 要有辨识度，避免和内置插件、别的插件重复。
3. 修改 `OnDanmaku` 和设置项。
