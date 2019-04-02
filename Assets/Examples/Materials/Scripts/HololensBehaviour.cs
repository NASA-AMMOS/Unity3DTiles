/*
 * "Copyright 2018, by the California Institute of Technology. ALL RIGHTS 
 * RESERVED. United States Government Sponsorship acknowledged. Any 
 * commercial use must be negotiated with the Office of Technology 
 * Transfer at the California Institute of Technology.
 * 
 * This software may be subject to U.S.export control laws.By accepting 
 * this software, the user agrees to comply with all applicable 
 * U.S.export laws and regulations. User has the responsibility to 
 * obtain export licenses, or other export authority as may be required 
 * before exporting such information to foreign countries or providing 
 * access to foreign persons."
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HololensBehaviour : MonoBehaviour {

    [SerializeField]
    GameObject pointer = null;

    float lastHitDistance = 2;

	// Use this for initialization
	void Start () {
#if UNITY_WSA
#if UNITY_2017_2_OR_NEWER
        // ActivateLatentFramePresentation depricated
#else
        // Needed at least for 2017.1 to avoid frame drop to 30 FPS whenver text is in the scene
        UnityEngine.VR.WSA.HolographicSettings.ActivateLatentFramePresentation(true);
#endif
#endif

    }

    // Update is called once per frame
    void Update () {
       
        var ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if (pointer != null)
            {
                pointer.transform.position = hit.point;
            }

            lastHitDistance = (Camera.main.transform.position - hit.point).magnitude;
        }
#if UNITY_WSA
        Vector3 normal = -Camera.main.transform.forward;
        Vector3 focusPoint = Camera.main.transform.position + Camera.main.transform.forward * lastHitDistance;
#if UNITY_2017_2_OR_NEWER
        UnityEngine.XR.WSA.HolographicSettings.SetFocusPointForFrame(focusPoint, normal);
#else
        UnityEngine.VR.WSA.HolographicSettings.SetFocusPointForFrame(focusPoint, normal);
#endif
#endif

    }

}
