using System.Diagnostics;
using System.IO;

namespace CPS.Control.Util
{
    public enum LEVEL
    {
        NONE,
        DEBUG,
        INFO,
        WARN,
        ERROR,
    }

    public class Logger
    {
        private static readonly Lazy<Logger> lazy = new Lazy<Logger>(() => new Logger());
        public static Logger Instance { get { return lazy.Value; } }

        private readonly object _syncLock = new object();

        private static string LOG_DIR = "Log";

        private static string DATE_FORMAT = "yyyyMMdd";
        private static string FILE_EXT = "log";

        private static string SUFFIX = "control";

        private DateTime StartTime;

        private string BaseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LOG_DIR);
        private string FilePath;

        public void Initialize(string directory)
        {
            BaseDirectory = Path.Combine(directory, LOG_DIR);

            StartTime = DateTime.Now.AddDays(-1);

            Today(DateTime.Now);
        }
        public void CleanUp()
        {
            try {

                DirectoryInfo di = new DirectoryInfo(BaseDirectory);
                FileInfo[] fi = di.GetFiles($"*.{FILE_EXT}");

                // 12 개월 이전 로그 삭제
                DateTime from = DateTime.Now.AddMonths(-12);
                
                for (int i = 0; i < fi.Length; i++) {

                    var fileInfo = fi[i];

                    if (fileInfo.CreationTime.CompareTo(from) < 0) {
                        fileInfo.Delete();
                    }
                }
            }
            catch (Exception ex) {
                Debug.WriteLine(ex);
            }

        }
        public void Today(DateTime dateTime)
        {
            if (StartTime.Day != dateTime.Day) {
                StartTime = dateTime;

                // 디렉토리
                if (!Directory.Exists(BaseDirectory)) {
                    Directory.CreateDirectory(BaseDirectory);
                }
                // 파일경로
                FilePath = Path.Combine(BaseDirectory, $"{dateTime.ToString(DATE_FORMAT)}_{SUFFIX}.{FILE_EXT}");
            }
        }
        public void Write(LEVEL level, string message)
        {
            try {

                Debug.WriteLine($"[{level}] {message}");

                DateTime current = DateTime.Now;

                Today(current);

                lock (_syncLock) {

                    var text = $"{current.ToString("yyyy-MM-dd HH:mm:ss.fff")} [{level}] {message}";

                    using (var sw = new StreamWriter(File.Open(FilePath, FileMode.Append))) {
                        sw.WriteLine(text);
                    }
                }

            }
            catch (Exception ex) {
                Debug.WriteLine(ex);
            }

        }
    }
}
