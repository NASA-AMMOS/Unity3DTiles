using UnityEngine;
using Unity3DTiles;

class DemoUX : MonoBehaviour
{
#pragma warning disable 0649
    public AbstractTilesetBehaviour tileset;
    public TilesetStatsHud hud;
    public MouseFly mouseFly;
    public MouseRotate mouseRotate;
    public GameObject pointer;
#pragma warning restore 0649

    public float pointerRadiusPixels = 10;
    
    private Vector3? lastMouse;
    private bool hasFocus = true;
    private bool didReset = false;

    public void OnApplicationFocus(bool focusStatus)
    {
        hasFocus = focusStatus;
    }

    public void Update()
    {
        if (tileset != null && tileset.Ready() && !didReset)
        {
            ResetView();
            didReset = true;
        }

        bool flyNav = mouseFly != null && mouseFly.enabled;
        bool rotNav = mouseRotate != null && mouseRotate.enabled;
        bool hasPick = pointer != null && pointer.activeSelf;

        if (hud != null)
        {
            hud.message = "press h to toggle HUD";
            hud.message += ", v for default view";
            hud.message += ", f to fit";

            if (flyNav || rotNav)
            {
                MouseNavBase.Modifier scaleMod = MouseNavBase.Modifier.None;
                MouseNavBase.Modifier rollMod = MouseNavBase.Modifier.None;
                if (flyNav)
                {
                    hud.message += "\nw/s/a/d/q/e to translate forward/back/left/right/up/down";
                    scaleMod = mouseFly.scaleModifier;
                    rollMod = mouseFly.rollModifier;
                }
                else if (rotNav)
                {
                    scaleMod = mouseRotate.scaleModifier;
                    rollMod = mouseRotate.rollModifier;
                    hud.message += "\nc to rotate about " + (hasPick ? "clicked point" : "centroid");
                }
                hud.message += "\ndrag mouse to rotate";
                hud.message += "\nmouse wheel to scale";
                if (scaleMod != MouseNavBase.Modifier.None)
                {
                    hud.message += " (or " + scaleMod + "-drag)";
                }
                hud.message += "\nright mouse to roll";
                if (rollMod != MouseNavBase.Modifier.None)
                {
                    hud.message += " (or " + rollMod + "-drag)";
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

        if (rotNav && Input.GetKeyDown(KeyCode.C))
        {
            if (hasPick)
            {
                mouseRotate.pivot = pointer.transform.position;
            }
            else if (tileset && tileset.Ready())
            {
                mouseRotate.pivot = tileset.transform.TransformPoint(tileset.BoundingSphere().position);
            } 
        }

        if (hasFocus && !MouseNavBase.MouseOnUI())
        {
            if (Input.GetMouseButtonDown(0))
            {
                lastMouse = Input.mousePosition;
            }
            else if (!Input.GetMouseButton(0) && lastMouse != null && lastMouse.Value == Input.mousePosition)
            {
                OnClick(Input.mousePosition);
                lastMouse = null;
            }
        }
        else
        {
            lastMouse = null;
        }

        if (hasPick)
        {
            var cam = Camera.main.transform;
            var w2cScale = cam.worldToLocalMatrix.lossyScale;
            var minScale = Mathf.Min(w2cScale.x, w2cScale.y, w2cScale.z);
            var vfov = Camera.main.fieldOfView * Mathf.Deg2Rad;
            var hfov = vfov * Camera.main.aspect;
            var maxRadPerPixel = Mathf.Max(vfov / Screen.height, hfov / Screen.width);
            pointer.transform.localScale = (1.0f / minScale) * Vector3.one *
                Vector3.Distance(pointer.transform.position, cam.position) *
                Mathf.Tan(pointerRadiusPixels * maxRadPerPixel);
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

            if (mouseRotate != null && tileset.Ready())
            {
                mouseRotate.pivot = tileset.transform.TransformPoint(tileset.BoundingSphere().position);
            }
        }
    }

    public void FitView()
    {
        if (tileset != null && tileset.Ready())
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

            if (mouseRotate != null)
            {
                mouseRotate.pivot = ctrInWorld;
            }
        }
    }

    public void OnClick(Vector3 mousePosition)
    {
        if (Physics.Raycast(Camera.main.ScreenPointToRay(mousePosition), out RaycastHit hit))
        {
            if (pointer != null)
            {
                pointer.SetActive(true);
                pointer.transform.position = hit.point;
            }
        }
        else
        {
            if (pointer != null)
            {
                pointer.SetActive(false);
            }
        }
    }
}
