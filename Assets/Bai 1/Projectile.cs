using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float damage = 10f;
    public float lifetime = 5f;
    public string targetTag; // "Player" or "Monster"

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag(targetTag))
        {
            if (targetTag == "Monster")
            {
                MonsterAI monster = other.gameObject.GetComponent<MonsterAI>();
                if (monster != null)
                    monster.TakeDamage(damage);
            }
            else if (targetTag == "Player")
            {
                PlayerController player = other.gameObject.GetComponent<PlayerController>();
                if (player != null)
                    player.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
    }
}
