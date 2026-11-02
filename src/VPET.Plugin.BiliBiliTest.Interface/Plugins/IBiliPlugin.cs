using LinePutScript;
using System.Windows.Controls;

namespace VPET.Evian.BiliBiliTest.Core
{
    /// <summary>
    /// 插件接入入口。实现该接口（通常继承 <see cref="BiliPluginBase"/>），编译成 dll 后放到 mod 目录的 SubPlugins/&lt;插件名&gt;/ 文件夹，
    /// 宿主启动时自动加载其中所有公开、非抽象、有公开无参构造的实现类。每次启用插件都会重新创建一个实例。
    /// <para>
    /// 生命周期（宿主按此顺序调用）：Initialize → [IPluginSettings.LoadSettings] → [IPluginSaveData.LoadData] → Start
    /// → …运行… → [IPluginSettings.SaveSettings]（保存设置时，可多次）→ [IPluginSaveData.SaveData] → Shutdown。
    /// 插件可在运行中被用户关闭再启用：关闭时宿主取消其全部事件订阅并调用 Shutdown，Shutdown 必须释放自己创建的
    /// 线程、定时器、挂到主窗口的控件和事件、数据库连接；再次启用时用工厂创建全新实例，从 Initialize 重新开始。
    /// 方括号为可选接口，插件需要设置、存档或说明文档时再实现 <see cref="IPluginDocs"/>。
    /// </para>
    /// </summary>
    public interface IBiliPlugin
    {
        /// <summary>唯一标识，也是该插件存档目录名</summary>
        string Id { get; }
        /// <summary>显示名称</summary>
        string DisplayName { get; }
        /// <summary>加载顺序，小的先加载，同时决定设置页签顺序</summary>
        int Order { get; }
        /// <summary>必须加载的插件：不能被关闭，设置文件里的关闭记录也会被忽略。其他插件依赖它提供的数据时使用</summary>
        bool Required => false;
        /// <summary>获得宿主上下文（此时设置与存档尚未读取）</summary>
        void Initialize(PluginContext context);
        /// <summary>设置与存档读取完毕，开始工作：订阅事件</summary>
        void Start();
        /// <summary>游戏结束时调用：释放资源（存档已由 SaveData 保存）</summary>
        void Shutdown();
    }

    /// <summary>
    /// 插件设置。设置保存在宿主统一的设置文件中，插件只通过文档读写自己的节
    /// </summary>
    public interface IPluginSettings
    {
        /// <summary>从已加载的设置文档读取本插件的设置</summary>
        void LoadSettings(LpsDocument config);
        /// <summary>把当前设置写入文档（文档已含现有设置，只更新本插件拥有的节）</summary>
        void SaveSettings(LpsDocument config);
        /// <summary>设置窗口中本插件提供的页签，每次打开设置窗口时调用</summary>
        IEnumerable<SettingsPage> CreateSettingsPages();
    }

    /// <summary>
    /// 插件存档（观众数据、数据库等）。每个插件独占 ModData/BiliVpet/Data/&lt;Id&gt; 目录
    /// </summary>
    public interface IPluginSaveData
    {
        /// <summary>读取存档。dataDirectory 是本插件独占的目录，已创建</summary>
        void LoadData(string dataDirectory);
        /// <summary>保存存档并释放文件占用（SQLite 等需要在此关闭连接）</summary>
        void SaveData();
    }

    /// <summary>
    /// 插件说明文档。设置窗口的“说明”页会列出文档标题和提供它的插件，点击后弹窗显示 Markdown 内容
    /// </summary>
    public interface IPluginDocs
    {
        IEnumerable<PluginDoc> CreateDocs();
    }

    /// <summary>一份说明文档</summary>
    public sealed class PluginDoc
    {
        public string Title { get; }
        /// <summary>Markdown 文件的完整路径，文中图片使用相对该文件的路径</summary>
        public string Path { get; }

        public PluginDoc(string title, string path)
        {
            Title = title;
            Path = path;
        }
    }

    /// <summary>设置窗口页签</summary>
    public sealed class SettingsPage
    {
        public string Header { get; }
        public UserControl Content { get; }
        /// <summary>点击设置窗口的“确定”或“应用”时调用，把界面内容写回插件</summary>
        public Action? Apply { get; }
        /// <summary>页面自己带滚动条(如表格)：设置窗口不再给它套外层滚动条，而是把可用高度全给它</summary>
        public bool Fill { get; init; }
        /// <summary>设置窗口关闭前调用，返回 false 取消关闭(如有未保存的修改，询问用户后返回)</summary>
        public Func<bool>? ConfirmClose { get; init; }

        public SettingsPage(string header, UserControl content, Action? apply = null)
        {
            Header = header;
            Content = content;
            Apply = apply;
        }
    }
}
