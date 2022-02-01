public class EGS_PlayerInputs
{
    private bool[] movementInputs;

    /// <summary>
    /// Getter for the MovementInputs.
    /// </summary>
    /// <returns>MovementInputs</returns>
    public bool[] GetMovementInputs() { return movementInputs; }

    /// <summary>
    /// Setter for the MovementInputs.
    /// </summary>
    /// <param name="i">New MovementInputs</param>
    public void SetMovementInputs(bool[] i) { movementInputs = i; }
}
