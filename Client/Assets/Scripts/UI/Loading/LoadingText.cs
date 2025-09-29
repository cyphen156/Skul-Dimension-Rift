using UnityEngine;
using TMPro;
using System.Collections;

public class LoadingText : MonoBehaviour
{    
    private const string defaultText = "불러오는 중";

    private void OnEnable()
    {
        StartCoroutine(SetLaodingText());
    }

    IEnumerator SetLaodingText()
    {
        int count = 0;

        while (true)
        {
            count++;
            if (count > 3)
            {
                count = 0;
            }
            string text = defaultText;
            for (int i = 0; i < count; ++i)
            {
                text += ".";
            }
            GetComponent<TMP_Text>().text = text;
            yield return new WaitForSeconds(0.3f);
        }
    }
}
