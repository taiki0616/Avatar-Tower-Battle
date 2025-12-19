using System.Collections;
using UnityEngine;

public class CraneController : MonoBehaviour
{
    public PoseBlockFactory poseBlockFactory;

    [Header("Prefab to spawn (fallback if factory is not set)")]
    public GameObject blockPrefab;

    [Header("Spawn timing")]
    public float nextSpawnDelay = 0.7f; // 次のブロックまでの待ち時間

    [Header("Spawn settings")]
    public Transform spawnPoint;
    public float spawnHeightOffset = 0f;

    [Header("Control settings (before drop)")]
    public float moveSpeed = 4f;
    public float rotateSpeed = 120f;
    public float xLimit = 6f;
    public float zLimit = 6f;

    [Header("Stop detection")]
    public float stopSpeedThreshold = 0.05f;

    private GameObject currentBlock;
    private Rigidbody currentRb;
    private bool isDropped = false;

    // ★重要：静止判定中にコルーチンを連打しないためのフラグ
    private bool isSpawningNext = false;

    void Start()
    {
        SpawnNewBlock();
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
            return;

        if (currentBlock == null) return;

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
            // 落下後：ブロックがほぼ静止したら、一定時間待って次を出す
            if (!isSpawningNext && currentRb != null && currentRb.linearVelocity.magnitude < stopSpeedThreshold)
            {
                isSpawningNext = true;
                StartCoroutine(SpawnWithDelay());
            }
        }
    }

    void HandleMove()
    {
        float x = 0f;
        float z = 0f;

        // 左右（X）
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  x = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x =  1f;

        // 前後（Z）
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    z =  1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  z = -1f;

        Vector3 delta = new Vector3(x, 0f, z);
        if (delta.sqrMagnitude > 1f) delta.Normalize(); // 斜め移動が速くならないように

        Vector3 pos = currentBlock.transform.position;
        pos += delta * moveSpeed * Time.deltaTime;

        pos.x = Mathf.Clamp(pos.x, -xLimit, xLimit);
        pos.z = Mathf.Clamp(pos.z, -zLimit, zLimit);

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

        currentRb.isKinematic = false;
        isDropped = true;

        if (GameManager.Instance != null)
            GameManager.Instance.NextTurn();
    }

    IEnumerator SpawnWithDelay()
    {
        yield return new WaitForSeconds(nextSpawnDelay);
        SpawnNewBlock();
    }

    void SpawnNewBlock()
    {
        // 参照をリセット
        currentBlock = null;
        currentRb = null;

        Vector3 p = spawnPoint != null ? spawnPoint.position : transform.position;
        p.y += spawnHeightOffset;

        GameObject block = null;

        if (poseBlockFactory != null)
        {
            block = poseBlockFactory.CreatePoseBlock(p, Quaternion.identity);
        }
        else if (blockPrefab != null)
        {
            block = Instantiate(blockPrefab, p, Quaternion.identity);
        }

        if (block == null)
        {
            Debug.LogError("No block source set. Assign PoseBlockFactory or blockPrefab.");
            return;
        }

        Rigidbody rb = block.GetComponent<Rigidbody>();
        if (rb == null) rb = block.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        currentBlock = block;
        currentRb = rb;

        isDropped = false;
        isSpawningNext = false; // ★次の生成が終わったのでフラグ解除
    }
}