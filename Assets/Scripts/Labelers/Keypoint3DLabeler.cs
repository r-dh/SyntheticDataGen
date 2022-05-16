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

        AnnotationDefinition keypointPositionDef;

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
            public KeypointsMessage[] keypointsMessage;
            public Keypoint3DPosition(
                AnnotationDefinition definition,
                string sensorId,
                KeypointsMessage[] messages
            )
                : base(definition, sensorId)
            {
                keypointsMessage = messages;
            }

            [Serializable]
            public struct KeypointsMessage : IMessageProducer
            {
                public string toolName;
                public Dictionary<string, Vector3> DataPoints;

                public void ToMessage(IMessageBuilder builder)
                {
                    builder.AddString("Instrument", toolName);
                    builder.AddFloatArray("B_Hinge_01", MessageBuilderUtils.ToFloatVector(DataPoints["B_Hinge_01"]));

                    builder.AddFloatArray("B_Hinge_02", MessageBuilderUtils.ToFloatVector(DataPoints["B_Hinge_02"]));
                    builder.AddFloatArray("_Joint_1_axis1", MessageBuilderUtils.ToFloatVector(DataPoints["_Joint_1_axis1"]));
                    builder.AddFloatArray("_Joint_1_axis2", MessageBuilderUtils.ToFloatVector(DataPoints["_Joint_1_axis2"]));

                    builder.AddFloatArray("B_Driver_01", MessageBuilderUtils.ToFloatVector(DataPoints["B_Driver_01"]));
                    builder.AddFloatArray("_Joint_2_axis1", MessageBuilderUtils.ToFloatVector(DataPoints["_Joint_2_axis1"]));
                    builder.AddFloatArray("_Joint_2_axis2", MessageBuilderUtils.ToFloatVector(DataPoints["_Joint_2_axis2"]));

                    builder.AddFloatArray("B_Tip_01", MessageBuilderUtils.ToFloatVector(DataPoints["B_Tip_01"]));
                    builder.AddFloatArray("B_Tip_02", MessageBuilderUtils.ToFloatVector(DataPoints["B_Tip_02"]));
                }
            }

            public override void ToMessage(IMessageBuilder builder)
            {
                base.ToMessage(builder);
                //builder.AddString("Instrument", ToolName);
                foreach (var km in keypointsMessage)
                {
                    //var nested = builder.AddNestedMessage(ToolName);
                    km.ToMessage(builder);
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
                List<Keypoint3DPosition.KeypointsMessage> keypointsMessages =
                    new List<Keypoint3DPosition.KeypointsMessage>();

                foreach (var label in LabelManager.singleton.registeredLabels)
                    ProcessLabel(label, ref keypointsMessages);
                // TODO: check how many Labels there are and in which way these are processed
                // If the instruments are processed one by one, great
                // If not, keep track of this

                var annotation = new Keypoint3DPosition(keypointPositionDef, sensorHandle.Id, keypointsMessages.ToArray());
                sensorHandle.ReportAnnotation(keypointPositionDef, annotation);
            }
        }

        void ProcessLabel(Labeling labeledEntity, ref List<Keypoint3DPosition.KeypointsMessage> keypointsMessages)
        {
            var entityGameObject = labeledEntity.gameObject;

            string toolName = entityGameObject.name;
            bool mayReport = false;
            // If the instruments are processed one by one, great
            // If not, keep track of this (name of entity in extra var on top of dict?)
            var dataPoints = new Dictionary<string, Vector3>()
            {
                {"B_Hinge_01", Vector3.zero},
                {"B_Hinge_02", Vector3.zero},
                {"_Joint_1_axis1", Vector3.zero},
                {"_Joint_1_axis2", Vector3.zero},
                {"B_Driver_01", Vector3.zero},
                {"_Joint_2_axis1", Vector3.zero},
                {"_Joint_2_axis2", Vector3.zero},
                {"B_Tip_01", Vector3.zero},
                {"B_Tip_02", Vector3.zero}
            };

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
            Keypoint3DPosition.KeypointsMessage message = new Keypoint3DPosition.KeypointsMessage { toolName = toolName, DataPoints = dataPoints };
            keypointsMessages.Add(message);
        }
    }
}

