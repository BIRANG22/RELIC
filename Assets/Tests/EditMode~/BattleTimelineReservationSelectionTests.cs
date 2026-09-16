using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class BattleTimelineReservationSelectionTests
{
    [TestCase(true, true)]
    [TestCase(false, false)]
    public void ReservationInput_PreservesInfoSelection(
        bool canAcceptPlayerInput,
        bool expected)
    {
        MethodInfo method = typeof(BattleTimelineController).GetMethod(
            "ShouldKeepInfoSelectionDuringReservation",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null,
            "예약 단계의 배경 클릭은 캐릭터/몬스터 선택을 해제하지 않아야 합니다.");

        bool actual = (bool)method.Invoke(null, new object[] { canAcceptPlayerInput });

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void IsPlayerReservationInputActive_ReturnsExecutorInputAvailability()
    {
        GameObject timelineObject = new("ReservationTimeline");
        GameObject executorObject = new("ReservationExecutor");

        try
        {
            BattleTimelineController timeline =
                timelineObject.AddComponent<BattleTimelineController>();
            BattleTurnExecutor executor =
                executorObject.AddComponent<BattleTurnExecutor>();
            SetPrivateField(executor, "isMonsterPlanReady", true);
            SetPrivateField(executor, "isPlayerInputReady", true);
            SetPrivateField(timeline, "turnExecutor", executor);

            PropertyInfo property = timeline.GetType().GetProperty(
                "IsPlayerReservationInputActive",
                BindingFlags.Instance | BindingFlags.Public);

            Assert.That(property, Is.Not.Null,
                "몬스터 배경 클릭도 타임라인의 예약 입력 상태를 사용해야 합니다.");
            Assert.That((bool)property.GetValue(timeline), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(executorObject);
            Object.DestroyImmediate(timelineObject);
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, $"Missing field: {fieldName}");
        field.SetValue(target, value);
    }
}
