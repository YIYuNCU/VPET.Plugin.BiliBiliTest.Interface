# 变更记录

版本号写在接口程序集的 `Version` 里（`src/VPET.Plugin.BiliBiliTest.Interface/VPET.Plugin.BiliBiliTest.Interface.csproj`）。主版本号变化表示有不兼容的修改，外部插件需要重新编译；次版本号变化表示向后兼容的新增。

## 1.0.0

首个版本。从 mod 内部抽出的外部插件接口。

- 插件接口：`IBiliPlugin`、`IPluginSettings`、`IPluginSaveData`、`IPluginDocs`，基类 `BiliPluginBase`，`PluginContext`，只读的直播状态 `ILiveSession`。
- 设置页 `SettingsPage`，带 `Fill` 和 `ConfirmClose`。
- 事件总线 `EventBus` / `PluginEvents` 和全部事件类型：直播间事件、会话事件、`SpeakRequestEvent`、`UserProfileQuery`、`UserMoneyAdjustEvent` 等。
- 工具类：`Log`、`Ui`、`AppPaths`、`Currency`、`ConfigStore`。
- 外部插件目录为 mod 目录下的 `SubPlugins/<插件名>/`，每个插件一个子文件夹。
- 示例插件 `samples/EchoSample` 和开发 Skill。
