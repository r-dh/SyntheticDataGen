using System;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;

[Serializable]
[AddRandomizerMenu("Perception/My Scale Randomizer")]
public class MyScaleRandomizer : Randomizer
{
    public FloatParameter scaleParameter;

    protected override void OnIterationStart()
    {
        var tags = tagManager.Query<MyScaleRandomizerTag>();

        foreach (var tag in tags)
        {
            tag.transform.localScale = scaleParameter.Sample() * Vector3.one;
        }
    }
}
