using System;
using System.Collections.Generic;
using Assets.Scripts.Interface;
using UnityEngine;

public class ScrollSystem : MonoBehaviour, ISubSystem
{
    [Serializable]
    public enum ScrollMode
    {
        None,
        Horizontal,
        Vertical,
        Both
    }

    [Serializable]
    public enum ParallaxMode
    {
        None,
        Horizontal,
        Vertical,
        Both
    }

    /// <summary>
    /// 인스펙터에서 세팅하는 저장용 데이터.
    /// </summary>
    [Serializable]
    public class ScrollAuthoring
    {
        public GameObject scrollObject;

        [Header("Settings")]

        [Header("Follow / Parallax")]
        public ParallaxMode parallaxMode = ParallaxMode.None;
        public float weightX = 0.0f;
        public float weightY = 0.0f;

        [Header("Infinity Scroll")]
        public ScrollMode scrollMode = ScrollMode.None;

        [Header("Auto Scroll")]
        public bool useAutoScroll = false;
        public Vector2 autoScrollSpeed = Vector2.zero;
    }

    /// <summary>
    /// 런타임에서 계산에 쓰는 값 타입 엔트리.
    /// Transform 참조 + 스크롤 관련 파라미터
    /// </summary>
    private struct ScrollRuntimeEntry
    {
        public Transform Segment;
        public Transform Root;
        public Vector2 Size;
        public Vector2 AutoScrollSpeed;
        public float WeightX;
        public float WeightY;
        public ScrollMode ScrollMode;
        public ParallaxMode ParallaxMode;
        public bool UseAutoScroll;
        public Vector3 CenterPos;
    }

    [Header("Follow target")]
    [SerializeField] private Transform follow;

    [Header("Scroll Authoring Entries")]
    [SerializeField] private List<ScrollAuthoring> scrollObjects = new List<ScrollAuthoring>();

    private readonly List<ScrollRuntimeEntry> _entries = new List<ScrollRuntimeEntry>();

    private Vector3 lastFollowPosition;
    private bool isInitialized;

    private void Awake()
    {
        BuildRuntimeEntries();
    }

    private void Start()
    {
        SetupFollow(follow);
        InitializeSubSystem();
    }

    private void LateUpdate()
    {
        TickSubSystem();
    }

    public void InitializeSubSystem()
    {
        isInitialized = true;

        if (follow != null)
        {
            lastFollowPosition = follow.position;
        }
    }

    public void TickSubSystem()
    {
        if (isInitialized == false)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        Vector3 followDelta = Vector3.zero;
        bool hasFollowDelta = false;

        if (follow != null)
        {
            Vector3 current = follow.position;
            followDelta = current - lastFollowPosition;

            if (followDelta.sqrMagnitude > Mathf.Epsilon)
            {
                hasFollowDelta = true;
                lastFollowPosition = current;
            }
        }

        UpdateScrolling(followDelta, deltaTime, hasFollowDelta);
    }

    public void ShutdownSubSystem()
    {
        int count = _entries.Count;

        for (int i = 0; i < count; i++)
        {
            Transform seg = _entries[i].Segment;

            if (seg == null)
            {
                continue;
            }

            if (seg.name.Contains("_Seg"))
            {
                Destroy(seg.gameObject);
            }
        }

        _entries.Clear();
        isInitialized = false;
    }

    public void SetupFollow(Transform target)
    {
        follow = target;

        if (follow != null)
        {
            lastFollowPosition = follow.position;
        }
    }

    private void BuildRuntimeEntries()
    {
        _entries.Clear();

        int count = scrollObjects.Count;

        for (int i = 0; i < count; i++)
        {
            ScrollAuthoring authoring = scrollObjects[i];

            if (authoring == null)
            {
                continue;
            }

            if (authoring.scrollObject == null)
            {
                continue;
            }

            SetupAuthoring(authoring);
        }
    }

    private void SetupAuthoring(ScrollAuthoring authoring)
    {
        Transform root = authoring.scrollObject.transform;

        // 세그먼트 개수 산정
        int segmentCount = GetSegmentCount(authoring.scrollMode);

        // 실제 렌더러 크기 얻기
        Vector2 size = GetSize(root);

        // 0번 세그먼트는 원본
        RegisterRuntimeEntry(authoring, root, size, 0);

        // 나머지 세그먼트는 클론 생성
        for (int i = 1; i < segmentCount; i++)
        {
            Transform clone = Instantiate(root, root.parent);
            clone.name = root.name + "_Seg" + i;
            clone.gameObject.SetActive(true);

            PositionClone(authoring.scrollMode, root, clone, size, i);

            RegisterRuntimeEntry(authoring, clone, size, i);
        }
    }

    private int GetSegmentCount(ScrollMode mode)
    {
        if (mode == ScrollMode.None)
        {
            return 1;
        }

        if (mode == ScrollMode.Horizontal)
        {
            return 2;
        }

        if (mode == ScrollMode.Vertical)
        {
            return 2;
        }

        if (mode == ScrollMode.Both)
        {
            return 4;
        }

        return 1;
    }

    private Vector2 GetSize(Transform root)
    {
        if (root == null)
        {
            return Vector2.zero;
        }

        Renderer renderer = root.GetComponentInChildren<Renderer>();

        if (renderer != null)
        {
            Vector3 bounds = renderer.bounds.size;
            return new Vector2(bounds.x, bounds.y);
        }

        return Vector2.zero;
    }

    private void PositionClone(ScrollMode mode, Transform baseSeg, Transform clone, Vector2 size, int index)
    {
        if (baseSeg == null || clone == null)
        {
            return;
        }

        Vector3 basePos = baseSeg.position;
        Vector3 pos = basePos;

        if (mode == ScrollMode.Horizontal)
        {
            // [0][1]
            if (index == 1)
            {
                pos.x += size.x;
            }
        }
        else if (mode == ScrollMode.Vertical)
        {
            // [1]
            // [0]
            if (index == 1)
            {
                pos.y += size.y;
            }
        }
        else if (mode == ScrollMode.Both)
        {
            //  [2][3]
            //  [0][1]
            if (index == 1)
            {
                pos.x += size.x;
            }
            else if (index == 2)
            {
                pos.y += size.y;
            }
            else if (index == 3)
            {
                pos.x += size.x;
                pos.y += size.y;
            }
        }

        clone.position = pos;
    }

    private void RegisterRuntimeEntry(ScrollAuthoring authoring, Transform seg, Vector2 size, int indexInStrip)
    {
        Transform root = authoring.scrollObject != null
            ? authoring.scrollObject.transform
            : seg;

        ScrollRuntimeEntry entry = new ScrollRuntimeEntry
        {
            Segment = seg,
            Root = root,
            Size = size,
            AutoScrollSpeed = authoring.autoScrollSpeed,
            WeightX = authoring.weightX,
            WeightY = authoring.weightY,
            ScrollMode = authoring.scrollMode,
            ParallaxMode = authoring.parallaxMode,
            UseAutoScroll = authoring.useAutoScroll,
            CenterPos = root.position
        };

        _entries.Add(entry);
    }


    private void UpdateScrolling(Vector3 followDelta, float deltaTime, bool hasFollowDelta)
    {
        int count = _entries.Count;

        if (count <= 0)
        {
            return;
        }

        Vector3 followPos = Vector3.zero;

        if (follow != null)
        {
            followPos = follow.position;
        }

        for (int i = 0; i < count; i++)
        {
            ScrollRuntimeEntry entry = _entries[i];
            Transform seg = entry.Segment;

            if (seg == null)
            {
                continue;
            }

            Vector3 pos = seg.position;

            // 1) Auto Scroll (항상 독립적으로 적용)
            if (entry.UseAutoScroll == true &&
                entry.AutoScrollSpeed != Vector2.zero)
            {
                pos.x += entry.AutoScrollSpeed.x * deltaTime;
                pos.y += entry.AutoScrollSpeed.y * deltaTime;
            }

            // 2) Parallax (followDelta가 있을 때만)
            if (hasFollowDelta == true)
            {
                ApplyParallax(ref pos, entry, followDelta);
            }

            // 3) Infinity Scroll
            if (entry.ScrollMode != ScrollMode.None &&
                entry.Size != Vector2.zero)
            {
                ApplyInfinity(ref pos, entry, followPos);
            }

            seg.position = pos;
        }
    }

    private void ApplyParallax(ref Vector3 pos, ScrollRuntimeEntry entry, Vector3 followDelta)
    {
        if (follow == null)
        {
            return;
        }

        if (entry.ParallaxMode == ParallaxMode.None)
        {
            return;
        }

        bool useHorizontal =
            entry.ParallaxMode == ParallaxMode.Horizontal ||
            entry.ParallaxMode == ParallaxMode.Both;

        bool useVertical =
            entry.ParallaxMode == ParallaxMode.Vertical ||
            entry.ParallaxMode == ParallaxMode.Both;

        float moveX = 0.0f;
        float moveY = 0.0f;

        if (useHorizontal == true &&
            Mathf.Approximately(entry.WeightX, 0.0f) == false)
        {
            moveX = followDelta.x * entry.WeightX;
        }

        if (useVertical == true &&
            Mathf.Approximately(entry.WeightY, 0.0f) == false)
        {
            moveY = followDelta.y * entry.WeightY;
        }

        if (Mathf.Approximately(moveX, 0.0f) == true &&
            Mathf.Approximately(moveY, 0.0f) == true)
        {
            return;
        }

        pos.x += moveX;
        pos.y += moveY;
    }

    private void ApplyInfinity(ref Vector3 pos, ScrollRuntimeEntry entry, Vector3 followPos)
    {
        float width = entry.Size.x;
        float height = entry.Size.y;

        bool useHorizontal =
            entry.ScrollMode == ScrollMode.Horizontal ||
            entry.ScrollMode == ScrollMode.Both;

        bool useVertical =
            entry.ScrollMode == ScrollMode.Vertical ||
            entry.ScrollMode == ScrollMode.Both;

        if (width <= 0.0f && height <= 0.0f)
        {
            return;
        }

        Vector3 referencePos;

        // 패럴랙스를 사용하는 배경이라면 카메라 기준 래핑
        if (follow != null &&
            entry.ParallaxMode != ParallaxMode.None)
        {
            referencePos = followPos;
        }
        else
        {
            // 그렇지 않은 배경은 고정 기준점(CenterPos) 기준 래핑
            referencePos = entry.CenterPos;
        }

        if (useHorizontal == true && width > 0.0f)
        {
            float dx = referencePos.x - pos.x;

            if (dx > width)
            {
                pos.x += width * 2.0f;
            }
            else if (dx < -width)
            {
                pos.x -= width * 2.0f;
            }
        }

        if (useVertical == true && height > 0.0f)
        {
            float dy = referencePos.y - pos.y;

            if (dy > height)
            {
                pos.y += height * 2.0f;
            }
            else if (dy < -height)
            {
                pos.y -= height * 2.0f;
            }
        }
    }
}
