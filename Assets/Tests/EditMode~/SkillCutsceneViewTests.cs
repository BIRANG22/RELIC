using System.Collections;
using NUnit.Framework;
using UnityEngine;

public sealed class SkillCutsceneViewTests
{
    [Test]
    public void PlayAndWait_WaitsWhileViewIsActiveAndCompletesAfterDeactivation()
    {
        GameObject gameObject = new("SkillCutsceneViewTests");
        gameObject.SetActive(false);
        SkillCutsceneView view = gameObject.AddComponent<SkillCutsceneView>();

        try
        {
            IEnumerator routine = view.PlayAndWait(null);

            Assert.That(routine.MoveNext(), Is.True);
            Assert.That(gameObject.activeSelf, Is.True);

            WaitUntil waitUntil = routine.Current as WaitUntil;
            Assert.That(waitUntil, Is.Not.Null);
            Assert.That(waitUntil.keepWaiting, Is.True);

            gameObject.SetActive(false);

            Assert.That(waitUntil.keepWaiting, Is.False);
            Assert.That(routine.MoveNext(), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }
}
