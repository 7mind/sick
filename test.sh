#!/usr/bin/env bash

set -x
set -e

# pushd .
# cd ./json-sick-scala
# sbt +clean +test:compile +test +test:run
# popd

pushd .
cd ./json-sick-csharp
dotnet build
dotnet test

popd
