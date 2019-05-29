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
using Google.AR.Sceneform.Rendering;
using Google.AR.Sceneform.UX;
using Java.Interop;

using Android.Support.V7;
using Android.Util;
using Google.AR.Core;
using Google.AR.Sceneform;

using Microsoft.Azure.SpatialAnchors;

using Java.Text;
using Java.Util;
using Java.Util.Concurrent;
using Android.Support.V7.App;

namespace TestApp
{
    [Activity(Label = "AzureSpatialAnchorsActivity")]
    public class AzureSpatialAnchorsActivity : AppCompatActivity
    {
        private string anchorID;
        private readonly Dictionary<String, AnchorVisual> anchorVisuals = new Dictionary<string, AnchorVisual>();
        private bool basicDemo = true;
        private AzureSpatialAnchorsManager cloudAnchorManager;
        private DemoStep currentDemoStep = DemoStep.Start;
        private bool enoughDataForSaving;
        private static readonly int numberOfNearbyAnchors = 3;
        private readonly object progressLock = new object();
        private readonly object renderLock = new object();
        private int saveCount = 0;

        // Materials
        private static Material failedColor;
        private static Material foundColor;
        private static Material readyColor;
        private static Material savedColor;

        // UI Elements
        private ArFragment arFragment;
        private Button actionButton;
        private Button backButton;
        private TextView scanProgressText;
        private ArSceneView sceneView;
        private TextView statusText;

        [Export("exitDemoClicked")]
        public void exitDemoClicked(View v)
        {
            lock (renderLock) {
                destroySession();
                Finish();
            }
        }
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_anchors);
            basicDemo = Intent.GetBooleanExtra("BasicDemo", true);

            arFragment = (ArFragment)SupportFragmentManager.FindFragmentById(Resource.Id.ux_fragment);
            arFragment.TapArPlane += (s, e) => onTapArPlaneListener(e.HitResult, e.Plane, e.MotionEvent);

            sceneView = arFragment.ArSceneView;

            Scene scene = sceneView.Scene;
            scene.Update += (s, e) =>
            {
                if (cloudAnchorManager != null)
                {
                    // Pass frames to Spatial Anchors for processing.
                    cloudAnchorManager.Update(sceneView.ArFrame);
                };
            };
            backButton = FindViewById<Button>(Resource.Id.backButton);
            statusText = FindViewById<TextView>(Resource.Id.statusText);
            scanProgressText = FindViewById<TextView>(Resource.Id.scanProgressText);
            actionButton = FindViewById<Button>(Resource.Id.actionButton);
            actionButton.Click += (s, e) => advanceDemo();

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

            if (AzureSpatialAnchorsManager.SpatialAnchorsAccountId == "Set me" || AzureSpatialAnchorsManager.SpatialAnchorsAccountKey == "Set me")
            {
                Toast.MakeText(this, "\"Set SpatialAnchorsAccountId and SpatialAnchorsAccountKey in AzureSpatialAnchorsManager.java\"", ToastLength.Long)
                        .Show();

                Finish();
            }
        }
        protected override void OnStart()
        {
            base.OnStart();
            startDemo();
        }

        private async void advanceDemo() {
            switch (currentDemoStep) {
                case DemoStep.SaveCloudAnchor:
                    AnchorVisual visual = anchorVisuals[""];
                    if (visual == null) {
                        return;
                    }

                    if (!enoughDataForSaving) {
                        return;
                    }

                    // Hide the back button until we're done
                    RunOnUiThread(() => backButton.Visibility = ViewStates.Gone);

                    setupLocalCloudAnchor(visual);

                    try
                    {
                        await cloudAnchorManager.CreateAnchorAsync(visual.CloudAnchor);
                    }
                    catch (System.Exception ex)
                    {
                        anchorSaveFailed(ex.Message);
                    }

                    lock (progressLock) {
                        RunOnUiThread(() => {
                            scanProgressText.Visibility = ViewStates.Gone;
                            scanProgressText.Text = "";
                            actionButton.Visibility = ViewStates.Invisible;
                            statusText.Text = "Saving cloud anchor...";
                        });
                        currentDemoStep = DemoStep.SavingCloudAnchor;
                    }

                    break;

                case DemoStep.CreateSessionForQuery:
                    cloudAnchorManager.Stop();
                    cloudAnchorManager.Reset();
                    clearVisuals();

                    RunOnUiThread(() => {
                        statusText.Text = "";
                        actionButton.Text = "Locate anchor";
                    });

                    currentDemoStep = DemoStep.LookForAnchor;

                    break;

                case DemoStep.LookForAnchor:
                    // We need to restart the session to find anchors we created.
                    startNewSession();

                    AnchorLocateCriteria criteria = new AnchorLocateCriteria();
                    criteria.SetIdentifiers(new string[] { anchorID });

                    // Cannot run more than one watcher concurrently
                    stopWatcher();

                    cloudAnchorManager.StartLocating(criteria);

                    RunOnUiThread(() =>
                    {
                        actionButton.Visibility = ViewStates.Invisible;
                        statusText.Text = "Look for anchor";
                    });

                    break;

                case DemoStep.LookForNearbyAnchors:
                    if (anchorVisuals.Count == 0 || !anchorVisuals.ContainsKey(anchorID)) {
                        RunOnUiThread(() => statusText.Text = "Cannot locate nearby. Previous anchor not yet located.");

                        break;
                    }

                    AnchorLocateCriteria nearbyLocateCriteria = new AnchorLocateCriteria();
                    NearAnchorCriteria nearAnchorCriteria = new NearAnchorCriteria();
                    nearAnchorCriteria.DistanceInMeters = 5;
                    nearAnchorCriteria.SourceAnchor = anchorVisuals[anchorID].CloudAnchor;
                    nearbyLocateCriteria.NearAnchor = nearAnchorCriteria;
                    // Cannot run more than one watcher concurrently
                    stopWatcher();
                    cloudAnchorManager.StartLocating(nearbyLocateCriteria);
                    RunOnUiThread(() => {
                        actionButton.Visibility = ViewStates.Invisible;
                        statusText.Text = "Locating...";
                    });

                    break;

                case DemoStep.End:
                    foreach (AnchorVisual toDeleteVisual in anchorVisuals.Values) {
                        var _ = cloudAnchorManager.DeleteAnchorAsync(toDeleteVisual.CloudAnchor);
                    }

                    destroySession();

                    RunOnUiThread(() => {
                        actionButton.Text = "Restart";
                        statusText.Text = "";
                        backButton.Visibility = ViewStates.Visible;
                    });

                    currentDemoStep = DemoStep.Restart;

                    break;

                case DemoStep.Restart:
                    startDemo();
                    break;
            }
        }

        private void anchorSaveSuccess(CloudSpatialAnchor result) {
            saveCount++;

            anchorID = result.Identifier;
            Log.Debug("ASADemo:", "created anchor: " + anchorID);

            AnchorVisual visual = anchorVisuals[""];
            visual.SetColor(savedColor);
            anchorVisuals[anchorID] = visual;
            anchorVisuals.Remove("");

            if (basicDemo || saveCount == numberOfNearbyAnchors) {
                RunOnUiThread(() => {
                    statusText.Text = "";
                    actionButton.Visibility = ViewStates.Visible;
                });

                currentDemoStep = DemoStep.CreateSessionForQuery;
                advanceDemo();
            }
            else {
                // Need to create more anchors for nearby demo
                RunOnUiThread(() => {
                    statusText.Text = "Tap a surface to create next anchor";
                    actionButton.Visibility = ViewStates.Invisible;
                });

                currentDemoStep = DemoStep.CreateLocalAnchor;
            }
        }

        private void anchorSaveFailed(String message) {
            RunOnUiThread(() => statusText.Text = message);
            AnchorVisual visual = anchorVisuals[""];
            visual.SetColor(failedColor);
        }

        private void clearVisuals() {
            foreach (var visual in anchorVisuals.Values) {
                visual.Destroy();
            }

            anchorVisuals.Clear();
        }

        private Anchor createAnchor(HitResult hitResult) {
            AnchorVisual visual = new AnchorVisual(hitResult.CreateAnchor());
            visual.SetColor(readyColor);
            visual.Render(arFragment);
            anchorVisuals[""] = visual;

            RunOnUiThread(() =>
            {
                scanProgressText.Visibility = ViewStates.Visible;
                if (enoughDataForSaving)
                {
                    statusText.Text = "Ready to save";
                    actionButton.Text = "Save cloud anchor";
                    actionButton.Visibility = ViewStates.Visible;
                }
                else
                {
                    statusText.Text = "Move around the anchor";
                }
            });

            currentDemoStep = DemoStep.SaveCloudAnchor;

            return visual.LocalAnchor;
        }

        private void destroySession() {
            if (cloudAnchorManager != null) {
                cloudAnchorManager.Stop();
                cloudAnchorManager = null;
            }

            clearVisuals();
        }

        private void onAnchorLocated(object sender, AnchorLocatedEventArgs args) {
            LocateAnchorStatus status = args.P0.Status;

            RunOnUiThread(() =>
            {
                if (status == LocateAnchorStatus.Located)
                    renderLocatedAnchor(args.P0.Anchor);
                else if (status == LocateAnchorStatus.NotLocatedAnchorDoesNotExist)
                    statusText.Text = "Anchor does not exist";
            });
        }

        private void onLocateAnchorsCompleted(object sender, LocateAnchorsCompletedEventArgs args) {
            RunOnUiThread(() => statusText.Text = "Anchor located!");

            if (!basicDemo && currentDemoStep == DemoStep.LookForAnchor) {
                RunOnUiThread(() =>
                {
                    actionButton.Visibility = ViewStates.Visible;
                    actionButton.Text = "Look for anchors nearby";
                });
                currentDemoStep = DemoStep.LookForNearbyAnchors;
            } else {
                stopWatcher();
                RunOnUiThread(() => {
                    actionButton.Visibility = ViewStates.Visible;
                    actionButton.Text = "Cleanup anchors";
                });
                currentDemoStep = DemoStep.End;
            }
        }

        private void onSessionUpdated(object sender, SessionUpdatedEventArgs args) {
            float progress = args.P0.Status.RecommendedForCreateProgress;
            enoughDataForSaving = progress >= 1.0;
            lock (progressLock) {
                if (currentDemoStep == DemoStep.SaveCloudAnchor) {
                    DecimalFormat decimalFormat = new DecimalFormat("00");
                    RunOnUiThread(() => {
                        string progressMessage = $"Scan progress is {progress.ToString("P0")}";
                        scanProgressText.Text = progressMessage;
                    });

                    if (enoughDataForSaving && actionButton.Visibility != ViewStates.Visible) {
                        // Enable the save button
                        RunOnUiThread(() => {
                            statusText.Text = "Ready to save";
                            actionButton.Text = "Save cloud anchor";
                            actionButton.Visibility = ViewStates.Visible;
                        });
                        currentDemoStep = DemoStep.SaveCloudAnchor;
                    }
                }
            }
        }

        private void onTapArPlaneListener(HitResult hitResult, Plane plane, MotionEvent motionEvent) {
            if (currentDemoStep == DemoStep.CreateLocalAnchor) {
                createAnchor(hitResult);
            }
        }

        private void renderLocatedAnchor(CloudSpatialAnchor anchor) {
            AnchorVisual foundVisual = new AnchorVisual(anchor.LocalAnchor);
            foundVisual.SetCloudAnchor(anchor);
            foundVisual.AnchorNode.SetParent(arFragment.ArSceneView.Scene);
            String cloudAnchorIdentifier = foundVisual.CloudAnchor.Identifier;
            foundVisual.SetColor(foundColor);
            foundVisual.Render(arFragment);
            anchorVisuals[cloudAnchorIdentifier] = foundVisual;
        }

        private void setupLocalCloudAnchor(AnchorVisual visual) {
            CloudSpatialAnchor cloudAnchor = new CloudSpatialAnchor();
            cloudAnchor.LocalAnchor = visual.LocalAnchor;
            visual.SetCloudAnchor(cloudAnchor);

            // In this sample app we delete the cloud anchor explicitly, but you can also set it to expire automatically
            Date now = new Date();
            Calendar cal = Calendar.Instance;
            cal.Time = now;
            cal.Add(CalendarField.Date, 7);
            Date oneWeekFromNow = cal.Time;
            cloudAnchor.Expiration = oneWeekFromNow;
        }

        private void startDemo()
        {
            saveCount = 0;
            startNewSession();
            RunOnUiThread(() =>
            {
                scanProgressText.Visibility = ViewStates.Gone;
                statusText.Text = "Tap a surface to create an anchor";
                actionButton.Visibility = ViewStates.Visible;
            });
            currentDemoStep = DemoStep.CreateLocalAnchor;
        }

        private void startNewSession() {
            destroySession();

            cloudAnchorManager = new AzureSpatialAnchorsManager(sceneView.Session);
            cloudAnchorManager.AnchorLocated += onAnchorLocated;
            cloudAnchorManager.LocateAnchorsCompleted += onLocateAnchorsCompleted;
            cloudAnchorManager.SessionUpdated += onSessionUpdated;
            cloudAnchorManager.Start();
        }

        private void stopWatcher() {
            if (cloudAnchorManager != null) {
                cloudAnchorManager.StopLocating();
            }
        }

        enum DemoStep {
            Start,                          ///< the start of the demo
            CreateLocalAnchor,      ///< the session will create a local anchor
            SaveCloudAnchor,        ///< the session will save the cloud anchor
            SavingCloudAnchor,      ///< the session is in the process of saving the cloud anchor
            CreateSessionForQuery,  ///< a session will be created to query for an anchor
            LookForAnchor,          ///< the session will run the query
            LookForNearbyAnchors,   ///< the session will run a query for nearby anchors
            End,                            ///< the end of the demo
            Restart,                        ///< waiting to restart
        }
    }
}