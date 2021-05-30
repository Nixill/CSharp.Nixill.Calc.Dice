using Nixill.CalcLib.Objects;

namespace Nixill.DiceLib {
  public class DiceDie : CalcNumber {
    private const string DISPLAY_FORMAT = "0.###;-0.###";
    private const string CODE_FORMAT = "0.###############;(-0.###############)";

    public CalcValue Sides { get; }

    public DiceDie(decimal value, decimal max) : base(value) {
      Sides = new CalcNumber(max);
    }

    public DiceDie(decimal value, CalcList sides) : base(value) {
      Sides = sides;
    }

    public override string ToCode() {
      return "{!die," + Value.ToString(CODE_FORMAT) + "," + Sides.ToCode() + "}";
    }

    public override string ToTree(int level) {
      return new string(' ', level * 2) + "Die:\n" +
        new string(' ', level * 2) + "  Value: " + Value.ToString(DISPLAY_FORMAT) + "\n" +
        new string(' ', level * 2) + "  Sides:\n" +
        Sides.ToTree(level + 1);
    }
  }

  public class DiceCoin : CalcNumber {
    private const string DISPLAY_FORMAT = "0.###;-0.###";
    private const string CODE_FORMAT = "0.###############;(-0.###############)";

    public decimal PotentialValue { get; }
    public bool Heads { get; }

    public DiceCoin(bool heads, decimal value) : base(heads ? value : 0) {
      Heads = heads;
      PotentialValue = value;
    }

    public override string ToString(int level) {
      return Heads ? "H" : "T";
    }

    public override string ToCode() {
      return "{!coin," + (Heads ? "1" : "0") + "," + PotentialValue.ToString(CODE_FORMAT) + "}";
    }

    public override string ToTree(int level) {
      return new string(' ', level * 2) + "Die:\n" +
        new string(' ', level * 2) + "  Heads: " + Heads + "\n" +
        new string(' ', level * 2) + "  Potential: " + PotentialValue.ToString(DISPLAY_FORMAT);
    }
  }

}