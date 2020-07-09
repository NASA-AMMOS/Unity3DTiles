/*
 * Copyright 2018, by the California Institute of Technology. ALL RIGHTS 
 * RESERVED. United States Government Sponsorship acknowledged. Any 
 * commercial use must be negotiated with the Office of Technology 
 * Transfer at the California Institute of Technology.
 * 
 * This software may be subject to U.S.export control laws.By accepting 
 * this software, the user agrees to comply with all applicable 
 * U.S.export laws and regulations. User has the responsibility to 
 * obtain export licenses, or other export authority as may be required 
 * before exporting such information to foreign countries or providing 
 * access to foreign persons.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Unity3DTiles;
using UnityGLTF.Extensions;

class DemoUX : MonoBehaviour
{
#pragma warning disable 0649
    public AbstractTilesetBehaviour tileset;
    public TilesetStatsHud hud;
    public MouseFly mouseFly;
    public MouseOrbit mouseOrbit;
    public GameObject pointer;
#pragma warning restore 0649

    public float pointerRadiusPixels = 10;

    public enum DrawBoundsMode { No, Selected, Parent, Ancestor, All, Leaf, Active };
    public DrawBoundsMode boundsMode;

    public bool resetOrbitPivotOnNavChange = true;

    public bool enablePicking = true;

    public bool drawSelectedAxes, drawRootAxes;

    public float relativeNavTransSpeed = 2000;

    private List<Unity3DTileset> tilesets;
    private Unity3DTile selectedTile;
    private Stack<Unity3DTile> selectedStack = new Stack<Unity3DTile>();
    private Stack<Unity3DTileset> showStack = new Stack<Unity3DTileset>();
    public bool forceSelectedTile;
    
    private Vector3? lastMouse;
    private Vector2 mouseIntegral;

    private bool pointerActive;
    private bool hasFocus = true;
    private bool didReset;
    private bool setFarClip;

    private StringBuilder builder = new StringBuilder();

    private List<KeyCode> alphaNumerals = new List<KeyCode>()
    {
        KeyCode.Alpha0, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
        KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9
    };

    private List<KeyCode> keypadNumerals = new List<KeyCode>()
    {
        KeyCode.Keypad0, KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3, KeyCode.Keypad4,
        KeyCode.Keypad5, KeyCode.Keypad6, KeyCode.Keypad7, KeyCode.Keypad8, KeyCode.Keypad9
    };
    
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

        pointerActive = pointer != null && pointer.activeSelf;

        builder.Clear();

        builder.Append("\npress h to toggle HUD, v for default view, f to fit");
        builder.Append("\npicking " + (enablePicking ? "enabled" : "disabled") + ",  press p to toggle");

        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (hud != null && Input.GetKeyDown(KeyCode.H))
        {
            hud.enabled = !hud.enabled;
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            ResetView(unlimited: shift);
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            FitView(unlimited: shift);
        }

        UpdateFarClip();

        UpdateNav();

        UpdateAxes();

        UpdateTilesets();

        UpdateSelectedTileset();

        UpdatePicking();

        if (hud != null)
        {
            hud.ExtraMessage = builder.ToString();
        }
    }

    private void UpdateFarClip(bool force = false)
    {
        if ((force || !setFarClip) && tileset != null && tileset.Ready())
        {
            var bounds = tileset.BoundingSphere();
            Camera.main.farClipPlane = Math.Max(Camera.main.farClipPlane, 1.1f * bounds.radius);
            setFarClip = true;
        }
    }

    private void UpdateNav()
    {
        MouseNavBase activeNav = null;
        if (mouseFly != null && mouseFly.enabled)
        {
            activeNav = mouseFly;
            builder.Append("\nfly navigation");
            if (mouseOrbit != null)
            {
                builder.Append(", press n to switch to orbit");
            }
            builder.Append("\nw/s/a/d/q/e to translate fwd/back/left/right/up/down");
        }
        else if (mouseOrbit != null && mouseOrbit.enabled)
        {
            activeNav = mouseOrbit;
            builder.Append("\norbit navigation");
            if (mouseFly != null)
            {
                builder.Append(", press n to switch to fly");
            }
            builder.Append("\npress c to rotate about ");
            builder.Append(pointerActive ? "pick point" : "centroid");
        }

        if (activeNav != null)
        {
            builder.Append("\ndrag mouse to rotate");
            builder.Append("\nmouse wheel to scale");
            if (activeNav.scaleModifier != MouseNavBase.Modifier.None)
            {
                builder.Append(" (or " + activeNav.scaleModifier + "-drag)");
            }
            builder.Append("\nright mouse to roll");
            if (activeNav.rollModifier != MouseNavBase.Modifier.None)
            {
                builder.Append(" (or " + activeNav.rollModifier + "-drag)");
            }
            if (activeNav.accelModifier != MouseNavBase.Modifier.None)
            {
                builder.Append("\npress " + activeNav.accelModifier + " to move faster");
            }
        }

        //toggle nav mode
        if (mouseFly != null && mouseOrbit != null && Input.GetKeyDown(KeyCode.N))
        {
            mouseFly.enabled = !mouseFly.enabled;
            mouseOrbit.enabled = !mouseOrbit.enabled;

            if (mouseOrbit.enabled && resetOrbitPivotOnNavChange)
            {
                if (Physics.Raycast(Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2)),
                                    out RaycastHit hit))
                {
                    mouseOrbit.pivot = hit.point;
                }
                else if (tileset && tileset.Ready())
                {
                    mouseOrbit.pivot = tileset.transform.TransformPoint(NonSkyBounds().position);
                } 
            }
        }

        //set orbit nav pivot
        if (activeNav != null && activeNav == mouseOrbit && Input.GetKeyDown(KeyCode.C))
        {
            if (pointerActive)
            {
                mouseOrbit.pivot = pointer.transform.position;
            }
            else if (tileset && tileset.Ready())
            {
                mouseOrbit.pivot = tileset.transform.TransformPoint(NonSkyBounds().position);
            } 
        }

        //tweak trans speed
        float ts = mouseFly != null ? mouseFly.transSpeed : mouseOrbit != null ? mouseOrbit.transSpeed : -1;
        if (ts >= 0)
        {
            builder.Append($"\ntrans speed {ts:f5} press [ slower, ] faster");
            float transAdj = 0.005f;
            if (Input.GetKey(KeyCode.LeftBracket))
            {
                if (mouseOrbit != null)
                {
                    mouseOrbit.transSpeed = (float)Math.Max(0.001, mouseOrbit.transSpeed - transAdj);
                }
                if (mouseFly != null)
                {
                    mouseFly.transSpeed = (float)Math.Max(0.001, mouseFly.transSpeed - transAdj);
                }
            }
            if (Input.GetKey(KeyCode.RightBracket))
            {
                if (mouseOrbit != null)
                {
                    mouseOrbit.transSpeed = mouseOrbit.transSpeed + transAdj; 
                }
                if (mouseFly != null)
                {
                    mouseFly.transSpeed = mouseFly.transSpeed + transAdj;
                }
            }
        }
    }

    private void UpdateAxes()
    {
        bool rootAxesVisible = false;
        if (tileset != null)
        {
            var axes = tileset.gameObject.GetComponent<AxesWidget>();
            if (drawRootAxes)
            {
                if (axes == null)
                {
                    axes = tileset.gameObject.AddComponent<AxesWidget>();
                    axes.Scale = 2;
                }
                axes.enabled = true;
                rootAxesVisible = true;
            }
            else if (axes != null)
            {
                axes.enabled = false;
            }
        }
        else
        {
            drawRootAxes = false;
        }
           
        bool selectedAxesVisible = false;
        if (selectedTile != null)
        {
            var got = selectedTile.Tileset.Behaviour.transform.Find("AxesWidget");
            var go = got != null ? got.gameObject : null;
            if (drawSelectedAxes)
            {
                if (go == null)
                {
                    go = new GameObject("AxesWidget");
                    go.transform.parent = selectedTile.Tileset.Behaviour.transform;
                    go.AddComponent<AxesWidget>().enabled = true;
                }
                else
                {
                    go.GetComponent<AxesWidget>().enabled = true;
                }

                selectedTile.Tileset.GetRootTransform(out Vector3 t, out Quaternion r, out Vector3 s);
                go.transform.localPosition = t;
                go.transform.localRotation = r;
                go.transform.localScale = s;

                selectedAxesVisible = true;
            }
            else if (go != null)
            {
                go.GetComponent<AxesWidget>().enabled = false;
            }
        }
        else
        {
            drawSelectedAxes = false;
        }

        builder.Append("\n");
        if (rootAxesVisible || selectedAxesVisible)
        {
            builder.Append("showing " +
                           (rootAxesVisible ? "root" : "") +
                           (rootAxesVisible && selectedAxesVisible ? " and " : "") +
                           (selectedAxesVisible ? "selected" : "") + " axes, ");
        }

        if (tileset != null || selectedTile != null)
        {
            builder.Append("press x to toggle axes");

            if (Input.GetKeyDown(KeyCode.X))
            {
                if (!drawSelectedAxes && !drawRootAxes)
                {
                    if (selectedTile != null)
                    {
                        drawSelectedAxes = true;
                    }
                    else
                    {
                        drawRootAxes = true;
                    }
                }
                else if (drawSelectedAxes && !drawRootAxes)
                {
                    drawSelectedAxes = false;
                    drawRootAxes = true;
                }
                else if (!drawSelectedAxes && drawRootAxes && selectedTile != null)
                {
                    drawSelectedAxes = true;
                    drawRootAxes = true;
                }
                else
                {
                    drawSelectedAxes = drawRootAxes = false;
                }
            }
        }
    }

    private void UpdateTilesets()
    {
        if (tileset is MultiTilesetBehaviour)
        {
            tilesets = ((MultiTilesetBehaviour)tileset).GetTilesets().ToList();
        }

        if (tilesets != null && tilesets.Count > 1)
        {
            string mods = "";
            if (tilesets.Count > 10)
            {
                mods += "[shift]+";
            }
            if (tilesets.Count > 20)
            {
                mods = "[ctrl]" + mods;
            }
            if (tilesets.Count > 40)
            {
                mods = "[alt]" + mods;
            }
            builder.Append("\npress " + mods + "0-9 to hide/show a tileset");
            int offset = 0;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                offset += 10;
            }
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                offset += 20;
            }
            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            {
                offset += 40;
            }
            int idx = Mathf.Max(alphaNumerals.FindIndex(code => Input.GetKeyDown(code)),
                                keypadNumerals.FindIndex(code => Input.GetKeyDown(code)));
            if (idx >= 0)
            {
                idx += offset;
                if (idx < tilesets.Count)
                {
                    tilesets[idx].TilesetOptions.Show = !tilesets[idx].TilesetOptions.Show;
                    if (!tilesets[idx].TilesetOptions.Show)
                    {
                        showStack.Push(tilesets[idx]);
                        if (selectedTile != null && selectedTile.Tileset == tilesets[idx])
                        {
                            selectedTile = null;
                            selectedStack.Clear();
                        }
                    }
                }
            }
        }

        if (!showStack.Any(ts => !ts.TilesetOptions.Show))
        {
            showStack.Clear();
        }

        if (showStack.Count > 0)
        {
            builder.Append("\npress o to show last hidden tileset");
            if (Input.GetKeyDown(KeyCode.O))
            {
                while (showStack.Count > 0)
                {
                    var ts = showStack.Pop();
                    if (!ts.TilesetOptions.Show)
                    {
                        ts.TilesetOptions.Show = true;
                        break;
                    }
                }
            }
        }
    }

    private void UpdateSelectedTileset()
    {
        if (tileset != null)
        {
            tileset.ClearForcedTiles();
        }

        if (selectedTile == null)
        {
            return;
        }

        float bv = selectedTile.BoundingVolume.Volume();
        float cbv = -1;
        if (selectedTile.ContentBoundingVolume != null)
        {
            cbv = selectedTile.ContentBoundingVolume.Volume();
        }

        builder.Append("\n");
        
        if (tilesets.Count > 0)
        {
            var sts = selectedTile.Tileset;
            builder.Append("\nselected tileset " + sts.TilesetOptions.Name +
                           " (" + tilesets.FindIndex(ts => ts == sts) + ")");
        }

        var opts = selectedTile.Tileset.TilesetOptions;
        double maxSSE = opts.MaximumScreenSpaceError;
        builder.Append("\nmax SSE " + opts.MaximumScreenSpaceError.ToString("F3") + " (hit PageUp/Down to adjust)");
        if (Input.GetKeyDown(KeyCode.PageDown))
        {
            opts.MaximumScreenSpaceError = Math.Max(0, opts.MaximumScreenSpaceError - 1);
        }

        if (Input.GetKeyDown(KeyCode.PageUp))
        {
            opts.MaximumScreenSpaceError = opts.MaximumScreenSpaceError + 1;
        }

        double dist = selectedTile.FrameState.DistanceToCamera;
        double ctrDist = selectedTile.FrameState.PixelsToCameraCenter;

        builder.Append("\nselected tile " + selectedTile.Id + ", depth " + selectedTile.Depth);
        builder.Append(", " + selectedTile.Children.Count + " children");
        builder.Append("\ngeometric error " + selectedTile.GeometricError.ToString("F3"));
        builder.Append(", distance " + (dist < float.MaxValue ? dist : -1).ToString("F3"));
        builder.Append(", SSE " + selectedTile.FrameState.ScreenSpaceError.ToString("F3"));
        builder.Append("\n" + ((int)ctrDist) + " pixels to view center");

        builder.Append("\nbounds vol " + bv + ": " + selectedTile.BoundingVolume.SizeString());
        if (cbv >= 0 && cbv != bv)
        {
            builder.Append(", content vol " + cbv);
        }
        
        var tc = selectedTile.Content;
        if (tc != null && selectedTile.ContentState == Unity3DTileContentState.READY)
        {
            builder.Append("\n" + FmtKMG(tc.FaceCount) + " tris, " + FmtKMG(tc.PixelCount) + " pixels, ");
            builder.Append(tc.TextureCount + " textures, max " + tc.MaxTextureSize.x + "x" + tc.MaxTextureSize.y);
            if (tc.Index != null)
            {
                builder.Append("\n" + tc.Index.Width + "x" + tc.Index.Height + " index, " +
                               tc.Index.NumNonzero + " nonzero");
            }
        }
        
        selectedTile.Tileset.GetRootTransform(out Vector3 translation, out Quaternion rotation, out Vector3 scale,
                                              convertToUnityFrame: false);
        if (translation != Vector3.zero)
        {
            builder.Append("\ntileset translation " + translation.ToString("f3"));
        }
        if (rotation != Quaternion.identity)
        {
            builder.Append("\ntileset rotation " + rotation.ToString("f3"));
        }
        if (scale != Vector3.one)
        {
            builder.Append("\ntileset scale " + scale.ToString("f3"));
        }

        if (selectedTile.Parent != null)
        {
            builder.Append("\npress up/left/right");
            if (selectedTile.Children.Count > 0)
            {
                builder.Append("/down");
            }
            builder.Append(" to select parent/sibling");
            if (selectedTile.Children.Count > 0)
            {
                builder.Append("/child");
            }
        }
        else if (selectedTile.Children.Count > 0)
        {
            builder.Append("\npress down to select child");
        }
        
        if (selectedTile.Parent != null && Input.GetKeyDown(KeyCode.UpArrow))
        {
            selectedStack.Push(selectedTile);
            selectedTile = selectedTile.Parent;
        }
        
        if (selectedTile.Children.Count > 0 && Input.GetKeyDown(KeyCode.DownArrow))
        {
            var child = selectedTile.Children
                .Where(c => c.BoundingVolume.Contains(pointer.transform.position))
                .FirstOrDefault();
            selectedTile = selectedStack.Count > 0 ? selectedStack.Pop() : (child ?? selectedTile.Children.First());
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
        
        if (Input.GetKeyDown(KeyCode.B))
        {
            var modes = (DrawBoundsMode[])Enum.GetValues(typeof(DrawBoundsMode));
            int curMode = Math.Max(Array.IndexOf(modes, boundsMode), 0);
            boundsMode = modes[(curMode + 1) % modes.Length];
        }

        builder.Append("\ndrawing " + boundsMode + " bounds, press b to toggle");

        switch (boundsMode)
        {
            case DrawBoundsMode.No: break;
            case DrawBoundsMode.Selected:
            {
                selectedTile.BoundingVolume.DebugDraw(Color.magenta, selectedTile.Tileset.Behaviour.transform);
                if (cbv >= 0 && cbv != bv)
                {
                    selectedTile.ContentBoundingVolume.DebugDraw(Color.red, selectedTile.Tileset.Behaviour.transform);
                }
                break;
            }
            case DrawBoundsMode.Parent:
            {
                var parent = selectedTile.Parent;
                if (parent != null)
                {
                    float pbv = parent.BoundingVolume.Volume();
                    float pcbv = parent.ContentBoundingVolume != null ? parent.ContentBoundingVolume.Volume() : -1;
                    parent.BoundingVolume.DebugDraw(Color.cyan, selectedTile.Tileset.Behaviour.transform);
                    if (pcbv >= 0 && pcbv != pbv)
                    {
                        parent.ContentBoundingVolume.DebugDraw(Color.blue, selectedTile.Tileset.Behaviour.transform);
                    }
                }
                break;
            }
            case DrawBoundsMode.Ancestor:
            {
                for (var ancestor = selectedTile; ancestor != null; ancestor = ancestor.Parent)
                {
                    ancestor.BoundingVolume.DebugDraw(Color.magenta, ancestor.Tileset.Behaviour.transform);
                }
                break;
            }
            case DrawBoundsMode.All:
            {
                void drawBounds(Unity3DTile tile)
                {
                    tile.BoundingVolume.DebugDraw(Color.magenta, tile.Tileset.Behaviour.transform);
                    foreach (var child in tile.Children)
                    {
                        drawBounds(child);
                    }
                }
                drawBounds(selectedTile.Tileset.Root);
                break;
            }
            case DrawBoundsMode.Leaf:
            {
                void drawBounds(Unity3DTile tile)
                {
                    if (tile.Children.Count == 0)
                    {
                        tile.BoundingVolume.DebugDraw(Color.magenta, tile.Tileset.Behaviour.transform);
                    }
                    foreach (var child in tile.Children)
                    {
                        drawBounds(child);
                    }
                }
                drawBounds(selectedTile.Tileset.Root);
                break;
            }
            case DrawBoundsMode.Active:
            {
                void drawBounds(Unity3DTile tile)
                {
                    if (tile.ContentActive)
                    {
                        tile.BoundingVolume.DebugDraw(Color.magenta, tile.Tileset.Behaviour.transform);
                    }
                    foreach (var child in tile.Children)
                    {
                        drawBounds(child);
                    }
                }
                drawBounds(selectedTile.Tileset.Root);
                break;
            }
            default: Debug.LogWarning("unknown bounds mode: " + boundsMode); break;
        }

        if (tilesets != null && tilesets.Count > 0)
        {
            builder.Append("\npress i to hide tileset");
            if (Input.GetKeyDown(KeyCode.I))
            {
                selectedTile.Tileset.TilesetOptions.Show = false;
                showStack.Push(selectedTile.Tileset);
                selectedTile = null;
                selectedStack.Clear();
            }
        }
        
        builder.Append("\npress esc to clear selection");
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            selectedTile = null;
            selectedStack.Clear();
        }

        if (selectedTile != null)
        {
            if (forceSelectedTile)
            {
                builder.Append("\nforcing selected tile to render, hit r to toggle");
                selectedTile.Tileset.Traversal.ForceTiles.Add(selectedTile);
            }
            else
            {
                builder.Append("\nnot forcing selected tile to render, hit r to toggle");
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                forceSelectedTile = !forceSelectedTile;
            }
        }
    }
    
    private void UpdatePicking()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            enablePicking = !enablePicking;
        }

        if (enablePicking && hasFocus && !MouseNavBase.MouseOnUI())
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
        if (pointerActive)
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

    private bool IsSky(Unity3DTileset ts)
    {
        return ts != null && ts.TilesetOptions != null && ts.TilesetOptions.Name != null &&
            ts.TilesetOptions.Name.EndsWith("sky", StringComparison.OrdinalIgnoreCase);
    }

    private BoundingSphere NonSkyBounds()
    {
        return tileset.BoundingSphere(ts => !IsSky(ts));
    }

    public void ResetView(bool unlimited = false)
    {
        if (tileset != null)
        {
            var so = tileset.SceneOptions;
            var c2t = Matrix4x4.TRS(so.DefaultCameraTranslation, so.DefaultCameraRotation, Vector3.one);
            var c2w = c2t.UnityMatrix4x4ConvertFromGLTF() * tileset.transform.localToWorldMatrix;
            var cam = Camera.main.transform;
            cam.position = new Vector3(c2w.m03, c2w.m13, c2w.m23);
            cam.rotation = c2w.rotation;
            cam.localScale = Vector3.one;

            if (tileset.Ready())
            {
                UpdateFarClip(force: true);

                var nsb = unlimited ? tileset.BoundingSphere() : NonSkyBounds();
                var diam = nsb.radius * 2;
                if (mouseOrbit != null)
                {
                    mouseOrbit.pivot = tileset.transform.TransformPoint(nsb.position);
                    if (diam > 0 && relativeNavTransSpeed > 0)
                    {
                        mouseOrbit.transSpeed = diam / relativeNavTransSpeed;
                    }
                }
                if (mouseFly != null && diam > 0 && relativeNavTransSpeed > 0)
                {
                    mouseFly.transSpeed = diam / relativeNavTransSpeed;
                }
            }
        }
    }

    public void FitView(bool unlimited = false)
    {
        if (tileset != null && tileset.Ready())
        {
            var cam = Camera.main.transform;
            var nsb = unlimited ? tileset.BoundingSphere() : NonSkyBounds();

            var ctrInWorld = tileset.transform.TransformPoint(nsb.position);
            cam.Translate(Vector3.ProjectOnPlane(ctrInWorld - cam.position, cam.forward), Space.World);

            var tilesetToCam = tileset.transform.localToWorldMatrix * cam.worldToLocalMatrix; //row major compose l->r
            var t2cScale = tilesetToCam.lossyScale;
            var maxScale = Mathf.Max(t2cScale.x, t2cScale.y, t2cScale.z);
            var radiusInCam = (nsb.radius > 0 ? nsb.radius : 10) * maxScale;

            var vfov = Camera.main.fieldOfView * Mathf.Deg2Rad;
            var hfov = vfov * Camera.main.aspect;
            var minFov = Mathf.Min(vfov, hfov);

            var dist = radiusInCam / Mathf.Tan(minFov / 2);
            cam.Translate(cam.forward * (Vector3.Distance(cam.position, ctrInWorld) - dist), Space.World);

            if (mouseOrbit != null)
            {
                mouseOrbit.pivot = ctrInWorld;
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
