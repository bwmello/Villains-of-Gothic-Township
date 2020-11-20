using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;  // For button
using UnityEngine.Networking;  // For UnityWebRequest.EscapeURL of MyEscapeURL()
using TMPro;  // for TMP_Text to fetch the input text of ReportBugScreen
using System.IO;  // For File.ReadAllText for loading json save files
using Shapes2D;  // For Shape
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
    public GameObject continueAfterAttackButton;  // Not really used as Animate.PauseUntilPlayerPushesContinue refers to the button directly
    public GameObject endSetupButton;
    public GameObject setupPanel;
    public GameObject x5ScaleContainerPrefab;
    public GameObject gameOverPanel;
    public GameObject uiAnimationContainer;  // Thus far just used so AllySetup draggables aren't dragged behind one another

    public GameObject scenarioMap;  // Needed to get currentRound for OpenMenu()


    public void HideUIOverlay()
    {
        openMenuButton.SetActive(false);
        endSetupButton.SetActive(false);
        setupPanel.SetActive(false);
        roundClock.SetActive(false);
        utilityBelt.SetActive(false);
    }

    public void ShowUIOverlay()
    {
        switch (MissionSpecifics.currentPhase)
        {
            case "Setup":
                ShowSetupUIOverlay();
                break;
            case "Hero":
                ShowHeroUIOverlay();
                break;
        }
    }

    public void ShowHeroUIOverlay()
    {
        openMenuButton.SetActive(true);
        roundClock.SetActive(true);
        utilityBelt.SetActive(true);
    }

    public void InitializeSetupUIOverlay()
    {
        Dictionary<string, GameObject> unitPrefabsMasterDict = scenarioMap.GetComponent<ScenarioMap>().unitPrefabsMasterDict;
        //List<GameObject> potentialAllies = new List<GameObject>();  // For the not working, commented out "prevent the shit ton of collisions on load" thing
        foreach (string allyName in scenarioMap.GetComponent<ScenarioMap>().potentialAlliesList)
        {
            GameObject scaleContainer = Instantiate(x5ScaleContainerPrefab, setupPanel.transform);
            //potentialAllies.Add(Instantiate(unitPrefabsMasterDict[allyName], scaleContainer.transform));
            GameObject potentialAlly = Instantiate(unitPrefabsMasterDict[allyName], scaleContainer.transform);
            potentialAlly.GetComponent<Unit>().isHeroAlly = true;  // To make these blue instead of red
            Shape potentialAllyShape = potentialAlly.GetComponent<Shape>();
            potentialAllyShape.settings.roundness = 10;
            potentialAllyShape.settings.outlineSize = 1;
            //potentialAllyShape.ComputeAndApply();  // Doesn't seem to change anything, outlineSize/roundness changes aren't applied until GameObject selected in editor
            potentialAlly.GetComponent<Draggable>().draggableType = "AllySetup";
            potentialAlly.GetComponent<Unit>().SetIsClickable(false);  // Redundant, but keep it just in case
            potentialAlly.GetComponent<Unit>().SetIsDraggable(true);
        }
        //foreach (GameObject potentialAlly in potentialAllies)  // Doesn't work, as I believe all this still happens before a frame update. Intention: Wait until all potentialAllies are created before SetIsDraggable(true), else you'll get a shit ton of collisions on load
        //{
        //    potentialAlly.GetComponent<Unit>().SetIsDraggable(true);
        //}
    }

    public void ShowSetupUIOverlay()
    {
        openMenuButton.SetActive(true);
        endSetupButton.SetActive(true);
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
