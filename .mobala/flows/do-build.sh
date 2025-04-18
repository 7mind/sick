#!/usr/bin/env bash

set -euo pipefail

function do-build() {
  step_run_cond run-test
  step_run_cond run-publish-scala
  step_run_cond run-publish-cs
}
