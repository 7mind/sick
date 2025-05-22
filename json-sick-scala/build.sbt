import com.github.sbt.git.SbtGit.GitKeys

val circeVersion = "0.14.13"

lazy val root = (project in file("."))
  .settings(
    name := "json-sick",
    libraryDependencies ++= Seq(
      "io.circe" %% "circe-core" % circeVersion,
      "io.circe" %% "circe-parser" % circeVersion
    ),
    libraryDependencies += "com.github.luben" % "zstd-jni" % "1.5.7-3" % Test,
    libraryDependencies += "org.scalatest" %% "scalatest" % "3.2.19" % Test
  )

ThisBuild / scalacOptions ++= Seq(
  "-encoding",
  "UTF-8",
  "-feature",
  "-unchecked",
  "-deprecation",
  "-language:higherKinds"
)
ThisBuild / scalacOptions ++= {
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
      "-opt-inline-from:izumi.sick.**"
    )
  } else {
    Seq(
      "-language:3.3",
      "-no-indent",
      "-explain",
      "-explain-types"
    )
  }
}

ThisBuild / crossScalaVersions := Seq(
  "3.3.6",
  "2.13.16"
)

ThisBuild / scalaVersion := (ThisBuild / crossScalaVersions).value.head

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

publishTo := (ThisBuild / publishTo).value

ThisBuild / credentials ++= Seq(
  Path.userHome / ".sbt" / "secrets" / "credentials.sonatype-new.properties",
  Path.userHome / ".sbt" / "secrets" / "credentials.sonatype-nexus.properties",
  file(".") / ".secrets" / "credentials.sonatype-nexus.properties"
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
    email = "team@7mind.io"
  )
)
ThisBuild / scmInfo := Some(
  ScmInfo(
    url("https://github.com/7mind/sick"),
    "scm:git:https://github.com/7mind/sick.git"
  )
)
