using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideHolograms : MonoBehaviour {

	// Use this for initialization
	void Start () {
        // toggles the visibility of this gameobject and all it's children
        var renderers = this.gameObject.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            r.enabled = !r.enabled;
        }
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
