using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Timeline;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

[RequireComponent(typeof(NavMeshAgent))]
public class ElephantAi : MonoBehaviour, IHuntableAnimal
{
    [SerializeField] AnimalType animalType;
    [SerializeField] int points = 10;
    
    [Header("Movement Settings")]
    [SerializeField] float wanderRadius = 15f;
    [SerializeField] float wanderSpeed = 3f;
    [SerializeField] float runSpeed = 8f;
    [SerializeField] float safetyCheckTime = 2f;
    [SerializeField] float idleTime = 3f;

    [Header("Detection Settings")]
    [SerializeField] float detectionRadius = 10f;
    [SerializeField] float detectionAngle = 140f;
    [SerializeField] float noiseDangerFactor = 2f;
    [SerializeField] float maxNoiseDetectionRadius = 15f;

    [Header("Attack Settings")]
    [SerializeField] float attackDistance = 8f;
    [SerializeField] float damage = 10f;
    
    [Header("Death Settings")]
    [SerializeField] float destroyDelay = 5f;
    
    CountdownTimer chaseTimer;
    CountdownTimer idleTimer;
    ConeDetectionStrategy coneDetectionStrategy;
    
    NavMeshAgent agent;
    Animator animator;
    Transform tigerTrasform;
    TigerController tiger;
    
    bool destinationReached;
    bool dangerDetected;
    bool isDead;
    bool attackedByTiger;
    bool attacking;

    private bool IsSafe => !dangerDetected && !chaseTimer.IsRunning;
    public AnimalType AnimalType => animalType;
    public int Points => points;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        var player = GameObject.FindGameObjectWithTag("Player");
        tigerTrasform = player.transform;
        tiger = player.GetComponent<TigerController>();
        
        chaseTimer = new CountdownTimer(safetyCheckTime);
        idleTimer = new CountdownTimer(idleTime);
        coneDetectionStrategy = new ConeDetectionStrategy(detectionAngle, detectionRadius);
    }

    private void OnEnable()
    {
        idleTimer.OnTimerStopped += () => 
        { 
            if(isDead) return;
            Vector3 destination = FindNewWanderPosition();
            
            agent.speed = wanderSpeed;
            agent.SetDestination(destination);
            
            animator.SetFloat("MoveSpeed", 1f);
        };

        GetComponentInChildren<ElephantAnimationHelper>().OnAttackAnimationEnd += () => { attacking = false; };
        GetComponentInChildren<ElephantAnimationHelper>().OnAttackImpact += () => {
            if(Vector3.Distance(tigerTrasform.position, transform.position) <= attackDistance)
                tiger.TakeDamage(damage);
        };
    }

    private void Start()
    {
        print($"{transform.position}");
        Vector3 destination = FindNewWanderPosition();
        agent.speed = wanderSpeed;
        agent.SetDestination(destination);
        animator.SetFloat("MoveSpeed", 1f);
    }

    private void Update()
    {
        if (isDead){
            return;
        }

        var distToTiger = Vector3.Distance(tigerTrasform.position, transform.position);

        print($"{distToTiger}");
        if (attackedByTiger && !attacking){
            if (distToTiger <= attackDistance){
                transform.LookAt(tigerTrasform);
                attacking = true;
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                    agent.ResetPath();
                }
                animator.SetTrigger("Attack");
            }else if (distToTiger <= detectionRadius){
                if (NavMesh.SamplePosition(tigerTrasform.position, out NavMeshHit navMeshHit, 1f, NavMesh.AllAreas)){
                    agent.speed = runSpeed;
                    agent.SetDestination(navMeshHit.position);
                    animator.SetFloat("MoveSpeed", 2f);
                }
            }
            else{
                attackedByTiger = false;
            }
        }
        
        dangerDetected = LevelManager.CurrentLevel >= LevelManager.instance.RequiredLevel(animalType) && 
            (coneDetectionStrategy.Execute(tigerTrasform, transform) || CheckNoiseDanger());

        if (dangerDetected && !chaseTimer.IsRunning){
            Vector3 destination = FindSafePosition();
            
            agent.speed = runSpeed;
            agent.SetDestination(destination);
            
            animator.SetTrigger("Attacked");
            animator.SetFloat("MoveSpeed", 2f);
            
            chaseTimer.Start();
            chaseTimer.Reset(safetyCheckTime);
        }
        else if (chaseTimer.IsRunning && agent.remainingDistance <= agent.stoppingDistance){
            Vector3 destination = FindSafePosition();
            
            agent.speed = runSpeed;
            agent.SetDestination(destination);
        }
        
        //animation 
        //animator.SetFloat("MoveSpeed", agent.velocity.magnitude / agent.speed);

        if (agent.remainingDistance <= agent.stoppingDistance && !idleTimer.IsRunning && IsSafe){
            animator.SetTrigger("Eating");
            animator.SetFloat("MoveSpeed", 0f);
            
            idleTimer.Start();
            idleTimer.Reset(idleTime); //if time is changed
        }

        //timer tick
        chaseTimer.Tick(Time.deltaTime);
        idleTimer.Tick(Time.deltaTime);
    }

    bool CheckNoiseDanger()
    {
        float noisePercent = tiger.Noise;
        float noiseDetectionRadius = maxNoiseDetectionRadius * noisePercent;
        
        return Vector3.Distance(tigerTrasform.position, transform.position) <= noiseDetectionRadius;
    }

    Vector3 FindNewWanderPosition()
    {
        Vector3 wanderDir = Random.insideUnitSphere * wanderRadius;
        wanderDir.y = 0;
        if (NavMesh.SamplePosition(transform.position + wanderDir, out NavMeshHit navMeshHit, 1f, NavMesh.AllAreas)){
            return navMeshHit.position;
        }

        return Vector3.zero;
    }

    Vector3 FindSafePosition()
    {
        Vector3 fleeDir = (transform.position - tigerTrasform.position).normalized;

// Optional: Add a slight angle deviation
        float angle = Random.Range(-20f, 20f);
        fleeDir = Quaternion.Euler(0, angle, 0) * fleeDir;

// Target point in flee direction
        Vector3 fleeTarget = transform.position + fleeDir * wanderRadius;

// Check if path to target is blocked
        if (NavMesh.Raycast(transform.position, fleeTarget, out NavMeshHit hit, NavMesh.AllAreas)){
            // If path is blocked, try the opposite side of the hit
            Vector3 altTarget = hit.position + hit.normal * 2f; // Small offset away from obstacle
            if (NavMesh.SamplePosition(altTarget, out hit, 1f, NavMesh.AllAreas)){
                return hit.position;
            }
        }
        else{
            // If path is clear, sample at that point directly
            if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit navHit, 1f, NavMesh.AllAreas)){
                return navHit.position;
            }
        }

// Fallback: Don't move
        return transform.position;
    }
    

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Draw cone lines
        Vector3 forward = transform.forward;
        Quaternion leftRayRotation = Quaternion.AngleAxis(-detectionAngle / 2f, transform.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(detectionAngle / 2f, transform.up);

        Vector3 leftRayDirection = leftRayRotation * forward;
        Vector3 rightRayDirection = rightRayRotation * forward;

        Gizmos.DrawRay(transform.position, leftRayDirection * detectionRadius);
        Gizmos.DrawRay(transform.position, rightRayDirection * detectionRadius);
        Gizmos.DrawLine(transform.position + leftRayDirection * detectionRadius, transform.position + rightRayDirection * detectionRadius);
            
        // Indicate current facing direction
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.5f);
    }
    
    public bool TryHunt()
    {
        if (LevelManager.CurrentLevel < LevelManager.instance.RequiredLevel(animalType)){
            attackedByTiger = true;
            return false;
        }
        
        isDead = true;
        animator.SetFloat("MoveSpeed", 0);
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
            agent.enabled = false;
        }

        animator.SetTrigger("Dead");

        StartCoroutine(DestroyAfterDelay());

        return true;
        
        IEnumerator DestroyAfterDelay()
        {
            yield return new WaitForSeconds(destroyDelay);
            Destroy(gameObject);
        }
    }
}