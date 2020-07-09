/*
 * Copyright 2018, by the California Institute of Technology. ALL RIGHTS 
 * RESERVED. United States Government Sponsorship acknowledged. Any 
 * commercial use must be negotiated with the Office of Technology 
 * Transfer at the California Institute of Technology.
 * 
 * This software may be subject to U.S.export control laws.By accepting 
 * this software, the user agrees to comply with all applicable 
 * U.S.export laws and regulations. User has the responsibility to 
 * obtain export licenses, or other export authority as may be required 
 * before exporting such information to foreign countries or providing 
 * access to foreign persons.
 */

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;

public class MouseNavBase : MonoBehaviour
{
    public enum Button { Left = 0, Right, Middle, None };
    protected bool[] buttons = new bool[3];

    //rotate on drag with rotateButton and rotateModifier pressed
    public Button rotateButton = Button.Left;

    //scale on
    //a) drag with rotateButton and scaleModifier pressed
    //b) drag with scaleButton pressed
    //c) mouse wheel
    public Button scaleButton = Button.Middle;

    public Button rollButton = Button.Right;

    public enum Modifier { Control = 0, Shift, Alt, Space, None };
    protected bool[] mods = new bool[4];

    public Modifier rotateModifier = Modifier.None;
    public Modifier scaleModifier = Modifier.Alt;
    public Modifier accelModifier = Modifier.Shift;
    public Modifier rollModifier = Modifier.Control;

    public float rotSpeed = 2.0f, zoomSpeed = 1.0f, transSpeed = 1.0f;
    public float accelFactor = 5.0f;

    protected bool hasFocus = true;
    public void OnApplicationFocus(bool focusStatus)
    {
        hasFocus = focusStatus;
    }

    protected Vector3 lastMouse;
    protected Vector4 mouseDiff; //x = pitch, y = yaw, z = zoom, w = roll
    protected float accel = 1;

    public virtual void Start()
    {
        lastMouse = Input.mousePosition;
    }

    public static bool MouseOnUI()
    {
#if UNITY_EDITOR
        var mow = EditorWindow.mouseOverWindow;
        if (mow != null && !mow.ToString().Contains("GameView"))
        {
            return true;
        }
#endif
        //https://answers.unity.com/questions/967170/detect-if-pointer-is-over-any-ui-element.html
        var es = EventSystem.current;
        return
            es.IsPointerOverGameObject() || es.currentSelectedGameObject != null ||
            (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began &&
             es.IsPointerOverGameObject(Input.GetTouch(0).fingerId));
    }

    public virtual void Update()
    {
        mouseDiff = Vector4.zero;

        for (int i = 0; i < mods.Length; i++)
        {
            mods[i] = false;
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i] = false;
        }

        if (!hasFocus || MouseOnUI())
        {
            return;
        }

        mods[(int)Modifier.Control] = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        mods[(int)Modifier.Shift] = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        mods[(int)Modifier.Alt] = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        mods[(int)Modifier.Space] = Input.GetKey(KeyCode.Space);

        //if the button just went down this frame then don't count it
        //because for the workaround below the very first mouseDiff during a drag will be bogus
        buttons[(int)Button.Left] = Input.GetMouseButton(0) && !Input.GetMouseButtonDown(0);
        buttons[(int)Button.Right] = Input.GetMouseButton(1) && !Input.GetMouseButtonDown(1);
        buttons[(int)Button.Middle] = Input.GetMouseButton(2) && !Input.GetMouseButtonDown(2);
        bool anyButton = buttons.Any(b => b);

        //workaround regular Input APIs may not work for mouse movement over remote desktop
        //https://forum.unity.com/threads/input-getaxis-mouse-x-and-y-axis-equivalent-dont-work-in-remote-desktop.115526
        //mouseDiff.x = Input.GetAxis("Mouse X");
        //mouseDiff.y = Input.GetAxis("Mouse Y");
        Vector3 mouse = Input.mousePosition;
        mouseDiff.x = mouse.x - lastMouse.x;
        mouseDiff.y = mouse.y - lastMouse.y;
        lastMouse = mouse;

        //scale on mouse wheel
        mouseDiff.z = Input.GetAxis("Mouse ScrollWheel");

        bool scaling = scaleButton != Button.None && buttons[(int)scaleButton];
        scaling |= anyButton && scaleModifier != Modifier.None && mods[(int)scaleModifier];
        if (scaling)
        {
            mouseDiff.z = 0.01f * mouseDiff.y;
            mouseDiff.x = mouseDiff.y = 0;
        }

        bool rolling = rollButton != Button.None && buttons[(int)rollButton];
        rolling |= anyButton && rollModifier != Modifier.None && mods[(int)rollModifier];
        if (rolling)
        {
            mouseDiff.w = mouseDiff.x;
            mouseDiff.x = mouseDiff.y = mouseDiff.z = 0;
        }

        bool rotating = rotateButton != Button.None && buttons[(int)rotateButton];
        rotating |= anyButton && rotateModifier != Modifier.None && mods[(int)rotateModifier];
        if (!rotating)
        {
            mouseDiff.x = mouseDiff.y = 0;
        }

        accel = accelModifier != Modifier.None && mods[(int)accelModifier] ? accelFactor : 1;
    }
}
