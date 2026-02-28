using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace AvatarChat.DebugSystem
{
    public static class NetworkDebug
    {
        private static string _logFilePath;
        private static DateTime _currentLogDate;
        private static readonly ConcurrentQueue<string> _logQueue = new();
        private static CancellationTokenSource _fileWriteTokenSource;

        private static readonly Regex _stripTagsRegex = new Regex("<.*?>", RegexOptions.Compiled);
        private const long MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (Application.isBatchMode || Application.isEditor)
            {
                Application.logMessageReceived += HandleLog;

                UpdateLogFilePath();
                _fileWriteTokenSource = new CancellationTokenSource();

                WriteLogsToFileTask(_fileWriteTokenSource.Token).Forget();

                Debug.Log($"[NetworkDebug] Initialized. Log file: {_logFilePath}");
            }
        }

        private static void UpdateLogFilePath()
        {
            _currentLogDate = DateTime.Today;
            _logFilePath = Path.Combine(Application.persistentDataPath, $"server_{_currentLogDate:yyyy-MM-dd}.log");
        }

        private static void HandleLog(string logString, string stackTrace, LogType type)
        {
            string cleanLogString = StripColorTags(logString);
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string formattedMessage = $"[{timestamp}] [{type}] {cleanLogString}";

            if (Application.isBatchMode)
            {
                Console.WriteLine(formattedMessage);
                if (type == LogType.Exception || type == LogType.Error)
                {
                    Console.WriteLine(stackTrace);
                }
            }

            _logQueue.Enqueue(formattedMessage);
            if (type == LogType.Exception || type == LogType.Error)
            {
                _logQueue.Enqueue(stackTrace);
            }
        }

        private static string StripColorTags(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return _stripTagsRegex.Replace(input, string.Empty);
        }

        private static async UniTaskVoid WriteLogsToFileTask(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (!_logQueue.IsEmpty)
                {
                    if (DateTime.Today != _currentLogDate)
                    {
                        UpdateLogFilePath();
                    }

                    try
                    {
                        FileInfo fileInfo = new FileInfo(_logFilePath);
                        if (fileInfo.Exists && fileInfo.Length > MAX_FILE_SIZE_BYTES)
                        {
                            await CleanupFileKeepErrorsAsync();
                        }

                        using (var stream = new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
                        using (var writer = new StreamWriter(stream, Encoding.UTF8))
                        {
                            while (_logQueue.TryDequeue(out string message))
                            {
                                await writer.WriteLineAsync(message);
                            }
                            await writer.FlushAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[NetworkDebug] Write error: {ex.Message}");
                    }
                }

                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: token);
            }
        }

        private static async UniTask CleanupFileKeepErrorsAsync()
        {
            try
            {
                List<string> errorLogs = new List<string>();

                using (var stream = new FileStream(_logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (line.Contains("[Error]") || line.Contains("[Exception]") || line.Contains("[Assert]"))
                        {
                            errorLogs.Add(line);
                        }
                    }
                }

                await File.WriteAllLinesAsync(_logFilePath, errorLogs, Encoding.UTF8);
                Console.WriteLine($"[NetworkDebug] File size exceeded. Cleaned up everything except {errorLogs.Count} error lines.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NetworkDebug] Cleanup error: {ex.Message}");
            }
        }

        public static void Log(object message) => Debug.Log(message);
        public static void LogWarning(object message) => Debug.LogWarning(message);
        public static void LogError(object message) => Debug.LogError(message);

        public static void Shutdown()
        {
            _fileWriteTokenSource?.Cancel();
            _fileWriteTokenSource?.Dispose();
            Application.logMessageReceived -= HandleLog;
        }
    }
}