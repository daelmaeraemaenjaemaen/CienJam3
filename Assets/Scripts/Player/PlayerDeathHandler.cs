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
        Debug.Log("Player Dead");

        if (playDeathCutscene && deathCutscenePlayer != null)
            deathCutscenePlayer.PlayDefault();
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
        bool hasEnemyTag = !string.IsNullOrEmpty(enemyTag) && other.tag == enemyTag;

        if (hasEnemyAI || hasEnemyTag)
            KillPlayer();
    }
}