#!/usr/bin/env bash

set -euo pipefail

function run-build-sjs() {
  cd json-sick-scala

  [[ "$CI_PULL_REQUEST" != "false"  ]] && exit 0

  sbt '++2.13 json-sickJS/fullOptJS'
}
