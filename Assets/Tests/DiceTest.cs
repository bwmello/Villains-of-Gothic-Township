using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class DiceTest
    {
        [Test]
        public void ExpectedValueWhiteDie()
        {
            Dice whiteDie = Resources.Load<GameObject>("Prefabs/Dice/WhiteDie").GetComponent<Dice>();
            Assert.AreEqual((2d / 3d), whiteDie.GetExpectedValue(0), .001);
            Assert.AreEqual(1.1111, whiteDie.GetExpectedValue(1), .001);
        }

        [Test]
        public void ExpectedValueWhiteRerollableDie()
        {
            Dice whiteDie = Resources.Load<GameObject>("Prefabs/Dice/WhiteRerollableDie").GetComponent<Dice>();
            Assert.AreEqual(1.1111, whiteDie.GetExpectedValue(0), .001);
        }

        [Test]
        public void ExpectedValueYellowDie()
        {
            Dice yellowDie = Resources.Load<GameObject>("Prefabs/Dice/YellowDie").GetComponent<Dice>();
            Assert.AreEqual((2d / 3d), yellowDie.GetExpectedValue(0), .001);
            Assert.AreEqual(1, yellowDie.GetExpectedValue(1), .001);
            Assert.AreEqual((7d / 6d), yellowDie.GetExpectedValue(2), .001);
        }

        [Test]
        public void ExpectedValueYellowRerollableDie()
        {
            Dice yellowRerollableDie = Resources.Load<GameObject>("Prefabs/Dice/YellowRerollableDie").GetComponent<Dice>();
            Assert.AreEqual(1, yellowRerollableDie.GetExpectedValue(0), .001);
            Assert.AreEqual((7d / 6d), yellowRerollableDie.GetExpectedValue(1), .001);
        }

        [Test]
        public void ExpectedValueOrangeDie()
        {
            Dice orangeDie = Resources.Load<GameObject>("Prefabs/Dice/OrangeDie").GetComponent<Dice>();
            Assert.AreEqual(1, orangeDie.GetExpectedValue(0), .001);
            Assert.AreEqual(1.3333, orangeDie.GetExpectedValue(1), .001);
        }

        [Test]
        public void ExpectedValueOrangeRerollableDie()
        {
            Dice orangeRerollableDie = Resources.Load<GameObject>("Prefabs/Dice/OrangeRerollableDie").GetComponent<Dice>();
            Assert.AreEqual(1.3333, orangeRerollableDie.GetExpectedValue(0), .001);
        }

        [Test]
        public void ExpectedValueRedDie()
        {
            Dice redDie = Resources.Load<GameObject>("Prefabs/Dice/RedDie").GetComponent<Dice>();
            Assert.AreEqual(1.5, redDie.GetExpectedValue(0), .001);
            Assert.AreEqual((23d / 12d), redDie.GetExpectedValue(1), .001);
            Assert.AreEqual((17d / 8d), redDie.GetExpectedValue(2), .001);
        }

        [Test]
        public void ExpectedValueRedRerollableDie()
        {
            Dice redRerollableDie = Resources.Load<GameObject>("Prefabs/Dice/RedRerollableDie").GetComponent<Dice>();
            Assert.AreEqual((23d / 12d), redRerollableDie.GetExpectedValue(0), .001);
            Assert.AreEqual((17d / 8d), redRerollableDie.GetExpectedValue(1), .001);
        }

        [Test]
        public void ExpectedValueBlackDie()
        {
            Dice blackDie = Resources.Load<GameObject>("Prefabs/Dice/BlackDie").GetComponent<Dice>();
            Assert.AreEqual(1.5, blackDie.GetExpectedValue(0), .001);
            Assert.AreEqual(2.3333, blackDie.GetExpectedValue(1), .001);
        }

        [Test]
        public void ExpectedValueBlackRerollableDie()
        {
            Dice blackRerollableDie = Resources.Load<GameObject>("Prefabs/Dice/BlackRerollableDie").GetComponent<Dice>();
            Assert.AreEqual(2.3333, blackRerollableDie.GetExpectedValue(0), .001);
        }


        [Test]
        public void ProbabilityOfResultYellowDie()
        {
            Dice yellowDie = Resources.Load<GameObject>("Prefabs/Dice/YellowDie").GetComponent<Dice>();
            Assert.AreEqual(.5, yellowDie.GetProbabilityOfResult(0, 0), .001);
            Assert.AreEqual(.25, yellowDie.GetProbabilityOfResult(0, 1), .001);
            Assert.AreEqual((1d / 3d), yellowDie.GetProbabilityOfResult(1, 0), .001);
            Assert.AreEqual((1d / 6d), yellowDie.GetProbabilityOfResult(1, 1), .001);
        }

        [Test]
        public void ProbabilityOfResultYellowRerollableDie()
        {
            Dice yellowRerollableDie = Resources.Load<GameObject>("Prefabs/Dice/YellowRerollableDie").GetComponent<Dice>();
            Assert.AreEqual(.25, yellowRerollableDie.GetProbabilityOfResult(0, 0), .001);
            Assert.AreEqual(.125, yellowRerollableDie.GetProbabilityOfResult(0, 1), .001);
            Assert.AreEqual((1d / 6d), yellowRerollableDie.GetProbabilityOfResult(1, 0), .001);
            Assert.AreEqual((1d / 12d), yellowRerollableDie.GetProbabilityOfResult(1, 1), .001);
        }

        [Test]
        public void ProbabilityOfResultBlackDie()
        {
            Dice blackDie = Resources.Load<GameObject>("Prefabs/Dice/BlackDie").GetComponent<Dice>();
            Assert.AreEqual(.5, blackDie.GetProbabilityOfResult(0, 0), .001);
            Assert.AreEqual(.25, blackDie.GetProbabilityOfResult(0, 1), .001);
            Assert.AreEqual((1d / 6d), blackDie.GetProbabilityOfResult(1, 0), .001);
            Assert.AreEqual((1d / 12d), blackDie.GetProbabilityOfResult(1, 1), .001);
        }

        [Test]
        public void ProbabilityOfResultBlackRerollableDie()
        {
            Dice blackRerollableDie = Resources.Load<GameObject>("Prefabs/Dice/BlackRerollableDie").GetComponent<Dice>();
            Assert.AreEqual(.25, blackRerollableDie.GetProbabilityOfResult(0, 0), .001);
            Assert.AreEqual(.125, blackRerollableDie.GetProbabilityOfResult(0, 1), .001);
            Assert.AreEqual((1d / 12d), blackRerollableDie.GetProbabilityOfResult(1, 0), .001);
            Assert.AreEqual((1d / 24d), blackRerollableDie.GetProbabilityOfResult(1, 1), .001);
        }
    }
}
