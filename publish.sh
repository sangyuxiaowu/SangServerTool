#!/bin/bash

rm -rf publish/*
mkdir publish
project="SangServerTool"
plates=("linux-x64" "linux-arm64" "osx-x64" "win-x64")

for plate in ${plates[*]}; do
    echo
    echo "=========开始发布：${plate} ========="
    echo
    dotnet publish $project/$project.csproj -c Release -f net6.0 --sc -r $plate -o=publish/$project.$plate -p:PublishSingleFile=true -p:PublishTrimmed=true
    echo
    echo "=========开始打包 ========="
    echo
    cd publish
    rm $project.$plate/$project.pdb -f
    tar -zcvf $project.$plate.tar.gz $project.$plate || exit 1 
    cd ../
done