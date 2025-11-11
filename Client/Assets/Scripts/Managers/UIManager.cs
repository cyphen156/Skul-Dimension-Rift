using Assets.Scripts.Interface;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using static Types;

/// <summary>
/// UI 관리를 위한 싱글톤 매니저 클래스입니다.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    private Stack<IInteractive> uiInputStack = new Stack<IInteractive>();
    private IInteractive focusedUI;
    private Dictionary<string, GameObject> uiObjects = new Dictionary<string, GameObject>();
    private Dictionary<string, Canvas> canvases = new Dictionary<string, Canvas>();
    private GameObject UIProxyPrefab;   /// 풀링 대상이 될 수 있음 풀대상으로 지정시 현재의 Interactive 캔버스가 아닌 독립 캔버스 사용 권장

#if UNITY_EDITOR
    [SerializeField] List<GameObject> _uiInputStack = new List<GameObject>();
    [SerializeField] GameObject _focusedUI;
    [SerializeField] private List<string> uiObjectnames = new List<string>();
    [SerializeField] List<Canvas> _canvas = new List<Canvas>();
#endif

    #region Unity Methods
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        Initialize();
    }


    // Editor에서 스택과 포커스된 UI를 확인하기 위한 용도
#if UNITY_EDITOR
    private void Update()
    {
        _uiInputStack.Clear();
        foreach (var ui in uiInputStack)
        {
            if (ui is MonoBehaviour mb)
            {
                _uiInputStack.Add(mb.gameObject);
            }
        }
        _focusedUI = focusedUI is MonoBehaviour focusedMB ? focusedMB.gameObject : null;
    }
#endif
    #endregion Unity Methods

    #region Custom Methods
    /// <summary>
    /// 초기화
    /// </summary>
    private void Initialize()
    {
        canvases.Clear();
        Canvas[] canvasList = transform.GetComponentsInChildren<Canvas>();

        foreach (Canvas canvas in canvasList)
        {
            canvases[canvas.name] = canvas;
            foreach (Transform child in canvas.transform)
            {
                uiObjects.Add(child.name, child.gameObject);
                child.gameObject.SetActive(false);
            }

#if UNITY_EDITOR
            uiObjectnames.Clear();
            foreach (var key in uiObjects.Keys)
            {
                uiObjectnames.Add(key);
            }
            _canvas.Clear();
            foreach (var obj in canvases.Values)
            {
                _canvas.Add(obj);
            }
#endif
        }
        uiInputStack.Clear();
        focusedUI = null;

        UIProxyPrefab = ResourceManager.instance.GetGameObject("UIProxy");
    }

    private void SetUIFocus(IInteractive UIObject)
    {
        if (UIObject == null)
        {
            Debug.LogWarning("UIManager: Attempted to set focus to a null UIObject.");
            return;
        }
        
        if (focusedUI == UIObject)
        {
            return;
        }

        uiInputStack.Push(UIObject);
        focusedUI = UIObject;
    }

    private void RemoveUIFocus(IInteractive targetUIHandler)
    {
        if (focusedUI != null && focusedUI == targetUIHandler)
        {
            uiInputStack.Pop();
            focusedUI = uiInputStack.Count > 0 ? uiInputStack.Peek() : null;
        }
        else
        {
            Debug.LogWarning("UIManager: Attempted to remove focus from a UIObject that is not at the top of the stack.");
        }
    }

    /// <summary>
    /// UI를 활성화하는 메서드
    /// </summary>
    /// <param name="UIName"></param>
    public void Show(string UIName)
    {
        GameObject uiObject = uiObjects.ContainsKey(UIName) ? uiObjects[UIName] : null;

        if (uiObject != null)
        {
            uiObject.SetActive(true);

            IInteractive inputHandler = uiObject.GetComponent<IInteractive>();
            if (inputHandler != null)
            {
                SetUIFocus(inputHandler);
            }
        }
        else
        {
            Debug.LogWarning($"UIManager: UI '{UIName}' not found in dictionary.");
        }
    }
    /// <summary>
    /// UI를 비활성화하는 메서드
    /// </summary>
    /// <param name="UIName"></param>
    public void Hide(string UIName)
    {
        GameObject uiObject = uiObjects.ContainsKey(UIName) ? uiObjects[UIName] : null;
        
        if (uiObject != null)
        {
            if (!uiObject.activeInHierarchy)
            {
                return;
            }
            if (uiObject.TryGetComponent<IInteractive>(out IInteractive uiInputHandler))
            {
                if (focusedUI == uiInputHandler)
                {
                    RemoveUIFocus(uiInputHandler);
                }
            }

            uiObject.SetActive(false);

            // ****추후 풀 대상으로 변경될 수 있음*****
            if (uiObject.name.Contains("Proxy"))
            {
                uiObjects.Remove(UIName);
                Destroy(uiObject);
                foreach (Canvas canvas in canvases.Values)
                {
                    canvas.GetComponent<CanvasGroup>().interactable = true;
                }
            }
        }
        else
        {
            Debug.LogWarning($"UIManager: UI '{UIName}' not found in dictionary.");
        }
        
    }
        
    public void HideAll()
    {
        foreach (var ui in uiObjects.Values)
        {
            ui.SetActive(false);
        }
        uiInputStack.Clear();
        focusedUI = null;
    }

    /// <summary>
    /// 프록시 패턴을 통해 기존 함수를 우회하여 대신 기능을 수행해주는 함수
    /// </summary>
    /// <param name="UIOrigin">원본 타겟</param>
    public void UseProxy(InteractiveUIBehaviour UIOrigin)
    {
        if (UIOrigin == null)
        {
            return;
        }
        GameObject UIProxyObject = Instantiate(UIProxyPrefab);
        string name = UIOrigin.name + "Proxy";
        UIProxyObject.name = name;
        uiObjects.Add(name, UIProxyObject);
        if (!canvases.TryGetValue("ProxyCanvas", out Canvas parentCanvas))
        {
            parentCanvas = canvases.Values.First();
        }
        UIProxyObject.transform.SetParent(parentCanvas.transform, false);
        UIProxyObject.GetComponent<UIProxy>().Bind(UIOrigin);
        foreach (Canvas canvas in canvases.Values)
        {
            if (canvas == parentCanvas)
            {
                continue;
            }
            canvas.GetComponent<CanvasGroup>().interactable = true;
        }
        Show(name);
    }

    #endregion Custom Methods

    #region Input Methods
    public void Execute(InputAction.CallbackContext ctx)
    {
        // 현재 입력 처리를 위한 UI가 없으면 입력 무시
        if (focusedUI == null)
        {
            Debug.Log("There is No Activated UI\nPlayer Input has been Locked");
            return;
        }

        focusedUI.Execute(ctx);
    }

    /// <summary>
    /// ESC 키 입력 처리
    /// </summary>
    /// <param name="ctx"></param>
    public void Execute_Internal(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            return;
        }

        string name = ctx.action.name;

        GameState gameState = GameState.Paused;

        // 현재 입력 처리를 위한 UI가 없으면 메뉴 열기
        if (focusedUI == null)
        {
            Show(name);  
        }
        // 현재 입력 처리를 위한 UI가 있으면 해당 UI 닫기
        else
        {
            string target = ((MonoBehaviour)focusedUI).gameObject.name;
            Hide(target);

            // 만약 방금 닫은 UI가 마지막 상호작용 가능한 UI라면
            // 게임 상태를 Playing으로 변경
            if (focusedUI == null)
            {
                gameState = GameState.Playing;
            }
        }
        GameManager.instance.ChangeGameState(gameState);
    }

    /// <summary>
    /// 외부 요인으로 인한 상호작용 가능한 UI 요소의 변경이 있엇을 때 호출
    /// InteractiveUI를 갱신
    /// </summary>
    public void RefreshUI(string targetUI = null, string param = null)
    {
        if (string.IsNullOrEmpty(targetUI))
        {
            RefreshAll();
            return;
        }

        // 존재하지 않으면 무시
        if (!uiObjects.TryGetValue(targetUI, out var target))
        {
            return;
        }

        target.GetComponent<InteractiveUIBehaviour>()?.Refresh(param);
    }

    private void RefreshAll()
    {
        foreach (var ui in uiObjects.Values)
        {
            if (ui != null && ui.activeInHierarchy)
            {
                ui.GetComponent<InteractiveUIBehaviour>()?.Refresh();
            }
        }
    }

    public GameObject TryGetUI(string UIName)
    {
        return uiObjects.ContainsKey(UIName) ? uiObjects[UIName] : null;
    }
    #endregion Input Methods
}
