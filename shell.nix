{ pkgs ? import <nixpkgs> { } }:

pkgs.mkShell {
  nativeBuildInputs = with pkgs.buildPackages; [
    ncurses
    graalvm-ce
    sbt
    dotnet-sdk_6
    git
  ];
}
