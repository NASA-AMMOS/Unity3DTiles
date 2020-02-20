using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    public Unity3DTile selectedTile;
    public Stack<Unity3DTile> selectedStack = new Stack<Unity3DTile>();
    
    private Vector3? lastMouse;
    private Vector2 mouseIntegral;

    private bool hasFocus = true;
    private bool didReset = false;

    private bool drawSelectedBounds, drawParentBounds;

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

        if (tileset != null)
        {
            tileset.ClearForcedTiles();
        }

        if (selectedTile != null)
        {
            float bv = selectedTile.BoundingVolume.Volume();
            float cbv = -1;
            if (selectedTile.ContentBoundingVolume != null)
            {
                cbv = selectedTile.ContentBoundingVolume.Volume();
            }

            if (hud != null)
            {
                hud.message += "\nselected tile " + selectedTile.Id;
                hud.message += ", depth " + selectedTile.Depth;
                hud.message += ", " + selectedTile.Children.Count + " children";
                hud.message += ", geometric error " + selectedTile.GeometricError.ToString("F3");
                
                hud.message += "\nbounds vol " + bv + ": " + selectedTile.BoundingVolume.SizeString();
                if (cbv >= 0 && cbv != bv)
                {
                    hud.message += ", content vol " + cbv;
                }
                
                var tc = selectedTile.Content;
                if (tc != null && selectedTile.ContentState == Unity3DTileContentState.READY)
                {
                    hud.message += "\n" + FmtKMG(tc.FaceCount) + " tris, " + FmtKMG(tc.PixelCount) + " pixels, ";
                    hud.message += tc.TextureCount + " textures, ";
                    hud.message += "max " + tc.MaxTextureSize.x + "x" + tc.MaxTextureSize.y;
                }
                hud.message += "\nb to toggle bounds";
                if (selectedTile.Parent != null)
                {
                    hud.message += "\nup/left/right";
                    if (selectedTile.Children.Count > 0)
                    {
                        hud.message += "/down";
                    }
                    hud.message += " to select parent/sibling";
                    if (selectedTile.Children.Count > 0)
                    {
                        hud.message += "/child";
                    }
                }
                else if (selectedTile.Children.Count > 0)
                {
                    hud.message += "\ndown to select child";
                }
            }

            if (drawSelectedBounds)
            {
                selectedTile.BoundingVolume.DebugDraw(Color.magenta, selectedTile.Tileset.Behaviour.transform);
                if (cbv >= 0 && cbv != bv)
                {
                    selectedTile.ContentBoundingVolume.DebugDraw(Color.red, selectedTile.Tileset.Behaviour.transform);
                }
            }

            if (drawParentBounds && selectedTile.Parent != null)
            {
                var parent = selectedTile.Parent;
                float pbv = parent.BoundingVolume.Volume();
                float pcbv = parent.ContentBoundingVolume != null ? parent.ContentBoundingVolume.Volume() : -1;
                parent.BoundingVolume.DebugDraw(Color.cyan, selectedTile.Tileset.Behaviour.transform);
                if (pcbv >= 0 && pcbv != pbv)
                {
                    parent.ContentBoundingVolume.DebugDraw(Color.blue, selectedTile.Tileset.Behaviour.transform);
                }
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                if (!drawSelectedBounds && !drawParentBounds)
                {
                    drawSelectedBounds = true;
                }
                else if (drawSelectedBounds && !drawParentBounds)
                {
                    drawParentBounds = true;
                }
                else
                {
                    drawSelectedBounds = drawParentBounds = false;
                }
            }

            if (selectedTile.Parent != null && Input.GetKeyDown(KeyCode.UpArrow))
            {
                selectedStack.Push(selectedTile);
                selectedTile = selectedTile.Parent;
            }

            if (selectedTile.Children.Count > 0 && Input.GetKeyDown(KeyCode.DownArrow))
            {
                selectedTile = selectedStack.Count > 0 ? selectedStack.Pop() : selectedTile.Children.First();
            }

            int sibling = Input.GetKeyDown(KeyCode.LeftArrow) ? -1 : Input.GetKeyDown(KeyCode.RightArrow) ? 1 : 0;
            if (selectedTile.Parent != null && sibling != 0)
            {
                var siblings = selectedTile.Parent.Children;
                int idx = siblings.FindIndex(c => c == selectedTile) + sibling;
                idx = idx < 0 ? siblings.Count - 1 : idx == siblings.Count ? 0 : idx;
                if (siblings[idx] != selectedTile)
                {
                    selectedStack.Clear();
                }
                selectedTile = siblings[idx];
            }

            selectedTile.Tileset.Traversal.ForceTiles.Add(selectedTile);
        }

        //toggle hud
        if (hud != null && Input.GetKeyDown(KeyCode.H))
        {
            hud.text.enabled = !hud.text.enabled;
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            ResetView();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            FitView();
        }

        //toggle nav mode
        if (mouseFly != null && mouseRotate != null && Input.GetKeyDown(KeyCode.N))
        {
            mouseFly.enabled = !mouseFly.enabled;
            mouseRotate.enabled = !mouseRotate.enabled;
        }

        //set rotNav pivot
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

        //handle mouse clicks
        if (hasFocus && !MouseNavBase.MouseOnUI())
        {
            if (Input.GetMouseButtonDown(0))
            {
                lastMouse = Input.mousePosition;
                mouseIntegral = Vector3.zero;
            }
            else if (Input.GetMouseButton(0) && lastMouse.HasValue)
            {
                var mouseDiff = Input.mousePosition - lastMouse.Value;
                mouseIntegral.x += Mathf.Abs(mouseDiff.x);
                mouseIntegral.y += Mathf.Abs(mouseDiff.y);
                lastMouse = Input.mousePosition;
            }
            else if (lastMouse.HasValue && mouseIntegral == Vector2.zero)
            {
                OnClick(Input.mousePosition);
                lastMouse = null;
            }
        }
        else
        {
            lastMouse = null;
        }

        //scale pointer to pointerRadiusPixels
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
        selectedTile = null;
        selectedStack.Clear();
        if (Physics.Raycast(Camera.main.ScreenPointToRay(mousePosition), out RaycastHit hit))
        {
            if (pointer != null)
            {
                pointer.SetActive(true);
                pointer.transform.position = hit.point;
            }
            var go = hit.collider.transform.gameObject;
            while (go != null)
            {
                var ti = go.GetComponent<TileInfo>();
                if (ti != null)
                {
                    selectedTile = ti.Tile;
                    break;
                }
                go = go.transform.parent != null ? go.transform.parent.gameObject : null;
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

    public static string FmtKMG(float b, float k = 1e3f)
    {
        if (Mathf.Abs(b) < k) return b.ToString("f0");
        else if (Mathf.Abs(b) < k*k) return string.Format("{0:f1}k", b/k);
        else if (Mathf.Abs(b) < k*k*k) return string.Format("{0:f1}M", b/(k*k));
        else return string.Format("{0:f1}G", b/(k*k*k));
    }
}
