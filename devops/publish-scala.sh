set -e
set -x

cd json-sick-scala

[[ "$CI_PULL_REQUEST" != "false"  ]] && exit 0

if [[ "$CI_BRANCH_TAG" =~ ^v.*$ ]] ; then
    sbt +clean +test +publishSigned sonatypeBundleRelease
else
    sbt +clean +test +publishSigned
fi
