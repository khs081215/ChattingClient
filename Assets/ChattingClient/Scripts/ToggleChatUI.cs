using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/**
 * 버튼으로 채팅 UI를 열고닫습니다.
 */
public class ToggleChatUI : MonoBehaviour
{
    public GameObject chatUI;
   public void OnClick()
    {
        if (chatUI.activeSelf == true) chatUI.SetActive(false);
        else chatUI.SetActive(true);
    }
}
