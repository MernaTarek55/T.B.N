using UnityEngine;

public class LavaProjectile : MonoBehaviour
{
    private QuadraticCurve quadraticCurve;
    private float lifetime = 2f; // 2 second lifetime
    private float spawnTime; // to remove it after certain time

    [System.Serializable]
    public struct Range4
    {
        public float minX, maxX, minZ, maxZ;

        public Range4(float minX, float maxX, float minY, float maxY)
        {
            this.minX = minX;
            this.maxX = maxX;
            this.minZ = minY;
            this.maxZ = maxY;
        }
    }

    public enum ProjectileEnemyType
    {
        Target,
        Random
    }

    [SerializeField] private float speed = 1f;
    [Header("Positions")]
    private Vector3 initialA;
    private Vector3 initialControl;
    private Vector3 initialB;
    [SerializeField] private Range4 randomRange; // (minX, maxX, minZ, maxZ)
    private Vector2 randomizer;
    [Header("References")]
    private QuadraticCurve curve;
    private Vector3 enemyPosition;
    public ProjectileEnemyType projectileType;
    private float _sampleTime;

    // Flag to track if projectile is properly initialized
    private bool isInitialized = false;

    private void OnEnable()
    {
        // Reset state when projectile is reused from pool
        ResetProjectile();

        if (CheckingForEmptyReferences())
        {
            StoringProjectPositions(); // Storing positions of start and end points of the projectile path
            InitializeMovement();
            isInitialized = true;
        }
        else
        {
            Debug.Log("LavaProjectile: Failed to initialize - missing references!");
            ReturnToPool();
        }
    }

    private void ResetProjectile()
    {
        _sampleTime = 0f;
        spawnTime = Time.time;
        isInitialized = false;
    }

    private bool CheckingForEmptyReferences()
    {
        // First check if quadraticCurve is assigned
        if (quadraticCurve == null)
        {
            Debug.Log("LavaProjectile: quadraticCurve is null! Make sure to call SetQuadraticCurve() before activating.");
            return false;
        }

        curve = quadraticCurve;

        // Check all required components
        if (curve == null)
        {
            Debug.Log("LavaProjectile: Missing QuadraticCurve reference!");
            return false;
        }

        if (curve.A == null)
        {
            Debug.Log("LavaProjectile: Missing QuadraticCurve point A!");
            return false;
        }

        if (curve.B == null)
        {
            Debug.Log("LavaProjectile: Missing QuadraticCurve point B!");
            return false;
        }

        if (curve.Control == null)
        {
            Debug.Log("LavaProjectile: Missing QuadraticCurve control point!");
            return false;
        }

        return true;
    }

    private void StoringProjectPositions()
    {
        // Cache positions at activation
        initialA = curve.A.position;
        initialControl = curve.Control.position;

        Debug.Log($"LavaProjectile initialized - A: {initialA}, Control: {initialControl}");

        // Generate random offset for random projectile type
        randomizer.x = Random.Range(randomRange.minX, randomRange.maxX);
        randomizer.y = Random.Range(randomRange.minZ, randomRange.maxZ);
        

        switch (projectileType)
        {
            case ProjectileEnemyType.Random:
                initialB = new Vector3(randomizer.x+ curve.B.position.x, curve.B.position.y, randomizer.y + curve.B.position.z);
                break;
            case ProjectileEnemyType.Target:
                initialB = curve.B.position;
                break;
        }
    }

    private void InitializeMovement()
    {
        // Initialize movement parameters
        _sampleTime = 0f;
        lifetime = 2f;
        spawnTime = Time.time;

        // Set initial position
        transform.position = initialA;
    }

    private void Update()
    {
        // Safety check - don't update if not properly initialized
        if (!isInitialized || !gameObject.activeSelf)
        {
            return;
        }

        // Check if lifetime has expired
        if (Time.time - spawnTime >= lifetime)
        {
            Debug.Log("LavaProjectile: Lifetime expired, returning to pool");
            ReturnToPool();
            return;
        }

        // Update movement along curve
        _sampleTime += Time.deltaTime * speed;
        _sampleTime = Mathf.Clamp01(_sampleTime);

        Vector3 position = Evaluate(_sampleTime);
        transform.position = position;

        // Calculate direction with adaptive look-ahead
        float lookAhead = Mathf.Max(0.001f, Mathf.Min(0.1f, (1f - _sampleTime) * 0.5f));
        Vector3 nextPosition = Evaluate(_sampleTime + lookAhead);

        // Debug visualization
        Debug.DrawLine(initialA, initialControl, Color.red, 0.1f);
        Debug.DrawLine(initialControl, initialB, Color.red, 0.1f);

        Vector3 direction = nextPosition - position;
        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.forward = direction.normalized;
        }

        // Check if projectile reached the end
        if (_sampleTime >= 1f)
        {
            Debug.Log("LavaProjectile: Reached end of path, returning to pool");
            ReturnToPool();
        }
    }

    private Vector3 Evaluate(float t)
    {
        t = Mathf.Clamp01(t);
        Vector3 ac = Vector3.Lerp(initialA, initialControl, t);
        Vector3 cb = Vector3.Lerp(initialControl, initialB, t);
        return Vector3.Lerp(ac, cb, t);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            other.gameObject.GetComponent<PlayerHealthComponent>().TakeDamage(10);
        }
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        isInitialized = false;
        gameObject.SetActive(false);

        // Try both pool systems for compatibility
        if (LavaProjectilePool.Instance != null)
        {
            LavaProjectilePool.Instance.ReturnToPool(gameObject);
        }
        else if (PoolManager.Instance != null)
        {
            PoolManager.Instance.ReturnToPool(PoolType.LavaProjectile, gameObject);
        }
    }

    // Public methods for setup
    public void SetQuadraticCurve(QuadraticCurve quadraticCurve)
    {
        this.quadraticCurve = quadraticCurve;
    }

    public void SetEnemyPosition(Vector3 enemyPos)
    {
        this.enemyPosition = enemyPos;
    }

    // Keep the old method name for backward compatibility
    public void SetenemyPosition(Vector3 enemyPos)
    {
        SetEnemyPosition(enemyPos);
    }

    // Method to properly initialize projectile when spawned
    public void Initialize(QuadraticCurve curve, Vector3 enemyPos, ProjectileEnemyType type)
    {
        SetQuadraticCurve(curve);
        SetEnemyPosition(enemyPos);
        projectileType = type;
    }
}