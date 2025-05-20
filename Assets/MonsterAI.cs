using UnityEngine;

public class MonsterAI : MonoBehaviour
{
    public Transform player;
    public float health = 50f;
    public GameObject projectilePrefab;
    public GameObject monsterPrefab;
    public float projectileSpeed = 15f;
    public float shootCooldown = 1f;
    public float meleeDamage = 10f;
    private float shootTimer = 0f;
    private float wanderCooldown = 3f;
    private float wanderTimer = 0f;
    private Vector3 wanderTarget;
    private bool hasSpawnedMonster = false;
    private BTNode behaviorTree;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;

        behaviorTree = new Selector(
            new Sequence(
                new Leaf(() => health <= 25f && !hasSpawnedMonster),
                new Leaf(SpawnIfLowHealth)
            ),
            new Sequence(
                new Leaf(
                    () => Vector3.Distance(transform.position, player.position) < 2f && health > 0
                ),
                new Leaf(MeleeAttack)
            ),
            new Sequence(
                new Leaf(
                    () => Vector3.Distance(transform.position, player.position) < 5f && health > 0
                ),
                new Leaf(ChasePlayer)
            ),
            new Sequence(
                new Leaf(() =>
                {
                    float dist = Vector3.Distance(transform.position, player.position);
                    return dist >= 5f && dist < 10f && health > 0;
                }),
                new Leaf(RangedAttack)
            ),
            new Sequence(new Leaf(() => health > 0), new Leaf(RandomWander))
        );
    }

    bool ChasePlayer()
    {
        Debug.Log("Monster is chasing the player.");
        transform.position = Vector3.MoveTowards(
            transform.position,
            player.position,
            3f * Time.deltaTime
        );
        return true;
    }

    void Update()
    {
        behaviorTree.Execute();
        shootTimer -= Time.deltaTime;
    }

    bool MeleeAttack()
    {
        Debug.Log("Monster does melee attack!");
        player.GetComponent<PlayerController>().TakeDamage(meleeDamage);
        return true;
    }

    bool RangedAttack()
    {
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));

        if (shootTimer <= 0)
        {
            Vector3 direction = (player.position - transform.position).normalized;
            GameObject projectile = Instantiate(
                projectilePrefab,
                transform.position + direction * 1f,
                Quaternion.identity
            );
            projectile.GetComponent<Rigidbody>().velocity = direction * projectileSpeed;
            projectile.GetComponent<Projectile>().damage = 5f;
            projectile.GetComponent<Projectile>().targetTag = "Player";
            shootTimer = shootCooldown;
        }
        return true;
    }

    bool RandomWander()
    {
        wanderTimer -= Time.deltaTime;

        if (wanderTimer <= 0)
        {
            float range = 5f;
            Vector3 randomDirection = new Vector3(
                Random.Range(-range, range),
                0,
                Random.Range(-range, range)
            );
            wanderTarget = transform.position + randomDirection;
            wanderTimer = wanderCooldown;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            wanderTarget,
            2f * Time.deltaTime
        );
        return true;
    }

    void SpawnNewMonster()
    {
        Vector3 spawnPos =
            transform.position + new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
        GameObject newMonster = Instantiate(monsterPrefab, spawnPos, Quaternion.identity);
        MonsterAI newAI = newMonster.GetComponent<MonsterAI>();
        newAI.projectilePrefab = projectilePrefab;
        newAI.monsterPrefab = monsterPrefab;
        newAI.player = player;
        newAI.health = 50f; // Reset máu cho quái mới
    }

    bool SpawnIfLowHealth()
    {
        if (health <= 25f && !hasSpawnedMonster)
        {
            SpawnNewMonster();
            hasSpawnedMonster = true;
            Debug.Log("Spawned new monster because health is low");
        }
        return true; // luôn trả về true để tree không bị chặn
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Debug.Log("Monster defeated!");
            Destroy(gameObject);
        }
    }
}
