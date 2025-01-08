using UnityEngine;

public class LimbCollision : MonoBehaviour
{
    [Header("Utlility Parameters")]
    [SerializeField] private RagdollController controller;
    [SerializeField] private bool canControllGrounded = false;

    [Header("Attack Parameters")]
    [SerializeField] private bool canAttack = true;

    private void Start()
    {
        controller = GameObject.FindAnyObjectByType<RagdollController>().GetComponent<RagdollController>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (canControllGrounded) 
        { 
            controller.isGrounded = true; 
        }

        //allow for punching
        if(canAttack && controller.canLimbAttack)
        {
            EnemyController enemyController;
            if (collision.gameObject.TryGetComponent<EnemyController>(out enemyController))
            {
                enemyController.ApplyDamage(controller.limbAttackDamage);
            }
        }
    }
}
