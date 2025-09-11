#!/usr/bin/env bash

set -euo pipefail

function run-publish-scala() {
  cd json-sick-scala
  
  [[ "$CI_PULL_REQUEST" != "false"  ]] && exit 0

  if [[ "$CI_BRANCH_TAG" =~ ^v.*$ ]] ; then
      sbt +clean +test +publishSigned sonaUpload sonaRelease
      sbt '++2.13 json-sickJS/fullOptJS'
  else
      sbt +clean +test +publishSigned
  fi
}
