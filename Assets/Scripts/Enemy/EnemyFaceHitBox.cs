using UnityEngine;

public class EnemyFaceHitBox : MonoBehaviour
{
    [SerializeField] private EnemyAI ownerEnemy;

    private void Awake()
    {
        if (ownerEnemy == null)
            Debug.LogWarning("EnemyFaceHitBox: ownerEnemy is not assigned.");
    }

    public EnemyAI GetOwnerEnemy()
    {
        return ownerEnemy;
    }
}
