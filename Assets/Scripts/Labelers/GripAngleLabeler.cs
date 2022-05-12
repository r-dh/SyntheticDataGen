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

        MetricDefinition angleTopMetricDefinition;
        MetricDefinition angleBotMetricDefinition;
        MetricDefinition gripClosedMetricDefinition;

        // We will use this in future versions of perception
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
            public float AngleTop, AngleBot;
            public bool IsClosed;
            
            public GripAngle(AnnotationDefinition definition, string sensorId, float angleTop, float angleBot, float maxAngleDegrees)
                : base(definition, sensorId)
            {
                AngleTop = angleTop;
                AngleBot = angleBot;
                IsClosed = AngleTop - AngleBot * -1.0f < maxAngleDegrees;
            }


            public override void ToMessage(IMessageBuilder builder)
            {
                base.ToMessage(builder);
                builder.AddFloat("AngleTop", AngleTop);
                builder.AddFloat("AngleBot", AngleBot);
                builder.AddBool("isClosed", IsClosed);
            }

            public override bool IsValid() => true;
        }

        protected override void Setup()
        {
            gripAngleDef = new GripAngleDef("gripAngle");
            DatasetCapture.RegisterAnnotationDefinition(gripAngleDef);
        }

        /*bool TryToGetTemplateIndexForJoint(KeypointTemplate template, JointLabel joint, out int index)
        {
            index = -1;

            foreach (var label in joint.labels)
            {
                for (var i = 0; i < template.keypoints.Length; i++)
                {
                    if (template.keypoints[i].label == label)
                    {
                        index = i;
                        return true;
                    }
                }
            }

            return false;
        }*/

        protected override void OnBeginRendering(ScriptableRenderContext scriptableRenderContext)
        {
            //Get local z rotation of both grips of the left tool
            //var lightPos = targetLight.transform.position;
            //var metric = new GenericMetric(new[] { lightPos.x, lightPos.y, lightPos.z }, angleTopMetricDefinition);
            //DatasetCapture.ReportMetric(angleTopMetricDefinition, metric);

            //Get local z rotation of both grips of the right tool

            if (perceptionCamera.SensorHandle.ShouldCaptureThisFrame)
            {
                foreach (var label in LabelManager.singleton.registeredLabels)
                    ProcessLabel(label);
            }

        }

        void ProcessLabel(Labeling labeledEntity)
        {
            var entityGameObject = labeledEntity.gameObject;
            float angleTop = 0f, angleBot = 0f;
            bool mayReport = false;
            
            // TODO: we can optimize this, no need to iterate over everything
            foreach (var joint in entityGameObject.transform.GetComponentsInChildren<JointLabel>())
            {
                //TryToGetTemplateIndexForJoint(activeTemplate, joint, out var idx)
                foreach (var label in joint.labels) // Usually this is just a list of one label
                {
                    if (label == "B_Driver_01")        //TODO: Expose hardcoded var
                    {
                        // TODO: check
                        //Debug.Log($"Driver 1 found {entityGameObject.name} " + joint.transform.localEulerAngles);
                        angleTop = joint.transform.localEulerAngles.y;

                    }
                    else if (label == "B_Driver_02")   //TODO: Expose hardcoded var
                    {
                        // TODO: idem ditto
                        //Debug.Log($"Driver 2 found {entityGameObject.name} " + joint.transform.localEulerAngles);
                        angleBot = joint.transform.localEulerAngles.y;
                        mayReport = true;
                    }
                }
            }

            if (!mayReport) return;

            // TODO: We are using metrics because ReportAnnotation() is broken in 0.10.0-preview.1
            // We can switch to ReportAnnotation() in the future.
            // TODO: aggregate angleTop & angleBot to one variable, add index
/*            var values = new Dictionary<string, string>()
            {
                {"angleTop", angleTop.ToString()},
                {"angleBot", angleBot.ToString()},
                {"gripClosed", (angleTop - angleBot < maxAngleDegrees).ToString()}
            };*/

            //Debug.Log($"{angleTop} {angleBot} {Math.Abs(angleTop - angleBot) < maxAngleDegrees}");
/*            var v = new Dictionary<string, Dictionary<string, string>>()
            {
                {labeledEntity.instanceId.ToString(), values}
            };*/
            // TODO: GenericMetric accepts only float array, not dict
            // Follow up: https://github.com/Unity-Technologies/com.unity.perception/issues/485
/*            var metricTop = new GenericMetric( new float[] { angleTop, angleBot, (Math.Abs(angleTop - angleBot) < maxAngleDegrees) ? 1.0f : 0.0f }, angleTopMetricDefinition);
            DatasetCapture.ReportMetric(angleTopMetricDefinition, metricTop);*/
/*            var metricBot = new GenericMetric(new[] { angleBot }, angleBotMetricDefinition);
            DatasetCapture.ReportMetric(angleBotMetricDefinition, metricBot);
            var metricGripClosed = new GenericMetric(new[] { angleTop - (angleBot * -1.0f) < maxAngleDegrees }, gripClosedMetricDefinition);
            DatasetCapture.ReportMetric(gripClosedMetricDefinition, metricGripClosed);*/

            var annotation = new GripAngle(gripAngleDef, sensorHandle.Id, angleTop, angleBot, maxAngleDegrees);
            sensorHandle.ReportAnnotation(gripAngleDef, annotation);
        }
    }
}
// Example metric that is added each frame in the dataset:
// {
//   "capture_id": null,
//   "annotation_id": null,
//   "sequence_id": "9768671e-acea-4c9e-a670-0f2dba5afe12",
//   "step": 1,
//   "metric_definition": "lightMetric1",
//   "values": [
//      96.1856,
//      192.675964,
//      -193.838638
//    ]
// },

// Example annotation that is added to each capture in the dataset:
// {
//     "annotation_id": "target1",
//     "model_type": "targetPosDef",
//     "description": "The position of the target in the camera's local space",
//     "sensor_id": "camera",
//     "id": "target1",
//     "position": [
//         1.85350215,
//         -0.253945172,
//         -5.015307
//     ]
// }