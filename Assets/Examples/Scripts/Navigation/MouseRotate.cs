using UnityEngine;

public class MouseRotate : MouseNavBase {

    public override void Update() {

        base.Update();

        //rotate
        if (mouseDiff.x != 0 || mouseDiff.y != 0) {
            Quaternion rotX = Quaternion.AngleAxis(rotSpeed * mouseDiff.x, dirX);
            Quaternion rotY = Quaternion.AngleAxis(rotSpeed * mouseDiff.y, dirY);
            transform.localRotation = rotY * rotX * transform.localRotation;
        }

        //scale
        if (mouseDiff.z != 0) {
            Vector3 s = transform.localScale;
            float sx = s.x - zoomSpeed * mouseDiff.z;
            sx = Mathf.Clamp(sx, minScale, maxScale);
            float ds = sx / s.x;
            transform.localScale *= ds;
        }
    }
}
