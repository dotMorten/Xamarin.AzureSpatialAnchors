using System;
using Android.App;
using Android.Runtime;
using Microsoft;

namespace TestApp
{
    [Application]
    public class SampleApplication : Application
    {
        public SampleApplication(IntPtr handle, JniHandleOwnership transfer)
            : base(handle, transfer)
        {
            
        }
        public override void OnCreate()
        {
            base.OnCreate();

            // Use application's context to initialize CloudServices!
            // CloudServices.Initialize(this); //TODO: throws looking for AppCenter SDK
        }
    }
}