using TMPro;
using UnityEngine;

public class NPCAI : MonoBehaviour
{
    public TextMeshProUGUI dialogueText;
    public GameObject projectilePrefab;
    public float projectileSpeed = 15f;
    public float shootCooldown = 1f;
    private bool hasTalked = false;
    private bool hasGivenItem = false;
    private bool hasAssisted = false;
    private bool playerInRange = false;
    private float shootTimer = 0f;
    private BTNode behaviorTree;
    int interactionStage = 0;
    private bool playerRequestedItem = false;
    private GameObject player;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        behaviorTree = new Selector(
            new Sequence(
                new Leaf(
                    () =>
                        playerInRange
                        && interactionStage == 2
                        && player.GetComponent<PlayerController>().health < 100f
                ),
                new Leaf(Assist)
            ),
            new Sequence(
                new Leaf(() => playerInRange && interactionStage == 1 && playerRequestedItem),
                new Leaf(GiveItem)
            ),
            new Sequence(new Leaf(() => playerInRange && interactionStage == 0), new Leaf(Talk)),
            new Leaf(Idle)
        );
    }

    void Update()
    {
        playerInRange = Vector3.Distance(transform.position, player.transform.position) < 20f;
        shootTimer -= Time.deltaTime;
        if (!playerInRange)
        {
            interactionStage = 0;
            playerRequestedItem = false;
            dialogueText.gameObject.SetActive(false);
        }
        else
        {
            dialogueText.gameObject.SetActive(true);
        }
        if (playerInRange && Input.GetKeyDown(KeyCode.F))
        {
            playerRequestedItem = true;
        }
        behaviorTree.Execute();
    }

    bool Idle()
    {
        return true;
    }

    bool Talk()
    {
        dialogueText.text = "Hello, adventurer! Press F to receive an item.";
        Debug.Log("Hello, adventurer! Press F to receive an item.");
        interactionStage = 1;
        return true;
    }

    bool GiveItem()
    {
        dialogueText.text = "Here's a health potion! If you're in danger, I will help.";
        player.GetComponent<PlayerController>().AddHealth(20f);
        interactionStage = 2;
        return true;
    }

    bool Assist()
    {
        dialogueText.text = "I'll help you fight!";
        hasAssisted = true;

        if (shootTimer <= 0)
        {
            GameObject[] monsters = GameObject.FindGameObjectsWithTag("Monster");
            GameObject closestMonster = null;
            float closestDistance = Mathf.Infinity;

            foreach (GameObject monster in monsters)
            {
                if (monster == null)
                    continue;

                float distance = Vector3.Distance(transform.position, monster.transform.position);
                if (distance < closestDistance)
                {
                    closestMonster = monster;
                    closestDistance = distance;
                }
            }

            if (closestMonster != null)
            {
                Vector3 direction = (
                    closestMonster.transform.position - transform.position
                ).normalized;

                GameObject projectile = Instantiate(
                    projectilePrefab,
                    transform.position + direction * 1f,
                    Quaternion.identity
                );

                projectile.GetComponent<Rigidbody>().velocity = direction * projectileSpeed;
                projectile.GetComponent<Projectile>().damage = 10f;
                projectile.GetComponent<Projectile>().targetTag = "Monster";

                Debug.Log("Shoot Monster");
                shootTimer = shootCooldown;
            }
            else
            {
                Debug.Log("No monsters found to assist against.");
            }
        }

        return true;
    }

    public void Interact()
    {
        interactionStage = 0;
        playerRequestedItem = false; // reset khi chuyển qua hành động khác
        behaviorTree.Execute();
    }
}
