using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;

public class MouseNavBase : MonoBehaviour {

    public enum Button { Left = 0, Right, Middle, None };
    protected bool[] buttons = new bool[3];

    //rotate on drag with rotateButton and rotateModifier pressed
    public Button rotateButton = Button.Left;

    //scale on
    //a) drag with rotateButton and scaleModifier pressed
    //b) drag with scaleButton pressed
    //c) mouse wheel
    public Button scaleButton = Button.Middle;

    public enum Modifier { Control = 0, Shift, Alt, Space, None };
    protected bool[] mods = new bool[4];

    public Modifier rotateModifier = Modifier.None;
    public Modifier scaleModifier = Modifier.Alt;
    public Modifier accelModifier = Modifier.Shift;

    public float rotSpeed = 2.0f, zoomSpeed = 1.0f;
    public float accelFactor = 2.0f;

    public float minScale = 0.1f, maxScale = 10.0f;

    protected Vector3 dirX = new Vector3(0, -1, 0), dirY = new Vector3(1, 0, 0);

    protected bool hasFocus = true;
    public void OnApplicationFocus(bool focusStatus) { hasFocus = focusStatus; }

    protected Vector3 lastMouse;
    protected Vector3 mouseDiff;

    public bool debug = false;

    protected Vector3 initialPosition, initialScale;
    protected Quaternion initialRotation;

    public virtual void Start() {
        lastMouse = Input.mousePosition;
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
        initialScale = transform.localScale;
    }

    private bool Any(bool[] arr) {
        for (int i = 0; i < arr.Length; i++) if (arr[i]) return true;
        return false;
    }

    protected virtual void ResetNav() {
        transform.localPosition = initialPosition;
        transform.localRotation = initialRotation;
        transform.localScale = initialScale;
    }

    public static bool MouseOnUI() {
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

    //derived class Update() should call this first
    //it sets buttons, mods, and mouseDiff
    //(also sets lastMouse and hasFocus)
    //mouseDiff.{x,y} should be used to rotate
    //mouseDiff.z should be used to scale
    public virtual void Update() {

        mouseDiff = Vector3.zero;
        for (int i = 0; i < mods.Length; i++) mods[i] = false;
        for (int i = 0; i < buttons.Length; i++) buttons[i] = false;

        if (!hasFocus || MouseOnUI()) return;

        mods[(int)Modifier.Control] = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        mods[(int)Modifier.Shift] = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        mods[(int)Modifier.Alt] = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        mods[(int)Modifier.Space] = Input.GetKey(KeyCode.Space);
        if (debug && Any(mods))
            Debug.Log("ctrl=" + mods[(int)Modifier.Control] + ", shift=" + mods[(int)Modifier.Shift] +
                      ", alt=" + mods[(int)Modifier.Alt] + ", space=" + mods[(int)Modifier.Space]);

        //if the button just went down this frame then don't count it
        //because for the workaround below the very first mouseDiff during a drag will be bogus
        buttons[(int)Button.Left] = Input.GetMouseButton(0) && !Input.GetMouseButtonDown(0);
        buttons[(int)Button.Right] = Input.GetMouseButton(1) && !Input.GetMouseButtonDown(1);
        buttons[(int)Button.Middle] = Input.GetMouseButton(2) && !Input.GetMouseButtonDown(2);
        bool anyButton = Any(buttons);
        if (debug && anyButton)
            Debug.Log("mouse left=" + buttons[(int)Button.Left] + ", right=" + buttons[(int)Button.Right] +
                      ", middle=" + buttons[(int)Button.Middle]);

        //workaround regular Input APIs may not work for mouse movement over remote desktop
        //https://forum.unity.com/threads/input-getaxis-mouse-x-and-y-axis-equivalent-dont-work-in-remote-desktop.115526
        //mouseDiff.x = Input.GetAxis("Mouse X");
        //mouseDiff.y = Input.GetAxis("Mouse Y");
        Vector3 mouse = Input.mousePosition;
        mouseDiff = mouse - lastMouse;
        lastMouse = mouse;

        //scale on mouse wheel
        mouseDiff.z = Input.GetAxis("Mouse ScrollWheel");

        //override scale if dragging scaleButton or any button with scaleModifier
        bool scaling = scaleButton != Button.None && buttons[(int)scaleButton];
        scaling |= anyButton && scaleModifier != Modifier.None && mods[(int)scaleModifier];
        if (scaling) {
            mouseDiff.z = 0.01f * mouseDiff.y;
            mouseDiff.x = mouseDiff.y = 0;
        }

        bool rotating = rotateButton == Button.None || buttons[(int)rotateButton];
        rotating |= anyButton && rotateModifier != Modifier.None && mods[(int)rotateModifier];
        if (!rotating) mouseDiff.x = mouseDiff.y = 0;

        if (accelModifier != Modifier.None && mods[(int)accelModifier]) mouseDiff *= accelFactor;

        if (debug && mouseDiff.magnitude != 0) Debug.Log("mouse diff: " + mouseDiff);
    }
}
