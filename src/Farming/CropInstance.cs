namespace stardew_medieval_v3.Farming;

/// <summary>
/// Runtime mutable state for a single planted crop.
/// </summary>
public class CropInstance
{
    public CropData Data { get; }
    public int DayCount { get; private set; }
    public bool IsWilted { get; private set; }

    public bool IsRipe => Data.IsRipe(DayCount);

    public CropInstance(CropData data)
    {
        Data = data;
    }

    /// <summary>
    /// Advance one growth day. Returns true if stage visually changed.
    /// </summary>
    public bool AdvanceDay()
    {
        int oldStage = Data.GetStageIndex(DayCount);
        DayCount++;
        int newStage = Data.GetStageIndex(DayCount);
        return oldStage != newStage;
    }

    /// <summary>
    /// Check and apply wilting for ripe crops.
    /// </summary>
    public bool CheckWilt()
    {
        if (!IsWilted && Data.IsWilted(DayCount))
        {
            IsWilted = true;
            return true;
        }
        return false;
    }

    public void SetState(int dayCount, bool isWilted)
    {
        DayCount = dayCount;
        IsWilted = isWilted;
    }
}
