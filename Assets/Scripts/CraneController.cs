using UnityEngine;

public class CraneController : MonoBehaviour
{
    [Header("Prefab to spawn")]
    public GameObject blockPrefab;

    [Header("Spawn settings")]
    public Transform spawnPoint;
    public float spawnHeightOffset = 0f;

    [Header("Control settings (before drop)")]
    public float moveSpeed = 4f;       // horizontal speed
    public float rotateSpeed = 120f;   // degrees per second
    public float xLimit = 6f;          // left-right boundary

    private GameObject currentBlock;
    private Rigidbody currentRb;
    private bool isDropped = false;

    void Start()
    {
        SpawnNewBlock();
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
            return;
            
        if (currentBlock == null) return;

        // Only allow control before dropping
        if (!isDropped)
        {
            HandleMove();
            HandleRotate();

            if (Input.GetKeyDown(KeyCode.Space))
            {
                Drop();
            }
        }
        else
        {
            // If the block is almost stopped, spawn next one (temporary rule)
            if (currentRb != null && currentRb.linearVelocity.magnitude < 0.05f)
            {
                // small delay-ish to avoid instant respawn on first contact
                // (simple hack: require it to be dropped for a short moment)
                // You can replace with a coroutine later.
                SpawnNewBlock();
            }
        }
    }

    void HandleMove()
    {
        float h = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  h = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h =  1f;

        Vector3 pos = currentBlock.transform.position;
        pos.x += h * moveSpeed * Time.deltaTime;
        pos.x = Mathf.Clamp(pos.x, -xLimit, xLimit);
        currentBlock.transform.position = pos;
    }

    void HandleRotate()
    {
        float r = 0f;
        if (Input.GetKey(KeyCode.Q)) r = 1f;
        if (Input.GetKey(KeyCode.E)) r = -1f;

        currentBlock.transform.Rotate(Vector3.up, r * rotateSpeed * Time.deltaTime, Space.World);
    }

    void Drop()
    {
        if (currentRb == null) return;

        // Enable physics
        currentRb.isKinematic = false;
        isDropped = true;
        GameManager.Instance.NextTurn();
    }

    void SpawnNewBlock()
    {
        // Clean up reference (do NOT destroy old one)
        currentBlock = null;
        currentRb = null;

        Vector3 p = spawnPoint != null ? spawnPoint.position : transform.position;
        p.y += spawnHeightOffset;

        GameObject block = Instantiate(blockPrefab, p, Quaternion.identity);

        // Ensure it has a Rigidbody but keep it kinematic until dropped
        Rigidbody rb = block.GetComponent<Rigidbody>();
        if (rb == null) rb = block.AddComponent<Rigidbody>();
        rb.isKinematic = true; // controlled by script until drop

        currentBlock = block;
        currentRb = rb;
        isDropped = false;
    }
}