using System.Windows;
using System.Windows.Controls;
using LinePutScript;
using LinePutScript.Converter;
using VPET.Evian.BiliBiliTest.Core;
using VPET.Evian.BiliBiliTest.Core.Events;

namespace EchoSample
{
    /// <summary>
    /// 最小的外部插件示例：观众发出的弹幕以指令词开头时，桌宠把后面的内容复述一遍。
    /// 演示：继承 BiliPluginBase、订阅事件并拦截、发布朗读请求、读写设置、提供设置页、把指令词交给读弹幕插件屏蔽
    /// </summary>
    public sealed class EchoPlugin : BiliPluginBase, IPluginSettings
    {
        public override string Id => "EchoSample";          // 唯一，也是设置节名和日志目录名
        public override string DisplayName => "回声示例";
        public override int Order => 200;                    // 内置插件占用 10 到 90，外部插件建议 100 以上

        // 默认值写在字段上：首次运行没有设置文件时 LoadSettings 不会改它们
        private string keyword = "回声";
        private int maxLength = 30;

        protected override void OnStart()
        {
            // 优先级 20：排在好感度指令(0)、桌宠动作(5)、投喂(10)之后，弹幕计数(100)之前
            Events.Subscribe<DanmakuEvent>(OnDanmaku, priority: 20);
            Events.Subscribe<BlockedWordsResendEvent>(_ => PublishBlockedWords());
            PublishBlockedWords();
        }

        private void OnDanmaku(DanmakuEvent e)
        {
            if (e.Handled || !e.Message.StartsWith(keyword))
                return;
            e.Handled = true;
            var text = e.Message[keyword.Length..].Trim();
            if (text.Length > maxLength)
                text = text[..maxLength];
            Log.AddInfoLog($"{e.UserName}({e.OpenId}) 说:{text}");
            // 说话只发布请求，由主程序的朗读队列排队朗读
            Events.Publish(new SpeakRequestEvent { Content = $"{e.UserName}说：{text}", Type = "Notice" });
        }

        /// <summary>指令词不应该被读弹幕插件当成普通弹幕朗读</summary>
        private void PublishBlockedWords()
        {
            Events.Publish(new BlockedWordsRequestEvent { Words = { keyword } });
        }

        public void LoadSettings(LpsDocument config)
        {
            var section = config[Id];
            if (section.Count == 0)
                return;
            // 键不存在时读到空字符串或 0，这时保持字段上的默认值
            string? savedKeyword = section[(gstr)"Keyword"];
            if (!string.IsNullOrEmpty(savedKeyword))
                keyword = savedKeyword;
            int savedLength = section[(gint)"MaxLength"];
            if (savedLength >= 1)
                maxLength = savedLength;
        }

        public void SaveSettings(LpsDocument config)
        {
            var section = config[Id];
            section[(gbol)"EnableLog"] = Log.Enable;     // 日志开关约定放在自己设置节的 EnableLog
            section[(gstr)"Keyword"] = keyword;
            section[(gint)"MaxLength"] = maxLength;
        }

        public IEnumerable<SettingsPage> CreateSettingsPages()
        {
            var keywordBox = new TextBox { Text = keyword, Margin = new Thickness(10) };
            var lengthBox = new TextBox { Text = maxLength.ToString(), Margin = new Thickness(10) };
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = "指令词:", Margin = new Thickness(10, 10, 10, 0) });
            panel.Children.Add(keywordBox);
            panel.Children.Add(new TextBlock { Text = "最多复述字数:", Margin = new Thickness(10, 10, 10, 0) });
            panel.Children.Add(lengthBox);
            var page = new UserControl { Content = panel };
            // 点设置窗口的“确定”“应用”时才把界面上的值写回插件，之后宿主会调用 SaveSettings
            yield return new SettingsPage("回声示例", page, () =>
            {
                if (keywordBox.Text.Length > 0)
                    keyword = keywordBox.Text;
                if (int.TryParse(lengthBox.Text, out var length) && length >= 1)
                    maxLength = length;
                PublishBlockedWords();
            });
        }
    }
}
