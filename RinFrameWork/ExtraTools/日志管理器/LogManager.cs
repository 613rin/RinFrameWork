using UnityEngine;
using System;
using System.IO;
using System.Text;

public class LogManager : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("日志保留天数")]
    public int keepDays = 30;

    [Tooltip("是否记录普通Log (false可以只记录Warning和Error)")]
    public bool logNormalMessages = true;

    private string logDirectory;
    private string currentLogFilePath;
    private string lastDateString;
    
    private StreamWriter logWriter;
    private readonly object lockObj = new object(); // 线程锁

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        // 设置日志路径
#if UNITY_EDITOR
        logDirectory = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs");
#else
        logDirectory = Path.Combine(Application.persistentDataPath, "Logs");
#endif

        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        CleanUpOldLogs();
        InitializeLogWriter();

        Debug.Log($"[LogManager] 日志系统已启动。路径: {logDirectory}");
    }

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void OnDestroy()
    {
        CloseLogWriter();
    }

    void OnApplicationQuit()
    {
        CloseLogWriter();
    }

    /// <summary>
    /// 初始化日志写入器
    /// </summary>
    void InitializeLogWriter()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        lastDateString = today;
        currentLogFilePath = Path.Combine(logDirectory, $"run_log_{today}.txt");

        try
        {
            // 使用 FileStream + StreamWriter，保持文件流打开
            // autoFlush=true 确保每次写入立即刷新到磁盘
            FileStream fileStream = new FileStream(
                currentLogFilePath, 
                FileMode.Append, 
                FileAccess.Write, 
                FileShare.Read  // 允许其他程序读取
            );
            logWriter = new StreamWriter(fileStream, Encoding.UTF8) { AutoFlush = true };
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LogManager] 初始化日志文件失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 关闭日志写入器
    /// </summary>
    void CloseLogWriter()
    {
        lock (lockObj)
        {
            if (logWriter != null)
            {
                try
                {
                    logWriter.Flush();
                    logWriter.Close();
                    logWriter.Dispose();
                    logWriter = null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LogManager] 关闭日志文件失败: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// 处理日志
    /// </summary>
    void HandleLog(string logString, string stackTrace, LogType type)
    {
        // 可选：过滤普通日志
        if (!logNormalMessages && type == LogType.Log)
            return;

        // 检查跨天
        CheckAndHandleDateChange();

        // 格式化日志
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"{timestamp} - [{type}] - {logString}");

        if (type == LogType.Error || type == LogType.Exception)
        {
            sb.AppendLine($"Stack Trace:\n{stackTrace}");
        }

        // 线程安全写入
        lock (lockObj)
        {
            try
            {
                if (logWriter != null)
                {
                    logWriter.Write(sb.ToString());
                }
            }
            catch (Exception ex)
            {
                // 防止递归，用 Console
                Console.WriteLine($"[LogManager] 写入日志失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 检查并处理日期变化（跨天）
    /// </summary>
    void CheckAndHandleDateChange()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");

        if (lastDateString != today)
        {
            lock (lockObj)
            {
                // 双重检查（double-check）
                if (lastDateString != today)
                {
                    CloseLogWriter();
                    lastDateString = today;
                    InitializeLogWriter();
                }
            }
        }
    }

    /// <summary>
    /// 清理旧日志
    /// </summary>
    void CleanUpOldLogs()
    {
        try
        {
            DirectoryInfo info = new DirectoryInfo(logDirectory);
            FileInfo[] files = info.GetFiles("run_log_*.txt");
            DateTime cutoffDate = DateTime.Now.AddDays(-keepDays);

            foreach (FileInfo file in files)
            {
                if (file.LastWriteTime < cutoffDate)
                {
                    file.Delete();
                    Debug.Log($"[LogManager] 已删除过期日志: {file.Name}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LogManager] 清理旧日志失败: {ex.Message}");
        }
    }
}