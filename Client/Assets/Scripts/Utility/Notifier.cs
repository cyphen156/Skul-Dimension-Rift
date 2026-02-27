using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Assets.Scripts.Utility
{
    public enum NotifyChannel
    {
        Native
    }

    public static class Notifier
    {
        private static readonly object sync = new object();
        private static readonly Dictionary<int, Action> callbacks = new Dictionary<int, Action>();
        private static int nextToken = 1;

        public static void NotifyWarning(string title, string message, NotifyChannel channel, Action onClosed)
        {
            DisplayMessageBox(title, message, channel, true, onClosed);
        }

        public static void NotifyError(string title, string message, NotifyChannel channel, Action onClosed)
        {
            DisplayMessageBox(title, message, channel, false, onClosed);
        }

        private static void DisplayMessageBox(
            string title,
            string message,
            NotifyChannel channel,
            bool isWarning,
            Action onClosed
        )
        {
            if (channel != NotifyChannel.Native)
            {
                return;
            }

            EnsureRunner();

            string t = string.IsNullOrEmpty(title) ? "Notification" : title;
            string m = message == null ? string.Empty : message;

            int token = Register(onClosed);

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

            uint flags = 0;
            flags |= MB_OK;
            flags |= MB_TOPMOST;
            flags |= MB_SETFOREGROUND;

            if (isWarning)
            {
                flags |= MB_ICONWARNING;
            }
            else
            {
                flags |= MB_ICONERROR;
            }

            MessageBoxW(IntPtr.Zero, m, t, flags);

            Dispatch(token);
            return;

#elif UNITY_ANDROID && !UNITY_EDITOR

            ShowAndroidAlert(t, m, token);
            return;

#elif (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR

            Notifier_ShowAlert_iOS(t, m, isWarning ? 1 : 0, token);
            return;

#elif (UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX) && !UNITY_EDITOR

            Notifier_ShowAlert_macOS(t, m, isWarning ? 1 : 0, token);
            return;

#elif UNITY_WEBGL && !UNITY_EDITOR

            Notifier_ShowAlert_WebGL(t, m);
            Dispatch(token);
            return;

#else

            Dispatch(token);
            return;

#endif
        }

        private static int Register(Action onClosed)
        {
            if (onClosed == null)
            {
                return 0;
            }

            lock (sync)
            {
                int token = nextToken;
                nextToken++;

                callbacks[token] = onClosed;
                return token;
            }
        }

        private static void Dispatch(int token)
        {
            if (token == 0)
            {
                return;
            }

            EnsureRunner();
            NotifierRunner.Enqueue(token);
        }

        private static void Invoke(int token)
        {
            Action cb = null;

            lock (sync)
            {
                if (callbacks.TryGetValue(token, out cb))
                {
                    callbacks.Remove(token);
                }
            }

            if (cb == null)
            {
                return;
            }

            cb();
        }

        private static void EnsureRunner()
        {
            NotifierRunner.Ensure();
        }

        private sealed class NotifierRunner : MonoBehaviour
        {
            private static NotifierRunner instance;

            private static readonly object queueLock = new object();
            private static readonly Queue<int> queue = new Queue<int>();

            public static void Ensure()
            {
                if (instance != null)
                {
                    return;
                }

                GameObject go = new GameObject("NotifierRunner");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<NotifierRunner>();
            }

            public static void Enqueue(int token)
            {
                lock (queueLock)
                {
                    queue.Enqueue(token);
                }
            }

            private void Update()
            {
                while (true)
                {
                    int token;

                    lock (queueLock)
                    {
                        if (queue.Count == 0)
                        {
                            break;
                        }

                        token = queue.Dequeue();
                    }

                    Notifier.Invoke(token);
                }
            }

            public void OnNativeDialogClosed(string tokenText)
            {
                int token;

                if (int.TryParse(tokenText, out token) == false)
                {
                    return;
                }

                Enqueue(token);
            }
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int MessageBoxW(
            IntPtr hWnd,
            string text,
            string caption,
            uint type
        );

        private const uint MB_OK = 0x00000000;
        private const uint MB_ICONWARNING = 0x00000030;
        private const uint MB_ICONERROR = 0x00000010;
        private const uint MB_TOPMOST = 0x00040000;
        private const uint MB_SETFOREGROUND = 0x00010000;

#endif

#if UNITY_ANDROID && !UNITY_EDITOR

        private static void ShowAndroidAlert(string title, string message, int token)
        {
            AndroidJavaClass unityPlayer;
            AndroidJavaObject activity;

            try
            {
                unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            }
            catch
            {
                Dispatch(token);
                return;
            }

            if (activity == null)
            {
                Dispatch(token);
                return;
            }

            AndroidAlertRunnable runnable = new AndroidAlertRunnable(title, message, token);
            activity.Call("runOnUiThread", new AndroidJavaRunnable(runnable.Run));
        }

        private sealed class AndroidAlertRunnable
        {
            private readonly string title;
            private readonly string message;
            private readonly int token;

            public AndroidAlertRunnable(string title, string message, int token)
            {
                this.title = title;
                this.message = message;
                this.token = token;
            }

            public void Run()
            {
                AndroidJavaClass unityPlayer;
                AndroidJavaObject activity;

                try
                {
                    unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                }
                catch
                {
                    Notifier.Dispatch(token);
                    return;
                }

                if (activity == null)
                {
                    Notifier.Dispatch(token);
                    return;
                }

                AndroidJavaObject builder;

                try
                {
                    builder = new AndroidJavaObject("android.app.AlertDialog$Builder", activity);
                }
                catch
                {
                    Notifier.Dispatch(token);
                    return;
                }

                builder.Call<AndroidJavaObject>("setTitle", title);
                builder.Call<AndroidJavaObject>("setMessage", message);

                AndroidJavaProxy okListener = new AndroidOkClickListener(token);
                builder.Call<AndroidJavaObject>("setPositiveButton", "OK", okListener);

                AndroidJavaObject dialog = builder.Call<AndroidJavaObject>("create");
                if (dialog == null)
                {
                    Notifier.Dispatch(token);
                    return;
                }

                dialog.Call("setCancelable", false);
                dialog.Call("show");
            }
        }

        private sealed class AndroidOkClickListener : AndroidJavaProxy
        {
            private readonly int token;

            public AndroidOkClickListener(int token)
                : base("android.content.DialogInterface$OnClickListener")
            {
                this.token = token;
            }

            public void onClick(AndroidJavaObject dialog, int which)
            {
                if (dialog != null)
                {
                    dialog.Call("dismiss");
                }

                Notifier.Dispatch(token);
            }
        }

#endif

#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR

        [DllImport("__Internal")]
        private static extern void Notifier_ShowAlert_iOS(string title, string message, int isWarning, int token);

#endif

#if (UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX) && !UNITY_EDITOR

        [DllImport("__Internal")]
        private static extern void Notifier_ShowAlert_macOS(string title, string message, int isWarning, int token);

#endif

#if UNITY_WEBGL && !UNITY_EDITOR

        [DllImport("__Internal")]
        private static extern void Notifier_ShowAlert_WebGL(string title, string message);

#endif
    }
}
