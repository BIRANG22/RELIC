using UnityEngine;

public static class BattleRoomIntroSequenceUtility
{
    public static IBattleRoomIntroSequence FindFirst(GameObject roomRoot)
    {
        if (roomRoot == null)
            return null;

        MonoBehaviour[] behaviours = roomRoot.GetComponentsInChildren<MonoBehaviour>(false);

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IBattleRoomIntroSequence sequence)
                return sequence;
        }

        return null;
    }
}
