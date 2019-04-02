using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity3DTiles
{
    /// <summary>
    /// This style can be used to render a tileset with a specified shader
    /// </summary>
    [System.Serializable]
    public class SimpleShaderTilesetStyle : Unity3DTilesetStyle
    {
        public Shader Shader;

        public SimpleShaderTilesetStyle() { }

        public SimpleShaderTilesetStyle(Shader s)
        {
            this.Shader = s;
        }

        protected override object CreateDefault()
        {
            var r = new SimpleShaderTilesetStyle();
            return r;
        }

        protected override void UpdateAndApply(Unity3DTilesetStyle targetStyle, Unity3DTileContent content)
        {
            SimpleShaderTilesetStyle target = (SimpleShaderTilesetStyle)targetStyle;
            // Check to see if style attributes are out of date and lazily update
            if (target.Shader != this.Shader)
            {
                this.Shader = target.Shader;
                var renderers = content.GetRenderers();
                for (int i = 0; i < renderers.Length; i++)
                {
                    var r = renderers[i];
                    for (int j = 0; j < r.materials.Length; j++)
                    {
                        r.materials[j].shader = this.Shader;                        
                    }
                }
            }
        }
    }
}