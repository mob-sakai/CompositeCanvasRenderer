namespace CompositeCanvas
{
    public enum TransformSensitivity
    {
        Low,
        Medium,
        High
    }

    public enum DownSamplingRate
    {
        None = 0,
        x1 = 1,
        x2 = 2,
        x4 = 4,
        x8 = 8
    }

    public enum BlendType
    {
        Custom,
        AlphaBlend,
        Additive,
        MultiplyAdditive
    }

    public enum ColorMode
    {
        Multiply,
        Additive,
        Subtract,
        Fill
    }
}
