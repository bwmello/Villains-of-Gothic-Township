using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;  // For Queryable

public class Dice : MonoBehaviour
{
    readonly System.Random random = new System.Random();

    [SerializeField]
    int[] faces;
    [SerializeField]
    bool rerollable = false;

    double averageSuccesses;

    void Start()
    {
        averageSuccesses = Queryable.Average(faces.AsQueryable());
    }

    // Start is called before the first frame update
    public int RollDice(int quantity)
    {
        int successes = 0;
        for (int i = 0; i < quantity; i++)
        {
            int currentSuccesses = faces[random.Next(faces.Length)];
            if (rerollable && currentSuccesses < averageSuccesses)
            {
                currentSuccesses = faces[random.Next(faces.Length)];
            }
            successes += currentSuccesses;
        }
        return successes;
    }
}
