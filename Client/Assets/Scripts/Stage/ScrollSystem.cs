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
    /// 상태 플래그 베이킹용 열거형.
    /// 이 값들을 통해 비트 연산으로 플래그를 설정/해제/조회함.
    /// ScrollRuntimeEntry.Flags 필드에 저장됨.
    /// 최종적으로 ScrollObject의 세팅 값이 
    /// 65(-> 8비트마스킹 후 72)바이트에서 
    /// 64바이트로 CPU 캐시 정렬 최적화됨.
    /// </summary>
    [Flags]
    private enum EntryFlags : uint
    {
        None = 0,

        HasAutoScroll = 1u << 0,

        UseParallaxX = 1u << 1,
        UseParallaxY = 1u << 2,

        UseInfinityX = 1u << 3,
        UseInfinityY = 1u << 4,

        InfinityRefFollow = 1u << 5,
    }

    /// <summary>
    /// 런타임에서 계산에 쓰는 값 타입 엔트리.
    /// Transform 참조 + 스크롤 관련 파라미터
    /// </summary>
    private struct ScrollRuntimeEntry
    {
        public Transform Segment;        // 8
        public Vector3 Position;         // 12
        public Vector3 CenterPos;        // 12
        public Vector2 Size;             // 8
        public Vector2 AutoScrollSpeed;  // 8
        public float WeightX;            // 4
        public float WeightY;            // 4
        public EntryFlags Flags;         // 4 (uint)
    }

    [Header("Follow target")]
    [SerializeField] private Transform follow;

    [Header("Scroll Authoring Entries")]
    [SerializeField] private List<ScrollAuthoring> scrollObjects = new List<ScrollAuthoring>();

    private ScrollRuntimeEntry[] _entries = Array.Empty<ScrollRuntimeEntry>();

    private Vector3 lastFollowPosition;
    private bool isInitialized;

    private const float FollowDeltaSqrThreshold = 0.00001f;

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

            if (followDelta.sqrMagnitude > FollowDeltaSqrThreshold)
            {
                hasFollowDelta = true;
                lastFollowPosition = current;
            }
        }

        UpdateScrolling(followDelta, deltaTime, hasFollowDelta);
    }

    public void ShutdownSubSystem()
    {
        int count = _entries.Length;

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

        _entries = Array.Empty<ScrollRuntimeEntry>();
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
        int authoringCount = scrollObjects.Count;
        int totalEntryCount = 0;

        for (int i = 0; i < authoringCount; i++)
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

            totalEntryCount += GetSegmentCount(authoring.scrollMode);
        }

        _entries = new ScrollRuntimeEntry[totalEntryCount];

        int writeIndex = 0;

        for (int i = 0; i < authoringCount; i++)
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

            SetupAuthoring(authoring, ref writeIndex);
        }
    }

    private void SetupAuthoring(ScrollAuthoring authoring, ref int writeIndex)
    {
        Transform root = authoring.scrollObject.transform;

        int segmentCount = GetSegmentCount(authoring.scrollMode);
        Vector2 size = GetSize(root);

        WriteEntry(authoring, root, size, root, ref writeIndex);

        for (int i = 1; i < segmentCount; i++)
        {
            Transform clone = Instantiate(root, root.parent);
            clone.name = root.name + "_Seg" + i;
            clone.gameObject.SetActive(true);

            PositionClone(authoring.scrollMode, root, clone, size, i);

            WriteEntry(authoring, clone, size, root, ref writeIndex);
        }
    }

    private void WriteEntry(ScrollAuthoring authoring, Transform seg, Vector2 size, Transform root, ref int writeIndex)
    {
        Vector3 centerPos = Vector3.zero;

        if (root != null)
        {
            centerPos = root.position;
        }
        else if (seg != null)
        {
            centerPos = seg.position;
        }

        EntryFlags flags = EntryFlags.None;

        bool hasAutoScroll = authoring.useAutoScroll == true &&
                             authoring.autoScrollSpeed != Vector2.zero;

        if (hasAutoScroll == true)
        {
            flags |= EntryFlags.HasAutoScroll;
        }

        bool useParallaxX = authoring.parallaxMode == ParallaxMode.Horizontal ||
                            authoring.parallaxMode == ParallaxMode.Both;

        bool useParallaxY = authoring.parallaxMode == ParallaxMode.Vertical ||
                            authoring.parallaxMode == ParallaxMode.Both;

        if (useParallaxX == true && Mathf.Approximately(authoring.weightX, 0.0f) == false)
        {
            flags |= EntryFlags.UseParallaxX;
        }

        if (useParallaxY == true && Mathf.Approximately(authoring.weightY, 0.0f) == false)
        {
            flags |= EntryFlags.UseParallaxY;
        }

        bool useInfinityX = authoring.scrollMode == ScrollMode.Horizontal ||
                            authoring.scrollMode == ScrollMode.Both;

        bool useInfinityY = authoring.scrollMode == ScrollMode.Vertical ||
                            authoring.scrollMode == ScrollMode.Both;

        if (useInfinityX == true && size.x > 0.0f)
        {
            flags |= EntryFlags.UseInfinityX;
        }

        if (useInfinityY == true && size.y > 0.0f)
        {
            flags |= EntryFlags.UseInfinityY;
        }

        if ((flags & (EntryFlags.UseInfinityX | EntryFlags.UseInfinityY)) != 0 &&
            authoring.parallaxMode != ParallaxMode.None)
        {
            flags |= EntryFlags.InfinityRefFollow;
        }

        _entries[writeIndex] = new ScrollRuntimeEntry
        {
            Segment = seg,
            Position = seg != null ? seg.position : Vector3.zero,
            CenterPos = centerPos,
            Size = size,
            AutoScrollSpeed = authoring.autoScrollSpeed,
            WeightX = authoring.weightX,
            WeightY = authoring.weightY,
            Flags = flags
        };

        writeIndex++;
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
            if (index == 1)
            {
                pos.x += size.x;
            }
        }
        else if (mode == ScrollMode.Vertical)
        {
            if (index == 1)
            {
                pos.y += size.y;
            }
        }
        else if (mode == ScrollMode.Both)
        {
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

    private void UpdateScrolling(Vector3 followDelta, float deltaTime, bool hasFollowDelta)
    {
        int count = _entries.Length;

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
            ref ScrollRuntimeEntry entry = ref _entries[i];
            Transform seg = entry.Segment;

            if (seg == null)
            {
                continue;
            }

            EntryFlags flags = entry.Flags;

            Vector3 pos = entry.Position;

            if ((flags & EntryFlags.HasAutoScroll) != 0)
            {
                pos.x += entry.AutoScrollSpeed.x * deltaTime;
                pos.y += entry.AutoScrollSpeed.y * deltaTime;
            }

            if (hasFollowDelta == true &&
                (flags & (EntryFlags.UseParallaxX | EntryFlags.UseParallaxY)) != 0)
            {
                ApplyParallax(ref pos, entry, followDelta);
            }

            if ((flags & (EntryFlags.UseInfinityX | EntryFlags.UseInfinityY)) != 0)
            {
                ApplyInfinity(ref pos, entry, followPos);
            }

            entry.Position = pos;
            seg.position = pos;
        }
    }

    private void ApplyParallax(ref Vector3 pos, ScrollRuntimeEntry entry, Vector3 followDelta)
    {
        EntryFlags flags = entry.Flags;

        if ((flags & EntryFlags.UseParallaxX) != 0)
        {
            pos.x += followDelta.x * entry.WeightX;
        }

        if ((flags & EntryFlags.UseParallaxY) != 0)
        {
            pos.y += followDelta.y * entry.WeightY;
        }
    }

    private void ApplyInfinity(ref Vector3 pos, ScrollRuntimeEntry entry, Vector3 followPos)
    {
        EntryFlags flags = entry.Flags;

        Vector3 referencePos;

        if ((flags & EntryFlags.InfinityRefFollow) != 0 && follow != null)
        {
            referencePos = followPos;
        }
        else
        {
            referencePos = entry.CenterPos;
        }

        if ((flags & EntryFlags.UseInfinityX) != 0)
        {
            float width = entry.Size.x;
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

        if ((flags & EntryFlags.UseInfinityY) != 0)
        {
            float height = entry.Size.y;
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