using UnityEngine;
using System.Collections.Generic;

public class PoseBlockFactory : MonoBehaviour
{
    public enum ColliderMode
    {
        SingleBox,
        HumanoidCapsules,
        KeepExisting
    }

    [Header("Collider")]
    public ColliderMode colliderMode = ColliderMode.HumanoidCapsules;

    [Tooltip("Capsule radius scale relative to bone length. 0.18~0.30 is a good range.")]
    [Range(0.05f, 0.5f)]
    public float capsuleRadiusScale = 0.22f;

    [Tooltip("If true, colliders are created under a child object named 'Colliders'.")]
    public bool createCollidersUnderChild = true;

    [Header("Assign in Inspector")]
    public GameObject posePreviewPrefab;     // PosePreview.prefab
    public Transform posePreviewInScene;     // シーン上のPosePreview（動かしてるやつ）

    [Header("Physics")]
    public float mass = 2.0f;
    public float drag = 0.2f;
    public float angularDrag = 0.2f;

    [Header("Collider material (optional)")]
    [Tooltip("Optional PhysicMaterial to reduce bouncing / sliding. If null, a sensible runtime material is created.")]
    public PhysicsMaterial colliderMaterial;

    public GameObject CreatePoseBlock(Vector3 position, Quaternion rotation)
    {
        var block = Instantiate(posePreviewPrefab, position, rotation);
        block.name = "PoseBlock";

        // ★ サイズ決定
        float scale = GetRandomScale();
        block.transform.localScale = Vector3.one * scale;

        // ポーズコピー
        CopyLocalRotations(posePreviewInScene, block.transform);

        // Rigidbody
        var rb = block.GetComponent<Rigidbody>();
        if (rb == null) rb = block.AddComponent<Rigidbody>();

        // ★ 質量は体積（scale^3）に比例させる
        rb.mass = mass * scale * scale * scale;
        rb.linearDamping = drag;
        rb.angularDamping = angularDrag;

        //rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        //rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.isKinematic = true;

        EnsureColliders(block);

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

    void EnsureColliders(GameObject go)
    {
        if (go == null) return;

        if (colliderMode == ColliderMode.KeepExisting)
        {
            // Prefab側にコライダーがあるならそのまま使う
            return;
        }

        // 既存のコライダーを消して簡易化（暴れ防止）
        foreach (var c in go.GetComponentsInChildren<Collider>(true))
        {
            Destroy(c);
        }

        if (colliderMode == ColliderMode.HumanoidCapsules)
        {
            // まずはボーンベースでカプセルを配置。失敗したらBoxにフォールバック
            if (!TryCreateHumanoidCapsules(go))
            {
                EnsureSingleBoxColliderRuntime(go);
            }
        }
        else
        {
            EnsureSingleBoxColliderRuntime(go);
        }
    }

    void EnsureSingleBoxColliderRuntime(GameObject go)
    {
        // Renderer全体のBoundsから1つのBoxColliderを付ける（最終手段）
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

        var bc = go.AddComponent<BoxCollider>();
        bc.material = GetOrCreateRuntimeMaterial();
        bc.center = go.transform.InverseTransformPoint(bounds.center);
        bc.size = bounds.size;
    }

    float GetRandomScale()
    {
        if (!randomizeSize)
            return smallScale;

        int r = Random.Range(0, 3); // 0,1,2

        switch (r)
        {
            case 0: return smallScale;   // 小
            case 1: return mediumScale;  // 中
            case 2: return largeScale;   // 大
            default: return smallScale;
        }
    }

    bool TryCreateHumanoidCapsules(GameObject go)
    {
        // ボーン名は今のRig（B-hips, B-spine...）に寄せている。
        // 名前が違うモデルだと失敗するので、そのときはBoxColliderにフォールバックする。

        Transform root = go.transform;

        Transform hips   = FindFirstByNames(root, "B-hips", "Hips", "hips");
        Transform spine  = FindFirstByNames(root, "B-spine", "Spine", "spine");
        Transform chest  = FindFirstByNames(root, "B-chest", "Chest", "chest");
        Transform neck   = FindFirstByNames(root, "B-neck", "Neck", "neck");
        Transform headB  = FindFirstByNames(root, "B-head", "Head", "head");

        Transform shL = FindFirstByNames(root, "B-shoulder.L", "B-shoulder_L", "Shoulder.L", "LeftShoulder");
        Transform shR = FindFirstByNames(root, "B-shoulder.R", "B-shoulder_R", "Shoulder.R", "RightShoulder");

        // Arms (try many common naming conventions)
        Transform upArmL  = FindFirstByNames(root, "B-upperArm.L", "B-upper_arm.L", "B-upperarm.L", "UpperArm.L", "LeftUpperArm", "B-arm.L");
        Transform lowArmL = FindFirstByNames(root, "B-foreArm.L", "B-forearm.L", "B-lowerArm.L", "LowerArm.L", "LeftLowerArm", "B-elbow.L");
        Transform handL   = FindFirstByNames(root, "B-hand.L", "Hand.L", "LeftHand");

        Transform upArmR  = FindFirstByNames(root, "B-upperArm.R", "B-upper_arm.R", "B-upperarm.R", "UpperArm.R", "RightUpperArm", "B-arm.R");
        Transform lowArmR = FindFirstByNames(root, "B-foreArm.R", "B-forearm.R", "B-lowerArm.R", "LowerArm.R", "RightLowerArm", "B-elbow.R");
        Transform handR   = FindFirstByNames(root, "B-hand.R", "Hand.R", "RightHand");

        Transform thL = FindFirstByNames(root, "B-thigh.L", "Thigh.L", "LeftUpperLeg", "UpperLeg.L");
        Transform shnL = FindFirstByNames(root, "B-shin.L", "Shin.L", "LeftLowerLeg", "LowerLeg.L");
        Transform ftL = FindFirstByNames(root, "B-foot.L", "Foot.L", "LeftFoot");

        Transform thR = FindFirstByNames(root, "B-thigh.R", "Thigh.R", "RightUpperLeg", "UpperLeg.R");
        Transform shnR = FindFirstByNames(root, "B-shin.R", "Shin.R", "RightLowerLeg", "LowerLeg.R");
        Transform ftR = FindFirstByNames(root, "B-foot.R", "Foot.R", "RightFoot");

        // 必須ボーンがなければ失敗
        if (hips == null || spine == null || thL == null || shnL == null || thR == null || shnR == null)
            return false;

        // Parent colliders under hips so they follow pose changes
        Transform collRoot = hips;
        if (createCollidersUnderChild)
        {
            var existing = hips.Find("Colliders");
            if (existing != null) Destroy(existing.gameObject);

            var child = new GameObject("Colliders");
            child.transform.SetParent(hips, false);
            collRoot = child.transform;
        }

        // Torso (make a chain so the upper body has reliable ground contact)
        CreateCapsuleBetween(collRoot, hips, spine, "col_pelvis");

        if (chest != null)
        {
            CreateCapsuleBetween(collRoot, spine, chest, "col_spine");

            if (neck != null)
            {
                CreateCapsuleBetween(collRoot, chest, neck, "col_chest");
            }
        }
        else
        {
            // If no chest bone, at least cover hips->spine as the torso
            // (already created as col_pelvis)
        }

        // If neck/head exists, add a short upper-torso/neck capsule to prevent "no upper body" contact.
        if (neck != null)
        {
            var upper = (chest != null ? chest : spine);
            CreateCapsuleBetween(collRoot, upper, neck, "col_neck");
        }

        // Head (prefer head bone if exists, else approximate from neck)
        {
            Vector3 headPos;
            if (headB != null)
            {
                headPos = headB.position;
            }
            else if (neck != null)
            {
                headPos = neck.position + (neck.up * 0.12f);
            }
            else
            {
                headPos = (chest != null ? chest.position : go.transform.position) + Vector3.up * 0.35f;
            }

            var head = new GameObject("col_head");
            head.transform.SetParent(collRoot, false);
            head.transform.localRotation = Quaternion.identity;
            var sc = head.AddComponent<SphereCollider>();
            sc.radius = 0.14f;
            sc.material = GetOrCreateRuntimeMaterial();

            // Follow head/neck so the head collider moves with pose updates.
            var sf = head.AddComponent<BoneSphereFollower>();
            sf.target = (headB != null ? headB : neck);
            sf.sphere = sc;
            sf.Refresh();
        }

        // Legs
        CreateCapsuleBetween(collRoot, thL, shnL, "col_thigh_L");
        if (ftL != null) CreateCapsuleBetween(collRoot, shnL, ftL, "col_shin_L");

        CreateCapsuleBetween(collRoot, thR, shnR, "col_thigh_R");
        if (ftR != null) CreateCapsuleBetween(collRoot, shnR, ftR, "col_shin_R");

        // Arms (create as much as the rig provides)
        if (shL != null && chest != null) CreateCapsuleBetween(collRoot, chest, shL, "col_shoulder_L");
        if (shR != null && chest != null) CreateCapsuleBetween(collRoot, chest, shR, "col_shoulder_R");

        // Left arm chain
        if (shL != null)
        {
            if (upArmL != null) CreateCapsuleBetween(collRoot, shL, upArmL, "col_upperArm_L");
            if (upArmL != null && lowArmL != null) CreateCapsuleBetween(collRoot, upArmL, lowArmL, "col_foreArm_L");
            else if (lowArmL != null) CreateCapsuleBetween(collRoot, shL, lowArmL, "col_foreArm_L");

            if (lowArmL != null && handL != null) CreateCapsuleBetween(collRoot, lowArmL, handL, "col_hand_L");
        }

        // Right arm chain
        if (shR != null)
        {
            if (upArmR != null) CreateCapsuleBetween(collRoot, shR, upArmR, "col_upperArm_R");
            if (upArmR != null && lowArmR != null) CreateCapsuleBetween(collRoot, upArmR, lowArmR, "col_foreArm_R");
            else if (lowArmR != null) CreateCapsuleBetween(collRoot, shR, lowArmR, "col_foreArm_R");

            if (lowArmR != null && handR != null) CreateCapsuleBetween(collRoot, lowArmR, handR, "col_hand_R");
        }

        return true;
    }

    Transform FindByName(Transform root, string name)
    {
        if (root == null) return null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name) return t;
        }
        return null;
    }

    Transform FindFirstByNames(Transform root, params string[] names)
    {
        if (root == null || names == null) return null;
        foreach (var n in names)
        {
            if (string.IsNullOrEmpty(n)) continue;
            var t = FindByName(root, n);
            if (t != null) return t;
        }
        return null;
    }

    PhysicsMaterial GetOrCreateRuntimeMaterial()
    {
        if (colliderMaterial != null) return colliderMaterial;

        // Create a runtime material to avoid "mystery bounciness".
        var pm = new PhysicsMaterial("PoseBlock_Runtime")
        {
            dynamicFriction = 0.8f,
            staticFriction = 0.9f,
            bounciness = 0.0f,
            frictionCombine = PhysicsMaterialCombine.Maximum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };
        return pm;
    }

    void CreateCapsuleBetween(Transform parent, Transform a, Transform b, string name)
    {
        if (a == null || b == null || parent == null) return;

        // Create collider object under the collider root (usually hips/Colliders)
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var cap = go.AddComponent<CapsuleCollider>();
        cap.material = GetOrCreateRuntimeMaterial();

        // Follow bones every frame so colliders track pose changes.
        var follower = go.AddComponent<BoneCapsuleFollower>();
        follower.a = a;
        follower.b = b;
        follower.cap = cap;
        follower.radiusScale = capsuleRadiusScale;
        follower.minRadius = 0.05f;
        follower.maxRadius = 0.30f;

        // Force an initial update immediately
        follower.Refresh();
    }

    public static void BakeAndDisableColliderFollowers(GameObject poseBlock)
    {
        if (poseBlock == null) return;

        foreach (var f in poseBlock.GetComponentsInChildren<BoneCapsuleFollower>(true))
        {
            if (f == null) continue;
            f.Refresh();
            f.enabled = false;
        }

        foreach (var f in poseBlock.GetComponentsInChildren<BoneSphereFollower>(true))
        {
            if (f == null) continue;
            f.Refresh();
            f.enabled = false;
        }
    }

    public enum AvatarSize
    {
        Small,
        Medium,
        Large
    }

    [Header("Avatar Size")]
    public bool randomizeSize = true;

    public float smallScale  = 1.0f;
    public float mediumScale = 5.0f;
    public float largeScale  = 10.0f;
}

// Updates a CapsuleCollider GameObject to stay between two bones every frame.
public class BoneCapsuleFollower : MonoBehaviour
{
    public Transform a;
    public Transform b;
    public CapsuleCollider cap;
    public float radiusScale = 0.22f;
    public float minRadius = 0.05f;
    public float maxRadius = 0.30f;

    public void Refresh()
    {
        if (a == null || b == null || cap == null) return;

        // Work in parent's local space so the follower tracks bone motion correctly.
        var parent = transform.parent;
        if (parent == null) return;

        Vector3 localA = parent.InverseTransformPoint(a.position);
        Vector3 localB = parent.InverseTransformPoint(b.position);

        Vector3 dir = (localB - localA);
        float len = dir.magnitude;
        if (len < 0.0005f) return;

        Vector3 mid = (localA + localB) * 0.5f;
        Vector3 up = dir / len;

        transform.localPosition = mid;
        transform.localRotation = Quaternion.FromToRotation(Vector3.up, up);

        float radius = Mathf.Clamp(len * radiusScale, minRadius, maxRadius);
        cap.direction = 1; // Y
        cap.radius = radius;
        cap.height = Mathf.Max(len + radius * 2f, radius * 2f);
        cap.center = Vector3.zero;
    }

    void LateUpdate()
    {
        Refresh();
    }
}

// Updates a SphereCollider GameObject to stay at a bone every frame.
public class BoneSphereFollower : MonoBehaviour
{
    public Transform target;
    public SphereCollider sphere;

    public void Refresh()
    {
        if (target == null || sphere == null) return;
        var parent = transform.parent;
        if (parent == null) return;

        Vector3 local = parent.InverseTransformPoint(target.position);
        transform.localPosition = local;
        transform.localRotation = Quaternion.identity;
    }

    void LateUpdate()
    {
        Refresh();
    }
}