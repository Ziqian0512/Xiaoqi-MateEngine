using UnityEngine;

public class RoomSceneExit : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) XiaoqiModeController.ExitRoom();
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(20, 20, 150, 36), "回到桌面"))
            XiaoqiModeController.ExitRoom();
    }
}
