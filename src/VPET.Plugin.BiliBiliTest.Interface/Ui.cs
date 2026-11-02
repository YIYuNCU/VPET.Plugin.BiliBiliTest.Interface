using System.Windows.Threading;
using Panuon.WPF.UI;

namespace VPET.Evian.BiliBiliTest.Core
{
    /// <summary>
    /// UI 线程入口。只有控件相关操作才需要经过这里，数据计算与业务逻辑不要放进 Dispatcher
    /// </summary>
    public static class Ui
    {
        /// <summary>界面线程的 Dispatcher，宿主启动时已经设置好，插件直接使用</summary>
        public static Dispatcher Dispatcher { get; private set; } = null!;

        /// <summary>
        /// 由宿主在启动时调用，插件不要调用。VPet 在线程池线程上加载 mod，
        /// 加载代码并不在 UI 线程上运行，所以取应用程序的 Dispatcher，不能取当前线程的
        /// </summary>
        public static void Init()
        {
            Dispatcher = System.Windows.Application.Current.Dispatcher;
        }

        /// <summary>把操作投递到界面线程异步执行（不等待结果）</summary>
        public static void Post(Action action)
        {
            Dispatcher.BeginInvoke(action);
        }

        /// <summary>在界面线程弹出消息框（不等待用户关闭）</summary>
        public static void ShowMessage(string text)
        {
            Dispatcher.BeginInvoke(() => MessageBoxX.Show(text));
        }
    }
}
