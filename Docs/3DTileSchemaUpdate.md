### Updating the 3D Tiles Scheme

The following instructions describe how to update the 3D-Tiles schema to support updates the the specification.

1. Download latest [schema](https://github.com/KhronosGroup/glTF/tree/master/specification) from 3D Tiles github and place files in `SchemaGen\Generate\3DTileSchema`.  
2. Update the references in `SchemaGen\Generate.csproj` under the `3DTileSchema` folder to include any new schema files or remove any old ones.   Also ensure that all schema files have "Copy to Output Directory=Copy if newer"  in the properties window otherwise you will get an exception hen running
3. Build and run SchemaGen\Generate.sln which will replace Assets\Unity3DTiles\TilesetSchema.cs
4. Manually edit TilesetSchema.cs to build under .net 3.5 and to fix name errors such as:
   1. Properties2 - delete and use Dictionary<string,object> instead
   
   2. Rename/Refactor the class `Tile` and all references to `TileContent` (be careful not to change other references in the project outside of TilesetSchema.cs)
   
   3. Rename/Refactor the class `Tile2` becomes `Tile`
   
   4. Rename/Refactor the class `Tile2Refine` to `TileRefine`
   
   5. Remove {get;set;} for any property with a default value
   
   6. Find and replace `System.Array` with `List`.  Add `using System.Collections.Generic;`
   
   7. Made Tile.Refine nullable (i.e. `public TileRefine` to `public TileRefine?`)
   
   8. Edited TileContent.Uri section to support loading older tilesets that used "url"
   
      ``````
      /// <summary>A uri that points to the tile's content. When the uri is relative, it is relative to the referring tileset JSON file.</summary>
          [Newtonsoft.Json.JsonProperty("uri", NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
          public string Uri { get; set; }
      
          /// <summary>
          /// Property to support legacy format that used url instead of uri
          /// </summary>
          [Newtonsoft.Json.JsonProperty("url", NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
          public string Url { get; set; }
      
          /// <summary>
          /// Helper method to get uri or uri parameter for backwards compatability
          /// </summary>
          /// <returns></returns>
          public string GetUri()
          {
              if(Uri!= null)
              {
                  return Uri;
              }
              return Url;
          }
      ```