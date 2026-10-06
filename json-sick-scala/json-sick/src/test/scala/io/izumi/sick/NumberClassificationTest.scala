package io.izumi.sick

import io.circe.Json
import izumi.sick.SICK
import izumi.sick.model.RefKind
import org.scalatest.wordspec.AnyWordSpec

class NumberClassificationTest extends AnyWordSpec {
  private val rootName: String = "number"

  private def kindOf(decimal: String): RefKind = {
    val json = Json.fromBigDecimal(BigDecimal(decimal))
    SICK.packJson(json, rootName, dedup = false, dedupPrimitives = false, avoidBigDecimals = false).root.kind
  }

  "Exact decimals" should {
    "be stored as Float exactly when the shortest Float decimal equals them, on every platform" in {
      val expected = List(
        "1.2" -> RefKind.TFlt,
        "0.1" -> RefKind.TFlt,
        "1.5" -> RefKind.TFlt,
        "3.14159" -> RefKind.TFlt,
        "-2.75" -> RefKind.TFlt,
        "1.23456789" -> RefKind.TDbl,
        "0.30000000000000004" -> RefKind.TDbl,
        "1e-50" -> RefKind.TDbl,
      )
      expected.foreach {
        case (decimal, kind) =>
          assert(kindOf(decimal) == kind, s"decimal=$decimal")
      }
    }
  }
}
