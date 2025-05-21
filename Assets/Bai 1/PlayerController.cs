using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 2f;
    public float health = 100f;
    public float interactionRange = 3f;
    public GameObject projectilePrefab;
    public Transform shootPoint;
    public GameObject[] weaponObjects;
    public float[] weaponDamages = { 10f, 15f };
    public float[] weaponFireRates = { 0.5f, 0.3f };
    public int[] weaponAmmo = { 12, 30 };
    private int currentWeapon = 0;
    private int[] currentAmmo;
    private float fireTimer = 0f;
    private float reloadTimer = 0f;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private bool isDashing = false;
    private Camera mainCamera;
    public float projectileSpeed = 20f;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
            Debug.LogError("Main Camera not found!");
        if (shootPoint == null)
            Debug.LogError("ShootPoint not assigned!");
        if (weaponObjects.Length != 2)
            Debug.LogError("Exactly 2 weapon objects must be assigned!");

        currentAmmo = new int[2] { weaponAmmo[0], weaponAmmo[1] };
        UpdateWeaponObjects();
    }

    void Update()
    {
        // Rotate and move towards mouse direction
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Ground")))
        {
            Vector3 targetPos = new Vector3(hit.point.x, transform.position.y, hit.point.z);
            Vector3 direction = (targetPos - transform.position).normalized;

            // Rotate to face mouse
            if (direction != Vector3.zero)
            {
                Quaternion rotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Euler(0, rotation.eulerAngles.y, 0);
            }

            // Move forward/backward with W/S
            float verticalInput = Input.GetAxisRaw("Vertical"); // W/S
            float horizontalInput = Input.GetAxisRaw("Horizontal"); // A/D

            if (verticalInput != 0 || horizontalInput != 0)
            {
                float speed = isDashing ? dashSpeed : moveSpeed;

                Vector3 forward = transform.forward;
                Vector3 right = transform.right;

                Vector3 moveDirection = (
                    forward * verticalInput + right * horizontalInput
                ).normalized;

                transform.position += moveDirection * speed * Time.deltaTime;
            }
        }
        else
        {
            Debug.LogWarning("Raycast did not hit Ground layer!");
        }

        // Dash
        if (Input.GetKeyDown(KeyCode.Space) && dashCooldownTimer <= 0)
        {
            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
        }
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0)
                isDashing = false;
        }
        dashCooldownTimer -= Time.deltaTime;

        // Shoot
        fireTimer -= Time.deltaTime;
        if (
            Input.GetMouseButton(0)
            && fireTimer <= 0
            && currentAmmo[currentWeapon] > 0
            && reloadTimer <= 0
        )
        {
            Shoot();
            currentAmmo[currentWeapon]--;
            fireTimer = weaponFireRates[currentWeapon];
        }

        // Reload
        if (Input.GetKeyDown(KeyCode.R) && currentAmmo[currentWeapon] < weaponAmmo[currentWeapon])
        {
            reloadTimer = 1.5f;
        }
        if (reloadTimer > 0)
        {
            reloadTimer -= Time.deltaTime;
            if (reloadTimer <= 0)
                currentAmmo[currentWeapon] = weaponAmmo[currentWeapon];
        }

        // Switch weapon
        if (Input.GetKeyDown(KeyCode.Q))
        {
            currentWeapon = (currentWeapon + 1) % 2;
            fireTimer = 0;
            UpdateWeaponObjects();
            Debug.Log($"Switched to weapon: {(currentWeapon == 0 ? "Pistol" : "Rifle")}");
        }

        // Interaction
        if (Input.GetKeyDown(KeyCode.E))
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, interactionRange);
            foreach (var collider in colliders)
            {
                if (collider.CompareTag("Monster"))
                {
                    collider.GetComponent<MonsterAI>().TakeDamage(10f);
                }
                else if (collider.CompareTag("NPC"))
                {
                    collider.GetComponent<NPCAI>().Interact();
                    Debug.Log("Player interacted with NPC");
                }
            }
        }
    }

    void Shoot()
    {
        if (shootPoint == null)
        {
            Debug.LogError("ShootPoint is not assigned!");
            return;
        }
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Ground")))
        {
            Vector3 direction = (
                new Vector3(hit.point.x, shootPoint.position.y, hit.point.z) - shootPoint.position
            ).normalized;
            GameObject projectile = Instantiate(
                projectilePrefab,
                shootPoint.position,
                Quaternion.identity
            );
            projectile.GetComponent<Rigidbody>().velocity = direction * projectileSpeed;
            projectile.GetComponent<Projectile>().damage = weaponDamages[currentWeapon];
            projectile.GetComponent<Projectile>().targetTag = "Monster";
        }
    }

    void UpdateWeaponObjects()
    {
        for (int i = 0; i < weaponObjects.Length; i++)
        {
            if (weaponObjects[i] != null)
                weaponObjects[i].SetActive(i == currentWeapon);
        }
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        if (health <= 0)
            Debug.Log("Player defeated!");
    }

    public void AddHealth(float amount)
    {
        health = Mathf.Min(health + amount, 100f);
    }
}
