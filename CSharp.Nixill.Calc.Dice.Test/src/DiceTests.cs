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
      CLContextProvider context = new CLContextProvider();
      context.Add(rand);

      CLLocalStore vars = new CLLocalStore();

      // ... so let's actually remember to load the freakin modules
      MainModule.Load();
      DiceModule.Load();

      // First off, let's test dice rolling!
      string test1 = TestLine("d16", vars, context, "{!die,9,16}"); // generated: 1
      string test2 = TestLine("2d16", vars, context, "[{!die,6,16},{!die,7,16}]"); // generated: 3
      string test3 = TestLine("1d[1,2,3,4]", vars, context, "[{!die,1,[1,2,3,4]}]"); // generated: 4

      string test4 = TestLine("10u=5", vars, context, "[{!die,3,10},{!die,7,10},{!die,1,10},{!die,4,10},{!die,3,10},{!die,7,10},{!die,9,10}]"); // generated: 12

      // Let's set a limit for dice rolls now.
      DiceContext diceContext = new DiceContext();
      diceContext.PerFunctionLimit = 10;
      diceContext.PerRollLimit = 4;

      context.Add(diceContext);

      // And try to exceed it.
      string test5 = TestLine("10u=5+{_u}", vars, context, "[{!die,6,10},{!die,8,10},{!die,9,10},{!die,1,10},0]"); // generated: 16
      string test6 = TestLine("10u!%5+{_u}", vars, context, "[{!die,5,10},{!die,10,10},{!die,1,10}]"); // generated: 19

      // Now we need to start testing the keeper operators!
      string test7 = TestLine(test4 + "kh4", vars, context, "[{!die,7,10},{!die,4,10},{!die,7,10},{!die,9,10}]");
      string test8 = TestLine("{_d}", vars, context, "[{!die,3,10},{!die,1,10},{!die,3,10}]");

      // Test keeps by comparison
      string test9 = TestLine(test4 + "k%3", vars, context, "[{!die,3,10},{!die,3,10},{!die,9,10}]");
      string test10 = TestLine("{_d}", vars, context, "[{!die,7,10},{!die,1,10},{!die,4,10},{!die,7,10}]");

      // And the repeat operator!
      diceContext.PerFunctionUsed = 0;
      Assert.Throws<LimitedDiceException>(() => CLInterpreter.Interpret("4d6kh3**6").GetValue(vars, context)); // generated: 29
      diceContext.PerFunctionUsed = 0;
      diceContext.PerFunctionLimit = 24;
      string test11 = TestLine("4d6kh3**6", vars, context, "[[{!die,6,6},{!die,5,6},{!die,6,6}],[{!die,6,6},{!die,3,6},{!die,3,6}],[{!die,6,6},{!die,6,6},{!die,4,6}],[{!die,4,6},{!die,5,6},{!die,6,6}],[{!die,4,6},{!die,6,6},{!die,2,6}],[{!die,4,6},{!die,6,6},{!die,6,6}]]");
      // generated: 53

      diceContext.PerFunctionUsed = 0;
      diceContext.PerRollLimit = 10;
      string test12 = TestLine("6d10", vars, context, "[{!die,8,10},{!die,7,10},{!die,6,10},{!die,4,10},{!die,7,10},{!die,3,10}]");
      // generated: 59
      diceContext.PerFunctionUsed = 0;
      string test13 = TestLine(test12 + "r%2", vars, context, "[{!die,3,10},{!die,7,10},{!die,10,10},{!die,6,10},{!die,7,10},{!die,3,10}]");
      // generated: 62
      diceContext.PerFunctionUsed = 0;
      string test14 = TestLine(test13 + "x%3", vars, context, "[{!die,3,10},{!die,4,10},{!die,7,10},{!die,10,10},{!die,6,10},{!die,4,10},{!die,7,10},{!die,3,10},{!die,10,10}]");
      // generated: 65
      diceContext.PerFunctionUsed = 0;
      string test15 = TestLine(test14 + "xr%2", vars, context,
        "[{!die,3,10},{!die,4,10},{!die,1,10},{!die,7,10},{!die,10,10},{!die,9,10},{!die,6,10},{!die,7,10},{!die,4,10},{!die,4,10},{!die,9,10},{!die,7,10},{!die,3,10},{!die,10,10},{!die,7,10}]");
      // generated: 71

      // now some issues we've had
      rand.Next(); // 72
      rand.Next(); // 73
      diceContext.PerFunctionUsed = 0;
      string test16 = TestLine("6d6khd6", vars, context, "[{!die,5,6},{!die,6,6},{!die,6,6}]");
      // generated: 80
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