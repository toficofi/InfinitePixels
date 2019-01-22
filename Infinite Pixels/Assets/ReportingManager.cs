using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReportingManager : MonoBehaviour {
    MenuController menu;
    public int secondsBeforeClosingMenu = 5;
    public Text reportButtonText;
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
        SendReport(null); 
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


    public bool SendReport(Texture2D image)
    {
        // Upload report to servers
        return true;
    }

	// Update is called once per frame
	void Update () {
		
	}
}
