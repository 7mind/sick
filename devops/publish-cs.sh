set -e
set -x

cd json-sick-csharp

[[ "$CI_PULL_REQUEST" != "false"  ]] && exit 0
[[ -z "$TOKEN_NUGET" ]] && exit 0
[[ -z "$CI_BUILD_UNIQ_SUFFIX" ]] && exit 0

if [[ "$CI_BRANCH_TAG" =~ ^v.*$ ]] ; then
    dotnet build -c Release
else
    dotnet build -c Release --version-suffix "alpha.${CI_BUILD_UNIQ_SUFFIX}"
fi

find . -name '*.nupkg' -type f -exec dotnet nuget push {} -k "${TOKEN_NUGET}" --source https://api.nuget.org/v3/index.json \;