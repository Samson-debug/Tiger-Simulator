using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    //[SerializeField] private Image healthBar;
    //[SerializeField] private float maxHealth = 100f;
    public float destroyDelay = 5f;
    
    public float health;
    Animator animator;
    NavMeshAgent agent;

    private void Awake()
    {
        //health = maxHealth;
        //healthBar.fillAmount = 1;
        
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
    }

    /*public void TakeDamage(uint damage)
    {
        health -= damage;
        UpdateHealthBar();
        
        if (health <= 0){
            Debug.Log($"{gameObject.name} died!");
            if (onHealthEnd == HealthEndType.Reload)
                Restart();
            else if(onHealthEnd == HealthEndType.Die){
                Die();
            }
        }
    }*/

    public void Die()
    {
        if (agent == null || !agent.isOnNavMesh)
            return;

        agent.isStopped = true;       // Stop the agent from processing movement
        agent.velocity = Vector3.zero; // Reset velocity to avoid sliding
        agent.ResetPath();            // Clear the current path
        
        animator.SetTrigger("Dead");

        StartCoroutine(DestroyAfterDelay());

        IEnumerator DestroyAfterDelay()
        {
            yield return new WaitForSeconds(destroyDelay);
            Destroy(gameObject);
        }
    }

    /*private void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }*/

    /*public void Reset()
    {
        health = maxHealth;
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        healthBar.fillAmount = health / maxHealth;
    }*/
}