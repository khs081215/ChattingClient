using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
 * 게임 시작시 세팅에 대해 관리합니다.
 */
public class SettingManager : GenericSingleton<SettingManager>
{
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
    }
    
    protected override void InitReference()
    {
        Cursor.lockState = CursorLockMode.Confined;
    }
}
