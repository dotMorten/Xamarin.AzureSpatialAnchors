using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace TestApp
{
    internal static class MainThreadContext
    {
        private static readonly Handler mainHandler = new Handler(Looper.MainLooper);
        private static readonly Looper mainLooper = Looper.MainLooper;

        public static void RunOnUiThread(Action runnable)
        {
            if (mainLooper.IsCurrentThread)
            {
                runnable();
            }
            else
            {
                mainHandler.Post(runnable);
            }
        }
    }
}