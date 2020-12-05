using System;
using Nixill.CalcLib.Modules;
using Nixill.CalcLib.Objects;
using Nixill.CalcLib.Parsing;
using Nixill.CalcLib.Varaibles;
using Nixill.DiceLib;
using NUnit.Framework;

namespace Nixill.Testing {
  public class DiceTests {
    [Test]
    public void TestDice() {
      // We're going to use a fake randomizer so we know how random numbers go.
      Random rand = new Random(237);
      CLContextProvider randContext = new CLContextProvider();
      randContext.Add(rand);

      CLLocalStore vars = new CLLocalStore();

      // ... so let's actually remember to load the freakin modules
      MainModule.Load();
      DiceModule.Load();

      // First off, let's test dice rolling!
      string test1 = TestLine("d16", vars, randContext, "9");
      string test2 = TestLine("2d16", vars, randContext, "[6,7]");
      string test3 = TestLine("1d[1,2,3,4]", vars, randContext, "[1]");

      string test4 = TestLine("10u=5", vars, randContext, "[3,7,1,4,3,7,9]");
    }

    public string TestLine(string line, CLLocalStore vars, CLContextProvider context, string expected) {
      // We'll parse the line as usual
      CalcObject obj1 = CLInterpreter.Interpret(line);
      string code2 = obj1.ToCode();
      CalcObject obj3 = CLInterpreter.Interpret(code2);
      string code4 = obj3.ToCode();

      // Make sure things look right
      Assert.AreEqual(code2, code4);

      // Now get the value out of it
      CalcValue val5 = obj3.GetValue(vars, context);
      string code6 = val5.ToCode();
      CalcObject obj7 = CLInterpreter.Interpret(code6);
      string code8 = val5.ToCode();
      CalcValue val9 = obj7.GetValue(vars, context);

      // Make sure things still look right
      Assert.AreEqual(code6, code8);
      Assert.AreEqual(val5, val9);

      // Now we should also make sure the values reported are as expected.
      Assert.AreEqual(code8, expected);

      // And we'll return the final value so it can be used later!
      return code8;
    }
  }
}