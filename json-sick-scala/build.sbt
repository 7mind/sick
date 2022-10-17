ThisBuild / version := "0.1.0-SNAPSHOT"

ThisBuild / scalaVersion := "2.13.8"

lazy val root = (project in file("."))
  .settings(
    name := "json-sick"
  )

val circeVersion = "0.14.1"

libraryDependencies ++= Seq(
  "io.circe" %% "circe-core",
  "io.circe" %% "circe-generic",
//  "io.circe" %% "circe-derivation-",
  "io.circe" %% "circe-generic-extras",
  "io.circe" %% "circe-parser",
).map(_ % circeVersion)

libraryDependencies += "com.github.luben" % "zstd-jni" % "1.5.2-3"

scalacOptions ++= Seq(
  "-Xsource:3",
  "-deprecation",
  "-language:higherKinds",
)