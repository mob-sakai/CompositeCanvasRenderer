namespace CompositeCanvas.Enums
{
    /// <summary>
    /// Defines how blur is applied.
    /// <para />
    /// <b>Uniform</b>: Applies the same blur radius to both the horizontal (X) and vertical (Y) axes.
    /// <para />
    /// <b>SeparateAxis</b>: Allows independent blur radii per axis (different strength for X and Y).
    /// <para />
    /// <b>Motion</b>: Applies a directional blur intended to simulate motion; typically elongated along a direction.
    /// </summary>
    public enum BlurMode
    {
        Uniform,
        SeparateAxis,
        Motion
    }
}
