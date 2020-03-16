using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;  // For Queryable

public class Dice : MonoBehaviour
{
    readonly System.Random random = new System.Random();

    [SerializeField]
    int[] faces;

    public bool rerollable = false;
    public double averageSuccesses;

    void Start()
    {
        averageSuccesses = Queryable.Average(faces.AsQueryable());
    }

    public int Roll()
    {
        return faces[random.Next(faces.Length)];
    }
}
