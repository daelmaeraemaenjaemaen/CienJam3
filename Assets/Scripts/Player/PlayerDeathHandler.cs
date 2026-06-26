using UnityEngine;

public class PlayerDeathHandler : MonoBehaviour
{
    public bool IsDead { get; private set; }

    public void KillPlayer()
    {
        if (IsDead)
            return;

        IsDead = true;
        Debug.Log("Player Dead");
    }
}
