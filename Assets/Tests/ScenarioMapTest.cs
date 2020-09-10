using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class ScenarioMapTest
    {
        // A Test behaves as an ordinary method
        [Test]
        public void GetEarliestPossibleActivationRoundTest()
        {
            ScenarioMap scenarioMap = new ScenarioMap();
            scenarioMap.villainRiver = new List<string>() { "UZI", "CHAINS", "PISTOLS", "REINFORCEMENT", "CROWBAR", "SHOTGUN", "BARN" };
            MissionSpecifics.missionName = "ASinkingFeeling";

            scenarioMap.currentRound = MissionSpecifics.GetFinalRound() - 1;  // Last villain turn
            Assert.Negative(scenarioMap.GetEarliestPossibleActivationRound("UZI", 2));  // All activations used already
            Assert.AreEqual(0, scenarioMap.GetEarliestPossibleActivationRound("UZI", 1));
            Assert.AreEqual(0, scenarioMap.GetEarliestPossibleActivationRound("UZI"));
            Assert.Negative(scenarioMap.GetEarliestPossibleActivationRound("REINFORCEMENT", 1));
            Assert.AreEqual(0, scenarioMap.GetEarliestPossibleActivationRound("REINFORCEMENT"));
            Assert.Negative(scenarioMap.GetEarliestPossibleActivationRound("CROWBAR"));

            scenarioMap.currentRound = MissionSpecifics.GetFinalRound() - 2;  // 2nd to last villain turn
            Assert.AreEqual(1, scenarioMap.GetEarliestPossibleActivationRound("SHOTGUN"));

            scenarioMap.currentRound = MissionSpecifics.GetFinalRound() - 3;  // 3rd to last villain turn
            Assert.AreEqual(2, scenarioMap.GetEarliestPossibleActivationRound("BARN", 1));
        }
    }
}
