set -e
set -x

COMMAND="sbt +clean +test"

cd json-sick-scala

if [[ "$GITHUB_REF" == refs/heads/main || "$CI_BRANCH_TAG" =~ ^v.*$ ]] ; then
mkdir .secrets
echo "$SONATYPE_CREDENTIALS_FILE" > ".secrets/credentials.sonatype-nexus.properties"
openssl aes-256-cbc -K ${OPENSSL_KEY} -iv ${OPENSSL_IV} -in ../secrets.tar.enc -out secrets.tar -d
tar xvf secrets.tar
COMMAND="$COMMAND +publishSigned"
if [[ "$CI_BRANCH_TAG" =~ ^v.*$ ]] ; then
    COMMAND="$COMMAND sonatypeBundleRelease"
fi
fi

echo $COMMAND
eval $COMMAND
