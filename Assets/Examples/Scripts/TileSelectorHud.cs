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
            TileSelectorController.instance.onTreeViewOffsetChanged += OnTileSelectorViewOffsetChanged;

            // simulate setengaged and tileselected events
            OnTileSelectorEngagedSet(TileSelectorController.instance.isEngaged);
            OnTileSelected(TileSelectorController.instance.selectedTile);
        }
        private void OnDestroy()
        {
            TileSelectorController.instance.onEngagedSet -= OnTileSelectorEngagedSet;
            TileSelectorController.instance.onTileSelected -= OnTileSelected;
            TileSelectorController.instance.onTreeViewOffsetChanged -= OnTileSelectorViewOffsetChanged;
        }
        private void Update()
        {
            RefreshInfoText();
        }

        // info text
        private void RefreshInfoText()
        {
            var selectedTile = TileSelectorController.instance.selectedTile;
            var viewMode = TileSelectorController.instance.highlightMode;

            // tile selector controller state
            var tileSelectorStateText = string.Format(
                "  View Mode: {0}",
                viewMode);

            var selectedTileText = "  No tile selected. Left click anywhere on the mesh to select a tile.";
            var renderedTileInfo = "  No Tile Selected";
            if (null != selectedTile)
            {
                var maxDepthFromSelected = GetMaxLeafDepthFromTile(selectedTile);

                // selected tile info
                selectedTileText = string.Format(
                        "  Id: {0}\n  Depth in Tree: {1}/{2}\n  Geometric Error: {3}\n  Screen Space Error: {4}",
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
                renderedTileInfo = string.Format(
                        "  Offset: {0}\n  Depth in Tree: {1}/{2}\n  Num Displayed: {3}\n  Ids:  {4}",
                        TileSelectorController.instance.treeViewOffset,
                        TileSelectorController.instance.treeViewOffset + selectedTile.Depth,
                        maxDepthFromSelected,
                        renderedTiles.Length,
                        tileIds);
            }

            _selectedTileInfoText.text = string.Format(
                "TILE SELECTOR STATE:\n{0}\n\nSELECTED TILE INFO:\n{1}\n\nRENDERED TILE INFO:\n{2}",
                tileSelectorStateText,
                selectedTileText,
                renderedTileInfo);
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
            //RefreshInfoText();
        }
        private void OnTileSelectorViewOffsetChanged(int viewOffset)
        {
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
