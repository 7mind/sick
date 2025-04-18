#!/usr/bin/env bash

set -euo pipefail

function run-test() {
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
}