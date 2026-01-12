using UnityEngine;
using System.Collections;

public class CraneController : MonoBehaviour
{
    public PoseBlockFactory poseBlockFactory;

    [Header("Prefab to spawn")]
    public GameObject blockPrefab;

    [Header("Spawn timing")]
    public float nextSpawnDelay = 0.7f;

    [Header("Spawn settings")]
    public Transform spawnPoint;
    public float spawnHeightOffset = 0f;

    [Header("Control settings (before drop)")]
    public float moveSpeed = 4f;
    public float rotateSpeed = 120f;
    public float xLimit = 6f;
    public float zLimit = 6f;

    private GameObject currentBlock;
    private Rigidbody currentRb;
    private bool isDropped = false;
    private bool spawnScheduled = false; // ★追加：連続でSpawnWithDelayが走らないように

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
            HandleMoveByArrowKeys();   // 位置移動は矢印キーのみ
            HandleRotate();

            if (Input.GetKeyDown(KeyCode.Space))
            {
                Drop();
            }
        }
        else
        {
            // 安全に velocity を使う（Unity互換）
            if (!spawnScheduled && currentRb != null && currentRb.linearVelocity.magnitude < 0.05f)
            {
                spawnScheduled = true;
                StartCoroutine(SpawnWithDelay());
            }
        }
    }

    // 矢印キーだけで移動
    void HandleMoveByArrowKeys()
    {
        float x = 0f;
        float z = 0f;

        if (Input.GetKey(KeyCode.LeftArrow))  x = -1f;
        if (Input.GetKey(KeyCode.RightArrow)) x =  1f;

        if (Input.GetKey(KeyCode.UpArrow))    z =  1f;
        if (Input.GetKey(KeyCode.DownArrow))  z = -1f;

        Vector3 delta = new Vector3(x, 0f, z);
        if (delta.sqrMagnitude > 1f) delta.Normalize();

        Vector3 pos = currentBlock.transform.position;
        pos += delta * moveSpeed * Time.deltaTime;

        pos.x = Mathf.Clamp(pos.x, -xLimit, xLimit);
        pos.z = Mathf.Clamp(pos.z, -zLimit, zLimit);

        currentBlock.transform.position = pos;
    }
    // ★ UIから呼ぶ用：ブロックを平行移動
    public void MoveBlock(float x, float z)
    {
        if (currentBlock == null || isDropped) return;

        Vector3 delta = new Vector3(x, 0f, z);
        if (delta.sqrMagnitude > 1f) delta.Normalize();

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
        if (currentRb == null || currentBlock == null) return;

        // 1) 見た目のポーズ更新を止める（今あるやつ）
        FreezePoseUpdates(currentBlock);

        // 2) ★追従コライダーを「今の姿勢で固定」して追従停止
        PoseBlockFactory.BakeAndDisableColliderFollowers(currentBlock);

        // 3) ★落下瞬間の “余計な速度” を消す
        currentRb.linearVelocity = Vector3.zero;     // Unity6なら linearVelocity
        currentRb.angularVelocity = Vector3.zero;

        // 4) ★Transform→Physics を同期してから dynamic 化
        Physics.SyncTransforms();

        currentRb.isKinematic = false;
        isDropped = true;

        if (GameManager.Instance != null)
            GameManager.Instance.NextTurn();
    }

    // ★追加：落下後、そのブロックが二度とポーズ更新されないようにする
    void FreezePoseUpdates(GameObject block)
    {
        if (block == null) return;

        // 1) KeyboardPosePreview がブロック側に付いている場合は止める
        var previews = block.GetComponentsInChildren<KeyboardPosePreview>(true);
        foreach (var p in previews)
            p.enabled = false;

        // 2) Animator が動いていて骨が追従してる場合は止める（これが効くことが多い）
        var animators = block.GetComponentsInChildren<Animator>(true);
        foreach (var a in animators)
            a.enabled = false;
    }

    void SpawnNewBlock()
    {
        currentBlock = null;
        currentRb = null;
        spawnScheduled = false;

        Vector3 p = spawnPoint != null ? spawnPoint.position : transform.position;
        p.y += spawnHeightOffset;

        GameObject block;
        if (poseBlockFactory != null)
        {
            block = poseBlockFactory.CreatePoseBlock(p, Quaternion.identity);
        }
        else
        {
            block = Instantiate(blockPrefab, p, Quaternion.identity);
        }

        Rigidbody rb = block.GetComponent<Rigidbody>();
        if (rb == null) rb = block.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        currentBlock = block;
        currentRb = rb;
        isDropped = false;
    }

    System.Collections.IEnumerator SpawnWithDelay()
    {
        yield return new WaitForSeconds(nextSpawnDelay);
        SpawnNewBlock();
    }

    // ===== UI Button wrappers (no-arg) =====
    public void MoveLeft()  { MoveBlock(-1f,  0f); }
    public void MoveRight() { MoveBlock( 1f,  0f); }
    public void MoveUp()    { MoveBlock( 0f,  1f); }
    public void MoveDown()  { MoveBlock( 0f, -1f); }

}