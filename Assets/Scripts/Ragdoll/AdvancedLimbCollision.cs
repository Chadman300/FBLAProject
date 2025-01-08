using UnityEngine;

public class AdvancedLimbCollision : MonoBehaviour
{
    [Header("Utlility Parameters")]
    [SerializeField] private AdvancedRagdollController controller;
    [SerializeField] private bool canControllGrounded = false;

    [Header("Attack Parameters")]
    [SerializeField] private bool canAttack = true;

    private void Start()
    {
        controller = GameObject.FindAnyObjectByType<AdvancedRagdollController>().GetComponent<AdvancedRagdollController>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (canControllGrounded)
        {
            controller.isGrounded = true;
        }

        //get dmg
        var damage = controller.limbAttackDamage * (controller.hipsRb.linearVelocity.magnitude / controller.limbVelocityDividend);

        //allow for punching
        if (canAttack && controller.canLimbAttack && damage >= controller.limbDamageThreshold)
        {
            EnemyController enemyController;
            if (collision.gameObject.TryGetComponent<EnemyController>(out enemyController))
            {
                StartCoroutine(controller.LimbDelay());
                enemyController.ApplyDamage(damage);
                Debug.Log(damage);
            }
        }
    }
}
