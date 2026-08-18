using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

/**
 * 플레이어의 이름을 입력하고, 다음 씬을 로드합니다.
 * 이때 아무것도 입력하지 않으면 디폴트 네임인 "Witch"가 자동으로 설정됩니다.
 */
public class Title : MonoBehaviour
{
    public TMP_InputField chattingInputField;


    void Update()
    {
        if (Input.GetKey("escape"))
            Application.Quit();
    }

    public void MoveScene()
    {
        if (chattingInputField.text.Length == 0) UserName.name = "Witch";
        else UserName.name = chattingInputField.text;

        SceneManager.LoadScene(1);
    }
}
