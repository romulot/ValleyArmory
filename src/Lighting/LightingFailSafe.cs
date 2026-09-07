namespace ValleyArmory.Lighting;

internal sealed class LightingFailSafe
{
    private bool warningIssued;

    public bool Enabled { get; private set; } = true;

    public bool DisableAndShouldWarn()
    {
        this.Enabled = false;
        if (this.warningIssued)
            return false;

        this.warningIssued = true;
        return true;
    }
}
