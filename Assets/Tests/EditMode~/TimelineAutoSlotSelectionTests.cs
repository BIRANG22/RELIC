using NUnit.Framework;

public class TimelineAutoSlotSelectionTests
{
    [Test]
    public void FindBestSlot_PrefersEarliestEmptySlotBeforeCurrent()
    {
        TimelineAutoSlotState[] slots =
        {
            Empty(),
            OccupiedOther(),
            Empty(),
            OccupiedOther(),
            Empty(),
        };

        int result = TimelineAutoSlotSelectionUtility.FindBestSlot(slots, 3);

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void FindBestSlot_KeepsCurrentSlotWhenItIsEmptyAndNoEarlierEmptySlotExists()
    {
        TimelineAutoSlotState[] slots =
        {
            OccupiedOther(),
            OccupiedOther(),
            Empty(),
            Empty(),
            Empty(),
        };

        int result = TimelineAutoSlotSelectionUtility.FindBestSlot(slots, 2);

        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void FindBestSlot_UsesEarliestEmptySlotAfterCurrentWhenEarlierSlotsAreUnavailable()
    {
        TimelineAutoSlotState[] slots =
        {
            OccupiedOther(),
            OccupiedOther(),
            OccupiedOther(),
            Empty(),
            Empty(),
        };

        int result = TimelineAutoSlotSelectionUtility.FindBestSlot(slots, 1);

        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void FindBestSlot_FallsBackToEarliestSlotAlreadyOwnedByCharacter()
    {
        TimelineAutoSlotState[] slots =
        {
            OccupiedOther(),
            SameCharacter(),
            OccupiedOther(),
            SameCharacter(),
            OccupiedOther(),
        };

        int result = TimelineAutoSlotSelectionUtility.FindBestSlot(slots, 2);

        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void FindBestSlot_ReturnsMinusOneWhenNoSlotCanAcceptCharacter()
    {
        TimelineAutoSlotState[] slots =
        {
            OccupiedOther(),
            FullSameCharacter(),
            OccupiedOther(),
        };

        int result = TimelineAutoSlotSelectionUtility.FindBestSlot(slots, 1);

        Assert.That(result, Is.EqualTo(-1));
    }

    private static TimelineAutoSlotState Empty()
    {
        return new TimelineAutoSlotState(true, true, true, true, false);
    }

    private static TimelineAutoSlotState OccupiedOther()
    {
        return new TimelineAutoSlotState(true, false, false, true, false);
    }

    private static TimelineAutoSlotState SameCharacter()
    {
        return new TimelineAutoSlotState(true, false, true, true, true);
    }

    private static TimelineAutoSlotState FullSameCharacter()
    {
        return new TimelineAutoSlotState(true, false, true, false, true);
    }
}
