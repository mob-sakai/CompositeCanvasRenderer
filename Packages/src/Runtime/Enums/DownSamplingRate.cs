namespace CompositeCanvas.Enums
{
    /// <summary>
    /// Down sampling rate for baking.
    /// The higher this value, the lower the resolution of the bake, but the performance will improve.
    /// </summary>
    public enum DownSamplingRate
    {
        None = 0,
        x1 = 1,
        x2 = 2,
        x4 = 4,
        x8 = 8
    }
}
