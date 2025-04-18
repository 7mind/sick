#!/usr/bin/env bash

set -euo pipefail

read_trimmed_string() { [[ -s "$1" ]] && sed -e 's/^[[:space:]]*//' -e 's/[[:space:]]*$//' "$1" || echo "$2"; }

export CACHE_DIR="${XDG_CACHE_HOME:-"${HOME}/.cache"}"
export MOBALA_CACHE="${CACHE_DIR}/mobala.sh"
export MOBALA_CACHE_TMP="${CACHE_DIR}/mobala.sh.tmp"
export MOBALA_VERSION=$(read_trimmed_string ".mobala/version.txt" "release")
export MOBALA_BASE="https://raw.githubusercontent.com/7mind/mobala/refs/heads/${MOBALA_VERSION}"
export MOBALA_FILE="${MOBALA_BASE}/mobala.sh"

script_path="$(realpath "$0")"
script_dirname="$(dirname "$script_path")"

export MOBALA_PATH="${script_dirname}"
export MOBALA_SUBDIR=${MOBALA_SUBDIR:-".mobala"}
export MOBALA_KEEP=${MOBALA_KEEP:-"${MOBALA_PATH}/${MOBALA_SUBDIR}/keep.env"}
export MOBALA_ENV=${MOBALA_ENV:-"${MOBALA_PATH}/${MOBALA_SUBDIR}/env.sh"}
export MOBALA_MODS=${MOBALA_MODS:-"${MOBALA_PATH}/${MOBALA_SUBDIR}/mods"}
export MOBALA_PARAMS=${MOBALA_PARAMS:-"${MOBALA_PATH}/${MOBALA_SUBDIR}/params"}

function check-cache() {
    if [[ -f "${MOBALA_CACHE}" ]]; then
        echo "[info] Mobala.sh cache found at '${MOBALA_CACHE}'"
    else
        echo "[info] Mobala.sh cache not found at '${MOBALA_CACHE}'"
    fi
}

function download-file() {
    origin="${1}"
    target="${2}"
    mkdir -p "${CACHE_DIR}"
    cache_name="$(basename "$target")"
    cache_tmp="${CACHE_DIR}/${cache_name}.tmp"
    rm -rf "${cache_tmp}"
    download_response=$(curl -sLJ0 -H 'Cache-Control: no-cache, no-store' -o "${cache_tmp}" -w "%{response_code}" "${origin}" || true)
    if [[ "${download_response}" == "200" ]]; then
        rm -rf "${target}"
        mv "${cache_tmp}" "${target}"
        echo "[info] cache updated: ${target}"
    else
        echo "[warn] download failed with ${download_response} status code for ${target}"
        rm "${cache_tmp}"
    fi
}


function update-cache() {
    download-file "${MOBALA_FILE}" "${MOBALA_CACHE}"
}

function verify-cache() {
    if ! [[ -f "${MOBALA_CACHE}" ]]; then
        >&2 echo "[error] Mobala.sh cache not found."
        exit 1
    fi
}

function update-self(){
  if [[ "${CI:-false}" == "false" ]] ; then
    set -xe
    download-file "${MOBALA_BASE}/mobala-resolver.sh" "${script_path}"
    chmod +x "${script_path}"
  fi
}

trap 'update-self' EXIT

check-cache
update-cache
verify-cache

bash "${MOBALA_CACHE}" "$@"
