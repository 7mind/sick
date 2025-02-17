#!/usr/bin/env bash

set -x
set -e

git clean -fxd -e private -e .idea

pushd .
cd ./json-sick-scala
sbt +clean +test:compile +test
popd

pushd .
cd ./json-sick-csharp
dotnet build
dotnet test

popd
