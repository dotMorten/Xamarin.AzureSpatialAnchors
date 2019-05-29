using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using Google.AR.Core;
using Google.AR.Sceneform;
using Google.AR.Sceneform.Rendering;
using Google.AR.Sceneform.UX;
using Java.Interop;
using Java.Util.Concurrent;
using Microsoft.Azure.SpatialAnchors;

namespace TestApp
{
    [Activity(Label = "SharedActivity")]
    public class SharedActivity : AppCompatActivity
    {
        enum DemoStep
        {
            Choosing, // Choosing to create or locate
            Creating, // Creating an anchor
            Saving,   // Saving an anchor to the cloud
            EnteringAnchorNumber, // Picking an anchor to find
            StepLocating  // Looking for an anchor
        }

        // Set this string to the URL created when publishing your Shared anchor service in the Sharing sample.
        private static readonly string SharingAnchorsServiceUrl = "";

        private string anchorId = "";
        private readonly Dictionary<string, AnchorVisual> anchorVisuals = new Dictionary<string, AnchorVisual>();
        private AzureSpatialAnchorsManager cloudAnchorManager;
        private DemoStep currentStep = DemoStep.Choosing;
        private string feedbackText;
        private readonly object renderLock = new Object();

        // Materials
        private static Material failedColor;
        private static Material savedColor;
        private static Material readyColor;
        private static Material foundColor;

        // UI Elements
        private EditText anchorNumInput;
        private ArFragment arFragment;
        private Button createButton;
        private TextView editTextInfo;
        private Button locateButton;
        private ArSceneView sceneView;
        private TextView textView;


        [Export("createButtonClicked")]
        public void createButtonClicked(View v)
        {
            textView.Text = "Scan your environment and place an anchor";
            destroySession();

            cloudAnchorManager = new AzureSpatialAnchorsManager(sceneView.Session);

            cloudAnchorManager.SessionUpdated += (s, args) =>
            {
                if (currentStep == DemoStep.Creating)
                {
                    float progress = args.P0.Status.RecommendedForCreateProgress;
                    if (progress >= 1.0)
                    {
                        AnchorVisual visual = anchorVisuals[""];
                        if (visual != null)
                        {
                            //Transition to saving...
                            transitionToSaving(visual);
                        }
                        else
                        {
                            feedbackText = "Tap somewhere to place an anchor.";
                        }
                    }
                    else
                    {
                        feedbackText = $"Progress is {progress.ToString("P2")}";
                    }
                }
            };

            cloudAnchorManager.Start();
            currentStep = DemoStep.Creating;
            enableCorrectUIControls();
        }

        [Export("exitDemoClicked")]
        public void exitDemoClicked(View v)
        {
            lock (renderLock)
            {
                destroySession();
                Finish();
            }
        }

        [Export("locateButtonClicked")]
        public async void locateButtonClicked(View v)
        {
            if (currentStep == DemoStep.Choosing)
            {
                currentStep = DemoStep.EnteringAnchorNumber;
                textView.Text = "Enter an anchor number and press locate";
                enableCorrectUIControls();
            }
            else
            {
                string inputVal = anchorNumInput.Text;
                if (!string.IsNullOrEmpty(inputVal))
                {

                    AnchorGetter anchorExchanger = new AnchorGetter(SharingAnchorsServiceUrl);
                    currentStep = DemoStep.StepLocating;
                    enableCorrectUIControls();
                    var anchor = await anchorExchanger.GetAnchor(inputVal);
                    anchorLookedUp(anchor);

                }
            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_shared);

            arFragment = (ArFragment)SupportFragmentManager.FindFragmentById(Resource.Id.ux_fragment);
            arFragment.TapArPlane += (s, e) => onTapArPlaneListener(e.HitResult, e.Plane, e.MotionEvent);

            sceneView = arFragment.ArSceneView;

            textView = FindViewById<TextView>(Resource.Id.textView);
            textView.Visibility = ViewStates.Visible;
            locateButton = FindViewById<Button>(Resource.Id.locateButton);
            createButton = FindViewById<Button>(Resource.Id.createButton);
            anchorNumInput = FindViewById<EditText>(Resource.Id.anchorNumText);
            editTextInfo = FindViewById<TextView>(Resource.Id.editTextInfo);
            enableCorrectUIControls();

            Scene scene = sceneView.Scene;
            scene.Update += (s, e) =>
            {
                if (cloudAnchorManager != null)
                {
                    // Pass frames to Spatial Anchors for processing.
                    cloudAnchorManager.Update(sceneView.ArFrame);
                }
            };
            MaterialFactory.MakeOpaqueWithColor(this, new Color(Android.Graphics.Color.Red)).GetAsync().ContinueWith(t => failedColor = (Material)t.Result);
            MaterialFactory.MakeOpaqueWithColor(this, new Color(Android.Graphics.Color.Green)).GetAsync().ContinueWith(t => savedColor = (Material)t.Result);
            MaterialFactory.MakeOpaqueWithColor(this, new Color(Android.Graphics.Color.Yellow)).GetAsync().ContinueWith(t => readyColor = foundColor = (Material)t.Result);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            destroySession();
        }

        protected override void OnResume()
        {
            base.OnResume();
            // ArFragment of Sceneform automatically requests the camera permission before creating the AR session,
            // so we don't need to request the camera permission explicitly
            if (!SceneformHelper.hasCameraPermission(this))
            {
                return;
            }

            if (sceneView != null && sceneView.Session == null)
            {
                SceneformHelper.setupSessionForSceneView(this, sceneView);
            }

            if ((AzureSpatialAnchorsManager.SpatialAnchorsAccountId == null || AzureSpatialAnchorsManager.SpatialAnchorsAccountId == "Set me")
                    || (AzureSpatialAnchorsManager.SpatialAnchorsAccountKey == null || AzureSpatialAnchorsManager.SpatialAnchorsAccountKey == "Set me"))
            {
                Toast.MakeText(this, "\"Set SpatialAnchorsAccountId and SpatialAnchorsAccountKey in AzureSpatialAnchorsManager.java\"", ToastLength.Long)
                        .Show();

                Finish();
                return;
            }

            if (string.IsNullOrEmpty(SharingAnchorsServiceUrl))
            {
                Toast.MakeText(this, "Set the SharingAnchorsServiceUrl in SharedActivity.java", ToastLength.Long)
                        .Show();

                Finish();
            }

            updateStatic();
        }


        private void anchorLookedUp(String anchorId)
        {
            Log.Debug("ASADemo", "anchor " + anchorId);
            this.anchorId = anchorId;
            destroySession();

            cloudAnchorManager = new AzureSpatialAnchorsManager(sceneView.Session);
            cloudAnchorManager.AnchorLocated += (s, e) =>
                    RunOnUiThread(() =>
                    {
                        CloudSpatialAnchor anchor = e.P0.Anchor;
                        if (e.P0.Status == LocateAnchorStatus.AlreadyTracked || e.P0.Status == LocateAnchorStatus.Located)
                        {
                            AnchorVisual foundVisual = new AnchorVisual(anchor.LocalAnchor);
                            foundVisual.SetCloudAnchor(anchor);
                            foundVisual.AnchorNode.SetParent(arFragment.ArSceneView.Scene);
                            String cloudAnchorIdentifier = foundVisual.CloudAnchor.Identifier;
                            foundVisual.SetColor(foundColor);
                            foundVisual.Render(arFragment);
                            anchorVisuals[cloudAnchorIdentifier] = foundVisual;
                        }
                    });


            cloudAnchorManager.LocateAnchorsCompleted += (s, e) =>
            {
                currentStep = DemoStep.Choosing;

                RunOnUiThread(() =>
                {
                    textView.Text = "Anchor located!";
                    enableCorrectUIControls();
                });
            };
            cloudAnchorManager.Start();
            AnchorLocateCriteria criteria = new AnchorLocateCriteria();
            criteria.SetIdentifiers(new string[] { anchorId });
            cloudAnchorManager.StartLocating(criteria);
        }
        void anchorPosted(String anchorNumber)
        {
            textView.Text = "Anchor Number: " + anchorNumber;
            currentStep = DemoStep.Choosing;
            cloudAnchorManager.Stop();
            cloudAnchorManager = null;
            clearVisuals();
            enableCorrectUIControls();
        }

        private Anchor createAnchor(HitResult hitResult)
        {

            AnchorVisual visual = new AnchorVisual(hitResult.CreateAnchor());
            visual.SetColor(readyColor);
            visual.Render(arFragment);
            anchorVisuals[""] = visual;

            return visual.LocalAnchor;
        }

        private void clearVisuals()
        {
            foreach (AnchorVisual visual in anchorVisuals.Values)
            {
                visual.Destroy();
            }

            anchorVisuals.Clear();
        }

        private void createAnchorExceptionCompletion(string message)
        {
            textView.Text = message;
            currentStep = DemoStep.Choosing;
            cloudAnchorManager.Stop();
            cloudAnchorManager = null;
            enableCorrectUIControls();
        }

        private void destroySession()
        {
            if (cloudAnchorManager != null)
            {
                cloudAnchorManager.Stop();
                cloudAnchorManager = null;
            }

            stopWatcher();

            clearVisuals();
        }

        private void enableCorrectUIControls()
        {
            switch (currentStep)
            {
                case DemoStep.Choosing:
                    textView.Visibility = ViewStates.Visible;
                    locateButton.Visibility = ViewStates.Visible;
                    createButton.Visibility = ViewStates.Visible;
                    anchorNumInput.Visibility = ViewStates.Gone;
                    editTextInfo.Visibility = ViewStates.Gone;
                    break;
                case DemoStep.Creating:
                    textView.Visibility = ViewStates.Visible;
                    locateButton.Visibility = ViewStates.Gone;
                    createButton.Visibility = ViewStates.Gone;
                    anchorNumInput.Visibility = ViewStates.Gone;
                    editTextInfo.Visibility = ViewStates.Gone;
                    break;
                case DemoStep.StepLocating:
                    textView.Visibility = ViewStates.Visible;
                    locateButton.Visibility = ViewStates.Gone;
                    createButton.Visibility = ViewStates.Gone;
                    anchorNumInput.Visibility = ViewStates.Gone;
                    editTextInfo.Visibility = ViewStates.Gone;
                    break;
                case DemoStep.Saving:
                    textView.Visibility = ViewStates.Visible;
                    locateButton.Visibility = ViewStates.Gone;
                    createButton.Visibility = ViewStates.Gone;
                    anchorNumInput.Visibility = ViewStates.Gone;
                    editTextInfo.Visibility = ViewStates.Gone;
                    break;
                case DemoStep.EnteringAnchorNumber:
                    textView.Visibility = ViewStates.Visible;
                    locateButton.Visibility = ViewStates.Visible;
                    createButton.Visibility = ViewStates.Gone;
                    anchorNumInput.Visibility = ViewStates.Visible;
                    editTextInfo.Visibility = ViewStates.Visible;
                    break;
            }
        }

        private void onTapArPlaneListener(HitResult hitResult, Plane plane, MotionEvent motionEvent)
        {
            if (currentStep == DemoStep.Creating)
            {
                AnchorVisual visual = anchorVisuals[""];
                if (visual == null)
                {
                    createAnchor(hitResult);
                }
            }
        }

        private void stopWatcher()
        {
            if (cloudAnchorManager != null)
            {
                cloudAnchorManager.StopLocating();
            }
        }

        private async void transitionToSaving(AnchorVisual visual)
        {
            Log.Debug("ASADemo:", "transition to saving");
            currentStep = DemoStep.Saving;
            enableCorrectUIControls();
            Log.Debug("ASADemo", "creating anchor");
            CloudSpatialAnchor cloudAnchor = new CloudSpatialAnchor();
            visual.SetCloudAnchor(cloudAnchor);
            cloudAnchor.LocalAnchor = visual.LocalAnchor;
            try
            {
                var anchor = await cloudAnchorManager.CreateAnchorAsync(cloudAnchor);
                String anchorId = anchor.Identifier;
                Log.Debug("ASADemo:", "created anchor: " + anchorId);
                visual.SetColor(savedColor);
                anchorVisuals[anchorId] = visual;
                anchorVisuals.Remove("");

                Log.Debug("ASADemo", "recording anchor with web service");
                AnchorPoster poster = new AnchorPoster(SharingAnchorsServiceUrl);
                Log.Debug("ASADemo", "anchorId: " + anchorId);
                var result = await poster.PostAnchorAsync(anchorId);
                anchorPosted(result);
            }
            catch (System.Exception ex)
            {
                string exceptionMessage = (ex is CloudSpatialException cse) ? cse.ErrorCode.ToString() : ex.Message;
                createAnchorExceptionCompletion(exceptionMessage);
                visual.SetColor(failedColor);
            }
        }

        private void updateStatic()
        {
            new Android.OS.Handler().PostDelayed(() =>
            {
                switch (currentStep)
                {
                    case DemoStep.Choosing:
                        break;
                    case DemoStep.Creating:
                        textView.Text = feedbackText;
                        break;
                    case DemoStep.StepLocating:
                        textView.Text = "searching for\n" + anchorId;
                        break;
                    case DemoStep.Saving:
                        textView.Text = "saving...";
                        break;
                    case DemoStep.EnteringAnchorNumber:

                        break;
                }

                updateStatic();
            }, 500);
        }
    }
}