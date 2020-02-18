using UnityEngine;

public class MouseFly : MouseNavBase {

    private float yaw, pitch;

    public enum ScaleMode { FOV, Object }
    public ScaleMode scaleMode = ScaleMode.FOV;
    public float minFOV = 1, maxFOV = 120;

    private float initialFOV;
    private Vector3 initialObjectPos;

    public float transSpeed = 3;

    protected override void ResetNav() {

        yaw = pitch = 0;

        Camera.main.transform.localPosition = initialPosition;
        Camera.main.transform.localRotation = initialRotation;

        Camera.main.fieldOfView = initialFOV;
        transform.localScale = initialScale;
        transform.localPosition = initialObjectPos;
    }

    public override void Start() {
        base.Start();

        initialPosition = Camera.main.transform.localPosition;
        initialRotation = Camera.main.transform.localRotation;

        initialFOV = Camera.main.fieldOfView;
        initialScale = transform.localScale;
        initialObjectPos = transform.localPosition;
    }

    public override void Update() {

        base.Update();

        //MouseLook rotate
        if (mouseDiff.x != 0 || mouseDiff.y != 0) {
            yaw += rotSpeed * -mouseDiff.x;
            pitch += rotSpeed * -mouseDiff.y;
            Camera.main.transform.localRotation = Quaternion.AngleAxis(pitch, dirY) * Quaternion.AngleAxis(yaw, dirX);
        }

        //WASD translate
        Vector3 transDiff =
            new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")) * Time.deltaTime * transSpeed;
        if (debug && transDiff.magnitude > 0) Debug.Log("trans diff: " + transDiff);
        Camera.main.transform.Translate(transDiff);

        //scale
        if (mouseDiff.z != 0) {
            switch (scaleMode) {

                case ScaleMode.FOV: {
                    //simulate zooming by changing camera FOV
                    //may not work in VR mode where FOV can be fixed by hardware
                    float fov = Camera.main.fieldOfView;
                    fov += zoomSpeed * 10 * mouseDiff.z;
                    fov = Mathf.Clamp(fov, minFOV, maxFOV);
                    if (debug) Debug.Log("fov: " + fov);
                    Camera.main.fieldOfView = fov;
                    break;
                }

                case ScaleMode.Object: {

                    //scale the object about the camera
                    //this is mostly for stereo VR
                    //for a monocular camera this doesn't actually change how things look from the current position
                    //due to monocular scale ambiguity
                    //but it will change how much translation it takes to navigate around the object

                    Vector3 s = transform.localScale;
                    float sx = s.x - zoomSpeed * mouseDiff.z;
                    sx = Mathf.Clamp(sx, minScale, maxScale);
                    float ds = sx / s.x;
                    transform.localScale *= ds;

                    Vector3 pivot = Camera.main.transform.position;
                    if (transform.parent != null) pivot = transform.parent.InverseTransformPoint(pivot);

                    transform.localPosition = ds * transform.localPosition + (1 - ds) * pivot;

                    break;
                }
            }
        }
    }
}
