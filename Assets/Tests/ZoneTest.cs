using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;

namespace Tests
{
    public class ZoneTest
    {
        // A Test behaves as an ordinary method
        [Test]
        public void ZoneTestSimplePasses()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Subway.unity");  //Open the Scene in the Editor (do not enter Play Mode)
            GameObject[] allZones = GameObject.FindGameObjectsWithTag("ZoneInfoPanel");
            foreach (GameObject zone in allZones)
            {
                ZoneInfo currentZone = zone.GetComponent<ZoneInfo>();
                foreach (GameObject losZone in currentZone.lineOfSightZones)
                {
                    ZoneInfo.LineOfSight lineOfSight = currentZone.GetLineOfSightWithZone(losZone);
                    Assert.IsNotNull(lineOfSight);
                }
            }
        }
    }
}
