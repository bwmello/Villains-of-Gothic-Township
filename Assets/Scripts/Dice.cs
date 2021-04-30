using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dice : MonoBehaviour
{
    readonly System.Random random = new System.Random();

    public string color;
    public int[] faces;
    public bool rerollable = false;
    public double averageSuccesses;  // TODO Delete both of these since you can get these results just as easily from GetExpectedValue()
    public double averageSuccessesWithReroll;  // Will be the same as averageSuccesses unless rerollable == true

    public int Roll()
    {
        return faces[random.Next(faces.Length)];
    }

    public int GetLargestPossibleResult()
    {
        return faces[faces.Length - 1];  // faces[] is organized from smallest to largest, so get last result
    }

    public double GetProbabilityOfResult(int result, int totalRerolls, bool ignoreRerollable = false)
    {
        if (!ignoreRerollable && rerollable)
        {
            totalRerolls++;
        }

        //return DiceMath.GetProbabilityOfDieResultToAvoid(color, totalRerolls, result, result);  // Fix this with desiredResult instead of result twice and replace all below
        if (totalRerolls > 0)
        {
            double resultProbablility = Mathf.Pow((float)GetProbabilityOfResult(result, 0, true), totalRerolls + 1);  // This assumes you're using rerolls to avoid the result, giving you a lower probability. If that's not the case, resultProbability = 1 - this equation
            return resultProbablility;
        }
        else
        {
            int totalFacesWithResult = 0;
            foreach (int face in faces)
            {
                if (face == result)
                {
                    totalFacesWithResult++;
                }
                else if (face > result)
                {
                    break;  // faces is sorted in ascending order, so break when you've gone too far
                }
            }
            return (double)totalFacesWithResult / (double)faces.Length;  // faces.Length is always going to be 6, for a 6 sided die
        }
    }

    public double GetExpectedValue(int totalRerolls, bool ignoreRerollable = false)
    {
        double expectedValue = 0;
        if (!ignoreRerollable && rerollable)
        {
            totalRerolls++;
        }

        switch (color)
        {
            case "white":
                expectedValue = DiceMath.whiteDieExpectedValues[totalRerolls];
                //switch (totalRerolls)  // Unity can't handle C# 8 syntax yet, so don't simplify this into a switch expression until then
                //{
                //    case 0:
                //        expectedValue = 2d / 3d;
                //        break;
                //    default:
                //        expectedValue = Mathf.Pow(4f / 6f, totalRerolls) * GetExpectedValue(0, true) + 2 - Mathf.Pow(2, totalRerolls + 1) * Mathf.Pow(3, -totalRerolls);
                //        break;
                //}
                break;
            case "yellow":
                expectedValue = DiceMath.yellowDieExpectedValues[totalRerolls];
                //switch (totalRerolls)
                //{
                //    case 0:
                //        expectedValue = 2d / 3d;
                //        break;
                //    case 1:
                //        expectedValue = 1;
                //        break;
                //    case 2:
                //        expectedValue = 7d / 6d;
                //        break;
                //    default:
                //        expectedValue = Mathf.Pow(5f / 6f, totalRerolls - 2) * GetExpectedValue(2, true) + (1d / 3d) * (6 - Mathf.Pow(5, totalRerolls - 2) * Mathf.Pow(6, 3 - totalRerolls));
                //        break;
                //}
                break;
            case "orange":
                expectedValue = DiceMath.orangeDieExpectedValues[totalRerolls];
                //switch (totalRerolls)
                //{
                //    case 0:
                //        expectedValue = 1;
                //        break;
                //    default:
                //        expectedValue = Mathf.Pow(4f / 6f, totalRerolls) * GetExpectedValue(0, true) + 2 - Mathf.Pow(2, totalRerolls + 1) * Mathf.Pow(3, -totalRerolls);
                //        break;
                //}
                break;
            case "red":
                expectedValue = DiceMath.redDieExpectedValues[totalRerolls];
                //switch (totalRerolls)
                //{
                //    case 0:
                //        expectedValue = 1.5;
                //        break;
                //    case 1:
                //        expectedValue = 23d / 12d;
                //        break;
                //    case 2:
                //        expectedValue = 17d / 8d;
                //        break;
                //    default:
                //        expectedValue = Mathf.Pow(5f / 6f, totalRerolls - 2) * GetExpectedValue(2, true) + 3 - Mathf.Pow(2, 2 - totalRerolls) * Mathf.Pow(3, 3 - totalRerolls) * Mathf.Pow(5, totalRerolls - 2);
                //        break;
                //}
                break;
            case "black":
                expectedValue = DiceMath.blackDieExpectedValues[totalRerolls];
                //switch (totalRerolls)
                //{
                //    case 0:
                //        expectedValue = 1.5;
                //        break;
                //    default:
                //        expectedValue = Mathf.Pow(4f / 6f, totalRerolls) * GetExpectedValue(0, true) + 4 - Mathf.Pow(2, totalRerolls + 2) * Mathf.Pow(3, -totalRerolls);
                //        break;
                //}
                break;
        }
        return expectedValue;
    }
}