using System;
using System.Collections.Generic;
using Nixill.CalcLib.Exception;
using Nixill.CalcLib.Objects;
using Nixill.CalcLib.Operators;
using Nixill.CalcLib.Varaibles;
using static Nixill.DiceLib.Casting;

namespace Nixill.DiceLib {
  public class DiceModule {
    public static bool Loaded { get; private set; }

    public static int DicePriority = 100;
    public static int KeepPriority => DicePriority - 5;

    public static CLBinaryOperator BinaryDice { get; private set; }
    public static CLBinaryOperator BinaryKeepHigh { get; private set; }
    public static CLBinaryOperator BinaryKeepLow { get; private set; }
    public static CLBinaryOperator BinaryKeepFirst { get; private set; }
    public static CLBinaryOperator BinaryKeep { get; private set; }
    public static CLBinaryOperator BinaryDropHigh { get; private set; }
    public static CLBinaryOperator BinaryDropLow { get; private set; }
    public static CLBinaryOperator BinaryRepeat { get; private set; }
    public static CLPrefixOperator PrefixDie { get; private set; }
    public static CLComparisonOperatorSet ComparisonUntil { get; private set; }
    public static CLComparisonOperatorSet ComparisonDrop { get; private set; }
    public static CLComparisonOperatorSet ComparisonKeep { get; private set; }

    public void Load() {
      // First we need some local types
      Type num = typeof(CalcNumber);
      Type lst = typeof(CalcList);
      Type str = typeof(CalcString);
      Type val = typeof(CalcValue);

      // The binary "d" operator.
      BinaryDice = CLOperators.BinaryOperators.GetOrNull("d") ?? new CLBinaryOperator("d", DicePriority, true, true);
      BinaryDice.AddFunction(num, num, BinDiceNumber);
      BinaryDice.AddFunction(num, lst, BinDiceList);
      BinaryDice.AddFunction(lst, num, (left, right, vars, context) => BinDiceNumber(ListToNum((CalcList)left), right, vars, context));
      BinaryDice.AddFunction(lst, lst, (left, right, vars, context) => BinDiceList(ListToNum((CalcList)left), right, vars, context));
    }

    private static CalcValue BinDiceNumber(CalcObject left, CalcObject right, CLLocalStore vars, CLContextProvider context) {
      int limit = int.MaxValue;

      // We need to get the limits if they've been set
      if (context.ContainsDerived(typeof(DiceContext), out Type actualDiceContext)) {
        DiceContext dc = (DiceContext)(context.Get(actualDiceContext));
        limit = Math.Min(dc.PerRollLimit, dc.PerFunctionLimit - dc.PerFunctionUsed);
      }

      CalcNumber numLeft = (CalcNumber)left;
      CalcNumber numRight = (CalcNumber)right;

      // Now figure out how many dice to roll...
      int count = (int)(numLeft.Value);

      // ... and whether or not it's within limits (including the limitation that it must be positive)
      if (count < 0) {
        throw new CLException("The number of dice to roll must be non-negative.");
      }
      else if (count > limit) {
        count = limit;
      }

      // Now figure out how many sides each die has...
      int sides = (int)(numRight.Value);

      // ... and ensure it's at least two.
      if (sides < 2) {
        throw new CLException("Dice must have at least two sides.");
      }

      // Now we can roll the dice!
      CalcValue[] ret = new CalcValue[count];

      Random rand = null;

      if (context.ContainsDerived(typeof(Random), out Type actualRandom)) {
        rand = (Random)(context.Get(actualRandom));
      }
      else {
        rand = new Random();
      }

      for (int i = 0; i < count; i++) {
        ret[i] = new CalcNumber(rand.Next(sides) + 1);
      }

      return new CalcList(ret);
    }

    private CalcValue BinDiceList(CalcObject left, CalcObject right, CLLocalStore vars, CLContextProvider context) {
      int limit = int.MaxValue;

      // We need to get the limits if they've been set
      if (context.ContainsDerived(typeof(DiceContext), out Type actualDiceCContext)) {
        DiceContext dc = (DiceContext)(context.Get(actualDiceCContext));
        limit = Math.Min(dc.PerRollLimit, dc.PerFunctionLimit - dc.PerFunctionUsed);
      }

      CalcNumber numLeft = (CalcNumber)left;
      CalcList lstRight = (CalcList)right;

      // Now figure out how many dice to roll...
      int count = (int)(numLeft.Value);

      // ... and whether or not it's within limits (including the limitation that it must be positive)
      if (count < 0) {
        throw new CLException("The number of dice to roll must be non-negative.");
      }
      else if (count > limit) {
        count = limit;
      }

      // Now figure out how many sides each die has...
      int sides = (int)(lstRight.Count);

      // ... and ensure it's at least two.
      if (sides < 2) {
        throw new CLException("Dice must have at least two sides (items in the list).");
      }

      // Now we can roll the dice!
      CalcValue[] ret = new CalcValue[count];

      Random rand = null;

      if (context.ContainsDerived(typeof(Random), out Type actualRandom)) {
        rand = (Random)(context.Get(actualRandom));
      }
      else {
        rand = new Random();
      }

      for (int i = 0; i < count; i++) {
        ret[i] = lstRight[rand.Next(sides) + 1];
      }

      return new CalcList(ret);
    }
  }
}