using UnityEngine;

public class PlayerDeathHandler : MonoBehaviour
{
    [SerializeField] private bool dieOnEnemyContact = true;
    [SerializeField] private string enemyTag = "Enemy";

    [Header("Death Cutscene")]
    [SerializeField] private bool playDeathCutscene = true;
    [SerializeField] private CutscenePlayer deathCutscenePlayer;

    public bool IsDead { get; private set; }

    public void Die()
    {
        KillPlayer();
    }

    public void KillPlayer()
    {
        if (IsDead)
            return;

        IsDead = true;
        Debug.Log("[PlayerDeathHandler] Player caught by enemy.");

        if (playDeathCutscene && deathCutscenePlayer != null)
        {
            deathCutscenePlayer.PlayDefault();
        }
        else if (playDeathCutscene)
        {
            Debug.LogWarning("[PlayerDeathHandler] deathCutscenePlayer is not assigned.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDieFromContact(other != null ? other.gameObject : null);
    }

    private void OnTriggerStay(Collider other)
    {
        TryDieFromContact(other != null ? other.gameObject : null);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryDieFromContact(collision != null ? collision.gameObject : null);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        TryDieFromContact(hit != null && hit.collider != null ? hit.collider.gameObject : null);
    }

    private void TryDieFromContact(GameObject other)
    {
        if (!dieOnEnemyContact || IsDead || other == null)
            return;

        bool hasEnemyAI = other.GetComponentInParent<EnemyAI>() != null;
        bool hasEnemyTag = HasTagInHierarchy(other.transform, enemyTag);

        if (hasEnemyAI || hasEnemyTag)
            KillPlayer();
    }

    private bool HasTagInHierarchy(Transform target, string tagName)
    {
        if (target == null || string.IsNullOrEmpty(tagName))
            return false;

        Transform current = target;
        while (current != null)
        {
            if (current.tag == tagName)
                return true;

            current = current.parent;
        }

        return false;
    }
}
