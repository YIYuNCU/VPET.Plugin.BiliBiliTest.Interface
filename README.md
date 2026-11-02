# VPET.Plugin.BiliBiliTest.Interface

[B站直播助手](https://steamcommunity.com/sharedfiles/filedetails/?id=3356069105)（VPet 桌宠的 B 站直播互动 mod）的外部插件接口。引用这个程序集，就能写自己的插件：订阅弹幕、礼物、点赞、上舰事件，让桌宠说话，给观众加好感度和金钱，提供设置页和说明文档。

mod 本体不开源。这个仓库包含写插件需要的全部内容：接口程序集的源码、文档、示例插件和给 AI 编程助手用的开发 Skill。

## 能做什么

- 响应直播间事件：弹幕、礼物、点赞、上舰、充电弹幕、进房、开播状态。
- 让桌宠说话：发布一个请求，由 mod 的朗读队列排队朗读。
- 和内置功能配合：查询观众的好感度、金钱、舰长类型，增减金钱，拦截弹幕指令。
- 在 mod 设置窗口里加自己的设置页、说明文档，保存自己的设置和数据。

## 文档

| 文档 | 内容 |
| --- | --- |
| [docs/getting-started.md](docs/getting-started.md) | 从零写第一个插件：建工程、写代码、部署、验证、调试、分享 |
| [docs/plugin-api.md](docs/plugin-api.md) | 接口参考：生命周期、基类、设置、存档、说明文档、日志、文件位置、金额单位、部署 |
| [docs/events.md](docs/events.md) | 事件参考：全部事件和字段、处理器优先级、内置插件、常见用法 |
| [docs/faq.md](docs/faq.md) | 常见问题：插件没加载、没反应、设置没保存，以及尚未验证的行为 |
| [samples/EchoSample](samples/EchoSample) | 最小的外部插件示例 |
| [skills/bili-plugin-dev](skills/bili-plugin-dev/SKILL.md) | 开发 Skill，让 AI 编程助手按本仓库的约定帮你写插件 |
| [CHANGELOG.md](CHANGELOG.md) | 版本变更 |

## 快速开始

需要 Windows、[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)，以及已安装 mod 的 VPet。

```
git clone https://github.com/YIYuNCU/VPET.Plugin.BiliBiliTest.Interface.git
cd VPET.Plugin.BiliBiliTest.Interface/samples/EchoSample
dotnet build -c Release
```

把生成的 `EchoSample.dll` 复制到 mod 目录的 `SubPlugins/EchoSample/` 文件夹（每个插件一个子文件夹，没有就新建），重启桌宠。mod 目录是包含 `info.lps` 和 `plugin` 文件夹的那一层。设置窗口里会出现“回声示例”页，直播间观众发“回声 你好”，桌宠会说“<昵称>说：你好”。

写自己的插件：复制 `samples/EchoSample` 改名，修改 `EchoPlugin.cs`，详细步骤见 [docs/getting-started.md](docs/getting-started.md)。

## 仓库结构

```
src/VPET.Plugin.BiliBiliTest.Interface/   接口程序集：IBiliPlugin 等接口、BiliPluginBase、事件总线和全部事件类型、Log、Ui、AppPaths、Currency
samples/EchoSample/                       示例插件
docs/                                     文档
skills/bili-plugin-dev/                   开发 Skill
```

## 注意事项

- 插件文件夹里只放插件自己的 dll 和依赖。接口程序集、`LinePutScript`、VPet 的 dll 由 mod 提供，不要复制进去。示例工程已经配置好。
- 外部插件编译时引用的接口版本，要和用户安装的 mod 对应。接口有不兼容的修改时会提高主版本号，见 [CHANGELOG.md](CHANGELOG.md)。
- 部分行为没有在真实的 VPet 和 B 站服务器上验证过，列在 [docs/faq.md](docs/faq.md#没有验证的行为)。

## 反馈

遇到问题或有需求，在本仓库提 [Issue](https://github.com/YIYuNCU/VPET.Plugin.BiliBiliTest.Interface/issues)，或到 QQ 群 776377490。请附上 `ModData/BiliVpet/Log/Main/` 里相关的日志片段。

## 许可证

Apache License 2.0，见 [LICENSE](LICENSE)。用这个接口写的插件，许可证由你自己决定。
