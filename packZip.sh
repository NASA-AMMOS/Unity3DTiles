#!/bin/sh

# packs Unity3DTilesWeb.zip not including StreamingAssets, which just has some bulky demo data
# this script may not work in Git bash because zip is not available there by default
# (use cygwin instead)

dir=Unity3DTilesWeb 
if [ ! -d $dir ]; then
    echo "first build GenericWeb WebGL scene, see README.md#building-the-generic-web-scene"
    exit 1
fi

mkdir tmp
cd tmp
mkdir $dir
cp -r ../$dir/Build $dir
cp -r ../Assets/Examples/Options/*.json $dir
cp ../$dir/index.html $dir
zip -rp $dir.zip $dir
mv $dir.zip ..
cd ..
rm -rf tmp
