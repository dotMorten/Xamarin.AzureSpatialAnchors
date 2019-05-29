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
using Java.Interop;

namespace TestApp
{
    [Activity(Label = "AzureSpatialAnchorsActivity", MainLauncher = true, Icon = "@mipmap/ic_launcher", Theme = "@style/Theme.AppCompat.NoActionBar",
        ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize,
        ScreenOrientation = Android.Content.PM.ScreenOrientation.Locked)]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_main);
        }

        [Export("onHelloARDemoClick")]
        public void onHelloARDemoClick(View v)
        {
            Intent intent = new Intent(this, typeof(HelloARActivity));
            StartActivity(intent);
        }

        [Export("onBasicDemoClick")]
        public void onBasicDemoClick(View v)
        {
            Intent intent = new Intent(this, typeof(AzureSpatialAnchorsActivity));
            intent.PutExtra("BasicDemo", true);
            StartActivity(intent);
        }

        [Export("onNearbyDemoClick")]
        public void onNearbyDemoClick(View v)
        {
            Intent intent = new Intent(this, typeof(AzureSpatialAnchorsActivity));
            intent.PutExtra("BasicDemo", false);
            StartActivity(intent);
        }

        [Export("onSharedDemoClick")]
        public void onSharedDemoClick(View v)
        {
            //Intent intent = new Intent(this, typeof(SharedActivity));
            //StartActivity(intent);
        }
    }
}