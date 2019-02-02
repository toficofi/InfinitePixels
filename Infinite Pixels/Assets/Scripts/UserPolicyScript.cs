using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UserPolicyScript : MonoBehaviour
{
    public int policyLastUpdatedAt;
    public GameObject loadingScreen;

    // Start is called before the first frame update
    void Start()
    {
        // When the policy was last accepted
        int acceptedPolicyAt = PlayerPrefs.GetInt("AcceptedPolicyAt", 0);
        Debug.Log("Accepted policy at: " + acceptedPolicyAt);
        Debug.Log("Policy updated at: " + policyLastUpdatedAt);

        if (acceptedPolicyAt > policyLastUpdatedAt)
        {
            // Player already accepted policy, skip to game
            SceneManager.LoadScene("Main");
        } else
        {
            loadingScreen.SetActive(false);
        }
    }

    public void AcceptPolicy()
    {
        PlayerPrefs.SetInt("AcceptedPolicyAt", Util.UnixTime());
        SceneManager.LoadScene("Main");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
