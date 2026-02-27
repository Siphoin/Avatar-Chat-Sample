using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace AvatarChat.Main
{
    public class SignalBus
    {
        private readonly Dictionary<Type, object> _streams = new Dictionary<Type, object>();

        public SignalBus ()
        {
            LogSignalBus("Created signal bus.");
        }

        public IObservable<T> GetStream<T>() where T : class
        {
            if (!_streams.TryGetValue(typeof(T), out var stream))
            {
                stream = new Subject<T>();
                _streams[typeof(T)] = stream;
                LogSignalBus($"Created new stream for {typeof(T).Name}");
            }
            return (Subject<T>)stream;
        }

        public void Fire<T>(T signal) where T : class
        {
            if (_streams.TryGetValue(typeof(T), out var stream))
            {
                LogSignalBus($"Firing <color=orange>{typeof(T).Name}</color> signal");
                ((Subject<T>)stream).OnNext(signal);
            }
            else
            {
                LogSignalBus($"<color=orange>{typeof(T).Name}</color> signal fired but no listeners found", LogType.Warning);
            }
        }

        private void LogSignalBus(string message, LogType logType = LogType.Log)
        {
            string logMessage = $"[<color=orange>Signal Bus</color>] {message}";

            switch (logType)
            {
                case LogType.Log:
                    Debug.Log(logMessage);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(logMessage);
                    break;
                case LogType.Error:
                    Debug.LogError(logMessage);
                    break;
            }
        }
    }
}