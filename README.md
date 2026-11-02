# VPET.Plugin.BiliBiliTest.Interface

VPet“B站直播互动” mod（[VPET.Plugin.BiliBiliTest](https://github.com/YIYuNCU/VPET.Plugin.BiliBiliTest)）的外部插件接口。引用这个程序集，就可以写自己的插件：订阅弹幕、礼物、点赞、上舰事件，让桌宠说话，提供设置页，不需要 mod 的其他源代码。

## 内容

| 位置 | 说明 |
| --- | --- |
| `src/VPET.Plugin.BiliBiliTest.Interface` | 接口程序集：`IBiliPlugin` 等插件接口、`BiliPluginBase`、事件总线与全部事件类型、`Log`、`Ui`、`AppPaths`、`Currency` |
| `samples/EchoSample` | 最小的外部插件示例：订阅弹幕、拦截指令、让桌宠复述、读写设置、提供设置页 |
| `docs/plugin-api.md` | 接口参考：生命周期、事件、处理器优先级、文件位置、金额单位、部署 |
| `skills/bili-plugin-dev` | 给 AI 编程助手用的开发 Skill，内容是开发外部插件的步骤和约定 |

mod 本体（登录、长连接、设置窗口、内置插件）在主仓库中。主仓库把本仓库作为 git 子模块 `Interface/` 引用，接口只在这里修改。

## 快速开始

需要 .NET 8 SDK（Windows）。

```
git clone https://github.com/YIYuNCU/VPET.Plugin.BiliBiliTest.Interface.git
cd VPET.Plugin.BiliBiliTest.Interface/samples/EchoSample
dotnet build -c Release
```

生成的 `EchoSample.dll` 复制到 mod 目录的 `Plugins` 文件夹（没有就新建），重启桌宠。设置窗口里会出现“回声示例”页，直播间观众发“回声 你好”，桌宠会说“<昵称>说：你好”。

写自己的插件：复制 `samples/EchoSample` 改名，修改 `EchoPlugin.cs`。引用接口程序集的写法、必须遵守的约定、部署和调试方法见 [skills/bili-plugin-dev/SKILL.md](skills/bili-plugin-dev/SKILL.md) 和 [docs/plugin-api.md](docs/plugin-api.md)。

## 注意

- 输出里只保留插件自己的 dll。接口程序集、`LinePutScript`、VPet 的 dll 由宿主提供，复制第二份会造成类型不一致。示例工程已经配置好。
- 外部插件使用的接口版本要和用户安装的 mod 对应。接口有不兼容的修改时，会提高本仓库的主版本号。
- 没有在真实的 VPet 和 B 站服务器上验证过的行为，在 mod 主仓库的 `docs/design-review.md` 里记录。

## 许可证

Apache License 2.0，见 [LICENSE](LICENSE)。
