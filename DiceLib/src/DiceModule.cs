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
      BinaryDice.AddFunction(lst, num, (left, right, vars, context) => BinDice(ListToNum(left), right, vars, context));
      BinaryDice.AddFunction(lst, lst, (left, right, vars, context) => BinDice(ListToNum(left), right, vars, context));

      // The prefix "d" operator.
      PrefixDie = CLOperators.PrefixOperators.GetOrNull("d") ?? new CLPrefixOperator("d", DicePriority, true);
      PrefixDie.AddFunction(num, (oper, vars, context) => ((CalcList)BinDice(new CalcNumber(1), oper, vars, context))[0]);
      PrefixDie.AddFunction(lst, (oper, vars, context) => ((CalcList)BinDice(new CalcNumber(1), oper, vars, context))[0]);

      // The comparison "u" operator.
      ComparisonUntil = (CLOperators.BinaryOperators.GetOrNull("u=") as CLComparisonOperator)?.Parent ?? new CLComparisonOperatorSet("u", DicePriority, true, true);
      ComparisonUntil.AddFunction(num, num, CompUntil);
      ComparisonUntil.AddFunction(lst, num, CompUntil);
      ComparisonUntil.AddFunction(num, lst, (left, comp, right, vars, context) => CompUntil(left, comp, ListToNum(right), vars, context));
      ComparisonUntil.AddFunction(lst, lst, (left, comp, right, vars, context) => CompUntil(left, comp, ListToNum(right), vars, context));

      // The binary "k" operator.
      BinaryKeep = CLOperators.BinaryOperators.GetOrNull("k") ?? new CLBinaryOperator("k", KeepPriority, true, true);
      BinaryKeep.AddFunction(num, num, (left, right, vars, context) => KeepDropProxy(ValToList(left), right, true, true, vars));
      BinaryKeep.AddFunction(lst, num, (left, right, vars, context) => KeepDropProxy(left, right, true, true, vars));
      BinaryKeep.AddFunction(num, lst, (left, right, vars, context) => KeepDropProxy(ValToList(left), ListToNum(right), true, true, vars));
      BinaryKeep.AddFunction(lst, lst, (left, right, vars, context) => KeepDropProxy(left, ListToNum(right), true, true, vars));

      // The binary "kh" operator.
      BinaryKeepHigh = CLOperators.BinaryOperators.GetOrNull("kh") ?? new CLBinaryOperator("kh", KeepPriority, true, true);
      BinaryKeepHigh.AddFunction(num, num, (left, right, vars, context) => KeepDropProxy(ValToList(left), right, true, true, vars));
      BinaryKeepHigh.AddFunction(lst, num, (left, right, vars, context) => KeepDropProxy(left, right, true, true, vars));
      BinaryKeepHigh.AddFunction(num, lst, (left, right, vars, context) => KeepDropProxy(ValToList(left), ListToNum(right), true, true, vars));
      BinaryKeepHigh.AddFunction(lst, lst, (left, right, vars, context) => KeepDropProxy(left, ListToNum(right), true, true, vars));

      // The binary "kl" operator.
      BinaryKeepLow = CLOperators.BinaryOperators.GetOrNull("kl") ?? new CLBinaryOperator("kl", KeepPriority, true, true);
      BinaryKeepLow.AddFunction(num, num, (left, right, vars, context) => KeepDropProxy(ValToList(left), right, true, false, vars));
      BinaryKeepLow.AddFunction(lst, num, (left, right, vars, context) => KeepDropProxy(left, right, true, false, vars));
      BinaryKeepLow.AddFunction(num, lst, (left, right, vars, context) => KeepDropProxy(ValToList(left), ListToNum(right), true, false, vars));
      BinaryKeepLow.AddFunction(lst, lst, (left, right, vars, context) => KeepDropProxy(left, ListToNum(right), true, false, vars));

      // The binary "dh" operator.
      BinaryDropHigh = CLOperators.BinaryOperators.GetOrNull("kh") ?? new CLBinaryOperator("dh", KeepPriority, true, true);
      BinaryDropHigh.AddFunction(num, num, (left, right, vars, context) => KeepDropProxy(ValToList(left), right, false, true, vars));
      BinaryDropHigh.AddFunction(lst, num, (left, right, vars, context) => KeepDropProxy(left, right, false, true, vars));
      BinaryDropHigh.AddFunction(num, lst, (left, right, vars, context) => KeepDropProxy(ValToList(left), ListToNum(right), false, true, vars));
      BinaryDropHigh.AddFunction(lst, lst, (left, right, vars, context) => KeepDropProxy(left, ListToNum(right), false, true, vars));

      // The binary "dl" operator.
      BinaryDropLow = CLOperators.BinaryOperators.GetOrNull("kl") ?? new CLBinaryOperator("dl", KeepPriority, true, true);
      BinaryDropLow.AddFunction(num, num, (left, right, vars, context) => KeepDropProxy(ValToList(left), right, false, false, vars));
      BinaryDropLow.AddFunction(lst, num, (left, right, vars, context) => KeepDropProxy(left, right, false, false, vars));
      BinaryDropLow.AddFunction(num, lst, (left, right, vars, context) => KeepDropProxy(ValToList(left), ListToNum(right), false, false, vars));
      BinaryDropLow.AddFunction(lst, lst, (left, right, vars, context) => KeepDropProxy(left, ListToNum(right), false, false, vars));
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

    private static CalcValue CompUntil(CalcObject left, CLComparison comp, CalcObject right, CLLocalStore vars, CLContextProvider context) {
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
        numLeft = (CalcNumber)left;
        sides = (int)(numLeft.Value);
      }
      else if (left is CalcList) {
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
          vars["_u"] = value;
          return new CalcList(lstRet.ToArray());
        }
        else {
          lstRet.Add(value);
        }
      }

      vars["_u"] = new CalcNumber(0);
      return new CalcList(lstRet.ToArray());
    }

    private static CalcValue KeepDropNumber(CalcList list, int count, bool keep, bool highest, CLLocalStore vars) {
      // Shortcuts
      if (list.Count == 0) {
        vars["_d"] = new CalcList(new CalcValue[0]);
        return list;
      }
      if (count >= list.Count) {
        if (keep) {
          vars["_d"] = new CalcList(new CalcValue[0]);
          return list;
        }
        else {
          vars["_d"] = list;
          return new CalcList(new CalcValue[0]);
        }
      }
      else if (count <= 0) {
        if (keep) {
          vars["_d"] = list;
          return new CalcList(new CalcValue[0]);
        }
        else {
          vars["_d"] = new CalcList(new CalcValue[0]);
          return list;
        }
      }

      // The long way
      int found = 0;
      bool[] action = new bool[list.Count];
      decimal next = highest ? decimal.MinValue : decimal.MaxValue;
      decimal current = next;
      decimal[] values = new decimal[list.Count];

      for (int i = 0; i < list.Count; i++) {
        // Get value of list item
        CalcValue val = list[i];
        CalcNumber num = val as CalcNumber;
        if (num == null) {
          num = ListToNum(val);
        }
        values[i] = num.Value;
      }

      while (found < count) {
        for (int i = 0; i < list.Count && found < count; i++) {
          // If this is already one we found, skip it
          if (action[i]) continue;

          // Otherwise, if it's the next value, grab it
          if (values[i] == current) {
            action[i] = true;
            found++;
          }

          // Otherwise, check to see if it can be the next value
          if (highest) {
            next = Math.Max(next, values[i]);
          }
          else {
            next = Math.Min(next, values[i]);
          }
        }

        current = next;
        next = highest ? decimal.MinValue : decimal.MaxValue;
      }

      // Now make the outputs
      CalcValue[] fits = new CalcValue[count];
      CalcValue[] noFits = new CalcValue[list.Count - count];

      // Split the values into the two lists
      int fitCount = 0;
      int noFitCount = 0;

      for (int i = 0; i < list.Count; i++) {
        if (action[i]) {
          fits[fitCount++] = list[i];
        }
        else {
          noFits[noFitCount++] = list[i];
        }
      }

      if (!keep) {
        vars["_d"] = new CalcList(noFits);
        return new CalcList(fits);
      }
      else {
        vars["_d"] = new CalcList(fits);
        return new CalcList(noFits);
      }
    }

    private static CalcValue KeepDropProxy(CalcObject left, CalcObject right, bool keep, bool highest, CLLocalStore vars) {
      return KeepDropNumber((CalcList)left, (int)((CalcNumber)right).Value, keep, highest, vars);
    }
  }
}