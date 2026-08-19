using UnityEngine;
using INART.SlothyBakery.Base;

public class TestMinigame : MiniGameParent
{
    private void Update()
    {
        // Presionar T en Play Mode simula completar una ronda
        if (Input.GetKeyDown(KeyCode.T))
            ReportRoundComplete();
    }
}
