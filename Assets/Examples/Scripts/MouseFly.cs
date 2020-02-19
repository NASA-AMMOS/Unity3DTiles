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

        if (mouseDiff.w != 0)
        {
            cam.RotateAround(cam.position, cam.forward, rotSpeed * mouseDiff.w);
        }

        var t = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
        {
            t += cam.forward * accelFactor * transSpeed;
        }

        if (Input.GetKey(KeyCode.S))
        {
            t -= cam.forward * accelFactor * transSpeed;
        }

        if (Input.GetKey(KeyCode.A))
        {
            t -= cam.right * accelFactor * transSpeed;
        }

        if (Input.GetKey(KeyCode.D))
        {
            t = cam.right * accelFactor * transSpeed;
        }

        if (Input.GetKey(KeyCode.Q))
        {
            t = cam.up * accelFactor * transSpeed;
        }

        if (Input.GetKey(KeyCode.E))
        {
            t -= cam.up * accelFactor * transSpeed;
        }

        cam.Translate(t, Space.World);
    }
}
