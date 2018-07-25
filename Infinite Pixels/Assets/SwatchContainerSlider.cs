using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SwatchContainerSlider : MonoBehaviour {
    bool mouseDown = false;
    Vector2 lastPosition = new Vector2();
    Vector2 velocity = new Vector2();
    public float drag = 0.95f;
    public float minDistanceToBeConsideredDrag;
    RectTransform rect;
    public Canvas canvas;
    public float swatchPrefabWidth;
    public GameObject swatchPrefab;
    public float targettingOffset; // This offset is applied to nudge the swatch inside the selector
    public float snapSpeed; // Speed to snap the swatch to the chosen colour
    public EventSystem eventSystem;
    public GraphicRaycaster raycaster;
    public float thresholdBeforeSnappingSwatch;

    GameObject leftSwatch;
    GameObject rightSwatch;
    GameObject selectedSwatch;
    ColourManager colourManager;

    List<GameObject> swatches = new List<GameObject>();

    // Target position to lerp the swatch to

    bool isMovingToTarget = false;
    bool wasJustDragged = false;
    Vector2 startedClickAt;
    // Target position expressed as rect.anchoredPosition
    Vector2 targetPosition;

    // Use this for initialization
    void Start () {
        Debug.Log("Screen width: " + Screen.width);
        colourManager = GameObject.Find("PixelCanvas").GetComponent<ColourManager>();

        rect = this.gameObject.GetComponent<RectTransform>();

        float currentX = 0;
        for (int i = 0; i < 15; i++)
        {
            GameObject newSwatch = Instantiate(swatchPrefab, this.gameObject.transform);
            newSwatch.GetComponent<RectTransform>().localPosition = new Vector2(0, 0);
            //newSwatch.GetComponent<RectTransform>().localPosition = new Vector2(rect.position.x - rect.rect.width, 0);
            newSwatch.GetComponent<RectTransform>().Translate(new Vector2(currentX, 0));
            newSwatch.name = "Swatch" + i;
            newSwatch.transform.GetChild(0).GetComponent<CanvasRenderer>().SetColor(colourManager.colours[i]);

            currentX += swatchPrefabWidth * canvas.scaleFactor;

            // First swatch, set edge
            if (i == 0) leftSwatch = newSwatch;
            if (i == 14) rightSwatch = newSwatch;

            swatches.Add(newSwatch);
        }
    }
	
    // Raycasts on the plane of the swatch to see if it hits any colors, returns null otherwise
    GameObject GetSwatchAtX(float x)
    {
        /*
        foreach (GameObject swatch in swatches)
        {
            float position = swatch.transform.position.x;
            if (x > position && x < position + swatchPrefabWidth * canvas.scaleFactor) return swatch;
        }

        return null;*/

        //Set up the new Pointer Event
        PointerEventData m_PointerEventData = new PointerEventData(eventSystem);
        //Set the Pointer Event Position to that of the mouse position
        m_PointerEventData.position = new Vector2(x, this.transform.position.y + 5);

        //Create a list of Raycast Results
        List<RaycastResult> results = new List<RaycastResult>();

        //Raycast using the Graphics Raycaster and mouse click position
        raycaster.Raycast(m_PointerEventData, results);

        //For every result returned, output the name of the GameObject on the Canvas hit by the Ray
        foreach (RaycastResult result in results)
        {
            if (result.gameObject.tag == "Swatch") return result.gameObject.transform.parent.gameObject;
        }

        return null;
    }

	// Update is called once per frame
	void Update () {
        GameObject newSelectedSwatch = GetSwatchAtX(Screen.width / 2);
        
        if (newSelectedSwatch != null && newSelectedSwatch != selectedSwatch)
        {
            newSelectedSwatch.GetComponent<Animator>().SetBool("Selected", true);
            if (selectedSwatch != null) selectedSwatch.GetComponent<Animator>().SetBool("Selected", false);
            selectedSwatch = newSelectedSwatch;
            colourManager.OnColourChanged(swatches.IndexOf(selectedSwatch));
        }


        

        if (mouseDown)
        {
            isMovingToTarget = false;
            velocity = new Vector2(Input.mousePosition.x, Input.mousePosition.y) - lastPosition;
            lastPosition = Input.mousePosition;
            wasJustDragged = true;
           
        }

        if (isMovingToTarget)
        {
            rect.anchoredPosition = Vector2.SmoothDamp(rect.anchoredPosition, targetPosition, ref velocity, snapSpeed, Mathf.Infinity, Time.deltaTime);
            if (Vector2.Distance(rect.anchoredPosition, targetPosition) < 0.01f) isMovingToTarget = false;
            //this.transform.position = Vector3.SmoothDamp(this.transform.position, targetPosition, ref followerVelocity, followLooseness);
        }
        else
        {
            velocity *= drag;
            rect.anchoredPosition += new Vector2(velocity.x, 0);
            if (rect.anchoredPosition.x > 0) rect.anchoredPosition = new Vector2(0, rect.anchoredPosition.y);
            if (rect.anchoredPosition.x < -(swatchPrefabWidth * 15)) rect.anchoredPosition = new Vector2(-(swatchPrefabWidth * 15), rect.anchoredPosition.y);

            if (velocity.magnitude < thresholdBeforeSnappingSwatch && wasJustDragged)
            {
                if (selectedSwatch == null) return;
                wasJustDragged = false;
                isMovingToTarget = true;
                
                
                targetPosition = new Vector2(-selectedSwatch.transform.localPosition.x, rect.anchoredPosition.y);
            }
        }

        /*
        float screenPositionOfRightSide = rightSwatch.GetComponent<RectTransform>().position.x + swatchPrefabWidth * canvas.scaleFactor;
        float screenPositionOfLeftSide = leftSwatch.GetComponent<RectTransform>().position.x;


        if (screenPositionOfRightSide < Screen.width/2)
        {
            float distanceFromRightSideToEdge = Screen.width/2 - screenPositionOfRightSide;
            velocity.x += distanceFromRightSideToEdge / 10f;
        }


        if (screenPositionOfLeftSide > Screen.width/2)
        {
            float distanceFromLeftSideToEdge = screenPositionOfLeftSide - Screen.width/2;
            velocity.x -= distanceFromLeftSideToEdge / 10f;
        }*/

    }

    public void MouseButtonDown()
    {
        lastPosition = Input.mousePosition;
        startedClickAt = Input.mousePosition;
        mouseDown = true;
    }

    public void MouseButtonUp()
    {
        mouseDown = false;

        Vector2 delta = startedClickAt - new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        if (delta.magnitude < minDistanceToBeConsideredDrag)
        {
            Clicked();
        }

        lastPosition = Input.mousePosition;
    }

    public void Clicked()
    {
        Debug.Log("Click!");
        Vector2 position = Input.mousePosition;
        GameObject clickedSwatch = GetSwatchAtX(position.x);


        if (clickedSwatch == null) return; // If the player clicked outside a swatch

        // Set target to swatch
        targetPosition = new Vector2(-clickedSwatch.transform.localPosition.x, rect.anchoredPosition.y);
        isMovingToTarget = true;

        //rect.anchoredPosition = new Vector2(distanceToCover, rect.anchoredPosition.y);

    }
}
