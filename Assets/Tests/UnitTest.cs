using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class UnitTest
    {
        [Test]
        public void GetNextDiceFailComboTest()
        {
            Unit unit = Resources.Load<GameObject>("Prefabs/Units/Unit").GetComponent<Unit>();
            GameObject yellowDie = Resources.Load<GameObject>("Prefabs/Dice/YellowDie");
            // For requiredSuccesses = 4 and 3 dice
            // 0,0,0->1,0,0->2,0,0->3,0,0->0,1,0->1,1,0->2,1,0->0,2,0->1,2,0->0,3,0->0,0,1->1,0,1->2,0,1->0,1,1->1,1,1->0,2,1->0,0,2->1,0,2->0,1,2->0,0,3
            List<List<(int, GameObject)>> allFailCombos = new List<List<(int, GameObject)>>()
            {
                new List<(int, GameObject)>(){ (0, yellowDie), (0, yellowDie), (0, yellowDie) },
                new List<(int, GameObject)>(){ (1, yellowDie), (0, yellowDie), (0, yellowDie) },
                new List<(int, GameObject)>(){ (2, yellowDie), (0, yellowDie), (0, yellowDie) },
                new List<(int, GameObject)>(){ (3, yellowDie), (0, yellowDie), (0, yellowDie) },
                new List<(int, GameObject)>(){ (0, yellowDie), (1, yellowDie), (0, yellowDie) },
                new List<(int, GameObject)>(){ (1, yellowDie), (1, yellowDie), (0, yellowDie) },
                new List<(int, GameObject)>(){ (2, yellowDie), (1, yellowDie), (0, yellowDie) },
                new List<(int, GameObject)>(){ (0, yellowDie), (2, yellowDie), (0, yellowDie) },
                new List<(int, GameObject)>(){ (1, yellowDie), (2, yellowDie), (0, yellowDie) },
                new List<(int, GameObject)>(){ (0, yellowDie), (3, yellowDie), (0, yellowDie) },
                new List<(int, GameObject)>(){ (0, yellowDie), (0, yellowDie), (1, yellowDie) },
                new List<(int, GameObject)>(){ (1, yellowDie), (0, yellowDie), (1, yellowDie) },
                new List<(int, GameObject)>(){ (2, yellowDie), (0, yellowDie), (1, yellowDie) },
                new List<(int, GameObject)>(){ (0, yellowDie), (1, yellowDie), (1, yellowDie) },
                new List<(int, GameObject)>(){ (1, yellowDie), (1, yellowDie), (1, yellowDie) },
                new List<(int, GameObject)>(){ (0, yellowDie), (2, yellowDie), (1, yellowDie) },
                new List<(int, GameObject)>(){ (0, yellowDie), (0, yellowDie), (2, yellowDie) },
                new List<(int, GameObject)>(){ (1, yellowDie), (0, yellowDie), (2, yellowDie) },
                new List<(int, GameObject)>(){ (0, yellowDie), (1, yellowDie), (2, yellowDie) },
                new List<(int, GameObject)>(){ (0, yellowDie), (0, yellowDie), (3, yellowDie) },
            };
            for (int i = 1; i < allFailCombos.Count; i++)
            {
                //string debugString = "failCombo at index " + (i-1).ToString();
                //foreach ((int successes, GameObject die) in unit.GetNextDiceFailCombo(4, allFailCombos[i - 1]))
                //{
                //    debugString += "(" + successes.ToString() + ", yellowDie)";
                //}
                //Debug.Log(debugString);
                Assert.AreEqual(allFailCombos[i], unit.GetNextDiceFailCombo(4, allFailCombos[i - 1]));
            }
        }

        [Test]
        public void GetChanceOfSuccessUzi()
        {
            Unit uziUnit = Resources.Load<GameObject>("Prefabs/Units/UZI").GetComponent<Unit>();
            //GameObject whiteDie = Resources.Load<GameObject>("Prefabs/Dice/WhiteDie");
            //GameObject orangeDie = Resources.Load<GameObject>("Prefabs/Dice/OrangeDie");
            //Assert.AreEqual(.2222, unit.GetChanceOfSuccess(3, new List<GameObject>() { whiteDie, orangeDie }), .001);
            //Assert.AreEqual(.8395, unit.GetChanceOfSuccess(3, new List<GameObject>() { whiteDie, orangeDie }, 1), .001);
            Assert.AreEqual(.2222, uziUnit.GetChanceOfSuccess(3, uziUnit.actionProficiencies[1].proficiencyDice), .001);  // Manipulate bomb
            Assert.AreEqual(.8395, uziUnit.GetChanceOfSuccess(3, uziUnit.actionProficiencies[1].proficiencyDice, 1), .001);
            Assert.AreEqual(0, uziUnit.GetChanceOfSuccess(5, uziUnit.actionProficiencies[1].proficiencyDice), .001);
            //Assert.AreEqual(.8148, uziUnit.GetChanceOfSuccess(5, uziUnit.actionProficiencies[1].proficiencyDice, 1), .001);  // requiredSuccesses > 4 should be 0 no matter how many rerolls
            Assert.AreEqual(0, uziUnit.GetChanceOfSuccess(5, uziUnit.actionProficiencies[1].proficiencyDice, 1), .001);
        }

        [Test]
        public void GetChanceOfSuccessCrowbar()
        {
            Unit crowbarUnit = Resources.Load<GameObject>("Prefabs/Units/CROWBAR").GetComponent<Unit>();
            Assert.AreEqual(.4999, crowbarUnit.GetChanceOfSuccess(3, crowbarUnit.actionProficiencies[1].proficiencyDice), .001);  // Thought computer
            Assert.AreEqual(.8755, crowbarUnit.GetChanceOfSuccess(3, crowbarUnit.actionProficiencies[1].proficiencyDice, 1), .001);
            //Assert.AreEqual(.4444, crowbarUnit.GetChanceOfSuccess(5, crowbarUnit.actionProficiencies[1].proficiencyDice), .001);  // Thought computer with 2 hindrance, just like with uzi without max check in GetChanceOfSuccess
            Assert.AreEqual(0, crowbarUnit.GetChanceOfSuccess(5, crowbarUnit.actionProficiencies[1].proficiencyDice), .001);
        }

        [Test]
        public void GetChanceOfSuccessBarn()
        {
            Unit barnUnit = Resources.Load<GameObject>("Prefabs/Units/BARN").GetComponent<Unit>();
            Assert.AreEqual(.6666, barnUnit.GetChanceOfSuccess(2, barnUnit.actionProficiencies[1].proficiencyDice), .001);  // Manipulate bomb, munitionSpecialist 1
            Assert.AreEqual(.9629, barnUnit.GetChanceOfSuccess(2, barnUnit.actionProficiencies[1].proficiencyDice, 1), .001);
        }

        [Test]
        public void GetChanceOfSuccessOtterpop()
        {
            Unit otterpopUnit = Resources.Load<GameObject>("Prefabs/Units/OTTERPOP").GetComponent<Unit>();
            Assert.AreEqual(.7777, otterpopUnit.GetChanceOfSuccess(1, otterpopUnit.actionProficiencies[1].proficiencyDice), .001);  // Manipulate (grenade) from 1 zone away
            Assert.AreEqual(.9506, otterpopUnit.GetChanceOfSuccess(1, otterpopUnit.actionProficiencies[1].proficiencyDice, 1), .001);
            Assert.AreEqual(.5555, otterpopUnit.GetChanceOfSuccess(2, otterpopUnit.actionProficiencies[1].proficiencyDice), .001);  // Manipulate (grenade) from 2 zones away
            Assert.AreEqual(.9012, otterpopUnit.GetChanceOfSuccess(2, otterpopUnit.actionProficiencies[1].proficiencyDice, 1), .001);
        }
    }
}
