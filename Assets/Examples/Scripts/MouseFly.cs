using UnityEngine;

public class MouseFly : MouseNavBase {

    public override void Update() {

        base.Update();

        if (!hasFocus || MouseOnUI())
        {
            return;
        }
        var cam = Camera.main.transform;

        if (mouseDiff.x != 0)
        {
            cam.RotateAround(cam.position, cam.up, rotSpeed * accel * mouseDiff.x);
        }

        if (mouseDiff.y != 0)
        {
            cam.RotateAround(cam.position, -cam.right, rotSpeed * accel * mouseDiff.y);
        }

        if (mouseDiff.z != 0)
        {
            cam.Translate(-cam.forward * zoomSpeed * accel * mouseDiff.z, Space.World);
        }

        if (mouseDiff.w != 0)
        {
            cam.RotateAround(cam.position, cam.forward, rotSpeed * accel * mouseDiff.w);
        }

        var t = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
        {
            t += cam.forward * transSpeed * accel;
        }

        if (Input.GetKey(KeyCode.S))
        {
            t -= cam.forward * transSpeed * accel;
        }

        if (Input.GetKey(KeyCode.A))
        {
            t -= cam.right * transSpeed * accel;
        }

        if (Input.GetKey(KeyCode.D))
        {
            t = cam.right * transSpeed * accel;
        }

        if (Input.GetKey(KeyCode.Q))
        {
            t = cam.up * transSpeed * accel;
        }

        if (Input.GetKey(KeyCode.E))
        {
            t -= cam.up * transSpeed * accel;
        }

        if (t != Vector3.zero)
        {
            cam.Translate(t, Space.World);
        }
    }
}
