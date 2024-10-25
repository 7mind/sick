#!/usr/bin/env bash

set -e

(for e in "$@"; do [[ "$e" == "nix" ]] && exit 0; done) && NIXIFY=1 || NIXIFY=0

if [[ "$NIXIFY" == 1 && -z "${IN_NIX_SHELL+x}" ]]; then
    echo "Restarting in Nix..."
    self=$(realpath "$0")
    set -x
    nix flake lock
    nix flake metadata
    exec nix develop --command bash "$self" "$@"
fi

set -x
cd "$(dirname "$(readlink -f "$0")")"

for i in "$@"
do
case $i in
    nix) ;;

    publish-cs) ./devops/publish-cs.sh ;;
    publish-scala) ./devops/publish-scala.sh ;;

    test) ./devops/test.sh ;;

    *)
        echo "Unknown option: $i"
        exit 1
    ;;
esac
done
