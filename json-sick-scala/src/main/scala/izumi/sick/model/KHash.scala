package izumi.sick.model

import java.nio.charset.StandardCharsets

object KHash {
  def compute(s: String): Long = {
    var a: Int = 0x6BADBEEF
    s.getBytes(StandardCharsets.UTF_8).foreach {
      b =>
        a ^= a << 13
        a += (a ^ b) << 8
    }
    Integer.toUnsignedLong(a)
  }
}
