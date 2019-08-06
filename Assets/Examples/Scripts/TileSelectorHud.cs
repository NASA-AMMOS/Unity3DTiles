using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Unity3DTiles
{
    public class TileSelectorHud : MonoBehaviour
    {
        // FIELDS
        // editor
        [SerializeField]
        private Animator _animatorController = null;
        [SerializeField]
        private Text _selectedTileInfoText = null;
        [SerializeField]
        private Text _tabButtonText = null;

        // toggle controls
        private bool _showControlsText = true;
        public bool showControlsText
        {
            get
            {
                return _showControlsText;
            }
            set
            {
                _showControlsText = value;
                RefreshInfoText();
            }
        }

        // animation
        private readonly string ANIMATION_PARAMETER_ENGAGED = "engaged";
        private int engagedHash;

        // METHODS
        // unity
        private void Awake()
        {
            // get anim param hashes
            engagedHash = Animator.StringToHash(ANIMATION_PARAMETER_ENGAGED);
        }
        private void Start()
        {
            TileSelectorController.instance.onEngagedSet += OnTileSelectorEngagedSet;
            TileSelectorController.instance.onTileSelected += OnTileSelected;
            TileSelectorController.instance.onHighlightModeChanged += OnTileSelectorHighlightModeChanged;
            TileSelectorController.instance.onTreeViewOffsetChanged += OnTileSelectorViewOffsetChanged;
            var flyCam = Camera.main.GetComponent<FlyCamera>();
            if(flyCam != null)
            {
                flyCam.onCameraPositionChanged += OnCameraPositionChanged;
            }

            // simulate setengaged and tileselected events
            OnTileSelectorEngagedSet(TileSelectorController.instance.isEngaged);
            OnTileSelected(TileSelectorController.instance.selectedTile);
        }
        private void OnDestroy()
        {
            TileSelectorController.instance.onEngagedSet -= OnTileSelectorEngagedSet;
            TileSelectorController.instance.onTileSelected -= OnTileSelected;
            TileSelectorController.instance.onHighlightModeChanged -= OnTileSelectorHighlightModeChanged;
            TileSelectorController.instance.onTreeViewOffsetChanged -= OnTileSelectorViewOffsetChanged;
            var flyCam = Camera.main != null ? Camera.main.GetComponent<FlyCamera>() : null;
            if (flyCam != null)
            {
                flyCam.onCameraPositionChanged -= OnCameraPositionChanged;
            }
        }

        // info text
        private void RefreshInfoText()
        {
            var selectedTile = TileSelectorController.instance.selectedTile;

            // CONTROLS
            string controlsText = null;
            if (showControlsText)
            {
                controlsText =
                    "<b>CONTROLS</b>:\n" +
                    "  -Right Click: Set selected tile\n" +
                    "  -Backspace: Deselect tile\n" +
                    "  -Up Arrow: Show parent of selected\n" +
                    "  -Dn Arrow: Show children of selected\n" +
                    "  -Page Up: Select the parent of selected\n" +
                    "  -Page Dn: Select last visited child of selected\n" +
                    "  -0,1,2,3: Select corresponding child of selected\n" +
                    "  -H: Change highligh mode\n" +
                    "\n<i>**Click and drag geometric and screenspace error callouts to move them around**</i>";
            }

            // INFO
            string infoText = null;
            if (null != selectedTile)
            {
                var maxDepthFromSelected = GetMaxLeafDepthFromTile(selectedTile);

                // tile selector controller state
                var viewMode = TileSelectorController.instance.highlightMode;
                var tileSelectorStateText = string.Format(
                    "  Highlight Mode: {0}",
                    viewMode);

                // selected tile info
                var selectedTileText = string.Format(
                        "  Id: {0}\n" +
                        "  Depth in Tree: {1}/{2}\n" +
                        "  Geometric Error: {3}\n" +
                        "  Screen Space Error: {4}",
                        selectedTile.Id,
                        selectedTile.Depth,
                        maxDepthFromSelected,
                        selectedTile.GeometricError,
                        selectedTile.FrameState.ScreenSpaceError);

                // rendered tile info
                var renderedTiles = TileSelectorController.instance.GetTilesAtCurrentTreeViewOffset();
                string tileIds = string.Empty;
                for(int i = 0; i < renderedTiles.Length; ++i)
                {
                    tileIds += renderedTiles[i].Id + (i == renderedTiles.Length - 1 ? "" : ", ");
                }
                var renderedTileInfoText = string.Format(
                        "  Offset: {0}\n" +
                        "  Depth in Tree: {1}/{2}\n" +
                        "  Num Displayed: {3}\n" +
                        "  Ids:  {4}",
                        TileSelectorController.instance.treeViewOffset,
                        TileSelectorController.instance.treeViewOffset + selectedTile.Depth,
                        maxDepthFromSelected,
                        renderedTiles.Length,
                        tileIds);

                infoText = string.Format(
                    "<b>TILE SELECTOR STATE</b>:\n{0}\n\n" +
                    "<b>SELECTED TILE INFO</b>:\n{1}\n\n" +
                    "<b>RENDERED TILE INFO</b>:\n{2}",
                    tileSelectorStateText,
                    selectedTileText,
                    renderedTileInfoText);
            }

            if (null != controlsText && null != infoText)
            {
                _selectedTileInfoText.text = controlsText + "\n\n" + infoText;
            }
            else if(null != controlsText)
            {
                _selectedTileInfoText.text = controlsText;
            }
            else if(null != infoText)
            {
                _selectedTileInfoText.text = infoText;
            }
            else
            {
                _selectedTileInfoText.text = " ";
            }
        }

        // tile selector event handlers
        private void OnTileSelectorEngagedSet(bool engaged)
        {
            // change button text
            _tabButtonText.text = engaged ? ">" : "<";

            // set animation param
            _animatorController.SetBool(engagedHash, engaged);
        }
        private void OnTileSelected(Unity3DTile selectedTile)
        {
            RefreshInfoText();
        }
        private void OnTileSelectorHighlightModeChanged(TileSelectorController.HighlightMode mode)
        {
            RefreshInfoText();
        }
        private void OnTileSelectorViewOffsetChanged(int viewOffset)
        {
            RefreshInfoText();
        }
        private void OnCameraPositionChanged()
        {
            RefreshInfoText();
        }

        // helpers
        private int GetMaxLeafDepthFromTile(Unity3DTile myTile)
        {
            int maxDepth = 0;
            var dfsStack = new Stack<Unity3DTile>();
            dfsStack.Push(myTile);
            while (dfsStack.Count > 0)
            {
                var tile = dfsStack.Pop();

                // if we're at a leaf, record the depth and continue
                if (tile.Children.Count == 0)
                {
                    maxDepth = Mathf.Max(maxDepth, tile.Depth);
                    continue;
                }

                // otherwise, add all children
                for (int iChild = 0; iChild < tile.Children.Count; ++iChild)
                {
                    dfsStack.Push(tile.Children[iChild]);
                }
            }

            return maxDepth;
        }
    }
}
