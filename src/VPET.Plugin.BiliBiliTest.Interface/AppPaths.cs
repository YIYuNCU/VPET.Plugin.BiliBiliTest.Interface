namespace VPET.Evian.BiliBiliTest.Core
{
    /// <summary>
    /// 路径约定。mod 目录只放随 mod 发布的只读资源；设置、存档、日志放在 VPet 提供的 MOD 存储目录（游戏目录下的 ModData/BiliVpet），
    /// 不会被 mod 更新或重装覆盖。插件自己要写的文件都放在这里面：
    /// <code>
    /// ModData/BiliVpet/Configs/Config_User.lps   用户设置
    /// ModData/BiliVpet/Data/&lt;插件Id&gt;/...         各插件的存档
    /// ModData/BiliVpet/Log/&lt;日志名&gt;/...           日志
    /// ModData/BiliVpet/Resources/...             运行时下载的资源（礼物图标）
    /// </code>
    /// </summary>
    public static class AppPaths
    {
        /// <summary>MOD 存储目录（ModData/BiliVpet）。由宿主在加载时设置，插件只读</summary>
        public static string UserRoot { get; set; } = "";
        public static string Configs => Path.Combine(UserRoot, "Configs");
        public static string UserConfig => Path.Combine(Configs, "Config_User.lps");
        public static string Data => Path.Combine(UserRoot, "Data");
        public static string LogRoot => Path.Combine(UserRoot, "Log");
        /// <summary>运行时下载的资源（礼物图标缓存），不放在 mod 目录，避免被 mod 更新清掉</summary>
        public static string Resources => Path.Combine(UserRoot, "Resources");
    }
}
