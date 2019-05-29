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
using Google.AR.Core;
using Google.AR.Sceneform;
using Google.AR.Sceneform.Math;
using Google.AR.Sceneform.Rendering;
using Google.AR.Sceneform.UX;
using Microsoft.Azure.SpatialAnchors;

namespace TestApp
{
    class AnchorVisual
    {
        private readonly AnchorNode anchorNode;
        private CloudSpatialAnchor cloudAnchor;
        private Material color;
        private Renderable nodeRenderable;

        public AnchorVisual(Anchor localAnchor)
        {
            anchorNode = new AnchorNode(localAnchor);
        }

        public AnchorNode AnchorNode => anchorNode;

        public CloudSpatialAnchor CloudAnchor => cloudAnchor;

        public Anchor LocalAnchor => anchorNode.Anchor;

        public void Render(ArFragment arFragment)
        {
            MainThreadContext.RunOnUiThread(() =>
            {
                //TODO: See https://github.com/xamarin/XamarinComponents/issues/584
                //nodeRenderable = ShapeFactory.MakeSphere(0.1f, new Vector3(0.0f, 0.15f, 0.0f), color);
                anchorNode.Renderable = nodeRenderable;
                anchorNode.SetParent(arFragment.ArSceneView.Scene);

                TransformableNode sphere = new TransformableNode(arFragment.TransformationSystem);
                sphere.SetParent(anchorNode);
                sphere.Renderable = nodeRenderable;
                sphere.Select();
            });
        }

        public void SetCloudAnchor(CloudSpatialAnchor cloudAnchor)
        {
            this.cloudAnchor = cloudAnchor;
        }

        public void SetColor(Material material)
        {
            color = material;

            MainThreadContext.RunOnUiThread(() =>
            {
                anchorNode.Renderable = null;
                //TODO: See https://github.com/xamarin/XamarinComponents/issues/584
                // nodeRenderable = ShapeFactory.MakeSphere(0.1f, new Vector3(0.0f, 0.15f, 0.0f), color);
                anchorNode.Renderable = nodeRenderable;
            });
        }

        public void Destroy()
        {
            MainThreadContext.RunOnUiThread(() =>
            {
                anchorNode.Renderable = null;
                anchorNode.SetParent(null);
            });
        }
    }
}