using UnityEngine;
using Unity3DTiles;

class KeyboardInput : MonoBehaviour {

    public TilesetBehaviour tilesetBehaviour;
    public Unity3DTilesetStatsHud hud;
    public MouseFly mouseFly;
    public MouseRotate mouseRotate;

    public void Update()
    {
        bool flyNav = mouseFly != null && mouseFly.enabled;
        bool rotNav = mouseRotate != null && mouseRotate.enabled;

        if (hud != null)
        {
            hud.message = "press h to toggle HUD";
            if (tilesetBehaviour != null)
            {
                hud.message += ", v for default view";
            }

            if (flyNav || rotNav)
            {
                MouseNavBase.Modifier scaleMod = MouseNavBase.Modifier.None;
                if (flyNav)
                {
                    hud.message += "\nw/s/a/d to translate forward/back/left/right";
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
            ResetNav();
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            if (mouseFly != null && mouseRotate != null)
            {
                mouseFly.enabled = !mouseFly.enabled;
                mouseRotate.enabled = !mouseRotate.enabled;
                ResetNav();
            }
        }
    }

    public void ResetNav()
    {
        if (tilesetBehaviour != null)
        {
            Camera.main.transform.position = tilesetBehaviour.TilesetOptions.DefaultCameraPosition;
            Camera.main.transform.eulerAngles = tilesetBehaviour.TilesetOptions.DefaultCameraRotation;
            tilesetBehaviour.transform.position = Vector3.zero;
            tilesetBehaviour.transform.eulerAngles = Vector3.zero;
            tilesetBehaviour.transform.localScale = Vector3.one;
        }
    }
}
