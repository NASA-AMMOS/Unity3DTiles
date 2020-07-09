# Importing and Updating UnityGLTF

The [UnityGLTF](https://github.com/KhronosGroup/UnityGLTF) package is used for loading GLTF files.  For simplicity we have imported the project directly into this repository.  Instead of building UnityGLTF into a plugin, we instead import the code directly into unity to simplify building for multiple platforms and to enable Newtonsoft as our JSON parser.

Please note that UnityGLTF may be significantly out of date at times and should be periodically updated.  We attempt to push any important changes/bug fixes back to the project in the hope that some day it can easily be incorporated out of the box.

TODO document the last upstream version of UnityGLTF that was imported (I believe it was prior to April 2, 2019).

TODO document the custom changes that were made to it.

One thing we've changed is to add support for `KHR_materials_unlit` in GLTFSceneImporter.cs.

Another thing we changed is we catch and forward exceptions in AbstractWebRequestLoader.LoadStream().
