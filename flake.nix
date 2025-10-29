{
  description = "SICK build environment";

  inputs.nixpkgs.url = "github:NixOS/nixpkgs/release-24.11";

  inputs.flake-utils.url = "github:numtide/flake-utils";

  outputs = { self, nixpkgs, flake-utils }:
    flake-utils.lib.eachDefaultSystem
      (system:
        let
          pkgs = import nixpkgs {
            inherit system;
            config.allowUnfree = true;
          };
          sharedPkgs = with pkgs.buildPackages; [
            ncurses
            graalvm-ce
            sbt
            dotnet-sdk_9
            nodejs

            git

            openssl

            scala-cli
          ];
        in
        {
          devShells.default = pkgs.mkShell { nativeBuildInputs = sharedPkgs; };

          # use nix develop .#ide to enable JetBrains Rider from nixpkgs
          devShells.ide = pkgs.mkShell {
            nativeBuildInputs = with pkgs.buildPackages; sharedPkgs ++ [
              jetbrains.rider
            ];
            # LD_LIBRARY_PATH = pkgs.lib.makeLibraryPath [ pkgs.stdenv.cc.cc.lib ];
          };
        }
      );
}
