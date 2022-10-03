using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonPressAnim : MonoBehaviour {

	public Transform anyObject;
	public Vector3 offsetModifier;
	public float retractRate = 0.5f, pushRate = 5f;

	private Vector3 storedPosition;

	private float curProgress, curProgressReach;
	// Use this for initialization
	void Start () {
		storedPosition = anyObject.localPosition;
	}

	public void ButtonPush()
    {
		curProgressReach = 1f;
    }

	// Update is called once per frame
	void Update () {
		curProgressReach = Mathf.Max(curProgressReach - retractRate * Time.deltaTime, 0f);
		if (curProgress < curProgressReach)
		{
            curProgress += Time.deltaTime * pushRate;
			if (curProgress >= curProgressReach)
				curProgress = curProgressReach;
		}
		if (curProgress > curProgressReach)
        {
			curProgress -= Time.deltaTime * pushRate;
			if (curProgress <= curProgressReach)
				curProgress = curProgressReach;
		}
		anyObject.transform.localPosition = storedPosition + offsetModifier * curProgress;
	}
}
