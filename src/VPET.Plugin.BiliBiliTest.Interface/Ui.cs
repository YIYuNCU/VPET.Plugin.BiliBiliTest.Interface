using System.Windows.Threading;
using Panuon.WPF.UI;

namespace VPET.Evian.BiliBiliTest.Core
{
    /// <summary>
    /// UI 线程入口。只有控件相关操作才需要经过这里，数据计算与业务逻辑不要放进 Dispatcher
    /// </summary>
    public static class Ui
    {
        public static Dispatcher Dispatcher { get; private set; } = null!;

        /// <summary>
        /// 记录 UI 线程的 Dispatcher，插件构造函数中调用。VPet 在线程池线程上加载 mod，
        /// 构造函数并不在 UI 线程上运行，所以取应用程序的 Dispatcher，不能取当前线程的
        /// </summary>
        public static void Init()
        {
            Dispatcher = System.Windows.Application.Current.Dispatcher;
        }

        public static void Post(Action action)
        {
            Dispatcher.BeginInvoke(action);
        }

        public static void ShowMessage(string text)
        {
            Dispatcher.BeginInvoke(() => MessageBoxX.Show(text));
        }
    }
}
