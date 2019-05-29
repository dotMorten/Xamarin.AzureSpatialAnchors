using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Util.Concurrent;
using Microsoft.Azure.SpatialAnchors;

namespace TestApp
{
    class AzureSpatialAnchorsManager
    {
        // Set this string to the account ID provided for the Azure Spatial Service resource.
        public readonly string SpatialAnchorsAccountId = "Set me";

        // Set this string to the account key provided for the Azure Spatial Service resource.
        public readonly string SpatialAnchorsAccountKey = "Set me";

        private readonly IExecutorService executorService = Executors.NewFixedThreadPool(2);
        private readonly CloudSpatialAnchorSession spatialAnchorsSession;

        public AzureSpatialAnchorsManager(Google.AR.Core.Session arCoreSession)
        {
            spatialAnchorsSession = new CloudSpatialAnchorSession();
            spatialAnchorsSession.Configuration.AccountId = SpatialAnchorsAccountId;
            spatialAnchorsSession.Configuration.AccountKey = SpatialAnchorsAccountKey;
            spatialAnchorsSession.Session = arCoreSession;
            spatialAnchorsSession.LogLevel = SessionLogLevel.All;

            spatialAnchorsSession.LogDebug += SpatialAnchorsSession_LogDebug;
            spatialAnchorsSession.Error += SpatialAnchorsSession_Error;
        }

        public Task CreateAnchorAsync(CloudSpatialAnchor anchor)
        {
            return spatialAnchorsSession.CreateAnchorAsync(anchor);
        }

        public Task DeleteAnchorAsync(CloudSpatialAnchor anchor) => spatialAnchorsSession.DeleteAnchorAsync(anchor);


        public bool IsRunning { get; private set; }

        public void Reset()
        {
            StopLocating();
            spatialAnchorsSession.Reset();
        }

        public void Start()
        {
            spatialAnchorsSession.Start();
            IsRunning = true;
        }

        public CloudSpatialAnchorWatcher StartLocating(AnchorLocateCriteria criteria)
        {
            // Only 1 active watcher at a time is permitted.
            StopLocating();

            return spatialAnchorsSession.CreateWatcher(criteria);
        }

        public void StopLocating()
        {
            var watchers = spatialAnchorsSession.ActiveWatchers;

            if (watchers.Count == 0)
            {
                return;
            }

            // Only 1 watcher is at a time is currently permitted.
            CloudSpatialAnchorWatcher watcher = watchers[0];

            watcher.Stop();
        }

        public void Stop()
        {
            spatialAnchorsSession.Stop();
            StopLocating();
            IsRunning = false;
        }

        public void Update(Google.AR.Core.Frame frame)
        {
            spatialAnchorsSession.ProcessFrame(frame);
        }

        private void SpatialAnchorsSession_Error(object sender, SessionErrorEventArgs e)
        {
            Log.Debug("ASAError: ", e.P0.ErrorMessage); 
        }

        private void SpatialAnchorsSession_LogDebug(object sender, LogDebugEventArgs e)
        {
            Log.Debug("ASACloud: ", e.P0.Message);
        }
    }
}