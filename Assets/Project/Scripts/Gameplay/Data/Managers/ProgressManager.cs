namespace Relic.Gameplay.Data
{
    public class ProgressManager
    {
        public ProgressData Progress { get; } = new();

        public void SetLocation(string chapter, string area, string mapId)
        {
            Progress.CurrentChapter = chapter;
            Progress.CurrentArea = area;
            Progress.CurrentMap = mapId;
        }

        public void SetSaveSlot(int slotNumber) => Progress.SaveSlotNumber = slotNumber;
    }
}
