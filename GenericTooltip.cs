﻿using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Text.RegularExpressions;

//Originally created by Patrick Scott

/**Requires GenericTooltipPool and a prefab in Resources called "Tooltip" with an Image and a Text as children.*/
public class GenericTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{

    public enum Direction
    {
        Above,
        Below,
        Left,
        Right
    }

    public Direction direction = Direction.Left;
    public float offset = 0.5f;
    public string tooltipTitle;
    [TextArea]
    public string
        tooltipText;

    [Tooltip("If true (default), will child the tooltip to this gameObject. Doesn't work well with layout groups, so turn it off for those.")]
    public bool
        setParent = true;
    public bool useIPointerCalls = true;
    public bool useOnMouseEnter = false;
    /**Please only give one prerequisite. Only the last input delegate gets checked in Funcs. */
    public System.Func<bool> prerequisiteToOpen;

    [Tooltip("Ignore the GenericTooltipPool's set values.")]
    public bool
        overridePoolValues;
    public float waitDelay, fadeIn, fadeOut;

    public bool activeTip { get; private set; }

    /**Great for audio calls */
    public System.Action onFadeIn;

    /**Any effects, recoloring, or other processing on the text? Do it here */
    public System.Func<string, string> textProcessing;

    [HideInInspector]
    public bool
        lockedFromOpenClose;

    private GameObject currentTooltip;
    private Image panel;
    private Text text;


    /*
	 * PUBLIC CALLS
	 */

    /**Call the tooltip! Puts it adjacent to the the object (in 'direction'), as a child.
     */
    public void OpenTooltip()
    {
        //Make sure we can see it (not locked, tooltips allowed)
        if (!lockedFromOpenClose && (!PlayerPrefs.HasKey("Tooltips" || PlayerPrefs.GetInt("Tooltips") == 1)))
        {
            if (currentTooltip == null)
                currentTooltip = GenericTooltipPool.GetFreshTooltip();
            else
                currentTooltip.SetActive(true);

            panel = currentTooltip.GetComponentInChildren<Image>();
            text = currentTooltip.GetComponentInChildren<Text>();

            //Position
            currentTooltip.transform.SetParent(transform);
            SetPosition(Vector3.zero);

            //Override parent and rotation for GUI Main tips
            if (!setParent)
            {
                currentTooltip.transform.SetParent(transform.root);
                currentTooltip.transform.localEulerAngles = new Vector3(0, Camera.main.transform.eulerAngles.y, 0);
            }

            //Content
            UpdateText();

            //Alpha
            panel.CrossFadeAlpha(0, 0, true);	//Reset
            text.CrossFadeAlpha(0, 0, true);	//Reset
            StopCoroutine("FadeOut");
            StopCoroutine("ReturnTooltip");
            StartCoroutine("FadeIn", waitDelay);
        }
    }

    /**Return the tooltip to the pool.
	 */
    public void CloseTooltip()
    {
        if (!lockedFromOpenClose)
        {
            StopCoroutine("FadeIn");
            StartCoroutine("FadeOut", waitDelay);
            StartCoroutine("ReturnTooltip", fadeOut + waitDelay);
        }
    }



    /*
	 * FUNCTIONAL
	 */

    void SetPosition(Vector3 startingLocal)
    {
        if (currentTooltip != null)
        {
            RectTransform rect = currentTooltip.GetComponent<RectTransform>();

            switch (direction)
            {
                case Direction.Above:
                    rect.pivot = new Vector2(0.5f, 0);
                    currentTooltip.transform.localPosition = new Vector3(startingLocal.x, startingLocal.y + offset, 0);
                    break;
                case Direction.Below:
                    rect.pivot = new Vector2(0.5f, 1);
                    currentTooltip.transform.localPosition = new Vector3(startingLocal.x, startingLocal.y - offset, 0);
                    break;
                case Direction.Left:
                    rect.pivot = new Vector2(1, 0.5f);
                    currentTooltip.transform.localPosition = new Vector3(startingLocal.x - offset, startingLocal.y, 0);
                    break;
                case Direction.Right:
                    rect.pivot = new Vector2(0, 0.5f);
                    currentTooltip.transform.localPosition = new Vector3(startingLocal.x + offset, startingLocal.y, 0);
                    break;
            }
        }
    }

    void UpdateText()
    {
        if (currentTooltip != null)
        {
            //Clear
            text.text = "";
            //Title
            if (tooltipTitle.Trim() != "")
            {
                text.text += "<size=16>" + tooltipTitle + "</size>";

                //Space for body
                if (tooltipText != "")
                    text.text += "\n";
            }
			
            //Body
            if (tooltipText != "")
                text.text += tooltipText;

            if (textProcessing != null)
                text.text = textProcessing.Invoke(text.text);
        }
    }

    IEnumerator FadeIn(float time)
    {
        //Earliest point the tip is active and needs text updates
        activeTip = true;

        //Beginning FadeIn, do actions. For example: audio
        if (onFadeIn != null)
            onFadeIn.Invoke();

        yield return new WaitForSecondsRealtime(time);

        if (panel && text)
        {
            text.CrossFadeAlpha(1, fadeIn, true);       //Fade
            panel.CrossFadeAlpha(0.75f, fadeIn, true);  //Fade
        }
    }

    IEnumerator FadeOut(float time)
    {
        yield return new WaitForSecondsRealtime(time);

        if (panel && text)
        {
            text.CrossFadeAlpha(0, fadeOut, true);   //Fade
            panel.CrossFadeAlpha(0, fadeOut, true);  //Fade
        }
    }

    IEnumerator ReturnTooltip(float time)
    {
        yield return new WaitForSecondsRealtime(time);

        DirectReturnTooltip();
    }

    void DirectReturnTooltip()
    {
        GenericTooltipPool.ReturnTooltip(currentTooltip);
        currentTooltip = null;

        //Latest point the tip is active and can cease text updates
        activeTip = false;
    }


    /*
	 * AUTOMATIC CALLS
	 */


    public void OnPointerEnter(PointerEventData data)
    {
        if (useIPointerCalls)
        {
            if (prerequisiteToOpen == null || prerequisiteToOpen.Invoke())
                OpenTooltip();
        }
    }

    public void OnPointerExit(PointerEventData data)
    {
        if (useIPointerCalls)
        {
            CloseTooltip();
        }
    }

    //Will call the tooltip, if appropriate
    void OnMouseEnter()
    {
        if (useOnMouseEnter)
        {
            if (prerequisiteToOpen == null || prerequisiteToOpen.Invoke())
                OpenTooltip();
        }
    }

    //Will update the tooltip text while still active. Also does a safety open call.
    void OnMouseOver()
    {
        if (useOnMouseEnter)
        {
            if (currentTooltip == null)
            {
                if (prerequisiteToOpen == null || prerequisiteToOpen.Invoke())
                    OpenTooltip();
            }

            //Sync to mouse now for mouse control NEEDS WORK
            //SetPosition(Camera.main.ScreenToWorldPoint(Input.mousePosition));
            //Keep updated
            UpdateText();
        }
    }

    //Will return the tooltip, if appropriate
    void OnMouseExit()
    {
        if (useOnMouseEnter)
        {
            CloseTooltip();
        }
    }

    void Start()
    {
        if (!overridePoolValues)
        {
            waitDelay = GenericTooltipPool.waitDelay;
            fadeIn = GenericTooltipPool.fadeIn;
            fadeOut = GenericTooltipPool.fadeOut;
        }
    }

    void OnDisable()
    {
        if (activeTip)
            DirectReturnTooltip();
    }
}
