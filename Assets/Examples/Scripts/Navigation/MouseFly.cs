using UnityEngine;

public class MouseFly : MouseNavBase {

    public override void Update() {

        base.Update();

        var cam = Camera.main.transform;

        if (mouseDiff.x != 0)
        {
            cam.RotateAround(cam.position, cam.up, rotSpeed * mouseDiff.x);
        }

        if (mouseDiff.y != 0)
        {
            cam.RotateAround(cam.position, -cam.right, rotSpeed * mouseDiff.y);
        }

        if (mouseDiff.z != 0)
        {
            cam.Translate(-cam.forward * zoomSpeed * mouseDiff.z, Space.World);
        }

        var t = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
        {
            t += cam.forward * accelFactor * zoomSpeed;
        }

        if (Input.GetKey(KeyCode.S))
        {
            t -= cam.forward * accelFactor * zoomSpeed;
        }

        if (Input.GetKey(KeyCode.A))
        {
            t -= cam.right * accelFactor * zoomSpeed;
        }

        if (Input.GetKey(KeyCode.D))
        {
            t = cam.right * accelFactor * zoomSpeed;
        }

        if (Input.GetKey(KeyCode.Q))
        {
            t = cam.up * accelFactor * zoomSpeed;
        }

        if (Input.GetKey(KeyCode.E))
        {
            t -= cam.up * accelFactor * zoomSpeed;
        }

        cam.Translate(t, Space.World);
    }
}
