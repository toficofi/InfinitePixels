using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ReportingManager : MonoBehaviour {
    MenuController menu;
    public NetworkManagerScript network;
    public int secondsBeforeClosingMenu = 5;
    public Text reportButtonText;
    public Texture2D image;
    string originalReportButtonText;
    bool sending = false;
    bool sent = false;


	// Use this for initialization
	void Start () {
        menu = GetComponent<MenuController>();
        
        // Save the original report button text so we can restore it after changing text to "sending..."
        originalReportButtonText = reportButtonText.text;
	}
	
    public void ReportAcceptButtonClicked()
    {
        if (sending || sent) return;

        Debug.Log("Sending report...");

        sending = true;
        reportButtonText.text = "SENDING...";
        StartCoroutine(SendReport());
        reportButtonText.text = "SENT :)";
        sending = false;
        sent = true;
        StartCoroutine(CloseMenuAfterSeconds(secondsBeforeClosingMenu));
    }

    public void CloseMenu()
    {
        StopAllCoroutines(); // So it doesn't still try to close the menu after the timeout

        sending = false;
        sent = false;
        reportButtonText.text = originalReportButtonText;
        menu.CloseMenu();
    }

    public void ReportCancelButtonClicked()
    {
        if (sending) return;
        CloseMenu();
    }

    IEnumerator CloseMenuAfterSeconds(int seconds)
    {
        yield return new WaitForSeconds(seconds);
        CloseMenu();
    }


    public IEnumerator SendReport()
    {
        WWWForm form = new WWWForm();
        form.AddField("hash", network.uniqueIdentifier);
        form.AddField("x", (int)network.tvDude.gameObject.transform.position.x);
        form.AddField("y", (int)network.tvDude.gameObject.transform.position.z);
        form.AddBinaryData("image", image.EncodeToPNG());
        Debug.Log("http://" + network.GetHost() + ":69/report");
        UnityWebRequest www = UnityWebRequest.Post("http://" + network.GetHost() + ":69/report", form);
        yield return www.SendWebRequest();

        Debug.Log("Got back " + www.error);
    }

	// Update is called once per frame
	void Update () {
		
	}
}
