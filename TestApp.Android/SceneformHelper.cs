using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Content;
using Android.Views;
using Android.Widget;
using Google.AR.Core;
using Google.AR.Core.Exceptions;
using Google.AR.Sceneform;

namespace TestApp
{
    class SceneformHelper
    {
        // Check to see we have the necessary permissions for this app
        public static bool hasCameraPermission(Activity activity)
        {
            return ContextCompat.CheckSelfPermission(activity, Manifest.Permission.Camera) == Permission.Granted;
        }

        public static void setupSessionForSceneView(Context context, ArSceneView sceneView)
        {
            try
            {
                Session session = new Session(context);
                Config config = new Config(session);
                config.SetUpdateMode(Config.UpdateMode.LatestCameraImage);
                session.Configure(config);
                sceneView.SetupSession(session);
            }
            catch (UnavailableException e)
            {
                Android.Util.Log.Error("ASADemo: ", e.ToString());
            }
        }
    }
}