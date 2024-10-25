#!/usr/bin/env bash

set -e

self="$(realpath "$0")"
path="$(dirname "$self")"

(for e in "$@"; do [[ "$e" == "nix" ]] && exit 0; done) && NIXIFY=1 || NIXIFY=0

if [[ "$NIXIFY" == 1 && -z "${IN_NIX_SHELL+x}" ]]; then
    echo "Restarting in Nix..."
    set -x
    nix flake lock
    nix flake metadata
    exec nix develop --command bash "$self" "$@"
fi

set -x
cd "$path"

for i in "$@"
do
case $i in
    nix) ;;
    *) "./devops/$i.sh" ;;
esac
done
