using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity3DTiles
{
    public class TileSelectorController : MonoBehaviour
    {
        // TYPES
        public enum HighlightMode
        {
            none,
            selected,
            children,
            NUM_VIEW_MODES
        }
        public enum SelectionChangeType
        {
            standard,
            toPreviousChild,
            ToParent
        }

        // FIELDS
        // singleton interface
        public static TileSelectorController instance
        {
            get
            {
                return _instance;
            }
        }
        private static TileSelectorController _instance = null;

        // editor
        [SerializeField]
        private TilesetBehaviour _tileset = null;
        [SerializeField]
        private GameObject _selectedTilePointer = null;
        [SerializeField]
        private GameObject _selectedTileHighlight = null;
        [SerializeField]
        private GameObject[] _selectedTileChildHighlights = new GameObject[] { };
        [SerializeField]
        private GameObject _geometricErrorCallout = null;
        [SerializeField]
        private RectTransform _screenSpaceErrorCallout = null;
        [SerializeField]
        private int _minTreeViewOffset = 0;
        [SerializeField]
        private int _maxTreeViewOffset = 1;

        // selected tile
        public Unity3DTile selectedTile
        {
            get
            {
                return _selectedTile;
            }
        }
        private Unity3DTile _selectedTile = null;
        private Stack<Unity3DTile> _childSelectionStack = new Stack<Unity3DTile>();
        public int treeViewOffset
        {
            get
            {
                return _treeViewOffset;
            }
            private set
            {
                // don't allow setting outside of specified bounds
                if (value < _minTreeViewOffset || value > _maxTreeViewOffset)
                {
                    return;
                }

                // if there are no tiles at desired view offset, don't set
                if (null == GetTilesAtTreeViewOffset(value))
                {
                    return;
                }

                _treeViewOffset = value;
                if(null != onTreeViewOffsetChanged) { onTreeViewOffsetChanged(_treeViewOffset); }
            }
        }
        private int _treeViewOffset = 0;

        // view mode
        public HighlightMode highlightMode
        {
            get
            {
                return _highlightMode;
            }
            set
            {
                _highlightMode = value;

                // refresh highlights
                RefreshTileSelectionHighlight();
            }
        }
        private HighlightMode _highlightMode = HighlightMode.none;

        // events
        public event System.Action<bool> onEngagedSet;
        public event System.Action<Unity3DTile> onTileSelected;
        public event System.Action<HighlightMode> onHighlightModeChanged;
        public event System.Action<int> onTreeViewOffsetChanged;

        // engaged
        public bool isEngaged
        {
            get
            {
                return _isEngaged;
            }
            set
            {
                _isEngaged = value;

                // if turning off, put in a 'default' state
                if (!_isEngaged)
                {
                    SetSelectedTile(null);
                    highlightMode = HighlightMode.selected;
                }

                // inform listeners
                if (null != onEngagedSet) { onEngagedSet(_isEngaged); }
            }
        }
        private bool _isEngaged = false;

        // error callouts
        private Vector2 _dragBeginMousePos = Vector2.zero;
        private Vector2 _dragBeginScreenSpaceErrorPos = Vector2.zero;
        private Vector3 _dragBeginGeometricErrorPos = Vector3.zero;
        private float _dragBeginGeometricErrorMouseYCoordinate = 0f;

        // METHODS
        // unity
        private void Awake()
        {
            Debug.Assert(
                _instance == null,
                string.Format("Error: multiple instances of {0} exist in scene. Remove component on {1}.",
                typeof(TileSelectorController).Name,
                this.name));

            _instance = this;
        }
        private void Start()
        {
            // put geometric error in same coordinate system as tiles
            _geometricErrorCallout.transform.SetParent(_tileset.transform);

            // put highlights in same coordinate system as tiles
            _selectedTileHighlight.transform.SetParent(_tileset.transform);
            for (int iChildHighlight = 0; iChildHighlight < _selectedTileChildHighlights.Length; ++iChildHighlight)
            {
                _selectedTileChildHighlights[iChildHighlight].transform.SetParent(_tileset.transform);
            }

            // refresh visual
            RefreshGeometricErrorCallout();
            RefreshTileSelectionHighlight();
            ResetScreenSpaceErrorPosition();
        }
        private void Update()
        {
            HandleInput();
            RefreshScreenspaceErrorCallout();
        }

        // input handling
        private void HandleInput()
        {
            // if the tool is engaged
            if (isEngaged)
            {
                // if we perform a click
                if (Input.GetMouseButtonDown(0))
                {
                    var viewingCam = Camera.main;

                    // 'select' the clicked tile, if any
                    RaycastHit hitInfo;
                    Ray clickRay = viewingCam.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(clickRay, out hitInfo, viewingCam.farClipPlane))
                    {
                        var tile = GetTileCorrespondingToCollider(hitInfo.collider);
                        if (null != tile)
                        {
                            SetSelectedTile(tile);

                            // set pointer position
                            _selectedTilePointer.transform.position = hitInfo.point;
                        }
                    }
                }

                // if we're selecting something
                if (null != selectedTile)
                {
                    // if we press the 'deselect' key
                    if (Input.GetKeyDown(KeyCode.Backspace))
                    {
                        SetSelectedTile(null);
                    }
                    // if we press the 'show tiles one node up' key
                    else if (Input.GetKeyDown(KeyCode.UpArrow))
                    {
                        treeViewOffset -= 1;
                    }
                    // if we press the 'show tiles one node down' key
                    else if (Input.GetKeyDown(KeyCode.DownArrow))
                    {
                        treeViewOffset += 1;
                    }
                    // if we press the 'select next node up' key
                    else if (Input.GetKeyDown(KeyCode.PageUp) && null != selectedTile.Parent)
                    {
                        SetSelectedTile(selectedTile.Parent, SelectionChangeType.ToParent);
                    }
                    // if we press the 'select to previous node down' key
                    else if (Input.GetKeyDown(KeyCode.PageDown) && _childSelectionStack.Count > 0)
                    {
                        SetSelectedTile(_childSelectionStack.Pop(), SelectionChangeType.toPreviousChild);
                    }
                    // if we press a key to go to a specific child
                    else if ((Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0)) && selectedTile.Children.Count > 0)
                    {
                        SetSelectedTile(selectedTile.Children[0]);
                    }
                    else if ((Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) && selectedTile.Children.Count > 1)
                    {
                        SetSelectedTile(selectedTile.Children[1]);
                    }
                    else if ((Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) && selectedTile.Children.Count > 2)
                    {
                        SetSelectedTile(selectedTile.Children[2]);
                    }
                    else if ((Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) && selectedTile.Children.Count > 3)
                    {
                        SetSelectedTile(selectedTile.Children[3]);
                    }
                }

                // if we press the 'change view mode' key
                if (Input.GetKeyDown(KeyCode.H))
                {
                    int newHighlightModeInt = ((int)highlightMode + 1) % (int)HighlightMode.NUM_VIEW_MODES;
                    highlightMode = (HighlightMode)newHighlightModeInt;

                    if(null != onHighlightModeChanged) { onHighlightModeChanged(highlightMode); }
                }
            }
        }

        // engaged
        public void ToggleEngaged()
        {
            isEngaged = !isEngaged;
        }

        // selected tile
        private void SetSelectedTile(Unity3DTile newSelectedTile, SelectionChangeType selectionChangeType = SelectionChangeType.standard)
        {
            // only do something if setting to new value
            if (_selectedTile != newSelectedTile)
            {
                // update last selected child stack
                switch (selectionChangeType)
                {
                    case SelectionChangeType.standard:
                        _childSelectionStack.Clear();
                        break;

                    case SelectionChangeType.ToParent:
                        _childSelectionStack.Push(_selectedTile);
                        break;

                    case SelectionChangeType.toPreviousChild:
                        // do nothing (i.e. maintain the stack)
                        break;
                }

                // on new selection, reset treeViewOffset
                treeViewOffset = 0;

                // update selection
                _selectedTile = newSelectedTile;

                // refresh visual elements
                RefreshGeometricErrorCallout();
                RefreshTileSelectionHighlight();
                ResetScreenSpaceErrorPosition();

                // inform listeners of selection change
                if (null != onTileSelected) { onTileSelected(_selectedTile); }
            }
        }
        private Unity3DTile GetTileCorrespondingToCollider(Collider collider)
        {
            if(null == collider)
            {
                return null;
            }

            Stack<Unity3DTile> dfsStack = new Stack<Unity3DTile>();
            dfsStack.Push(_tileset.Tileset.Root);
            while (dfsStack.Count > 0)
            {
                var tile = dfsStack.Pop();

                // look for collider on this tile
                if (tile.Content != null && tile.Content.ContainsCollider(collider))
                {
                    return tile;
                }

                // look for collider in children
                for(int iChild = 0; iChild < tile.Children.Count; ++iChild)
                {
                    dfsStack.Push(tile.Children[iChild]);
                }
            }

            return null;
        }
        public Unity3DTile[] GetTilesAtCurrentTreeViewOffset()
        {
            return GetTilesAtTreeViewOffset(treeViewOffset);
        }
        private Unity3DTile[] GetTilesAtTreeViewOffset(int offset)
        {
            // early out if we have no selected tile
            if(null == selectedTile)
            {
                return null;
            }

            // if no offset, just return array with selected tile
            if (offset == 0)
            {
                return new Unity3DTile[] { selectedTile };
            }
            // otherwise, if offset is negative / parent-ward / upward in the tree
            else if (offset < 0)
            {
                // return array containing offset-th ancestor, if any
                Unity3DTile ancestorTile = selectedTile;
                while (ancestorTile != null && ancestorTile.Depth > selectedTile.Depth + offset)
                {
                    ancestorTile = ancestorTile.Parent;
                }

                return ancestorTile != null ? new Unity3DTile[] { ancestorTile } : null;
            }
            // otherwise... (if the offset is positive / child-ward / downward in the tree)
            else
            {
                // return array containing all descendants at current + offset depth, if any 
                var descendantTilesAtOffset = new List<Unity3DTile>();
                var dfsStack = new Stack<Unity3DTile>();
                dfsStack.Push(selectedTile);
                while (dfsStack.Count > 0)
                {
                    var tile = dfsStack.Pop();

                    // if we're at the depth we want, add it to list and continue on
                    if(tile.Depth == selectedTile.Depth + offset)
                    {
                        descendantTilesAtOffset.Add(tile);
                        continue;
                    }

                    // add all children
                    for(int iChild = 0; iChild < tile.Children.Count; ++iChild)
                    {
                        dfsStack.Push(tile.Children[iChild]);
                    }
                }

                return descendantTilesAtOffset.Count > 0 ? descendantTilesAtOffset.ToArray() : null;
            }
        }

        // highlights
        private void RefreshTileSelectionHighlight()
        {
            // turn everything off by default
            _selectedTileHighlight.SetActive(false);
            _selectedTilePointer.SetActive(false);
            for (int iChildHighlight = 0; iChildHighlight < _selectedTileChildHighlights.Length; ++iChildHighlight)
            {
                _selectedTileChildHighlights[iChildHighlight].SetActive(false);
            }

            // if we're selecting something
            if (selectedTile != null)
            {
                // turn on pointer
                _selectedTilePointer.SetActive(true);

                var highlightModeOverride = DetermineHighlightModeOverrideForCurrentState();
                switch (highlightModeOverride)
                {
                    case HighlightMode.none:

                        // show nothing

                        break;

                    case HighlightMode.selected:

                        // turn on and position selected highlight
                        _selectedTileHighlight.SetActive(true);
                        PositionHighlightAtTile(_selectedTileHighlight, selectedTile);

                        break;

                    case HighlightMode.children:

                        // if there are children to highlight
                        if (selectedTile.HasChildren)
                        {
                            // turn on and position children highlights
                            for (int iChild = 0; iChild < selectedTile.Children.Count && iChild < _selectedTileChildHighlights.Length; ++iChild)
                            {
                                var childNode = selectedTile.Children[iChild];
                                var childHighlight = _selectedTileChildHighlights[iChild];
                                childHighlight.SetActive(true);
                                PositionHighlightAtTile(childHighlight, childNode);
                            }
                        }

                        break;
                }
            }
        }
        private void PositionHighlightAtTile(GameObject highlight, Unity3DTile tile)
        {
            // if either input is null, early out
            if(null == highlight || null == tile)
            {
                return;
            }

            var highlightTransform = highlight.transform;
            var boundingBox = tile.BoundingVolume as TileOrientedBoundingBox;

            // set position
            highlightTransform.localPosition = boundingBox.Center;

            // set orientation
            Quaternion rotation = new Quaternion();
            rotation.SetLookRotation(boundingBox.HalfAxesZ, boundingBox.HalfAxesY);
            highlightTransform.localRotation = rotation;

            // set size
            var xScale = boundingBox.HalfAxesX.magnitude * 2;
            var yScale = boundingBox.HalfAxesY.magnitude * 2;
            var zScale = boundingBox.HalfAxesZ.magnitude * 2;
            highlightTransform.localScale = new Vector3(xScale, yScale, zScale);
        }

        // error callouts
        private void RefreshGeometricErrorCallout()
        {
            // turn off by default
            _geometricErrorCallout.SetActive(false);

            if (null != selectedTile && selectedTile.HasChildren)
            {
                var calloutTransform = _geometricErrorCallout.transform;
                var boundingBox = selectedTile.BoundingVolume as TileOrientedBoundingBox;

                // turn on
                _geometricErrorCallout.SetActive(true);

                // get bounding box dimensions
                var boxXSize = boundingBox.HalfAxesX.magnitude * 2f;
                var boxYSize = boundingBox.HalfAxesY.magnitude * 2f;
                var boxZSize = boundingBox.HalfAxesZ.magnitude * 2f;

                // determine callout dimensions
                var calloutHeight = (float)selectedTile.GeometricError;

                // set position
                calloutTransform.localPosition = new Vector3(
                    boundingBox.Center.x,
                    boundingBox.Center.y,
                    boundingBox.Center.z + boxZSize / 2f + calloutHeight / 2f + 1f);

                // set orientation
                calloutTransform.localRotation = Quaternion.identity;

                // set size
                calloutTransform.localScale = new Vector3(
                    boxXSize,
                    boxYSize,
                    calloutHeight);
            }
        }
        private void RefreshScreenspaceErrorCallout()
        {
            // turn off by default
            _screenSpaceErrorCallout.gameObject.SetActive(false);

            if (null != selectedTile && selectedTile.HasChildren)
            {
                // turn on
                _screenSpaceErrorCallout.gameObject.SetActive(true);

                // set size
                _screenSpaceErrorCallout.sizeDelta = new Vector2(
                    _screenSpaceErrorCallout.sizeDelta.x,
                    selectedTile.FrameState.ScreenSpaceError);
            }
        }
        private void ResetScreenSpaceErrorPosition()
        {
            // set offset as point directly in front of viewing camera
            var calloutParent = _screenSpaceErrorCallout.transform.parent as RectTransform;
            _screenSpaceErrorCallout.anchoredPosition = new Vector2(
                -calloutParent.rect.width / 4f,
                0f);
        }
        public void OnScreenSpaceErrorDragBegin()
        {
            if (Input.GetMouseButton(0))
            {
                _dragBeginMousePos = Input.mousePosition;
                _dragBeginScreenSpaceErrorPos = _screenSpaceErrorCallout.anchoredPosition;
            }
        }
        public void OnScreenSpaceErrorDrag()
        {
            if (Input.GetMouseButton(0))
            {
                Vector2 currentMousePos = Input.mousePosition;

                // set position
                _screenSpaceErrorCallout.anchoredPosition =
                    _dragBeginScreenSpaceErrorPos + (currentMousePos - _dragBeginMousePos);
            }
        }
        public void OnGeometricErrorDragBegin()
        {
            if (Input.GetMouseButton(0))
            {
                _dragBeginGeometricErrorPos = _geometricErrorCallout.transform.position;
                _dragBeginGeometricErrorMouseYCoordinate = GetGeometricErrorYCoordinateForMousePosition(Input.mousePosition);
            }
        }
        public void OnGeometricErrorDrag()
        {
            if (Input.GetMouseButton(0))
            {
                var currentMouseYCoordinate = GetGeometricErrorYCoordinateForMousePosition(Input.mousePosition);
                var yDiff = currentMouseYCoordinate - _dragBeginGeometricErrorMouseYCoordinate;

                _geometricErrorCallout.transform.position = _dragBeginGeometricErrorPos + Vector3.up * yDiff;
            }
        }
        private float GetGeometricErrorYCoordinateForMousePosition(Vector3 mousePos)
        {
            var viewingCam = Camera.main;
            var geoTransform = _geometricErrorCallout.transform;

            // create plane parallel to y-axis, facing towards camera
            var geoToCam = viewingCam.transform.position - geoTransform.position;
            geoToCam = Vector3.ProjectOnPlane(geoToCam, Vector3.up);
            Plane plane = new Plane(geoToCam, geoTransform.position);

            // find point of intersection with that plane and ray from mouse pointer
            var pointerRay = viewingCam.ScreenPointToRay(mousePos);
            float distance;
            plane.Raycast(pointerRay, out distance);
            var intersectPoint = pointerRay.GetPoint(distance);

            return intersectPoint.y;
        }

        // view mode
        private HighlightMode DetermineHighlightModeOverrideForCurrentState()
        {
            // if we are either not selecting a tile or selecting a tile that has no children...
            //   => override the child view mode with standard view mode
            if (highlightMode == HighlightMode.children && (null == selectedTile || !selectedTile.HasChildren))
            {
                return HighlightMode.selected;
            }

            return highlightMode;
        }
    }
}
