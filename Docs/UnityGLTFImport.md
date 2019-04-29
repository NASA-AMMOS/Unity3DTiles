# Importing and Updating UnityGLTF

The [UnityGLTF](https://github.com/KhronosGroup/UnityGLTF) package is used for loading GLTF files.  For simplicity we have imported the project directly into this repository.  Instead of building UnityGLTF into a plugin, we instead import the code directly into unity to simplify building for multiple platforms and to enable Newtonsoft as our JSON parser.

Please note that UnityGLTF may be significantly out of date at times and should be periodically updated.  We attempt to push any important changes/bug fixes back to the project in the hope that some day it can easily be incorporated out of the box.

In the mean time, the following steps were taken last time UnityGLTF was updated.  Some steps may now be out of date.

#### Updating the 3D Tiles Scheme

1. Download latest [schema](https://github.com/AnalyticalGraphicsInc/3d-tiles/tree/master/schema) from 3D Tiles github and place files in `SchemaGen\Generate\3DTileSchema`.  See also [instructions on UnityGLTF](https://github.com/KhronosGroup/UnityGLTF#gltfserializer)
2. Update the references in SchemaGen\Generate.csproj to include any new schema files or remove any old ones
3. Build SchemaGen\Generate.sln which will replace Assets\Unity3DTiles\Schema.cs
4. Manually edit Schema.cs to build under .net 3.5 and to fix name errors such as:
   1. Properties2 - delete and use Dictionary<string,object> instead
   2. Tile becomes TileContent
   3. Tile2 becomes Tile
   4. Remove {get;set;} for any property with a default value
   5. Find and replace System.Array with List
   6. Added additional methods onto BoundingVolume
   7. Changed type of Tile.ViewerRequestVolume from object to BoundingVolume
   8. Made Tile.Refine nullable
