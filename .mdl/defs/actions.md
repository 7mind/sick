# SICK Build Actions

# environment
- `LANG=C.UTF-8`

## passthrough
- `HOME`
- `USER`
- `CI_BRANCH`
- `CI_BRANCH_TAG`
- `CI_BUILD_UNIQ_SUFFIX`
- `CI_PULL_REQUEST`
- `TOKEN_NUGET`
- `NODE_AUTH_TOKEN`

# action: clean-worktree

```bash
set -euo pipefail

PROJECT_ROOT="${sys.project-root}"
cd "$PROJECT_ROOT"

git clean -fxd -e private -e .idea -e .mdl -e .mdl/runs

ret cleaned:bool=true
```

# action: test-scala

```bash
dep action.clean-worktree

set -euo pipefail

SCALA_DIR="${sys.project-root}/json-sick-scala"
cd "$SCALA_DIR"

sbt +clean +test:compile +test

ret success:bool=true
```

# action: test-csharp

```bash
dep action.test-scala

set -euo pipefail

CS_DIR="${sys.project-root}/json-sick-csharp"
cd "$CS_DIR"

dotnet build
dotnet test

ret success:bool=true
```

# action: test

```bash
dep action.test-csharp

ret success:bool=true
```

# action: publish-scala

```bash
set -euo pipefail

SCALA_DIR="${sys.project-root}/json-sick-scala"

CI_PULL_REQUEST_VAL="${CI_PULL_REQUEST:-true}"
CI_BRANCH_TAG_VAL="${CI_BRANCH_TAG:-}"

if [[ "$CI_PULL_REQUEST_VAL" != "false" ]]; then
  echo "Skipping Scala publish because this is a pull request."
  ret skipped:bool=true
  exit 0
fi

if [[ -z "$CI_BRANCH_TAG_VAL" ]]; then
  echo "CI_BRANCH_TAG is required to publish Scala artifacts." >&2
  exit 1
fi

cd "$SCALA_DIR"

if [[ "$CI_BRANCH_TAG_VAL" =~ ^v.*$ ]]; then
  sbt +clean +compile +publishSigned sonaUpload sonaRelease
  sbt '++2.13 json-sickJS/fullOptJS'
else
  sbt +clean +compile +publishSigned
fi

ret success:bool=true
```

# action: build-sjs

```bash
set -euo pipefail

SCALA_DIR="${sys.project-root}/json-sick-scala"
DIST_DIR="${SCALA_DIR}/target/dist"

CI_PULL_REQUEST_VAL="${CI_PULL_REQUEST:-true}"
if [[ "$CI_PULL_REQUEST_VAL" != "false" ]]; then
  echo "Skipping Scala.js build because this is a pull request."
  ret skipped:bool=true
  ret dist-dir:directory="$DIST_DIR"
  exit 0
fi

cd "$SCALA_DIR"
sbt '++2.13 json-sickJS/fullOptJS'

ret dist-dir:directory="$DIST_DIR"
```

# action: publish-cs

```bash
set -euo pipefail

CI_PULL_REQUEST_VAL="${CI_PULL_REQUEST:-true}"
TOKEN_NUGET_VAL="${TOKEN_NUGET:-}"
CI_BUILD_UNIQ_SUFFIX_VAL="${CI_BUILD_UNIQ_SUFFIX:-}"
CI_BRANCH_TAG_VAL="${CI_BRANCH_TAG:-}"

if [[ "$CI_PULL_REQUEST_VAL" != "false" ]]; then
  echo "Skipping C# publish because this is a pull request."
  ret skipped:bool=true
  exit 0
fi

if [[ -z "$TOKEN_NUGET_VAL" ]]; then
  echo "TOKEN_NUGET is required to publish C# artifacts; skipping." >&2
  ret skipped:bool=true
  exit 0
fi

if [[ -z "$CI_BUILD_UNIQ_SUFFIX_VAL" ]]; then
  echo "CI_BUILD_UNIQ_SUFFIX is required to publish C# artifacts; skipping." >&2
  ret skipped:bool=true
  exit 0
fi

CS_DIR="${sys.project-root}/json-sick-csharp"
cd "$CS_DIR"

if [[ "$CI_BRANCH_TAG_VAL" =~ ^v.*$ ]]; then
  dotnet build -c Release
else
  dotnet build -c Release --version-suffix "alpha.${CI_BUILD_UNIQ_SUFFIX_VAL}"
fi

find . -name '*.nupkg' -type f -print0 | xargs -I % -n 1 -0 dotnet nuget push % -k "${TOKEN_NUGET_VAL}" --source https://api.nuget.org/v3/index.json

ret success:bool=true
```

# action: publish-npm

```bash
dep action.build-sjs

set -euo pipefail

CI_PULL_REQUEST_VAL="${CI_PULL_REQUEST:-true}"
if [[ "$CI_PULL_REQUEST_VAL" != "false" ]]; then
  echo "Skipping npm publish because this is a pull request."
  ret skipped:bool=true
  exit 0
fi

PROJECT_ROOT="${sys.project-root}"
DIST_DIR="${action.build-sjs.dist-dir}"
PUBLISH_DIR="${PROJECT_ROOT}/npm-publish"
VERSION_FILE="${PROJECT_ROOT}/version.txt"
VERSION=$(tr -d '\n' < "$VERSION_FILE")

if [[ ! -d "$DIST_DIR" ]]; then
  echo "Scala.js distribution directory '$DIST_DIR' is missing." >&2
  exit 1
fi

rm -rf "$PUBLISH_DIR"
mkdir -p "$PUBLISH_DIR"

cp "${DIST_DIR}"/* "$PUBLISH_DIR"/
cp "${PROJECT_ROOT}/json-sick-scala/npm-template/"* "$PUBLISH_DIR"/

sed -i "s/VERSION_PLACEHOLDER/$VERSION/g" "$PUBLISH_DIR/package.json"

cd "$PUBLISH_DIR"

npm install --save-dev ava
npm test
npm publish --provenance --access public

ret success:bool=true
ret publish-dir:directory="$PUBLISH_DIR"
```
