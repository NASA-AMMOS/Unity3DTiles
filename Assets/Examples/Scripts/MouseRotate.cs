using UnityEngine;

public class MouseRotate : MouseNavBase {

    public Vector3 pivot = Vector3.zero; //in world

    public float minZoom = 0.5f;

    public override void Update() {

        base.Update();

        var cam = Camera.main.transform;

        cam.Translate(Vector3.ProjectOnPlane(pivot - cam.position, cam.forward), Space.World);

        if (mouseDiff.x != 0)
        {
            cam.RotateAround(pivot, cam.up, rotSpeed * mouseDiff.x);
        }

        if (mouseDiff.y != 0)
        {
            cam.RotateAround(pivot, -cam.right, rotSpeed * mouseDiff.y);
        }

        if (mouseDiff.z != 0)
        {
            float r = Vector3.Distance(pivot, cam.position);
            float d = zoomSpeed * mouseDiff.z * 0.1f * r;
            if (Mathf.Abs(d) < minZoom)
            {
                d = Mathf.Sign(d) * minZoom;
            }
            cam.Translate(-cam.forward * d, Space.World);
        }

        if (mouseDiff.w != 0)
        {
            cam.RotateAround(pivot, cam.forward, rotSpeed * mouseDiff.w);
        }
    }
}
