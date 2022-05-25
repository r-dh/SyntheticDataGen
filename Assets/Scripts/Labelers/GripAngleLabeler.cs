using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// A list of objects which rotations will be tracked.
        /// </summary>
        public string[] labelsTracked;

        /// <summary>
        /// The maximum angle between tips for which the tool is considered closed.
        /// </summary>
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

            var dataPoints = new List<Tuple<float, Quaternion>>();

            foreach (var go in entityGameObject.transform.GetComponentsInChildren<TrackOrientationTag>())
            {
                if (labelsTracked.Contains(go.name))
                {
                    dataPoints.Add(new Tuple<float, Quaternion>(
                            go.transform.localEulerAngles.y,
                            go.transform.localRotation)
                    );
                }
            }

            if (dataPoints.Count() != 2) return;

            float angleTop = dataPoints[0].Item1;
            float angleBot = dataPoints[1].Item1;
            bool isClosed = Math.Abs(angleTop - angleBot) < maxAngleDegrees;

            Quaternion qTop = dataPoints[0].Item2;
            Quaternion qBot = dataPoints[1].Item2;

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