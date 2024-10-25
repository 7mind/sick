set -e
set -x

cd json-sick-csharp

# dotnet clean -c Release
if [[ "$CI_BRANCH_TAG" =~ ^v.*$ ]] ; then
dotnet build -c Release
else
dotnet build -c Release --version-suffix "alpha.${CI_BUILD_UNIQ_SUFFIX}"
fi

for TRG in $(find . -name '*.nupkg' -type f -print)
do
dotnet nuget push $TRG -k ${TOKEN_NUGET} --source https://api.nuget.org/v3/index.json || exit 1
done
