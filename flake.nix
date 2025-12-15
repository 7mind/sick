{
  description = "SICK build environment";

  inputs.nixpkgs.url = "github:NixOS/nixpkgs/25.11";

  inputs.flake-utils.url = "github:numtide/flake-utils";

  inputs.mudyla.url = "github:7mind/mudyla";
  inputs.mudyla.inputs.nixpkgs.follows = "nixpkgs";

  outputs = { self, nixpkgs, flake-utils, mudyla }:
    flake-utils.lib.eachDefaultSystem
      (system:
        let
          pkgs = import nixpkgs {
            inherit system;
            config.allowUnfree = true;
          };
          sharedPkgs = with pkgs.buildPackages; [
            ncurses
            graalvmPackages.graalvm-ce
            sbt
            dotnet-sdk_9
            nodejs_24

            gitMinimal

            openssl

            scala-cli
          ] ++ [
            mudyla.packages.${system}.default
          ];
        in
        {
          devShells.default = pkgs.mkShell { nativeBuildInputs = sharedPkgs; };

          # use nix develop .#ide to enable JetBrains Rider from nixpkgs
          devShells.ide = pkgs.mkShell {
            nativeBuildInputs = sharedPkgs ++ [
              pkgs.buildPackages.jetbrains.rider
            ];
            # LD_LIBRARY_PATH = pkgs.lib.makeLibraryPath [ pkgs.stdenv.cc.cc.lib ];
          };
        }
      );
}
