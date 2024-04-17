using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneUtils : MonoBehaviour
{
    public static void ToInitialScene()
    {
        SceneManager.LoadScene(0);
    }

    public static void ToNextScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public static bool IsCurrentSceneFirstLevel()
    {
        return SceneManager.GetActiveScene().name.Contains("Minigame1");
    }

    public static bool IsCinematicScene()
    {
        return SceneManager.GetActiveScene().name.Contains("FinalCutscene");
    }
}
