using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Rendering;

namespace UnityEngine.Perception.GroundTruth
{
    [Serializable]
    public sealed class Keypoint3DLabeler : CameraLabeler
    {
        public override string description => "Keypoint3D labeler";
        public override string labelerId => annotationId;
        protected override bool supportsVisualization => false;

        public string annotationId = "keypoint3d_labeler";

        /// <summary>
        /// The active keypoint template. Required to annotate keypoint data.
        /// </summary>
        public KeypointTemplate activeTemplate;

        private AnnotationDefinition keypointPositionDef;

        class Keypoint3DPositionDef : AnnotationDefinition
        {
            public Keypoint3DPositionDef(string id)
                : base(id)
            {
            }

            public override string modelType => "keypoint3DDef";
            public override string description => "Tracking 3D positions of keypoints";
        }

        [Serializable]
        class Keypoint3DPosition : Annotation
        {
            //public string ToolName;
            public Keypoints3D[] keypoints3DList;

            public Keypoint3DPosition(
                AnnotationDefinition definition,
                string sensorId,
                Keypoints3D[] keypoints
            )
                : base(definition, sensorId)
            {
                keypoints3DList = keypoints;
            }

            [Serializable]
            public struct Keypoints3D : IMessageProducer
            {
                public string toolName;
                public Dictionary<string, Vector3> DataPoints;
                public KeypointTemplate Template;

                public void ToMessage(IMessageBuilder builder)
                {
                    builder.AddString("Instrument", toolName);

                    foreach (var kp in Template.keypoints)
                    {
                        builder.AddFloatArray(kp.label, MessageBuilderUtils.ToFloatVector(DataPoints[kp.label]));
                    }
                }
            }

            public override void ToMessage(IMessageBuilder builder)
            {
                base.ToMessage(builder);
                foreach (var kp in keypoints3DList)
                {
                    kp.ToMessage(builder);
                }
            }

            public override bool IsValid() => true;
        }

        protected override void Setup()
        {
            keypointPositionDef = new Keypoint3DPositionDef("keypoint3DDef");
            DatasetCapture.RegisterAnnotationDefinition(keypointPositionDef);
        }

        protected override void OnBeginRendering(ScriptableRenderContext scriptableRenderContext)
        {
            //Report using the PerceptionCamera's SensorHandle if scheduled this frame
            if (perceptionCamera.SensorHandle.ShouldCaptureThisFrame)
            {
                List<Keypoint3DPosition.Keypoints3D> keypoints3D =
                    new List<Keypoint3DPosition.Keypoints3D>();

                foreach (var label in LabelManager.singleton.registeredLabels)
                    ProcessLabel(label, activeTemplate, ref keypoints3D);

                var annotation = new Keypoint3DPosition(keypointPositionDef, sensorHandle.Id, keypoints3D.ToArray());
                sensorHandle.ReportAnnotation(keypointPositionDef, annotation);
            }
        }

        /// <summary>
        /// Process each keypoint and extract 3D position data.
        /// </summary>
        void ProcessLabel(Labeling labeledEntity, KeypointTemplate keypointTemplate, ref List<Keypoint3DPosition.Keypoints3D> keypointsMessages)
        {
            var entityGameObject = labeledEntity.gameObject;

            string toolName = entityGameObject.name;
            bool mayReport = false;

            var dataPoints = new Dictionary<string, Vector3>();

            foreach (var kp in keypointTemplate.keypoints)
            {
                dataPoints.Add(kp.label, Vector3.zero);
            }

            foreach (var joint in entityGameObject.transform.GetComponentsInChildren<JointLabel>())
            {
                foreach (var label in joint.labels) // Usually this is just a list of one label
                {
                    if (dataPoints.ContainsKey(label))
                    {
                        dataPoints[label] = joint.transform.position;
                        mayReport = true;
                    }
                }
            }

            if (!mayReport) return;
            Keypoint3DPosition.Keypoints3D keypoints = new Keypoint3DPosition.Keypoints3D { toolName = toolName.Replace("(Clone)", ""), DataPoints = dataPoints };
            keypointsMessages.Add(keypoints);
        }
    }
}

