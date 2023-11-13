import com.github.sbt.git.SbtGit.GitKeys

val circeVersion = "0.14.1"

lazy val root = (project in file("."))
  .settings(
    name := "json-sick",
    libraryDependencies ++= Seq(
      "io.circe" %% "circe-core",
      "io.circe" %% "circe-parser"
    ).map(_ % circeVersion),
    libraryDependencies  += "com.github.luben" % "zstd-jni" % "1.5.2-3" % Test,
    sonatypeProfileName := "io.7mind"
  )

ThisBuild / scalacOptions ++= Seq(
  "-Xsource:3",
  "-deprecation",
  "-language:higherKinds"
)

ThisBuild / organization := "io.7mind.izumi"

ThisBuild / sonatypeSessionName := s"[sbt-sonatype] ${name.value} ${version.value} ${java.util.UUID.randomUUID}"

ThisBuild / version := {
  val versionBase = IO.read(file("../version.txt")).trim()
  val isStable =
    GitKeys.gitCurrentTags.value.nonEmpty && !GitKeys.gitUncommittedChanges.value
  if (isStable) {
    versionBase
  } else {
    s"$versionBase-SNAPSHOT"
  }
}

ThisBuild / scalaVersion := "2.13.11"

ThisBuild /crossScalaVersions := Seq(
  "3.2.2",
  "2.13.11",
)

ThisBuild / publishTo :=
  (if (!isSnapshot.value) {
     sonatypePublishToBundle.value
   } else {
     Some(Opts.resolver.sonatypeSnapshots)
   })

publishTo := (ThisBuild / publishTo).value

ThisBuild / credentials ++= {
  val credTarget =
    Path.userHome / ".sbt" / "secrets" / "credentials.sonatype-nexus.properties"
  if (credTarget.exists) {
    Seq(Credentials(credTarget))
  } else {
    Seq.empty
  }
}

ThisBuild / credentials ++= {
  val credTarget =
    file(".") / ".secrets" / "credentials.sonatype-nexus.properties"
  if (credTarget.exists) {
    Seq(Credentials(credTarget))
  } else {
    Seq.empty
  }
}

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

