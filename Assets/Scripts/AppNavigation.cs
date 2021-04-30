using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;  // For SceneManager for switching scenes
using UnityEngine.Networking;  // For UnityWebRequest.EscapeURL of MyEscapeURL()
using System.Net.Mail;  // For MailMessage of SendSmtpEmail()
using System.Net;  // For ICredentialsByHost of SendSmtpEmail()
using System.Net.Security;  // For SslPolicyErrors of SendSmtpEmail()
using System.Security.Cryptography.X509Certificates;  // For X509Certificate of SendSmtpEmail()
using System.IO;  // For File.ReadAllText for loading json save files
using TMPro;  // for TMP_Text to fetch the input text


public class AppNavigation : MonoBehaviour
{
    //// Update is called once per frame
    //void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.Escape) == true)
    //    {
    //        ExitApp();
    //    }
    //}

    public void ExitApp()
    {
        Application.Quit();
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("StartMenu");
    }

    public void AverageChanceOfSuccess()
    {
        List<string> dieColors = new List<string>{ "yellow", "yellow" };
        int requiredSuccesses = 3;
        int rerolls = 0;
        int iterations = 1000000000;  // 1 billion
        //int iterations = 100000000;  // 100 million
        //int iterations = 1000000;  // 1 million
        Dictionary<int, int> resultsFrequencyMap = DiceMath.GenerateProbabilityOfSuccessForRequiredSuccesses(dieColors, rerolls, requiredSuccesses, iterations);
        foreach (KeyValuePair<int, int> resultFrequency in resultsFrequencyMap)
        {
            Debug.Log("Probability of getting " + resultFrequency.Key.ToString() + " successes: " + resultFrequency.Value.ToString() + " / " + iterations.ToString() + " = " + ((double)resultFrequency.Value / (double)iterations).ToString());
        }
    }

    public void AverageDice()
    {
        string dieColor = "white";
        int desiredResult = 2;
        //int rerolls = 1;
        int iterations = 1000000000;  // 1 billion
        //int iterations = 1000000;  // 1 million
        string debugString = "GenerateProbabilityOfResultsToAvoid(" + dieColor + ", desiredResult:" + desiredResult.ToString() + ")";
        //for (int i = 0; i < 3; i++)
        //{
        //    debugString += "\n For result " + i.ToString() + ":  ";
        //    for (int j = 0; j < 4; j++)
        //    {
        //        debugString += "\t" + die.GetProbabilityOfResult(i, j).ToString() + " with " + j.ToString() + " rerolls;";
        //    }
        //    //DiceMath.FindProbabilityOfResult(dieColor, desiredResult, i, iterations);
        //}
        //Debug.Log(debugString);
        //DiceMath.FindAverageDieResult(dieColor, rerolls, iterations: iterations);
        Dictionary<int, Dictionary<int, List<double>>> dieProbabilityOfResultToAvoid = new Dictionary<int, Dictionary<int, List<double>>>();
        for (int i = 0; i < 11; i++)
        {
            Dictionary<int, int> resultsFrequencyMap = DiceMath.GenerateProbabilityOfResultsToAvoid(dieColor, desiredResult, i, iterations);
            foreach (KeyValuePair<int, int> resultFrequency in resultsFrequencyMap)
            {
                if (!dieProbabilityOfResultToAvoid.ContainsKey(resultFrequency.Key))
                {
                    dieProbabilityOfResultToAvoid[resultFrequency.Key] = new Dictionary<int, List<double>> { { desiredResult, new List<double>() } };
                }
                dieProbabilityOfResultToAvoid[resultFrequency.Key][desiredResult].Add((double)resultFrequency.Value / (double)iterations);
            }
        }

        foreach (KeyValuePair<int, Dictionary<int, List<double>>> resultToAvoidProbabilities in dieProbabilityOfResultToAvoid)
        {
            debugString += "\n{ " + resultToAvoidProbabilities.Key.ToString() + ", new Dictionary<int, List<double>> {";
            foreach (KeyValuePair<int, List<double>> desiredResultProbabilities in resultToAvoidProbabilities.Value)
            {
                debugString += "\n\t{ " + desiredResult.ToString() + ", new List<double> { ";
                foreach (double probability in desiredResultProbabilities.Value)
                {
                    debugString += probability.ToString() + ", ";
                }
                debugString += "}";
            }
            debugString += "\n}";
        }
        //for (int i = 0; i < 11; i++)
        //{
        //    debugString += DiceMath.FindProbabilityOfResultToAvoid(dieColor, resultToAvoid, desiredResult, i, iterations) + ", ";
        //}
        //debugString += "}";
        Debug.Log(debugString);
    }
}
