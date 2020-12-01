using System;

namespace Nixill.Testing {
  public class FakeRandom : Random {
    private double[] Doubles = new double[] { 0.8325423429, 0.8119269431, 0.5059075930, 0.5548030888, 0.8232397426, 0.0130130086, 0.9365067873, 0.0677968875, 0.3737967908, 0.7849446639, 0.0474246438, 0.5655960556, 0.1216897105, 0.2086320496, 0.0094973519, 0.0736148416 };
    private byte[] Bytes = new byte[] { 0, 121, 107, 255, 100, 180, 135, 9, 3, 33, 27, 115, 115, 33, 44, 35 };
    private int[] Ints = new int[] {
      0x75f2851d, 0x03e66c7a, 0x299648be, 0x68d968fb,
      0x6eef2be8, 0x1daa3f1e, 0x4352542e, 0x2c9053e3,
      0x4b15d31a, 0x4810d481, 0x32371bdc, 0x47f09c47,
      0x55421d7f, 0x25a40df6, 0x000d739a, 0x2aba9c57
    };

    private int Index = 0;

    public override int Next() {
      return Ints[Index++ % 16];
    }

    public override int Next(int minValue, int maxValue) {
      return base.Next(minValue, maxValue);
    }

    public override void NextBytes(byte[] buffer) {
      for (int i = 0; i < buffer.Length; i++) {
        buffer[i] = Bytes[Index++ % 16];
      }
    }

    protected override double Sample() {
      return Doubles[Index++ % 16];
    }
  }
}