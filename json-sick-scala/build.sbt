import com.github.sbt.git.SbtGit.GitKeys
import sbtcrossproject.CrossPlugin.autoImport.{CrossType, crossProject}

val circeVersion = "0.14.13"
val scalatestVersion = "3.2.19"
val zstdVersion = "1.5.7-4"
val nodeTypesVersion = "18.11.9"
// Can't convert latest node due to error: `not found: type TReturn`
// val nodeTypesVersion = "24.3.1"

lazy val `json-sick` = crossProject(JVMPlatform, JSPlatform)
  .crossType(CrossType.Pure)
  .in(file("json-sick"))
  .settings(
    name := "json-sick",
    crossScalaVersions := Seq(
      "3.3.6",
      "2.13.16",
    ),
    scalaVersion := crossScalaVersions.value.head,
    libraryDependencies ++= Seq(
      "io.circe" %%% "circe-core" % circeVersion,
      "io.circe" %%% "circe-jawn" % circeVersion,
    ),
    libraryDependencies += "org.scalatest" %%% "scalatest" % scalatestVersion % Test,
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
  .jvmSettings(
    libraryDependencies += "com.github.luben" % "zstd-jni" % zstdVersion % Test
  )
  .jsSettings(
    // sourced from https://github.com/ScalablyTyped/Demos/blob/558213f6e21e6afbc6f015e06d053038f3a4e66f/build.sbt#L325
    jsEnv := new org.scalajs.jsenv.nodejs.NodeJSEnv,
    stStdlib := List("esnext"),
    stUseScalaJsDom := false,
    scalaJSLinkerConfig := { scalaJSLinkerConfig.value.withBatchMode(true).withModuleKind(ModuleKind.CommonJSModule) },
    Test / npmDependencies ++= Seq(
      "@types/node" -> nodeTypesVersion
    ),
  )
  .jsConfigure(
    project =>
      project
        .enablePlugins(ScalaJSBundlerPlugin)
        .enablePlugins(ScalablyTypedConverterPlugin)
  )

lazy val `json-sickJVM` = `json-sick`.jvm
lazy val `json-sickJS` = `json-sick`.js

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

lazy val `json-sick-scala` = project
  .in(file("."))
  .settings(
    publish / skip := true,
    crossScalaVersions := Nil,
  )
  .aggregate(
    `json-sickJVM`,
    `json-sickJS`,
  )
