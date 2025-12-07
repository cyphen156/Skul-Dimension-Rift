using System;
using System.Collections.Generic;
using UnityEngine;

public class InfinityScroller : MonoBehaviour
{
    [Serializable]
    public enum ScrollMode
    {
        Horizontal,
        Vertical,
        Both,
        None
    }

    [Serializable]
    public class InfinityScroll
    {
        public GameObject scrollObject;
        public float weight = 0.0f;
        public bool isParallax = true;
        public ScrollMode scrollMode = ScrollMode.Horizontal;
    }

    [Header("Follow target")]
    public Transform follow;

    [Header("InfinityScrollTargets")]
    public List<InfinityScroll> scrollObjects = new List<InfinityScroll>();
    
    private readonly List<Transform[]> scroller = new List<Transform[]>();
    private readonly List<Vector2> sizes = new List<Vector2>();
    private readonly List<int> scrollIndex = new List<int>();

    private Vector3 lastFollowPosition;
    private bool isInitialized;

    private void Awake()
    {
        int count = scrollObjects.Count;

        for (int i = 0; i < count; i++)
        {
            InfinityScroll info = scrollObjects[i];

            if (info == null)
            {
                continue;
            }

            if (info.scrollObject == null)
            {
                continue;
            }

            if (info.scrollMode == ScrollMode.None)
            {
                continue;
            }

            SetupScrollEntry(i, info);
        }
    }

    private void Start()
    {
        if (follow == null)
        {
            Debug.LogWarning("[InfinityScroller] Follow Target 이 설정되지 않았습니다.");
            isInitialized = false;
            return;
        }

        lastFollowPosition = follow.position;
        isInitialized = true;
    }

    private void LateUpdate()
    {
        if (isInitialized == false)
        {
            return;
        }

        Vector3 current = follow.position;
        Vector3 delta = current - lastFollowPosition;

        if (delta.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        int activeCount = scroller.Count;

        for (int i = 0; i < activeCount; i++)
        {
            int idx = scrollIndex[i];
            InfinityScroll info = scrollObjects[idx];

            Transform[] segments = scroller[i];
            Vector2 size = sizes[i];

            if (segments == null)
            {
                continue;
            }

            // 패럴렉스
            if (info.isParallax == true && info.weight != 0.0f)
            {
                Vector3 move = delta * info.weight;

                segments[0].position += move;
                segments[1].position += move;
            }

            // 무한 스크롤
            ApplyInfinity(info, segments, size);
        }

        lastFollowPosition = current;
    }

    private void SetupScrollEntry(int index, InfinityScroll info)
    {
        Transform root = info.scrollObject.transform;

        Transform[] targets = new Transform[2];

        // 원본
        targets[0] = root;

        // 복제본 생성
        Transform copy = Instantiate(root, root.parent);
        copy.name = root.name + "_Copy";
        copy.gameObject.SetActive(true);
        targets[1] = copy;

        // 사이즈 계산 (첫 번째 세그먼트 기준)
        Renderer renderer = root.GetComponentInChildren<Renderer>();
        Vector2 size = Vector2.zero;

        if (renderer != null)
        {
            Vector3 bounds = renderer.bounds.size;
            size = new Vector2(bounds.x, bounds.y);
        }

        // 초기 배치 (옆으로 혹은 위로 한 칸)
        ArrangeInitial(info, targets, size);

        scroller.Add(targets);
        sizes.Add(size);
        scrollIndex.Add(index);
    }

    private void ArrangeInitial(InfinityScroll info, Transform[] segments, Vector2 size)
    {
        if (segments == null || segments.Length < 2)
        {
            return;
        }

        Transform a = segments[0];
        Transform b = segments[1];

        Vector3 posA = a.position;
        Vector3 posB = posA;

        if (info.scrollMode == ScrollMode.Horizontal || info.scrollMode == ScrollMode.Both)
        {
            posB.x += size.x;
        }

        if (info.scrollMode == ScrollMode.Vertical || info.scrollMode == ScrollMode.Both)
        {
            posB.y += size.y;
        }

        b.position = posB;
    }

    private void ApplyInfinity(InfinityScroll info, Transform[] segments, Vector2 size)
    {
        if (segments == null || segments.Length < 2)
        {
            return;
        }

        Transform a = segments[0];
        Transform b = segments[1];

        Vector3 posA = a.position;
        Vector3 posB = b.position;

        if (info.scrollMode == ScrollMode.Horizontal || info.scrollMode == ScrollMode.Both)
        {
            float width = size.x;

            if (posA.x + width < follow.position.x)
            {
                posA.x = posB.x + width;
            }
            else if (posB.x + width < follow.position.x)
            {
                posB.x = posA.x + width;
            }
        }

        if (info.scrollMode == ScrollMode.Vertical || info.scrollMode == ScrollMode.Both)
        {
            float height = size.y;

            if (posA.y + height < follow.position.y)
            {
                posA.y = posB.y + height;
            }
            else if (posB.y + height < follow.position.y)
            {
                posB.y = posA.y + height;
            }
        }

        a.position = posA;
        b.position = posB;
    }
}