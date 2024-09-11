package izumi.sick.tools

import java.nio.charset.StandardCharsets

// Crappy but fast hash
object CBFHash {
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
