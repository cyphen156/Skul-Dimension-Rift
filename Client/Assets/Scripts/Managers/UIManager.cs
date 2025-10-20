using Assets.Scripts.Interface;
using System.Collections.Generic;
using System.Xml.Linq;
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

#if UNITY_EDITOR
    [SerializeField] List<GameObject> _uiInputStack = new List<GameObject>();
    [SerializeField] GameObject _focusedUI;
    [SerializeField] private List<string> uiObjectnames = new List<string>();
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
        foreach (Transform child in transform)
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
#endif
        uiInputStack.Clear();
        focusedUI = null;
    }

    private void SetUIFocus(IInteractive UIObject)
    {
        Debug.Log($"[SetUIFocus] Push {((MonoBehaviour)UIObject).name}");

        if (UIObject != null)
        {
            uiInputStack.Push(UIObject);
            focusedUI = UIObject;
        }
        else
        {
            Debug.LogWarning("UIManager: Attempted to set focus to a null UIObject.");
        }
    }

    private void RemoveUIFocus(IInteractive targetUIHandler)
    {
        if (focusedUI != null && focusedUI == targetUIHandler)
        {
            Debug.Log($"[RemoveUIFocus] Pop {((MonoBehaviour)focusedUI).name}");
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
                Debug.Log($"{uiObject} has been Already Disabled");
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

        if (ctx.performed)
        {
            focusedUI.Execute(ctx);
        }
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
    public void RefreshUI(GameObject targetUI = null)
    {
        if (targetUI != null && !uiObjects.ContainsKey(targetUI.name))
        {
            return;
        }

        targetUI = targetUI.gameObject;

        // 추후 화면 갱신 관련 정리 필요할 수도 있음
        targetUI.SetActive(false);
        targetUI.SetActive(true);
    }

    public GameObject TryGetUI(string UIName)
    {
        return uiObjects.ContainsKey(UIName) ? uiObjects[UIName] : null;
    }
    #endregion Input Methods
}
