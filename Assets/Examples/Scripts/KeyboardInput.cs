using UnityEngine;
using Unity3DTiles;

class KeyboardInput : MonoBehaviour
{
#pragma warning disable 0649
    public AbstractTilesetBehaviour tileset;
    public Unity3DTilesetStatsHud hud;
    public MouseFly mouseFly;
    public MouseRotate mouseRotate;
#pragma warning restore 0649

    public void Update()
    {
        bool flyNav = mouseFly != null && mouseFly.enabled;
        bool rotNav = mouseRotate != null && mouseRotate.enabled;

        if (hud != null)
        {
            hud.message = "press h to toggle HUD";
            hud.message += ", v for default view";
            hud.message += ", f to fit view";

            if (flyNav || rotNav)
            {
                MouseNavBase.Modifier scaleMod = MouseNavBase.Modifier.None;
                if (flyNav)
                {
                    hud.message += "\nw/s/a/d/q/e to translate forward/back/left/right/up/down";
                    scaleMod = mouseFly.scaleModifier;
                }
                else if (rotNav)
                {
                    scaleMod = mouseRotate.scaleModifier;
                }
                hud.message += "\ndrag mouse to rotate";
                hud.message += "\nmouse wheel to scale";
                if (scaleMod != MouseNavBase.Modifier.None)
                {
                    hud.message += " (or " + scaleMod + "-drag)";
                }
                if (mouseFly != null && mouseRotate != null)
                {
                    hud.message += "\nn to switch navigation";
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            if (hud != null)
            {
                hud.text.enabled = !hud.text.enabled;
            }
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            ResetView();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            FitView();
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            if (mouseFly != null && mouseRotate != null)
            {
                mouseFly.enabled = !mouseFly.enabled;
                mouseRotate.enabled = !mouseRotate.enabled;
            }
        }
    }

    public void ResetView()
    {
        if (tileset != null)
        {
            var cam = Camera.main.transform;
            cam.position = tileset.SceneOptions.DefaultCameraPosition;
            cam.eulerAngles = tileset.SceneOptions.DefaultCameraRotation;
            cam.localScale = Vector3.one;

            if (mouseRotate != null) {
                mouseRotate.pivot = tileset.transform.TransformPoint(tileset.BoundingSphere().position);
            }
        }
    }

    public void FitView()
    {
        if (tileset != null)
        {
            var cam = Camera.main.transform;
            var sph = tileset.BoundingSphere();

            var ctrInWorld = tileset.transform.TransformPoint(sph.position);
            cam.Translate(Vector3.ProjectOnPlane(ctrInWorld - cam.position, cam.forward), Space.World);

            var tilesetToCam = tileset.transform.localToWorldMatrix * cam.worldToLocalMatrix; //row major compose l->r
            var t2cScale = tilesetToCam.lossyScale;
            var maxScale = Mathf.Max(t2cScale.x, t2cScale.y, t2cScale.z);
            var radiusInCam = sph.radius * maxScale;

            var vfov = Camera.main.fieldOfView * Mathf.Deg2Rad;
            var hfov = vfov * Camera.main.aspect;
            var minFov = Mathf.Min(vfov, hfov);

            var dist = radiusInCam / Mathf.Tan(minFov / 2);
            cam.Translate(cam.forward * (Vector3.Distance(cam.position, ctrInWorld) - dist), Space.World);

            if (mouseRotate != null) mouseRotate.pivot = ctrInWorld;
        }
    }
}
