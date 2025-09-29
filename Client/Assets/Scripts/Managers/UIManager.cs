using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI 관리를 위한 싱글톤 매니저 클래스입니다.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    
    private string currentUI;
    public Dictionary<string, GameObject> uiDictionary = new Dictionary<string, GameObject>();
    
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
    #endregion Unity Methods

    #region Custom Methods
    /// <summary>
    /// 초기화
    /// </summary>
    private void Initialize()
    {
        foreach (Transform child in transform)
        {
            uiDictionary.Add(child.name, child.gameObject);
            child.gameObject.SetActive(false);
        }
    }

    public void Hide(string UIName)
    {
        if (uiDictionary.ContainsKey(UIName))
        {
            uiDictionary[UIName].SetActive(false);
            if (currentUI == UIName)
            {
                currentUI = null;
            }
        }
        else
        {
            Debug.LogWarning($"UIManager: UI '{UIName}' not found in dictionary.");
        }
    }

    public void HideAll()
    {
        foreach (var ui in uiDictionary.Values)
        {
            ui.SetActive(false);
        }
        currentUI = null;
    }

    public void Show(string UIName)
    {
        if (uiDictionary.ContainsKey(UIName))
        {
            if (!string.IsNullOrEmpty(currentUI) && uiDictionary.ContainsKey(currentUI))
            {
                uiDictionary[currentUI].SetActive(false);
            }
            uiDictionary[UIName].SetActive(true);
            currentUI = UIName;
        }
        else
        {
            Debug.LogWarning($"UIManager: UI '{UIName}' not found in dictionary.");
        }
    }
    #endregion Custom Methods
}
