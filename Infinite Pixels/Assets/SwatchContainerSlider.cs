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
    RectTransform rect;
    public Canvas canvas;
    public float swatchPrefabWidth;
    public GameObject swatchPrefab;

    GameObject leftSwatch;
    GameObject rightSwatch;
    GameObject selectedSwatch;
    ColourManager colourManager;

    List<GameObject> swatches = new List<GameObject>();

    // Use this for initialization
    void Start () {
        colourManager = GameObject.Find("PixelCanvas").GetComponent<ColourManager>();

        rect = this.gameObject.GetComponent<RectTransform>();

        float currentX = 0;
        for (int i = 0; i < 15; i++)
        {
            GameObject newSwatch = Instantiate(swatchPrefab, this.gameObject.transform);
            newSwatch.GetComponent<RectTransform>().localPosition = new Vector2(rect.position.x - rect.rect.width, 0);
            newSwatch.GetComponent<RectTransform>().Translate(new Vector2(currentX, 0));
            newSwatch.transform.GetChild(0).GetComponent<CanvasRenderer>().SetColor(colourManager.colours[i]);

            currentX += swatchPrefabWidth * canvas.scaleFactor;

            // First swatch, set edge
            if (i == 0) leftSwatch = newSwatch;
            if (i == 14) rightSwatch = newSwatch;

            swatches.Add(newSwatch);
        }
    }
	

    GameObject GetSwatchAtX(float x)
    {
        foreach (GameObject swatch in swatches)
        {
            float position = swatch.GetComponent<RectTransform>().position.x;
            if (x > position && x < position + swatchPrefabWidth * canvas.scaleFactor) return swatch;
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
            velocity = new Vector2(Input.mousePosition.x, Input.mousePosition.y) - lastPosition;
            lastPosition = Input.mousePosition;
           
        }

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
        }

        velocity *= drag;

        rect.anchoredPosition += new Vector2(velocity.x, 0);
    }
    
    public void MouseButtonDown()
    {
        lastPosition = Input.mousePosition;
        mouseDown = true;
    }

    public void MouseButtonUp()
    {
        mouseDown = false;
    }
}
