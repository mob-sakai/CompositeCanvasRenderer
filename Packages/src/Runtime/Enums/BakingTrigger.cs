namespace CompositeCanvas.Enums
{
    /// <summary>
    /// Baking trigger mode.
    /// <para />
    /// Automatic: Baking is performed automatically when the transform of the source graphic changes.
    /// <para />
    /// Manually: Baking is performed manually by calling SetDirty().
    /// <para />
    /// Always: Baking is performed every frame.
    /// <para />
    /// OnEnable: Baking is performed once when enabled.
    /// </summary>
    public enum BakingTrigger
    {
        Automatic,
        Manually,
        Always,
        OnEnable
    }
}
