using System;  // for [Serializable]
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public int lifePoints = 1;
    public int defense;

    public int movePoints;
    public int ignoreTerrainDifficulty = 0;
    public int ignoreElevation = 0;
    public int ignoreSize = 0;

    public int size = 1;
    public int menace = 1;
    public int reinforcementCost = 1;

    [Serializable]
    public class actionProficiency
    {
        public string action;
        public GameObject[] dice;
    }
    public actionProficiency[] actionProficiencies;

    // Unity can't expose dictionaries in the inspector, so below not possible without an addon:
    //public Dictionary<string, GameObject[]> actionProficiencies = new Dictionary<string, GameObject[]>() { { "MELEE", new GameObject[] { } }, { "RANGED", new GameObject[] { } }, { "MANIPULATION", new GameObject[] { } }, { "THOUGHT", new GameObject[] { } } };
}
