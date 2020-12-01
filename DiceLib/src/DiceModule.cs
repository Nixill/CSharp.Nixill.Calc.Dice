using System;
using System.Collections.Generic;
using Nixill.CalcLib.Exception;
using Nixill.CalcLib.Objects;
using Nixill.CalcLib.Operators;
using Nixill.CalcLib.Varaibles;
using static Nixill.DiceLib.Casting;

namespace Nixill.DiceLib {
  public static class DiceModule {
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

    public static void Load() {
      // First we need some local types
      Type num = typeof(CalcNumber);
      Type lst = typeof(CalcList);
      Type str = typeof(CalcString);
      Type val = typeof(CalcValue);

      // The binary "d" operator.
      BinaryDice = CLOperators.BinaryOperators.GetOrNull("d") ?? new CLBinaryOperator("d", DicePriority, true, true);
      BinaryDice.AddFunction(num, num, BinDice);
      BinaryDice.AddFunction(num, lst, BinDice);
      BinaryDice.AddFunction(lst, num, (left, right, vars, context) => BinDice(ListToNum((CalcList)left), right, vars, context));
      BinaryDice.AddFunction(lst, lst, (left, right, vars, context) => BinDice(ListToNum((CalcList)left), right, vars, context));

      // The prefix "d" operator.
      PrefixDie = CLOperators.PrefixOperators.GetOrNull("d") ?? new CLPrefixOperator("d", DicePriority, true);
      PrefixDie.AddFunction(num, (oper, vars, context) => ((CalcList)BinDice(new CalcNumber(1), oper, vars, context))[0]);
      PrefixDie.AddFunction(lst, (oper, vars, context) => ((CalcList)BinDice(new CalcNumber(1), oper, vars, context))[0]);
    }

    private static CalcValue BinDice(CalcObject left, CalcObject right, CLLocalStore vars, CLContextProvider context) {
      int limit = int.MaxValue;

      // We need to get the limits if they've been set
      if (context.ContainsDerived(typeof(DiceContext), out Type actualDiceContext)) {
        DiceContext dc = (DiceContext)(context.Get(actualDiceContext));
        limit = Math.Min(dc.PerRollLimit, dc.PerFunctionLimit - dc.PerFunctionUsed);
      }

      CalcNumber numLeft = (CalcNumber)left;
      CalcNumber numRight = null;
      CalcList lstRight = null;
      bool list = false;

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
      int sides = 0;

      // (Are we using a list or a number for the sides?)
      if (right is CalcNumber) {
        numRight = (CalcNumber)right;
        sides = (int)(numRight.Value);
      }
      else if (right is CalcList) {
        lstRight = (CalcList)right;
        sides = lstRight.Count;
        list = true;
      }

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
        int choice = rand.Next(sides);
        if (list) {
          ret[i] = lstRight[choice];
        }
        else {
          ret[i] = new CalcNumber(choice + 1);
        }
      }

      return new CalcList(ret);
    }

    private static CalcValue CompUntilNumber(CalcObject left, CLComparison comp, CalcObject right, CLLocalStore vars, CLContextProvider context) {
      int limit = int.MaxValue;

      // We need to get the limits if they've been set
      if (context.ContainsDerived(typeof(DiceContext), out Type actualDiceContext)) {
        DiceContext dc = (DiceContext)(context.Get(actualDiceContext));
        limit = Math.Min(dc.PerRollLimit, dc.PerFunctionLimit - dc.PerFunctionUsed);
      }

      CalcNumber numLeft = null;
      CalcList lstLeft = null;
      bool list = false;
      CalcNumber numRight = (CalcNumber)right;

      // Now figure out how many sides each die has...
      int sides = 0;

      // (Are we using a list or a number for the sides?)
      if (left is CalcNumber) {
        numLeft = (CalcNumber)right;
        sides = (int)(numLeft.Value);
      }
      else if (right is CalcList) {
        lstLeft = (CalcList)left;
        sides = lstLeft.Count;
        list = true;
      }

      // ... and ensure it's at least two.
      if (sides < 2) {
        throw new CLException("Dice must have at least two sides.");
      }

      // Now we can roll the dice!
      List<CalcValue> lstRet = new List<CalcValue>();

      Random rand = null;

      if (context.ContainsDerived(typeof(Random), out Type actualRandom)) {
        rand = (Random)(context.Get(actualRandom));
      }
      else {
        rand = new Random();
      }

      for (int i = 0; i < limit; i++) {
        // First determine the value
        int choice = rand.Next(sides);
        CalcNumber value = null;
        if (list) {
          CalcValue val = lstLeft[choice];
          if (val is CalcList) {
            value = ListToNum(val);
          }
          else {
            value = (CalcNumber)val;
          }
        }
        else {
          value = new CalcNumber(choice + 1);
        }

        // See if it satisfies the comparison
        if (comp.CompareFunction(value.Value, numRight.Value)) {
          vars["*u"] = value;
          return new CalcList(lstRet.ToArray());
        }
        else {
          lstRet.Add(value);
        }
      }

      vars["*u"] = new CalcNumber(0);
      return new CalcList(lstRet.ToArray());
    }
  }
}