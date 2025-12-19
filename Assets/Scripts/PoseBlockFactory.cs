using UnityEngine;
using System.Collections.Generic;

public class PoseBlockFactory : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public GameObject posePreviewPrefab;     // PosePreview.prefab
    public Transform posePreviewInScene;     // シーン上のPosePreview（動かしてるやつ）

    [Header("Physics")]
    public float mass = 2.0f;
    public float drag = 0.2f;
    public float angularDrag = 0.2f;

    public GameObject CreatePoseBlock(Vector3 position, Quaternion rotation)
    {
        // 1) ブロック生成
        var block = Instantiate(posePreviewPrefab, position, rotation);
        block.name = "PoseBlock";

        // 2) シーンのPosePreviewの姿勢（各TransformのlocalRotation）をコピー
        CopyLocalRotations(posePreviewInScene, block.transform);

        // 3) 物理を付与（最初はKinematicにしてクレーン操作できるようにする）
        var rb = block.GetComponent<Rigidbody>();
        if (rb == null) rb = block.AddComponent<Rigidbody>();
        rb.mass = mass;
        rb.linearDamping = drag;
        rb.angularDamping = angularDrag;
        rb.isKinematic = true;

        // 4) コライダー追加（まずは簡易で：全身を1つのBoxColliderにする）
        EnsureSingleBoxCollider(block);

        return block;
    }

    void CopyLocalRotations(Transform srcRoot, Transform dstRoot)
    {
        if (srcRoot == null || dstRoot == null) return;

        var srcList = new List<Transform>();
        var dstList = new List<Transform>();
        srcRoot.GetComponentsInChildren(true, srcList);
        dstRoot.GetComponentsInChildren(true, dstList);

        // 名前で対応付け（同じPrefabなら一致する）
        var dstMap = new Dictionary<string, Transform>();
        foreach (var t in dstList) dstMap[t.name] = t;

        foreach (var s in srcList)
        {
            if (dstMap.TryGetValue(s.name, out var d))
            {
                d.localRotation = s.localRotation;
            }
        }
    }

    void EnsureSingleBoxCollider(GameObject go)
    {
        // 既存のコライダーを消して簡易化（暴れ防止）
        foreach (var c in go.GetComponentsInChildren<Collider>())
        {
            Destroy(c);
        }

        // Renderer全体のBoundsから1つのBoxColliderを付ける
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

        var bc = go.AddComponent<BoxCollider>();
        bc.center = go.transform.InverseTransformPoint(bounds.center);
        bc.size = bounds.size;
    }
}