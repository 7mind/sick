#!/usr/bin/env bash

set -euo pipefail
if [[ "${DO_VERBOSE}" == 1 ]] ; then set -x ; fi

export NIXIFIED="${NIXIFIED:-0}"
export DO_VERBOSE="${DO_VERBOSE:-0}"

#------------------------------------------------------------------------------------------
# Tweak JAVA_OPTIONS
export _JAVA_OPTIONS="${_JAVA_OPTIONS:-""}"

# JVM ignores HOME and relies on getpwuid to determine home directory
# That fails when we run self-hosted github agent under non-dynamic user
# We need that for rootless docker to work
if [[ "${NIXIFIED}" == 1 ]] ; then
  _JAVA_OPTIONS+=" -Duser.home=${HOME}"
fi

# Append Java Options tail
#[help]- Set `JAVA_OPTIONS_TAIL` environment variable with additional Java arguments.
_JAVA_OPTIONS+=" ${JAVA_OPTIONS_TAIL:-""}"
# Format Java Options
_JAVA_OPTIONS="$(echo "${_JAVA_OPTIONS}" | grep -v '#' | tr '\n' ' ' | tr -s ' ')"
#------------------------------------------------------------------------------------------

if [[ "${DO_VERBOSE}" == 1 && "${VERBOSE_LEVEL}" -gt 1 ]] ; then
  environment=$(env)
  environment=$(echo "$environment" | grep -v '^\s*$' | sed "s/^/[verbose:env] /;s/$/ /")
  echo "[verbose] Environment set:"
  echo "$environment"
fi

# this script receives all the CLI args from the main script and may decide which flows should be enabled
flow_enable do-build