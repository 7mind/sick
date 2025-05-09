#!/usr/bin/env bash

set -euo pipefail

function run-publish-scala() {
  cd json-sick-scala
  
  [[ "$CI_PULL_REQUEST" != "false"  ]] && exit 0

  sbt +clean +test sonatypeBundleRelease
  
#  if [[ "$CI_BRANCH_TAG" =~ ^v.*$ ]] ; then
#      sbt +clean +test +publishSigned sonatypeBundleRelease
#  else
#      sbt +clean +test +publishSigned
#  fi
}