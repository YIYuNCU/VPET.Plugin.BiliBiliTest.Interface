using LinePutScript;

namespace VPET.Evian.BiliBiliTest.Core
{
    /// <summary>用户设置文件（Config_User.lps）的内存副本，启动时读取一次，保存时整体写回。文件不存在(首次运行)时从空文档开始，各插件的设置保持自己在代码里的默认值</summary>
    public sealed class ConfigStore
    {
        /// <summary>设置文件内容。宿主据此向各插件分发 LoadSettings / SaveSettings</summary>
        public LpsDocument Document { get; }

        public ConfigStore()
        {
            Document = new LpsDocument(File.Exists(AppPaths.UserConfig) ? File.ReadAllText(AppPaths.UserConfig) : "");
        }

        /// <summary>让 fill 更新文档后，写入设置文件</summary>
        public void Save(Action<LpsDocument> fill)
        {
            fill(Document);
            File.WriteAllText(AppPaths.UserConfig, Document.ToString());
        }
    }
}
