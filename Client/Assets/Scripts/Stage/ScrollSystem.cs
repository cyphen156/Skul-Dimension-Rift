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

    [Serializable]
    public class InfinityScroll
    {
        public GameObject scrollObject;

        [Header("Parallax")]
        public bool isParallax = true;
        public ParallaxMode parallaxMode = ParallaxMode.None;
        public float weightX = 0.0f;
        public float weightY = 0.0f;

        [Header("Infinity Scroll")]
        public ScrollMode scrollMode = ScrollMode.None;

        [Header("Auto Scroll")]
        public bool useAutoScroll = false;
        public Vector2 autoScrollSpeed = Vector2.zero;

        [NonSerialized] public Transform root;
        [NonSerialized] public Transform[] segments;
        [NonSerialized] public Vector2 size;
    }

    [Header("Follow target")]
    [SerializeField] private Transform follow;

    [Header("Infinity Scroll Targets")]
    [SerializeField] private List<InfinityScroll> scrollObjects = new List<InfinityScroll>();

    private Vector3 lastFollowPosition;
    private bool isInitialized;

    private void Awake()
    {
        InitializeEntries();
    }

    private void Start()
    {
        if (follow == null)
        {
            Debug.LogWarning("[ScrollSystem] Follow target is not assigned.");
        }

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
        Vector3 delta = Vector3.zero;
        bool hasDelta = false;

        if (follow != null)
        {
            Vector3 current = follow.position;
            delta = current - lastFollowPosition;

            if (delta.sqrMagnitude > Mathf.Epsilon)
            {
                hasDelta = true;
                lastFollowPosition = current;
            }
        }

        UpdateScrolling(delta, deltaTime, hasDelta);
    }

    public void ShutdownSubSystem()
    {
        int count = scrollObjects.Count;

        for (int i = 0; i < count; i++)
        {
            InfinityScroll entry = scrollObjects[i];

            if (entry == null)
            {
                continue;
            }

            if (entry.segments == null)
            {
                continue;
            }

            int length = entry.segments.Length;

            for (int j = 1; j < length; j++)
            {
                Transform seg = entry.segments[j];

                if (seg == null)
                {
                    continue;
                }

                Destroy(seg.gameObject);
            }

            entry.segments = null;
        }

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

    private void InitializeEntries()
    {
        int count = scrollObjects.Count;

        for (int i = 0; i < count; i++)
        {
            InfinityScroll entry = scrollObjects[i];

            if (entry == null)
            {
                continue;
            }

            if (entry.scrollObject == null)
            {
                continue;
            }

            SetupEntry(entry);
        }
    }

    private void SetupEntry(InfinityScroll entry)
    {
        entry.root = entry.scrollObject.transform;

        int segmentCount = GetSegmentCount(entry.scrollMode);
        entry.segments = new Transform[segmentCount];

        entry.segments[0] = entry.root;

        for (int i = 1; i < segmentCount; i++)
        {
            Transform clone = UnityEngine.Object.Instantiate(entry.root, entry.root.parent);
            clone.name = entry.root.name + "_Seg" + i;
            clone.gameObject.SetActive(true);
            entry.segments[i] = clone;
        }

        GetSize(entry);
        ArrangeInitial(entry);
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

    private void GetSize(InfinityScroll entry)
    {
        if (entry.root == null)
        {
            entry.size = Vector2.zero;
            return;
        }

        Renderer renderer = entry.root.GetComponentInChildren<Renderer>();

        if (renderer != null)
        {
            Vector3 bounds = renderer.bounds.size;
            entry.size = new Vector2(bounds.x, bounds.y);
        }
        else
        {
            entry.size = Vector2.zero;
        }
    }

    private void ArrangeInitial(InfinityScroll entry)
    {
        if (entry.segments == null)
        {
            return;
        }

        int length = entry.segments.Length;

        if (length <= 1)
        {
            return;
        }

        Transform baseSeg = entry.segments[0];
        Vector3 basePos = baseSeg.position;

        if (entry.scrollMode == ScrollMode.Horizontal)
        {
            Transform s1 = entry.segments[1];
            Vector3 pos1 = basePos;
            pos1.x += entry.size.x;
            s1.position = pos1;
        }
        else if (entry.scrollMode == ScrollMode.Vertical)
        {
            Transform s1 = entry.segments[1];
            Vector3 pos1 = basePos;
            pos1.y += entry.size.y;
            s1.position = pos1;
        }
        else if (entry.scrollMode == ScrollMode.Both)
        {
            if (length < 4)
            {
                return;
            }

            Transform s0 = entry.segments[0];
            Transform s1 = entry.segments[1];
            Transform s2 = entry.segments[2];
            Transform s3 = entry.segments[3];

            Vector3 pos0 = basePos;

            Vector3 pos1 = basePos;
            pos1.x += entry.size.x;

            Vector3 pos2 = basePos;
            pos2.y += entry.size.y;

            Vector3 pos3 = basePos;
            pos3.x += entry.size.x;
            pos3.y += entry.size.y;

            s0.position = pos0;
            s1.position = pos1;
            s2.position = pos2;
            s3.position = pos3;
        }
    }

    private void UpdateScrolling(Vector3 delta, float deltaTime, bool hasDelta)
    {
        int count = scrollObjects.Count;

        for (int i = 0; i < count; i++)
        {
            InfinityScroll entry = scrollObjects[i];

            if (entry == null)
            {
                continue;
            }

            if (entry.segments == null)
            {
                continue;
            }

            int length = entry.segments.Length;

            if (length <= 0)
            {
                continue;
            }

            ApplyAutoScroll(entry, deltaTime);

            if (entry.useAutoScroll == false && hasDelta == true)
            {
                ApplyParallax(entry, delta);
            }

            if (entry.scrollMode != ScrollMode.None)
            {
                ApplyInfinity(entry);
            }
        }
    }

    private void ApplyAutoScroll(InfinityScroll entry, float deltaTime)
    {
        if (entry.useAutoScroll == false)
        {
            return;
        }

        if (entry.autoScrollSpeed == Vector2.zero)
        {
            return;
        }

        int length = entry.segments.Length;

        if (length <= 0)
        {
            return;
        }

        Vector3 move = new Vector3
        (
            entry.autoScrollSpeed.x * deltaTime,
            entry.autoScrollSpeed.y * deltaTime,
            0.0f
        );

        for (int i = 0; i < length; i++)
        {
            Transform seg = entry.segments[i];

            if (seg == null)
            {
                continue;
            }

            seg.position += move;
        }
    }

    private void ApplyParallax(InfinityScroll entry, Vector3 delta)
    {
        if (entry.isParallax == false)
        {
            return;
        }

        bool useHorizontal = entry.parallaxMode == ParallaxMode.Horizontal ||
                             entry.parallaxMode == ParallaxMode.Both;

        bool useVertical = entry.parallaxMode == ParallaxMode.Vertical ||
                           entry.parallaxMode == ParallaxMode.Both;

        float moveX = 0.0f;
        float moveY = 0.0f;

        if (useHorizontal == true && Mathf.Approximately(entry.weightX, 0.0f) == false)
        {
            moveX = delta.x * entry.weightX;
        }

        if (useVertical == true && Mathf.Approximately(entry.weightY, 0.0f) == false)
        {
            moveY = delta.y * entry.weightY;
        }

        if (Mathf.Approximately(moveX, 0.0f) == true &&
            Mathf.Approximately(moveY, 0.0f) == true)
        {
            return;
        }

        int length = entry.segments.Length;
        Vector3 move = new Vector3(moveX, moveY, 0.0f);

        for (int i = 0; i < length; i++)
        {
            Transform seg = entry.segments[i];

            if (seg == null)
            {
                continue;
            }

            seg.position += move;
        }
    }

    private void ApplyInfinity(InfinityScroll entry)
    {
        if (entry.scrollMode == ScrollMode.None)
        {
            return;
        }

        if (entry.segments == null)
        {
            return;
        }

        if (entry.size == Vector2.zero)
        {
            return;
        }

        if (follow == null)
        {
            return;
        }

        int length = entry.segments.Length;

        if (length <= 0)
        {
            return;
        }

        Vector3 followPos = follow.position;
        float width = entry.size.x;
        float height = entry.size.y;

        bool useHorizontal = entry.scrollMode == ScrollMode.Horizontal ||
                             entry.scrollMode == ScrollMode.Both;

        bool useVertical = entry.scrollMode == ScrollMode.Vertical ||
                           entry.scrollMode == ScrollMode.Both;

        for (int i = 0; i < length; i++)
        {
            Transform seg = entry.segments[i];

            if (seg == null)
            {
                continue;
            }

            Vector3 pos = seg.position;

            if (useHorizontal == true && width > 0.0f)
            {
                float dx = followPos.x - pos.x;

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
                float dy = followPos.y - pos.y;

                if (dy > height)
                {
                    pos.y += height * 2.0f;
                }
                else if (dy < -height)
                {
                    pos.y -= height * 2.0f;
                }
            }

            seg.position = pos;
        }
    }
}
