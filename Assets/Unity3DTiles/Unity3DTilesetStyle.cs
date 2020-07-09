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
using UnityEngine;

namespace Unity3DTiles
{
    /// <summary>
    /// This base class can be used to define a style that gets applied to tiles before they are rendered
    /// A style should be set on the Unity3DTileset class to change how tiles in that tileset are rendered
    /// A copy of the tileset's style is created for each tile using the "CreateDefault" method
    /// The idea is that individual tiles can use this object to track what style attributes the tile was last
    /// rendered with.  The next time the tileset style is applied, the tile's style can compare its attributes
    /// with those of the tileset and only modify values that have changed.
    /// </summary>
    [System.Serializable]
    public abstract class Unity3DTilesetStyle
    {       
        /// <summary>
        /// This method should be called on the Tilesets style on each of the individual tile
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public void ApplyStyle(Unity3DTile tile)
        {
            // If tile doesn't have a style or its type doesn't match us (the target style), set the tile style to a clone of ourselves
            if (tile.Style == null || tile.Style.GetType() != this.GetType())
            {
                tile.Style = (Unity3DTilesetStyle)this.CreateDefault();
            }
            if (tile.Content != null)
            {
                tile.Style.UpdateAndApply(this, tile.Content);
            }
        }
        
        /// <summary>
        /// This method should copy any relevant attributes from the target style to 'this' style
        /// In cases where the attributes are different the method should update materials on the tile accordingly
        /// </summary>
        /// <param name="targetStyle"></param>
        /// <param name="tile"></param>
        protected abstract void UpdateAndApply(Unity3DTilesetStyle targetStyle, Unity3DTileContent content);

        /// <summary>
        /// This method should return a new instance of this style with default styling arguments
        /// These arguments should be out of the range of normal values to force them to be updated in UpdateAndAplly
        /// </summary>
        /// <returns></returns>
        protected abstract object CreateDefault();

    }
}
