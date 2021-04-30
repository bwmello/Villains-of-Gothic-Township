using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;  // For converting array.ToList()


public static class DiceMath
{
    // With 1 billion iterations, the first four decimal places are accurate (with the 5th place varying)
    // Each array is size 10, first expectedValue with 0 rerolls and last expectedValue with 10 rerolls
    public static double[] whiteDieExpectedValues = new double[] { 2d/3d, 1.11109263399945, 1.40742602599996, 1.6049497960004, 1.73660752000056, 1.82440928199983, 1.88294607399997, 1.92195493600006, 1.94797768800008, 1.96530828799996, 1.97687710799999 };
    public static double[] yellowDieExpectedValues = new double[] { 2d/3d, 1d, 7d/6d, 1.24997930600021, 1.29169110200026, 1.31250009699926, 1.3229210640033, 1.32810455000091, 1.33071303899938, 1.33200473799966, 1.33267567300133 };
    public static double[] orangeDieExpectedValues = new double[] { 1d, 1.33333333333333, 1.4444623880005, 1.4814407729991, 1.49383685999982, 1.49794517500087, 1.49928913099893, 1.49977320899952, 1.49994206899988, 1.49999327599978, 1.50000278200053 };
    public static double[] redDieExpectedValues = new double[] { 1.5, 23d/12d, 17d/8d, 2.22915954999971, 2.28124055099794, 2.30728490000529, 2.32031321900065, 2.32684340999926, 2.33007888201057, 2.33170303301169, 2.3325277889929 };
    public static double[] blackDieExpectedValues = new double[] { 1.5, 2.33333333333333, 2.88887547500048, 3.25930592199992, 3.50616968100033, 3.67079012499985, 3.78050179799966, 3.85367876900008, 3.90243894299998, 3.93496518099997, 3.95664696899998 };

    public static Dictionary<int, List<double>> whiteDieProbabilityOfResultAndRerollingLower = new Dictionary<int, List<double>> {
        { 0, new List<double> { 4d/6d, .44444444444, .29629629629, .19753086419, .13168724279, .08779149519, .05852766346, .03901844231, .02601229487, .01734152991, .01156101994 } },
        { 1, new List<double> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
        { 2, new List<double> { 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d } }
    };
    public static Dictionary<int, List<double>> yellowDieProbabilityOfResultAndRerollingLower = new Dictionary<int, List<double>> {
        { 0, new List<double> { 3d/6d, .25, .125, .0625, .03125, .015625, .0078125, .00390625, .001953125, .0009765625, .00048828125 } },
        { 1, new List<double> { 2d/6d, 0.277788046, 0.231460433, 0.192881385, 0.160737264, 0.133940439, 0.111650956, 0.093037006, 0.077519708, 0.064615926, 0.053827753 } },
        { 2, new List<double> { 1d/6d, 1d/6d, 1d/6d, 1d/6d, 1d/6d, 1d/6d, 1d/6d, 1d/6d, 1d/6d, 1d/6d, 1d/6d } }
    };
    public static Dictionary<int, List<double>> orangeDieProbabilityOfResultAndRerollingLower = new Dictionary<int, List<double>> {
        { 0, new List<double> { 2d/6d, .11111111111, .03703703703, .01234567901, .00411522633, .00137174211, .00045724737, .00015241579, .00005080526, .00001693508, .00000564502 } },
        { 1, new List<double> { 2d/6d, 0.22221344, 0.148157468, 0.098748476, 0.065841648, 0.043889457, 0.029252238, 0.019503035, 0.013005778, 0.008671373, 0.005781261 } },
        { 2, new List<double> { 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d } }
    };
    public static Dictionary<int, List<double>> redDieProbabilityOfResultAndRerollingLower = new Dictionary<int, List<double>> {
        { 0, new List<double> { 1d/6d, 1d/36d, 1d/216d, 1d/1296d, 1d/7776d, 1d/46656d, 1d/279936d, 1d/1679616d, 1d/10077696d, 1d/60466176d, 1d/362797056d } },
        { 1, new List<double> { 2d/6d, 0.166643341, 0.083338223, 0.041673035, 0.020832793, 0.010417586, 0.005209149, 0.00260431, 0.001302644, 0.000652367, 0.000325741 } },
        { 2, new List<double> { 2d/6d, 0.277754926, 0.231495078, 0.192861269, 0.160730941, 0.133963954, 0.11162821, 0.09302236, 0.07750642, 0.064607813, 0.053831499 } },
        { 3, new List<double> { 1d/6d, 1d/6d, 1d/6d, 1d/6d, 1d/6d, 1d/6d, 1d/6d, 1d/6d, 1d/6d, 1d/6d, 1d/6d } }
    };
    public static Dictionary<int, List<double>> blackDieProbabilityOfResultAndRerollingLower = new Dictionary<int, List<double>> {
        { 0, new List<double> { 3d/6d, .25, .125, .0625, .03125, .015625, .0078125, .00390625, .001953125, .0009765625, .00048828125 } },
        { 1, new List<double> { 1d/6d, 0.11110726, 0.074072372, 0.049386007, 0.032925726, 0.021940905, 0.014633427, 0.009751705, 0.006503793, 0.004337034, 0.002891432 } },
        { 4, new List<double> { 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d } }
    };

    public static Dictionary<int, Dictionary<int, List<double>>> whiteDieProbabilityOfResultToAvoid = new Dictionary<int, Dictionary<int, List<double>>> {
        // resultToAvoid
        { 0, new Dictionary<int, List<double>> {
            // desiredResult, list index is number of rerolls
            { 1, new List<double> { 4d / 6d, .44444444444, .29629629629, .19753086419, .13168724279, .08779149519, .05852766346, .03901844231, .02601229487, .01734152991, .01156101994 } }

        } },
        { 2, new Dictionary<int, List<double>> {
            { 2, new List<double> { 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d } }
        } }
    };
    public static Dictionary<int, Dictionary<int, List<double>>> yellowDieProbabilityOfResultToAvoid = new Dictionary<int, Dictionary<int, List<double>>> {
        { 0, new Dictionary<int, List<double>> {
            { 1, new List<double> { 3d/6d, .25, .125, .0625, .03125, .015625, .0078125, .00390625, .001953125, .0009765625, .00048828125 } },
            { 2, new List<double> { 3d/6d, 0.416664245, 0.347199333, 0.289356503, 0.241108024, 0.200922589, 0.167446631, 0.13954326, 0.116299467, 0.096904685, 0.080755424 } }
        } },
        { 1, new Dictionary<int, List<double>> {
            //{ 1, new List<double> { 2d/6d, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
            { 2, new List<double> { 2d/6d, 0.277792965, 0.231509662, 0.192913642, 0.1607567, 0.13397165, 0.111630728, 0.093044455, 0.077517795, 0.064591082, 0.053842319 } }
        } },
        { 2, new Dictionary<int, List<double>> {
            { 2, new List<double> { 1d/6d, 1d/6d, 1d/6d, 1d/6d, 1d/6d, 1d/6d, 1d/6d, 1d/6d, 1d/6d, 1d/6d, 1d/6d } }
        } }
    };
    public static Dictionary<int, Dictionary<int, List<double>>> orangeDieProbabilityOfResultToAvoid = new Dictionary<int, Dictionary<int, List<double>>> {
        { 0, new Dictionary<int, List<double>> {
            { 1, new List<double> { 2d/6d, .11111111111, .03703703703, .01234567901, .00411522633, .00137174211, .00045724737, .00015241579, .00005080526, .00001693508, .00000564502 } },
            { 2, new List<double> { 2d/6d, 0.222207853, 0.14815555, 0.098754556, 0.065832299, 0.043885809, 0.029268108, 0.0195099, 0.013010033, 0.00867467, 0.005780288 } }
        } },
        { 1, new Dictionary<int, List<double>> {
            //{ 1, new List<double> { 2d/6d, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
            { 2, new List<double> { 2d/6d, 0.222242719, 0.14815047, 0.09875345, 0.06584306, 0.043896843, 0.029265364, 0.0195139, 0.013002965, 0.008676897, 0.005781506 } }
        } },
        { 2, new Dictionary<int, List<double>> {
            { 2, new List<double> {2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d } }
        } }
    };
    public static Dictionary<int, Dictionary<int, List<double>>> redDieProbabilityOfResultToAvoid = new Dictionary<int, Dictionary<int, List<double>>> {
        { 0, new Dictionary<int, List<double>> {
            { 1, new List<double> { 1d/6d, 1d/36d, 1d/216d, 1d/1296d, 1d/7776d, 1d/46656d, 1d/279936d, 1d/1679616d, 1d/10077696d, 1d/60466176d, 1d/362797056d } },
            { 2, new List<double> { 1d/6d, 0.08332953, 0.041670394, 0.020832341, 0.010414301, 0.005207843, 0.002604621, 0.001301883, 0.000651218, 0.000325139, 0.000162928 } },
            { 3, new List<double> { 1d/6d, 0.138891997, 0.115744208, 0.096430399, 0.080370895, 0.066973207, 0.055827343, 0.046515281, 0.038771828, 0.032303747, 0.026921418 } }
        } },
        { 1, new Dictionary<int, List<double>> {
            { 2, new List<double> { 2d/6d, 0.166646898, 0.083335953, 0.041671507, 0.020830082, 0.010414359, 0.005207126, 0.00260441, 0.001303183, 0.00065225, 0.000325961 } },
            { 3, new List<double> { 2d/6d, 0.277769932, 0.231478515, 0.192896658, 0.160744969, 0.133955951, 0.111629166, 0.093020912, 0.077514762, 0.064604788, 0.053843966 } }
        } },
        { 2, new Dictionary<int, List<double>> {
            //{ 1, new List<double> { 2d/6d, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
            { 3, new List<double> { 2d/6d, 0.277787837, 0.231472197, 0.192916342, 0.160748955, 0.133973112, 0.111614707, 0.093030389, 0.07751615, 0.064588623, 0.053842103 } }
        } },
        { 3, new Dictionary<int, List<double>> {
            { 3, new List<double> {1d/6d, 1d/6d, 1d/6d, 1d/6d, 1d/6d, 1d/6d, 1d/6d, 1d/6d, 1d/6d, 1d/6d, 1d/6d } }
        } }
    };
    public static Dictionary<int, Dictionary<int, List<double>>> blackDieProbabilityOfResultToAvoid = new Dictionary<int, Dictionary<int, List<double>>> {
        { 0, new Dictionary<int, List<double>> {
            { 1, new List<double> { 3d/6d, .25, .125, .0625, .03125, .015625, .0078125, .00390625, .001953125, .0009765625, .00048828125 } },
            { 4, new List<double> { 3d/6d, 0.333339486, 0.22223082, 0.148164473, 0.098767172, 0.065840418, 0.043893146, 0.029270633, 0.019503069, 0.013000287, 0.008674561 } }
        } },
        { 1, new Dictionary<int, List<double>> {
            //{ 1, new List<double> { 1d/6d, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
            { 4, new List<double> { 1d/6d, 0.111110365, 0.074066099, 0.049373231, 0.032929473, 0.021950474, 0.014635375, 0.009757827, 0.006499381, 0.004335292, 0.002891531 } }
        } },
        { 4, new Dictionary<int, List<double>> {
            { 4, new List<double> {2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d, 2d/6d } }
        } }
    };


    public static Dice GetDieOfColor(string dieColor)
    {
        switch (dieColor)
        {
            case "black":
                return Resources.Load<GameObject>("Prefabs/Dice/BlackDie").GetComponent<Dice>();
            case "orange":
                return Resources.Load<GameObject>("Prefabs/Dice/OrangeDie").GetComponent<Dice>();
            case "red":
                return Resources.Load<GameObject>("Prefabs/Dice/RedDie").GetComponent<Dice>();
            case "white":
                return Resources.Load<GameObject>("Prefabs/Dice/WhiteDie").GetComponent<Dice>();
            case "yellow":
                return Resources.Load<GameObject>("Prefabs/Dice/YellowDie").GetComponent<Dice>();
        }
        return null;
    }

    public static double GetProbabilityOfDieResultToAvoid(string dieColor, int rerolls, int resultToAvoid, int desiredResult)
    {
        switch (dieColor)
        {
            case "white":
                return whiteDieProbabilityOfResultToAvoid[resultToAvoid][desiredResult][rerolls];
            case "yellow":
                return yellowDieProbabilityOfResultToAvoid[resultToAvoid][desiredResult][rerolls];
            case "orange":
                return orangeDieProbabilityOfResultToAvoid[resultToAvoid][desiredResult][rerolls];
            case "red":
                return redDieProbabilityOfResultToAvoid[resultToAvoid][desiredResult][rerolls];
            case "black":
                return blackDieProbabilityOfResultToAvoid[resultToAvoid][desiredResult][rerolls];
            default:
                Debug.Log("Error! In DiceMath.GetProbabilityOfDieResultToAvoid(dieColor:" + dieColor + ", rerolls:" + rerolls.ToString() + ", resultToAvoid:" + resultToAvoid.ToString() + ", desiredResult:" + desiredResult.ToString() + "), dieColor not recognized");
                return 0;
        }
    }

    // Below functions are used internally only to populate probability tables
    public static Dictionary<int, int> GenerateProbabilityOfResultsToAvoid(string dieColor, int desiredResult, int rerolls, int iterations)
    {
        Dictionary<int, int> resultsFrequencyMap = new Dictionary<int, int>();
        Dice die = GetDieOfColor(dieColor);

        //List<int> dieResults = new List<int>();
        //double currentAverage = 0;
        //int numberOfResults = 0;
        for (int i = 0; i < iterations; i++)
        {
            int successes = die.Roll();
            for (int j = 0; j < rerolls; j++)
            {
                if (successes < desiredResult)  // You're using rerolls to try and get the desiredResult
                {
                    successes = die.Roll();
                }
                else
                {
                    break;
                }
            }

            if (successes < desiredResult)
            {
                if (resultsFrequencyMap.ContainsKey(successes))
                {
                    resultsFrequencyMap[successes] += 1;
                }
                else
                {
                    resultsFrequencyMap[successes] = 1;
                }
            }
            //dieResults.Add(successes);

            //numberOfResults++;
            //currentAverage = (double)((currentAverage * (double)(numberOfResults - 1)) + successes) / (double)numberOfResults;
        }
        //double computedAverage = (double)dieResults.Sum() / (double)dieResults.Count;
        //string debugString = "FindProbabilityOfResult for resultToAvoid " + resultToAvoid.ToString() + " and desiredResult " + desiredResult.ToString() + " with " + dieColor + " die with " + rerolls.ToString() + " rerolls";
        //debugString += "\nprobability: " + ((double)resultOccurences / (double)(resultOccurences + notResultOccurences)).ToString() + " with " + resultOccurences.ToString() + " occurences of result over a total of " + (resultOccurences + notResultOccurences).ToString() + " over " + iterations.ToString() + " iterations.";
        //debugString += "\ncomputedAverage: " + computedAverage.ToString() + " calculated by dieResults.Sum() " + dieResults.Sum().ToString() + " / dieResults.Count " + dieResults.Count.ToString() +  " over " + dieResults.Count.ToString() + " iterations. DieResults: ";
        //foreach (int result in dieResults)
        //{
        //    debugString += result.ToString() + ", ";
        //}
        //Debug.Log(debugString);
        //return ((double)resultOccurences / (double)(resultOccurences + notResultOccurences)).ToString();
        return resultsFrequencyMap;
    }

    public static Dictionary<int, int> GenerateProbabilityOfSuccessForRequiredSuccesses(List<string> diceColors, int rerolls, int requiredSuccesses, int iterations)
    {
        int totalSuccesses = 0;
        Dictionary<int, int> resultsFrequencyMap = new Dictionary<int, int>();
        List<GameObject> dicePool = new List<GameObject>();
        foreach (string dieColor in diceColors)
        {
            dicePool.Add(GetDieOfColor(dieColor).gameObject);
        }
        Unit uziUnit = Resources.Load<GameObject>("Prefabs/Units/UZI").GetComponent<Unit>();

        for (int i = 0; i < iterations; i++)
        {
            int rolledSuccesses = uziUnit.RollAndReroll(dicePool, rerolls, requiredSuccesses);

            if (rolledSuccesses >= requiredSuccesses)
            {
                totalSuccesses += 1;
            }

            if (resultsFrequencyMap.ContainsKey(rolledSuccesses))
            {
                resultsFrequencyMap[rolledSuccesses] += 1;
            }
            else
            {
                resultsFrequencyMap[rolledSuccesses] = 1;
            }
            //dieResults.Add(successes);

            //numberOfResults++;
            //currentAverage = (double)((currentAverage * (double)(numberOfResults - 1)) + successes) / (double)numberOfResults;
        }
        //double computedAverage = (double)dieResults.Sum() / (double)dieResults.Count;
        //string debugString = "FindProbabilityOfResult for resultToAvoid " + resultToAvoid.ToString() + " and desiredResult " + desiredResult.ToString() + " with " + dieColor + " die with " + rerolls.ToString() + " rerolls";
        //debugString += "\nprobability: " + ((double)resultOccurences / (double)(resultOccurences + notResultOccurences)).ToString() + " with " + resultOccurences.ToString() + " occurences of result over a total of " + (resultOccurences + notResultOccurences).ToString() + " over " + iterations.ToString() + " iterations.";
        //debugString += "\ncomputedAverage: " + computedAverage.ToString() + " calculated by dieResults.Sum() " + dieResults.Sum().ToString() + " / dieResults.Count " + dieResults.Count.ToString() +  " over " + dieResults.Count.ToString() + " iterations. DieResults: ";
        //foreach (int result in dieResults)
        //{
        //    debugString += result.ToString() + ", ";
        //}
        //Debug.Log(debugString);
        //return ((double)resultOccurences / (double)(resultOccurences + notResultOccurences)).ToString();
        Debug.Log("generatedProbabilityOfSuccess for requiredSuccesses " + requiredSuccesses.ToString() + ": " + totalSuccesses.ToString() + " / " + iterations.ToString() + " = " + ((double)totalSuccesses/(double)iterations).ToString());
        return resultsFrequencyMap;
    }

    public static void FindAverageDieResult(string dieColor, int rerolls, int iterations = 10)
    {
        Dice die = GetDieOfColor(dieColor);
        //List<int> dieResults = new List<int>();
        double currentAverage = 0;
        int numberOfResults = 0;
        for (int i = 0; i < iterations; i++)
        {
            int successes = die.Roll();
            for (int j = 0; j < rerolls; j++)
            {
                if (successes >= die.averageSuccesses)
                {
                    break;
                }
                else
                {
                    successes = die.Roll();
                }
            }
            //dieResults.Add(successes);

            numberOfResults++;
            currentAverage = (double)((currentAverage * (double)(numberOfResults - 1)) + successes) / (double)numberOfResults;
        }
        //double computedAverage = (double)dieResults.Sum() / (double)dieResults.Count;
        string debugString = "FindAverageDieResult " + dieColor + " die with " + rerolls.ToString() + " rerolls";
        debugString += "\ncurrentAverage: " + currentAverage.ToString() + " over " + numberOfResults.ToString() + " iterations.";
        //debugString += "\ncomputedAverage: " + computedAverage.ToString() + " calculated by dieResults.Sum() " + dieResults.Sum().ToString() + " / dieResults.Count " + dieResults.Count.ToString() +  " over " + dieResults.Count.ToString() + " iterations. DieResults: ";
        //foreach (int result in dieResults)
        //{
        //    debugString += result.ToString() + ", ";
        //}
        Debug.Log(debugString);
    }
}
