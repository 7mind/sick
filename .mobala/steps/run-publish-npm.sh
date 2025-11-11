#!/usr/bin/env bash

set -euo pipefail

function run-publish-npm() {
  [[ "$CI_PULL_REQUEST" != "false"  ]] && exit 0

  VERSION=$(cat version.txt | tr -d '\n')
  mkdir -p npm-publish
  cp ./json-sick-scala/target/dist/* npm-publish/
  cp ./json-sick-scala/npm-template/* npm-publish/
  sed -i "s/VERSION_PLACEHOLDER/$VERSION/g" npm-publish/package.json
  ls -la npm-publish/
  cat npm-publish/package.json

  cd npm-publish

  nix develop --command bash -c "
    npm install --save-dev ava
    npm test
    npm publish --provenance --access public
  "
}
