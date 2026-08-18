using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

/**
 * 플레이어의 이름을 입력하고, 다음 씬을 로드합니다.
 * 이때 아무것도 입력하지 않으면 디폴트 네임인 "Nameless"이 자동으로 설정됩니다.
 */
public class Title : MonoBehaviour
{
    public TMP_InputField nameInputField;
    
    public void MoveScene()
    {
        if (nameInputField.text.Length == 0) UserName.name = "Nameless";
        else UserName.name = nameInputField.text;

        SceneManager.LoadScene(1);
    }
}
