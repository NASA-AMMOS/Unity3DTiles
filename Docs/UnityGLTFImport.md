# Importing and Updating UnityGLTF

The [UnityGLTF](https://github.com/KhronosGroup/UnityGLTF) package is used for loading GLTF files.  For simplicity we have imported the project directly into this repository.  Instead of building UnityGLTF into a plugin, we instead import the code directly into unity to simplify building for multiple platforms and to enable Newtonsoft as our JSON parser.

Please note that UnityGLTF may be significantly out of date at times and should be periodically updated.  We attempt to push any important changes/bug fixes back to the project in the hope that some day it can easily be incorporated out of the box.

In the mean time, the following steps were taken last time UnityGLTF was updated.  Some steps may now be out of date.

#### Updating the 3D Tiles Scheme

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
   7. ~~Added additional methods onto BoundingVolume~~
   8. ~~Changed type of Tile.ViewerRequestVolume from object to BoundingVolume~~
   9. Made Tile.Refine nullable (i.e. `public TileRefine` to `public TileRefine?`)