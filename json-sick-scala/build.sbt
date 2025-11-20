import com.github.sbt.git.SbtGit.GitKeys

val circeVersion = "0.14.13"
val scalatestVersion = "3.2.19"
val zstdVersion = "1.5.7-4"
val nodeTypesVersion = "18.11.9"
// Can't convert latest node due to error: `not found: type TReturn`
// val nodeTypesVersion = "24.3.1"

val scalaVersions = Seq("3.3.6", "2.13.16")

val `json-sick` = (projectMatrix in file("json-sick"))
  .jvmPlatform(scalaVersions, Seq(libraryDependencies += "com.github.luben" % "zstd-jni" % zstdVersion % Test))
  .settings(
    name := "json-sick",
    libraryDependencies ++= Seq(
      "io.circe" %% "circe-core" % circeVersion,
      "io.circe" %% "circe-jawn" % circeVersion,
    ),
    libraryDependencies += "org.scalatest" %% "scalatest" % scalatestVersion % Test,
    scalacOptions ++= {
      val s = scalaVersion.value
      if (s.startsWith("2")) {
        Seq(
          "-Xsource:3-cross",
          "-release:8",
          "-explaintypes",
          "-Wconf:cat=optimizer:warning",
          "-Wconf:cat=other-match-analysis:error",
          "-Vimplicits",
          "-Vtype-diffs",
          "-Wdead-code",
          "-Wextra-implicit",
          "-Wnumeric-widen",
          "-Woctal-literal",
          "-Wvalue-discard",
          "-Wunused:_",
          "-Wunused:-synthetics",
          // inlining
          "-opt:l:inline",
          "-opt-inline-from:izumi.sick.**",
        )
      } else {
        Seq(
          "-language:3.3",
          "-no-indent",
          "-explain",
          "-explain-types",
        )
      }
    },
  )
//  .jsPlatform(
//    scalaVersions,
//    Seq.empty,
//    _.settings(
//      Seq(
//        // sourced from https://github.com/ScalablyTyped/Demos/blob/558213f6e21e6afbc6f015e06d053038f3a4e66f/build.sbt#L325
////        jsEnv := new org.scalajs.jsenv.nodejs.NodeJSEnv,
////    stStdlib := List("esnext"),
////    stUseScalaJsDom := false,
////    Test / npmDependencies ++= Seq(
////      // Using manual bindings to avoid CI errors caused by ScalablyTyped. Uncomment "@types/node" and sbt-converter and ScalablyTypedConverterPlugin and its options to use generated bindings.
////      "@types/node" -> nodeTypesVersion
////    ),
//        libraryDependencies += "io.circe" %% "circe-scalajs" % circeVersion
////        Compile / fullOptJS / artifactPath := {
////          def outerRef = ProjectRef(file("."), "json-sick-scala")
////          (outerRef / target).value / "dist" / s"${moduleName.value}-${scalaBinaryVersion.value}-fullOpt.js"
////        },
////        scalaJSLinkerConfig := {
////          scalaJSLinkerConfig.value
////            .withBatchMode(true)
////            .withModuleKind(ModuleKind.CommonJSModule)
////            .withJSHeader("""/**
//// * @function decodeSickUint8Array
//// * Accepts an instance of `Uint8Array`, returns a dictionary where keys are root names and values are JSON
//// *
//// * @example
//// * decodeSickUint8Array(uint8Array) => { data: { a: 2, b: { c: 3 }, ...etc } }
//// *
//// * // export function decodeSickUint8Array(uint8Array: Uint8Array): {[key: string]: any};
//// *
//// * @function encodeObjToSickUint8Array
//// * Accepts a rootName and a JS object (all values should be valid JSON), returns a SICK-encoded binary Uint8Array
//// *
//// * @example
//// * encodeObjToSickUint8Array("data", { a: 2, b: { c: 3 }, ...etc }) => Uint8Array
//// *
//// * // export function encodeObjToSickUint8Array(rootName: string, obj: any): Uint8Array;
//// *
//// * @function encodeObjsToSickUint8Array
//// * Accepts dictionary where keys are root names and values are JS objects (all values should be valid JSON), returns a SICK-encoded binary Uint8Array
//// *
//// * @example
//// * encodeObjsToSickUint8Array({ data: { a: 2 }, data1: { b: 3 }, ...etc }) => Uint8Array
//// *
//// * // export function encodeObjsToSickUint8Array(objs: {[key: string]: any}): Uint8Array;
//// *
//// * @function encodeJSONStringsToSickUint8Array
//// * Accepts dictionary where keys are root names and values are strings that parse into a valid JSON object (e.g. results of JSON.stringify), returns a SICK-encoded binary Uint8Array
//// *
//// * @example
//// * encodeJSONStringsToSickUint8Array({ data: '{ "a": 1 }'}) => Uint8Array
//// *
//// * // export function encodeJSONStringsToSickUint8Array(objs: {[key: string]: string}): Uint8Array;
//// *
//// * @function encodeJSONBytesToSickUint8Array
//// *
//// * Accepts dictionary where keys are root names and values are Uint8Arrays containing valid UTF-8 text that parse into JSON, returns a SICK-encoded binary Uint8Array
//// *
//// * @example
//// * encodeJSONBytesToSickUint8Array({ data: new Uint8Array(file.buffer)}) => Uint8Array
//// *
//// * // export function encodeJSONBytesToSickUint8Array(objs: {[key: string]: Uint8Array}): Uint8Array;
//// */
////""".stripMargin)
////        },
//      )
//    ),
////      .enablePlugins(ScalaJSBundlerPlugin),
//  )

//lazy val `json-sickJVM` = `json-sick`.jvm
//lazy val `json-sickJS` = `json-sick`.js

ThisBuild / scalacOptions ++= Seq(
  "-encoding",
  "UTF-8",
  "-feature",
  "-unchecked",
  "-deprecation",
  "-language:higherKinds",
)

ThisBuild / organization := "io.7mind.izumi"

ThisBuild / version := {
  Option(System.getProperty("FORCE_BUILD_VERSION")).filter(_.nonEmpty) match {
    case None =>
      val versionBase = IO.read(file("../version.txt")).trim()
      val gitCleanTagged =
        GitKeys.gitCurrentTags.value.nonEmpty && !GitKeys.gitUncommittedChanges.value
      val stableOverride =
        Option(System.getProperty("FORCE_BUILD_STABLE")).contains("1")
      val isStable = gitCleanTagged || stableOverride
      if (isStable) {
        versionBase
      } else {
        s"$versionBase-SNAPSHOT"
      }

    case Some(customVersion) =>
      customVersion
  }
}

// https://github.com/sbt/sbt/issues/8131
ThisBuild / publishTo := {
  if (isSnapshot.value) {
    Some(
      "central-snapshots" at "https://central.sonatype.com/repository/maven-snapshots/"
    )
  } else {
    localStaging.value
  }
}

ThisBuild / credentials ++= Seq(
  Path.userHome / ".sbt" / "secrets" / "credentials.sonatype-new.properties",
  Path.userHome / ".sbt" / "secrets" / "credentials.sonatype-nexus.properties",
  file(".") / ".secrets" / "credentials.sonatype-nexus.properties",
)
  .filter(_.exists())
  .map(Credentials.apply)

ThisBuild / homepage := Some(url("https://github.com/7mind/sick"))
ThisBuild / licenses := Seq(
  "BSD-style" -> url("http://www.opensource.org/licenses/bsd-license.php")
)
ThisBuild / developers := List(
  Developer(
    id = "7mind",
    name = "Septimal Mind",
    url = url("https://github.com/7mind"),
    email = "team@7mind.io",
  )
)
ThisBuild / scmInfo := Some(
  ScmInfo(
    url("https://github.com/7mind/sick"),
    "scm:git:https://github.com/7mind/sick.git",
  )
)

val `json-sick-scala` = project
  .in(file("."))
  .settings(
    publish / skip := true
  )
  .autoAggregate
