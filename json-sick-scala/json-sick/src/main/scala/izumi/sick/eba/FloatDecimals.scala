package izumi.sick.eba

import java.math.{MathContext, RoundingMode}

object FloatDecimals {
  private final val maxFloatSignificantDigits: Int = 9

  def shortest(float: Float): java.math.BigDecimal = {
    require(!float.isInfinite && !float.isNaN, s"Cannot represent $float as a decimal")
    val exact = new java.math.BigDecimal(float.toDouble)
    (1 to maxFloatSignificantDigits).iterator
      .map(digits => exact.round(new MathContext(digits, RoundingMode.HALF_EVEN)))
      .find(_.floatValue == float)
      .getOrElse(exact)
  }

  def isShortestFloatDecimal(value: BigDecimal): Boolean = {
    val float = value.floatValue
    !float.isInfinite && !float.isNaN && shortest(float).compareTo(value.bigDecimal) == 0
  }
}
