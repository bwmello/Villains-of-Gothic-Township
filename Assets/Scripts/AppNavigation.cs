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

    public void ReportBugCancelButtonClicked()
    {
        transform.GetComponentInParent<ScenarioMap>().EnablePlayerUI();
        Destroy(gameObject);  // Destroys report bug panel
    }

    public void ReportBugSendButtonClicked(GameObject inputTextField)
    {
        //SendSmtpEmail(inputTextField.GetComponent<TMP_InputField>().text);
        SendEmail(inputTextField.GetComponent<TMP_InputField>().text);
        ReportBugCancelButtonClicked();
    }

    public void SendSmtpEmail(string msgBody)  // Email and password visible to network sniffers. May need OAuth 2.0 credentials to get around password security risk.
    {
        SmtpClient smtpServer = new SmtpClient("smtp.gmail.com");
        smtpServer.Port = 587;
        smtpServer.Credentials = new System.Net.NetworkCredential("blaineenterprisesofgothiccity@gmail.com", "PASSWORD") as ICredentialsByHost;
        smtpServer.EnableSsl = true;
        ServicePointManager.ServerCertificateValidationCallback =
        delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        { return true; };

        MailMessage mail = new MailMessage();
        mail.From = new MailAddress("blaineenterprisesofgothiccity@gmail.com");
        mail.To.Add("blaineenterprisesofgothiccity@gmail.com");
        mail.Subject = "Bug report";
        mail.Body = msgBody;

        int roundOfCurrentGame = PlayerPrefs.GetInt(MissionSpecifics.missionName);
        for (int i = 1; i <= roundOfCurrentGame; i++)
        {
            string saveName = i.ToString() + MissionSpecifics.missionName + ".json";
            string filePath = Application.persistentDataPath + "/" + saveName;
            Attachment attachement = new Attachment(filePath);
            mail.Attachments.Add(attachement);
        }

        smtpServer.Send(mail);  // For non-Development Builds on Android, "Internet Access" in Player Settings must be set to "Require" or email never sent
    }

    public void SendEmail(string msgBody)
    {
        string email = "blaineenterprisesofgothiccity@gmail.com";
        string subject = MyEscapeURL("Bug Report");

        //int roundOfCurrentGame = PlayerPrefs.GetInt(MissionSpecifics.missionName);  // Gets incorrect round if player just started new game or rewinds clock
        int roundOfCurrentGame = transform.parent.GetComponentInParent<ScenarioMap>().currentRound;  // Only works if gameObject in animationContainer
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
}
