# 变更记录

版本号写在接口程序集的 `Version` 里（`src/VPET.Plugin.BiliBiliTest.Interface/VPET.Plugin.BiliBiliTest.Interface.csproj`）。主版本号变化表示有不兼容的修改，外部插件需要重新编译；次版本号变化表示向后兼容的新增。

## 1.1.0

- 新增 `AppPaths.SubPlugins`。外部插件目录从 mod 目录下的 `SubPlugins/<插件名>/` 改到 MOD 存储目录 `ModData/BiliVpet/SubPlugins/<插件名>/`，不再受 mod 更新、重装影响。旧位置不再扫描，已放在 mod 目录下的插件要移过去。
- mod 设置窗口“基本设置”页新增“打开子插件文件夹”按钮。

## 1.0.0

首个版本。从 mod 内部抽出的外部插件接口。

- 插件接口：`IBiliPlugin`、`IPluginSettings`、`IPluginSaveData`、`IPluginDocs`，基类 `BiliPluginBase`，`PluginContext`，只读的直播状态 `ILiveSession`。
- 设置页 `SettingsPage`，带 `Fill` 和 `ConfirmClose`。
- 事件总线 `EventBus` / `PluginEvents` 和全部事件类型：直播间事件、会话事件、`SpeakRequestEvent`、`UserProfileQuery`、`UserMoneyAdjustEvent` 等。
- 工具类：`Log`、`Ui`、`AppPaths`、`Currency`、`ConfigStore`。
- 每个外部插件一个子文件夹（1.0.0 时在 mod 目录下，1.1.0 起改到 MOD 存储目录）。
- 示例插件 `samples/EchoSample` 和开发 Skill。
