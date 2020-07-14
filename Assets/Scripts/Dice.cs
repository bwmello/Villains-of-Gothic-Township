using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;  // For Queryable

public class Dice : MonoBehaviour
{
    readonly System.Random random = new System.Random();

    public int[] faces;
    public bool rerollable = false;
    public double averageSuccesses;
    public double averageSuccessesWithReroll;  // Will be the same as averageSuccesses unless rerollable == true
    // averageSuccess should never factor in rerollable. WhiteDie and WhiteRerollableDie should have the same averageSuccesses for determining whether or not a result is below the average and should be rerolled
    // To get averageSuccesses for a rerollable die, replace below average die faces with the normal average. Ex: For a rerollable black die, the average is (1.5+1.5+1.5+1.5+4+4)/6

    void Start()
    {
        //averageSuccesses = Queryable.Average(faces.AsQueryable());  // This isn't used as dice are used directly from prefabs and not instantiated
    }

    public int Roll()
    {
        return faces[random.Next(faces.Length)];
    }
}
