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
}
