package izumi.sick.tools

import java.nio.charset.StandardCharsets

// Crappy but fast hash
object CBFHash {
  @noinline def compute(s: String): Long = {
    var a: Int = 0x6BADBEEF
    val bs = s.getBytes(StandardCharsets.UTF_8)
    val sz = bs.length
    var i = 0
    while (i < sz) {
      a ^= a << 13
      a += (a ^ bs(i)) << 8

      i += 1
    }
    Integer.toUnsignedLong(a)
  }
}
