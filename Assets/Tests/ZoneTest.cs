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
        [Test]
        public void GetLineOfSightWithZoneTest()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Subway.unity");  //Open the Scene in the Editor (do not enter Play Mode)
            GameObject[] allZones = GameObject.FindGameObjectsWithTag("ZoneInfoPanel");
            foreach (GameObject zone in allZones)
            {
                ZoneInfo currentZone = zone.GetComponent<ZoneInfo>();
                foreach (GameObject losZone in currentZone.lineOfSightZones)  // TODO Could be expanded to make sure losZone has the same index in each currentZone.lineOfSightZones
                {
                    ZoneInfo.LineOfSight lineOfSight = currentZone.GetLineOfSightWithZone(losZone);  // Will throw error if ZoneInfo has another zone in its 
                    Assert.IsNotNull(lineOfSight);
                }
            }
        }
    }
}
