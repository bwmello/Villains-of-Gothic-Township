﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;  // For button
using UnityEngine.Networking;  // For UnityWebRequest.EscapeURL of MyEscapeURL()
using TMPro;  // for TMP_Text to fetch the input text of ReportBugScreen
using System.IO;  // For File.ReadAllText for loading json save files
//using Shapes2D;  // "The type or namespace name 'Shape2D' could not be found"
/*   Much of below needed for SendSmtpEmail
using System.Net.Mail;  // For MailMessage of SendSmtpEmail()
using System.Net;  // For ICredentialsByHost of SendSmtpEmail()
using System.Net.Security;  // For SslPolicyErrors of SendSmtpEmail()
using System.Security.Cryptography.X509Certificates;  // For X509Certificate of SendSmtpEmail()
*/

public class UIOverlay : MonoBehaviour
{
    public GameObject roundClock;
    public GameObject clockHand;
    readonly float clockHandOffset = 2;
    public GameObject utilityBelt;
    public GameObject openMenuButton;
    public GameObject menuPanel;
    public GameObject reportBugPanel;
    public GameObject endSetupButton;
    public GameObject setupPanel;
    public GameObject x5ScaleContainerPrefab;
    public GameObject gameOverPanel;

    public GameObject scenarioMap;  // Needed to get currentRound for OpenMenu()


    public void HideUIOverlay()
    {
        roundClock.SetActive(false);  // Is this necessarry?
        utilityBelt.SetActive(false);
        openMenuButton.SetActive(false);
    }

    public void ShowUIOverlay()
    {
        roundClock.SetActive(true);
        utilityBelt.SetActive(true);
        openMenuButton.SetActive(true);
    }

    public void ShowSetupUIOverlay()
    {
        openMenuButton.SetActive(true);
        endSetupButton.SetActive(true);
        Dictionary<string, GameObject> unitPrefabsMasterDict = scenarioMap.GetComponent<ScenarioMap>().unitPrefabsMasterDict;
        foreach (string allyName in scenarioMap.GetComponent<ScenarioMap>().potentialAlliesList)
        {
            GameObject scaleContainer = Instantiate(x5ScaleContainerPrefab, setupPanel.transform);
            GameObject potentialAlly = Instantiate(unitPrefabsMasterDict[allyName], scaleContainer.transform);
            //Shapes2D.Shape shape = potentialAlly.GetComponent<Shapes2D.Shape>();  // Not working, see "using Shapes2D" at top of file
            potentialAlly.GetComponent<Draggable>().draggableType = "AllySetup";
            potentialAlly.GetComponent<Unit>().SetIsClickable(false);
            potentialAlly.GetComponent<Unit>().SetIsDraggable(true);
        }
        setupPanel.SetActive(true);
    }

    public void HideSetupUIOverlay()
    {
        endSetupButton.SetActive(false);
        setupPanel.SetActive(false);
    }

    public void EndSetupButtonClicked()
    {
        HideSetupUIOverlay();
        scenarioMap.GetComponent<ScenarioMap>().StartFirstTurn();
    }

    public void SetClock(int currentHour)
    {
        float startingClockHandAngle = -(currentHour * 30) + clockHandOffset;
        clockHand.transform.eulerAngles = new Vector3(0, 0, startingClockHandAngle);
    }

    public IEnumerator AdvanceClock(int nextHour)
    {
        int previousHour = nextHour - 1;
        float previousClockHandAngle = -(previousHour * 30) + clockHandOffset;
        float newClockHandAngle = -(nextHour * 30) + clockHandOffset;
        yield return StartCoroutine(TurnClockHand(previousClockHandAngle, newClockHandAngle));
        yield return 0;
    }

    IEnumerator TurnClockHand(float currentAngle, float newAngle)
    {
        float t = 0;
        float uncoverTime = .5f;

        while (t < 1f)
        {
            t += Time.deltaTime * uncoverTime;

            float angle = Mathf.LerpAngle(currentAngle, newAngle, t);
            clockHand.transform.eulerAngles = new Vector3(0, 0, angle);

            yield return null;
        }

        yield return 0;
    }

    public void OpenMenu()
    {
        HideUIOverlay();
        Camera.main.GetComponent<PanAndZoom>().controlCamera = false;
        int currentRound = scenarioMap.GetComponent<ScenarioMap>().currentRound;
        if (currentRound > 1)
        {
            menuPanel.transform.Find("TurnBackClockButton").gameObject.SetActive(true);
        }
        else
        {
            menuPanel.transform.Find("TurnBackClockButton").gameObject.SetActive(false);
        }
        menuPanel.SetActive(true);
    }

    public void CloseMenu()
    {
        ShowUIOverlay();
        Camera.main.GetComponent<PanAndZoom>().controlCamera = true;
        menuPanel.SetActive(false);
    }

    public void ReportBugButtonClicked()
    {
        menuPanel.SetActive(false);  // Don't call CloseMenu(), let bugReportPanel EnablePlayerUI()
        reportBugPanel.SetActive(true);
    }

    public void ReportBugCancelButtonClicked()
    {
        GameObject inputTextField = reportBugPanel.transform.Find("InputField (TMP)").gameObject;
        inputTextField.GetComponent<TMP_InputField>().text = "";
        //transform.GetComponentInParent<ScenarioMap>().EnablePlayerUI();
        ShowUIOverlay();
        Camera.main.GetComponent<PanAndZoom>().controlCamera = true;
        reportBugPanel.SetActive(false);
    }

    public void ReportBugSendButtonClicked()
    {
        GameObject inputTextField = reportBugPanel.transform.Find("InputField (TMP)").gameObject;
        //SendSmtpEmail(inputTextField.GetComponent<TMP_InputField>().text);
        SendEmail(inputTextField.GetComponent<TMP_InputField>().text);
        ReportBugCancelButtonClicked();
    }

    //public void SendSmtpEmail(string msgBody)  // Email and password visible to network sniffers. May need OAuth 2.0 credentials to get around password security risk.
    //{
    //    SmtpClient smtpServer = new SmtpClient("smtp.gmail.com");
    //    smtpServer.Port = 587;
    //    smtpServer.Credentials = new System.Net.NetworkCredential("blaineenterprisesofgothiccity@gmail.com", "PASSWORD") as ICredentialsByHost;
    //    smtpServer.EnableSsl = true;
    //    ServicePointManager.ServerCertificateValidationCallback =
    //    delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    //    { return true; };

    //    MailMessage mail = new MailMessage();
    //    mail.From = new MailAddress("blaineenterprisesofgothiccity@gmail.com");
    //    mail.To.Add("blaineenterprisesofgothiccity@gmail.com");
    //    mail.Subject = "Bug report";
    //    mail.Body = msgBody;

    //    int roundOfCurrentGame = PlayerPrefs.GetInt(MissionSpecifics.missionName);
    //    for (int i = 1; i <= roundOfCurrentGame; i++)
    //    {
    //        string saveName = i.ToString() + MissionSpecifics.missionName + ".json";
    //        string filePath = Application.persistentDataPath + "/" + saveName;
    //        Attachment attachement = new Attachment(filePath);
    //        mail.Attachments.Add(attachement);
    //    }

    //    smtpServer.Send(mail);  // For non-Development Builds on Android, "Internet Access" in Player Settings must be set to "Require" or email never sent
    //}

    public void SendEmail(string msgBody)
    {
        string email = "blaineenterprisesofgothiccity@gmail.com";
        string subject = MyEscapeURL("Bug Report");

        //int roundOfCurrentGame = PlayerPrefs.GetInt(MissionSpecifics.missionName);  // Gets incorrect round if player just started new game or rewinds clock
        int roundOfCurrentGame = scenarioMap.GetComponentInParent<ScenarioMap>().currentRound;  // Only works if gameObject in animationContainer
        msgBody += "\n\n\nBelow are the boardstates so your issue can be reproduced.\n";
        for (int i = 1; i <= roundOfCurrentGame; i++)
        {
            string saveName = i.ToString() + MissionSpecifics.missionName + ".json";
            msgBody += "\n" + i.ToString() + MissionSpecifics.missionName + "\n" + File.ReadAllText(Application.persistentDataPath + "/" + saveName);
        }
        //Debug.Log("mailto:" + email + "?subject=" + subject + "&body=" + MyEscapeURL(msgBody));
        Application.OpenURL("mailto:" + email + "?subject=" + subject + "&body=" + MyEscapeURL(msgBody));

        //Debug.Log("mailto:" + email + "?subject=" + subject + "&body=" + body + "&Attachment=" + filePath);
        //Application.OpenURL("mailto:" + email + "?subject=" + subject + "&body=" + body + "&Attachment=" + filePath);
    }

    string MyEscapeURL(string URL)
    {
        return UnityWebRequest.EscapeURL(URL).Replace("+", "%20");
    }

    public void ShowGameOverPanel()
    {
        Camera.main.GetComponent<PanAndZoom>().controlCamera = false;
        Camera.main.orthographicSize = 100;  // Just like animate.CameraToMaxZoom()
        Camera.main.transform.position = new Vector3(0, 0, Camera.main.transform.position.z);
        if (MissionSpecifics.IsHeroVictory())
        {
            gameOverPanel.transform.Find("MissionStatusText").GetComponent<TMP_Text>().text = "<color=\"green\">Mission Success";
        }
        else
        {
            gameOverPanel.transform.Find("MissionStatusText").GetComponent<TMP_Text>().text = "<color=\"red\">Mission Failure";
        }
        HideUIOverlay();
        gameOverPanel.SetActive(true);
    }
}
