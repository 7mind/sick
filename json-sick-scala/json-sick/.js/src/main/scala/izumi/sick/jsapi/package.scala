package izumi.sick

import scala.scalajs.js.typedarray.{Int8Array, Uint8Array, *}

package object jsapi {

  def bytesToUint8Array(bytes: Array[Byte]): Uint8Array = {
    val int8Array = bytes.toTypedArray
    new Uint8Array(int8Array.buffer, int8Array.byteOffset, int8Array.length)
  }

  def uint8ArrayToBytes(uint8Array: Uint8Array): Array[Byte] = {
    new Int8Array(uint8Array.buffer, uint8Array.byteOffset, uint8Array.length).toArray
  }

}
