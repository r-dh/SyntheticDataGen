using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Perception.GroundTruth.DataModel;

namespace UnityEngine.Perception.GroundTruth
{
    [Serializable]
    public sealed class GripAngleLabeler : CameraLabeler
    {
        public override string description => "Grip angle labeler";

        public override string labelerId => annotationId;

        protected override bool supportsVisualization => false;

        public string annotationId = "grip_angle_labeler";
        public float maxAngleDegrees = 5;

        private AnnotationDefinition gripAngleDef;

        class GripAngleDef : AnnotationDefinition
        {
            public GripAngleDef(string id)
                : base(id) { }

            public override string modelType => "gripAngleDef";
            public override string description => "The grip rotation";
        }

        [Serializable]
        class GripAngle : Annotation
        {
            public AngleInformation[] AngleInfo;
            public GripAngle(
                AnnotationDefinition definition,
                string sensorId,
                AngleInformation[] angleInfo
                )
                : base(definition, sensorId)
            {
                AngleInfo = angleInfo; ;
            }

            [Serializable]
            public struct AngleInformation : IMessageProducer
            {
                public string ToolName;
                public float AngleTop, AngleBot;
                public Quaternion QuaternionTop, QuaternionBot;
                public bool IsClosed;

                public void ToMessage(IMessageBuilder builder)
                {
                    builder.AddString("ToolName", ToolName);
                    builder.AddFloat("AngleTop", AngleTop);
                    builder.AddFloat("AngleBot", AngleBot);
                    builder.AddBool("isClosed", IsClosed);
                    builder.AddString("QuaternionTop", QuaternionTop.ToString());
                    builder.AddString("QuaternionBot", QuaternionBot.ToString());
                }
            }

            public override void ToMessage(IMessageBuilder builder)
            {
                base.ToMessage(builder);

                foreach (var ai in AngleInfo)
                {
                    ai.ToMessage(builder);
                }
            }

            public override bool IsValid() => true;
        }

        protected override void Setup()
        {
            gripAngleDef = new GripAngleDef("gripAngle");
            DatasetCapture.RegisterAnnotationDefinition(gripAngleDef);
        }

        protected override void OnBeginRendering(ScriptableRenderContext scriptableRenderContext)
        {
            if (perceptionCamera.SensorHandle.ShouldCaptureThisFrame)
            {
                List<GripAngle.AngleInformation> angleInformation = new List<GripAngle.AngleInformation>();
                
                foreach (var label in LabelManager.singleton.registeredLabels)
                    ProcessLabel(label, ref angleInformation);

                var annotation = new GripAngle(gripAngleDef, sensorHandle.Id, angleInformation.ToArray());
                sensorHandle.ReportAnnotation(gripAngleDef, annotation);
            }

        }

        void ProcessLabel(Labeling labeledEntity, ref List<GripAngle.AngleInformation> angleInformation)
        {
            var entityGameObject = labeledEntity.gameObject;
            float angleTop = 0f, angleBot = 0f;
            Quaternion qTop = Quaternion.identity, qBot = Quaternion.identity;
            bool mayReport = false;

            foreach (var joint in entityGameObject.transform.GetComponentsInChildren<JointLabel>())
            {
                foreach (var label in joint.labels) // Usually this is just a list of one label
                {
                    if (label == "B_Driver_01")     
                    {
                        angleTop = joint.transform.localEulerAngles.y;
                        qTop = joint.transform.localRotation;
                    }
                    else if (label == "B_Driver_02")
                    {
                        angleBot = joint.transform.localEulerAngles.y;
                        qBot = joint.transform.localRotation;
                        mayReport = true;
                    }
                }
            }

            if (!mayReport) return;

            bool isClosed = Math.Abs(angleTop - angleBot) < maxAngleDegrees;

            GripAngle.AngleInformation angleInfo = new GripAngle.AngleInformation
            {
                ToolName = entityGameObject.name.Replace("(Clone)", ""),
                AngleTop = angleTop,
                AngleBot = angleBot,
                IsClosed = isClosed,
                QuaternionTop = qTop,
                QuaternionBot = qBot
            };
            angleInformation.Add(angleInfo);
        }
    }
}