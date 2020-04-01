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
    // To get averageSuccesses for a rerollable die, replace below average die faces with the normal average. Ex: For a rerollable black die, the average is (1.5+1.5+1.5+1.5+4+4)/6

    void Start()
    {
        averageSuccesses = Queryable.Average(faces.AsQueryable());
    }

    public int Roll()
    {
        return faces[random.Next(faces.Length)];
    }
}
