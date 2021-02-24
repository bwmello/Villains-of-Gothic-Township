using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SceneHandler
{
    public static string version = "v0.201";  // Update when save files are incompatible with previous versions. Used in MissionSelection to determine compatible saves
    public static string saveName = null;
    public static bool isFirstTimePlaying;
}
