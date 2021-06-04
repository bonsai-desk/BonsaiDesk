public class WholeEffect
{
    public float progress;
    public int framesSinceLastDamage;
    public BlockBreakHand.BreakMode mode;
    public bool activated;

    public WholeEffect(BlockBreakHand.BreakMode mode)
    {
        progress = 0;
        framesSinceLastDamage = 100;
        this.mode = mode;
        activated = false;
    }
}