package izumi.sick

import io.circe.Json

class Index() {
  val strings = new Bijection[String]("Strings")

  val bytes = new Bijection[Byte]("Bytes")
  val shorts = new Bijection[Short]("Shorts")
  val ints = new Bijection[Int]("Integers")
  val longs = new Bijection[Long]("Longs")
  val bigints = new Bijection[BigInt]("Bigints")

  val floats = new Bijection[Float]("Floats")
  val doubles = new Bijection[Double]("Doubles")
  val bigDecimals = new Bijection[BigDecimal]("BigDecs")

  val arrs = new Bijection[Arr]("Arrays")
  val objs = new Bijection[Obj]("Objects")
  val roots = new Bijection[Root]("Roots")

  def freeze() = {
    new ROIndex(
      bytes.freeze(),
      shorts.freeze(),
      ints.freeze(),
      longs.freeze(),
      bigints.freeze(),

      floats.freeze(),
      doubles.freeze(),
      bigDecimals.freeze(),

      strings.freeze(),
      arrs.freeze(),
      objs.freeze(),
      roots.freeze(),
    )
  }

  override def toString: String = {
    Seq(strings, bytes, shorts, ints, longs, bigints, floats, doubles, bigDecimals, arrs, objs, roots).filterNot(_.isEmpty).mkString("\n\n")
  }

  def reconstruct(ref: Ref): Json = {
    ref.kind match {
      case RefKind.TNul =>
        Json.Null
      case RefKind.TBit =>
        Json.fromBoolean(ref.ref == 1)

      case RefKind.TByte =>
        Json.fromInt(bytes(ref.ref))
      case RefKind.TShort =>
        Json.fromInt(shorts(ref.ref))
      case RefKind.TInt =>
        Json.fromInt(ints(ref.ref))
      case RefKind.TLng =>
        Json.fromLong(longs(ref.ref))
      case RefKind.TBigInt =>
        Json.fromBigInt(bigints(ref.ref))

      case RefKind.TFlt =>
        Json.fromFloat(floats(ref.ref)).get
      case RefKind.TDbl =>
        Json.fromDouble(doubles(ref.ref)).get
      case RefKind.TBigDec =>
        Json.fromBigDecimal(bigDecimals(ref.ref))

      case RefKind.TStr =>
        Json.fromString(strings(ref.ref))

      case RefKind.TArr =>
        val a = arrs(ref.ref)
        Json.fromValues(a.values.map(reconstruct) )
      case RefKind.TObj =>
        val o = objs(ref.ref)
        Json.fromFields(o.values.map {
          case (k, v) =>
            (strings(k), reconstruct(v))
        } )
    }
  }

  def traverse(id: String, j: Json): Ref = {
    val idRef = addString(id)
    val root = traverse(j)
    addRoot(Root(idRef.ref, root))
    root
  }
  def traverse(j: Json): Ref = {

    j.fold(
      Ref(RefKind.TNul, 0),
      b => Ref(RefKind.TBit, if (b) {1} else {0}),
      n => {
        n.toBigDecimal match {
          case Some(value) if value.isWhole && value.isValidInt  =>
            val intValue = value.toIntExact
            if (intValue <= java.lang.Byte.MAX_VALUE) {
              addByte(intValue.byteValue())
            }
            if (intValue <= java.lang.Short.MAX_VALUE) {
              addShort(intValue.shortValue())
            } else {
              addInt(intValue)
            }
          case Some(value) if value.isWhole && value.isValidLong  =>
            addLong(value.toLongExact)
          case Some(value) if value.isWhole  =>
            addBigInt(value.toBigIntExact.getOrElse(throw new IllegalStateException(s"Cannot decode BigInt $n")))
          case Some(value) if value.isDecimalFloat  =>
            addFloat(value.floatValue)
          case Some(value) if value.isDecimalDouble  =>
            addDouble(value.doubleValue)
          case Some(value)  =>
            addBigDec(value)
          case None =>
            throw new IllegalStateException(s"Cannot decode number $n")
        }
      },
      s =>  addString(s),
      arr => addArr(Arr(
        arr.map(traverse)
      )),
      obj => addObj(Obj(
        obj.toMap.toVector.map {
          case (k, v) =>
            (addString(k).ref, traverse(v))
        }
      ))
    )
  }

  def addString(s: String): Ref = Ref(RefKind.TStr, strings.add(s))

  def addByte(s: Byte): Ref = {
    Ref(RefKind.TByte, bytes.add(s))
  }
  def addShort(s: Short): Ref = Ref(RefKind.TShort, shorts.add(s))
  def addInt(s: Int): Ref = Ref(RefKind.TInt, ints.add(s))
  def addLong(s: Long): Ref = Ref(RefKind.TLng, longs.add(s))
  def addBigInt(s: BigInt): Ref = Ref(RefKind.TBigInt, bigints.add(s))

  def addFloat(s: Float): Ref = Ref(RefKind.TFlt, floats.add(s))
  def addDouble(s: Double): Ref = Ref(RefKind.TDbl, doubles.add(s))
  def addBigDec(s: BigDecimal): Ref = Ref(RefKind.TBigDec, bigDecimals.add(s))

  def addArr(s: Arr): Ref = Ref(RefKind.TArr, arrs.add(s))

  def addObj(s: Obj): Ref = Ref(RefKind.TObj, objs.add(s))

  def addRoot(s: Root): Ref = {
    roots.revGet(s) match {
      case Some(value) =>
        throw new IllegalStateException(s"Root $s already exists with ref $value")
      case None =>
        Ref(RefKind.TRoot, roots.add(s))
    }
  }
}
