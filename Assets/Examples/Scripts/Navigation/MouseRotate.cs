using UnityEngine;

public class MouseRotate : MouseNavBase {

    public Vector3 pivot = Vector3.zero; //in world

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
            cam.Translate(-cam.forward * zoomSpeed * mouseDiff.z * 0.1f * r, Space.World);
        }
    }
}
