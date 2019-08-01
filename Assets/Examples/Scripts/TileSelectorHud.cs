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
            TileSelectorController.instance.onViewModeChanged += OnTileSelectorViewModeChanged;

            // simulate setengaged and tileselected events
            OnTileSelectorEngagedSet(TileSelectorController.instance.isEngaged);
            OnTileSelected(TileSelectorController.instance.selectedTile);
        }
        private void OnDestroy()
        {
            TileSelectorController.instance.onEngagedSet -= OnTileSelectorEngagedSet;
            TileSelectorController.instance.onTileSelected -= OnTileSelected;
            TileSelectorController.instance.onViewModeChanged -= OnTileSelectorViewModeChanged;
        }
        private void Update()
        {
            RefreshInfoText();
        }

        // info text
        private void RefreshInfoText()
        {
            var selectedTile = TileSelectorController.instance.selectedTile;
            var viewMode = TileSelectorController.instance.viewMode;

            // tile selector controller state
            var tileSelectorStateText = string.Format(
                "  View Mode: {0}\n  Child Stack Count: {1}",
                viewMode,
                TileSelectorController.instance.ChildStackSize);

            // selected tile info
            var selectedTileText = "  No Tile Selected. Right Click anywhere on the mesh to select a tile.";
            if (null != selectedTile)
            {
                var boundingBox = selectedTile.BoundingVolume as TileOrientedBoundingBox;
                var frameState = selectedTile.FrameState;
                selectedTileText = string.Format(
                        "  Id: {0}\n  Center: {1}\n  Is Leaf: {2}\n  Geometric Error: {3}\n  Screen Space Error: {4}",
                        selectedTile.Id,
                        boundingBox.Center,
                        selectedTile.Children.Count == 0,
                        selectedTile.GeometricError,
                        null != frameState ? frameState.ScreenSpaceError.ToString() : "Unknown");

            }

            _selectedTileInfoText.text = string.Format("TILE SELECTOR STATE:\n{0}\n\nSELECTED TILE INFO:\n{1}", tileSelectorStateText, selectedTileText);
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
        private void OnTileSelectorViewModeChanged(TileSelectorController.SelectionViewMode viewMode)
        {
            //RefreshInfoText();
        }
    }
}
