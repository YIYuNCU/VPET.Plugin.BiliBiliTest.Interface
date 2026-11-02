using System.Collections.Concurrent;

namespace VPET.Evian.BiliBiliTest.Core
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
    }

    /// <summary>
    /// 日志。写入可来自任意线程，保存时一次性写出到 Log/&lt;Title&gt; 目录。
    /// 已保存的最近几百行留在内存里，供设置窗口实时查看
    /// </summary>
    public sealed class Log
    {
        /// <summary>保存后内存里继续保留的行数</summary>
        private const int KeepSaved = 300;

        /// <summary>同名日志只保留最新创建的那个（插件重新启用时会新建）</summary>
        private static readonly ConcurrentDictionary<string, Log> all = new();

        private readonly object sync = new();
        private readonly List<LogContent> entries = new();
        private readonly ConcurrentDictionary<string, byte> onceKeys = new();
        /// <summary>entries[0..saved) 已经写进文件</summary>
        private int saved;
        /// <summary>entries[0] 的序号。序号从 0 开始一直增长，供查看窗口接着读</summary>
        private long firstSeq;

        /// <summary>是否允许日志记录</summary>
        public bool Enable { get; set; }
        /// <summary>低于这个等级的日志不记录（所有日志共用）</summary>
        public static LogLevel MinLevel { get; set; }
        /// <summary>日志子目录名</summary>
        public string Title { get; }
        /// <summary>给人看的名字</summary>
        public string Name { get; }
        /// <summary>日志存储目录</summary>
        public string Locate => Path.Combine(AppPaths.LogRoot, Title);

        public Log(string title, bool enable, string? name = null)
        {
            Title = title;
            Name = name ?? title;
            Enable = enable;
            if (all.TryGetValue(title, out var old))
                old.SaveLog();
            all[title] = this;
        }

        /// <summary>现有的日志，按目录名排序</summary>
        public static List<Log> GetAll()
        {
            var list = new List<Log>(all.Values);
            list.Sort((a, b) => string.CompareOrdinal(a.Title, b.Title));
            return list;
        }

        public static Log? Find(string title)
        {
            return all.TryGetValue(title, out var log) ? log : null;
        }

        /// <summary>把所有日志对象中尚未写出的内容立即保存</summary>
        public static void SaveAll()
        {
            foreach (var log in all.Values)
                log.SaveLog();
        }

        public void AddInfoLog(string content) => Add(LogLevel.Info, content);
        public void AddDebugLog(string content) => Add(LogLevel.Debug, content);
        public void AddWarningLog(string content) => Add(LogLevel.Warning, content);
        public void AddErrorLog(string content) => Add(LogLevel.Error, content);
        /// <summary>仅首次添加（保存日志后重新计算）</summary>
        public void AddInfoLog_Once(string content) => AddOnce(LogLevel.Info, content);
        public void AddDebugLog_Once(string content) => AddOnce(LogLevel.Debug, content);
        public void AddWarningLog_Once(string content) => AddOnce(LogLevel.Warning, content);
        public void AddErrorLog_Once(string content) => AddOnce(LogLevel.Error, content);

        public void AddSpaceLine()
        {
            if (Enable)
                Append(new LogContent(LogLevel.Debug, null));
        }

        private void Add(LogLevel level, string content)
        {
            if (Enable && level >= MinLevel)
                Append(new LogContent(level, content));
        }

        private void AddOnce(LogLevel level, string content)
        {
            if (Enable && level >= MinLevel && onceKeys.TryAdd(level + content, 0))
                Append(new LogContent(level, content));
        }

        private void Append(LogContent entry)
        {
            lock (sync)
                entries.Add(entry);
        }

        /// <summary>
        /// 把序号 from 之后的新日志行加进 lines，返回下一次该传的序号。
        /// from 早于内存里保留的最早一行时，从最早的开始
        /// </summary>
        public long ReadFrom(long from, List<string> lines)
        {
            lock (sync)
            {
                for (long seq = Math.Max(from, firstSeq); seq < firstSeq + entries.Count; seq++)
                    lines.Add(entries[(int)(seq - firstSeq)].ToString());
                return firstSeq + entries.Count;
            }
        }

        /// <summary>保存日志</summary>
        public void SaveLog()
        {
            var lines = new List<string>();
            int count;
            lock (sync)
            {
                if (saved == entries.Count)
                    return;
                for (int i = saved; i < entries.Count; i++)
                    lines.Add(entries[i].ToString());
                count = entries.Count;
            }
            try
            {
                Directory.CreateDirectory(Locate);
                File.WriteAllLines(Path.Combine(Locate, "Log" + DateTime.Now.ToFileTime() + ".log"), lines);
            }
            catch (Exception ex)
            {
                Ui.ShowMessage(ex.Message);
                return;
            }
            onceKeys.Clear();
            lock (sync)
            {
                saved = count;
                int drop = saved - KeepSaved;
                if (drop > 0)
                {
                    entries.RemoveRange(0, drop);
                    saved -= drop;
                    firstSeq += drop;
                }
            }
        }
    }

    internal sealed class LogContent
    {
        private readonly DateTime time = DateTime.Now;
        private readonly LogLevel level;
        private readonly string? content;

        /// <param name="level">日志等级</param>
        /// <param name="content">null 表示空行</param>
        public LogContent(LogLevel level, string? content)
        {
            this.level = level;
            this.content = content;
        }

        public override string ToString()
        {
            return content == null ? "" : time + " " + level + ": " + content;
        }
    }
}
