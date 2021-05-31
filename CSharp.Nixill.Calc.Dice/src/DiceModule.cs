using System;
using System.Collections.Generic;
using Nixill.CalcLib.Exception;
using Nixill.CalcLib.Functions;
using Nixill.CalcLib.Objects;
using Nixill.CalcLib.Operators;
using Nixill.CalcLib.Varaibles;
using static Nixill.DiceLib.Casting;

namespace Nixill.DiceLib {
  public static class DiceModule {
    public static bool Loaded { get; private set; }

    public static int DicePriority = 100;
    public static int RerollPriority => DicePriority - 5;
    public static int KeepPriority => DicePriority - 10;

    public static int RepeatPriority = -10;

    public static CLBinaryOperator BinaryDice { get; private set; }
    public static CLBinaryOperator BinaryReroll { get; private set; }
    public static CLBinaryOperator BinaryExplode { get; private set; }
    public static CLBinaryOperator BinaryExplodeRecursive { get; private set; }
    public static CLBinaryOperator BinaryKeepHigh { get; private set; }
    public static CLBinaryOperator BinaryKeepLow { get; private set; }
    public static CLBinaryOperator BinaryKeepFirst { get; private set; }
    public static CLBinaryOperator BinaryKeep { get; private set; }
    public static CLBinaryOperator BinaryDropHigh { get; private set; }
    public static CLBinaryOperator BinaryDropLow { get; private set; }
    public static CLBinaryOperator BinaryRepeat { get; private set; }
    public static CLPrefixOperator PrefixDie { get; private set; }
    public static CLComparisonOperatorSet ComparisonUntil { get; private set; }
    public static CLComparisonOperatorSet ComparisonReroll { get; private set; }
    public static CLComparisonOperatorSet ComparisonExplode { get; private set; }
    public static CLComparisonOperatorSet ComparisonExplodeRecursive { get; private set; }
    public static CLComparisonOperatorSet ComparisonDrop { get; private set; }
    public static CLComparisonOperatorSet ComparisonKeep { get; private set; }

    public static CLCodeFunction FuncDie { get; private set; }

    public static void Load() {
      // First we need some local types
      Type num = typeof(CalcNumber);
      Type lst = typeof(CalcList);
      Type val = typeof(CalcValue);
      Type obj = typeof(CalcObject);

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

      // The binary "r" operator.
      BinaryReroll = (CLOperators.BinaryOperators.GetOrNull("r")) ?? new CLBinaryOperator("r", RerollPriority, true, true);
      BinaryReroll.AddFunction(num, num, (left, right, vars, context) => CompRerolls(ValToList(left), CLComparison.Equal, right, vars, context, false, false));
      BinaryReroll.AddFunction(lst, num, (left, right, vars, context) => CompRerolls(left, CLComparison.Equal, right, vars, context, false, false));
      BinaryReroll.AddFunction(num, lst, (left, right, vars, context) => CompRerolls(ValToList(left), CLComparison.Equal, ListToNum(right), vars, context, false, false));
      BinaryReroll.AddFunction(lst, lst, (left, right, vars, context) => CompRerolls(left, CLComparison.Equal, ListToNum(right), vars, context, false, false));

      // The binary "x" operator.
      BinaryExplode = (CLOperators.BinaryOperators.GetOrNull("x")) ?? new CLBinaryOperator("x", RerollPriority, true, true);
      BinaryExplode.AddFunction(num, num, (left, right, vars, context) => CompRerolls(ValToList(left), CLComparison.Equal, right, vars, context, true, false));
      BinaryExplode.AddFunction(lst, num, (left, right, vars, context) => CompRerolls(left, CLComparison.Equal, right, vars, context, true, false));
      BinaryExplode.AddFunction(num, lst, (left, right, vars, context) => CompRerolls(ValToList(left), CLComparison.Equal, ListToNum(right), vars, context, true, false));
      BinaryExplode.AddFunction(lst, lst, (left, right, vars, context) => CompRerolls(left, CLComparison.Equal, ListToNum(right), vars, context, true, false));

      // The binary "xr" operator.
      BinaryExplodeRecursive = (CLOperators.BinaryOperators.GetOrNull("xr")) ?? new CLBinaryOperator("xr", RerollPriority, true, true);
      BinaryExplodeRecursive.AddFunction(num, num, (left, right, vars, context) => CompRerolls(ValToList(left), CLComparison.Equal, right, vars, context, true, true));
      BinaryExplodeRecursive.AddFunction(lst, num, (left, right, vars, context) => CompRerolls(left, CLComparison.Equal, right, vars, context, true, true));
      BinaryExplodeRecursive.AddFunction(num, lst, (left, right, vars, context) => CompRerolls(ValToList(left), CLComparison.Equal, ListToNum(right), vars, context, true, true));
      BinaryExplodeRecursive.AddFunction(lst, lst, (left, right, vars, context) => CompRerolls(left, CLComparison.Equal, ListToNum(right), vars, context, true, true));

      // The comparison "r" operator.
      ComparisonReroll = (CLOperators.BinaryOperators.GetOrNull("r=") as CLComparisonOperator)?.Parent ?? new CLComparisonOperatorSet("r", RerollPriority, true, true);
      ComparisonReroll.AddFunction(num, num, (left, comp, right, vars, context) => CompRerolls(ValToList(left), comp, right, vars, context, false, false));
      ComparisonReroll.AddFunction(lst, num, (left, comp, right, vars, context) => CompRerolls(left, comp, right, vars, context, false, false));
      ComparisonReroll.AddFunction(num, lst, (left, comp, right, vars, context) => CompRerolls(ValToList(left), comp, ListToNum(right), vars, context, false, false));
      ComparisonReroll.AddFunction(lst, lst, (left, comp, right, vars, context) => CompRerolls(left, comp, ListToNum(right), vars, context, false, false));

      // The comparison "x" operator.
      ComparisonExplode = (CLOperators.BinaryOperators.GetOrNull("x=") as CLComparisonOperator)?.Parent ?? new CLComparisonOperatorSet("x", RerollPriority, true, true);
      ComparisonExplode.AddFunction(num, num, (left, comp, right, vars, context) => CompRerolls(ValToList(left), comp, right, vars, context, true, false));
      ComparisonExplode.AddFunction(lst, num, (left, comp, right, vars, context) => CompRerolls(left, comp, right, vars, context, true, false));
      ComparisonExplode.AddFunction(num, lst, (left, comp, right, vars, context) => CompRerolls(ValToList(left), comp, ListToNum(right), vars, context, true, false));
      ComparisonExplode.AddFunction(lst, lst, (left, comp, right, vars, context) => CompRerolls(left, comp, ListToNum(right), vars, context, true, false));

      // The comparison "xr" operator.
      ComparisonExplodeRecursive = (CLOperators.BinaryOperators.GetOrNull("xr=") as CLComparisonOperator)?.Parent ?? new CLComparisonOperatorSet("xr", RerollPriority, true, true);
      ComparisonExplodeRecursive.AddFunction(num, num, (left, comp, right, vars, context) => CompRerolls(ValToList(left), comp, right, vars, context, true, true));
      ComparisonExplodeRecursive.AddFunction(lst, num, (left, comp, right, vars, context) => CompRerolls(left, comp, right, vars, context, true, true));
      ComparisonExplodeRecursive.AddFunction(num, lst, (left, comp, right, vars, context) => CompRerolls(ValToList(left), comp, ListToNum(right), vars, context, true, true));
      ComparisonExplodeRecursive.AddFunction(lst, lst, (left, comp, right, vars, context) => CompRerolls(left, comp, ListToNum(right), vars, context, true, true));

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
      BinaryDropHigh = CLOperators.BinaryOperators.GetOrNull("dh") ?? new CLBinaryOperator("dh", KeepPriority, true, true);
      BinaryDropHigh.AddFunction(num, num, (left, right, vars, context) => KeepDropProxy(ValToList(left), right, false, true, vars));
      BinaryDropHigh.AddFunction(lst, num, (left, right, vars, context) => KeepDropProxy(left, right, false, true, vars));
      BinaryDropHigh.AddFunction(num, lst, (left, right, vars, context) => KeepDropProxy(ValToList(left), ListToNum(right), false, true, vars));
      BinaryDropHigh.AddFunction(lst, lst, (left, right, vars, context) => KeepDropProxy(left, ListToNum(right), false, true, vars));

      // The binary "dl" operator.
      BinaryDropLow = CLOperators.BinaryOperators.GetOrNull("dl") ?? new CLBinaryOperator("dl", KeepPriority, true, true);
      BinaryDropLow.AddFunction(num, num, (left, right, vars, context) => KeepDropProxy(ValToList(left), right, false, false, vars));
      BinaryDropLow.AddFunction(lst, num, (left, right, vars, context) => KeepDropProxy(left, right, false, false, vars));
      BinaryDropLow.AddFunction(num, lst, (left, right, vars, context) => KeepDropProxy(ValToList(left), ListToNum(right), false, false, vars));
      BinaryDropLow.AddFunction(lst, lst, (left, right, vars, context) => KeepDropProxy(left, ListToNum(right), false, false, vars));

      // The comparison "k" operator.
      ComparisonKeep = (CLOperators.BinaryOperators.GetOrNull("k=") as CLComparisonOperator)?.Parent ?? new CLComparisonOperatorSet("k", KeepPriority, true, true);
      ComparisonKeep.AddFunction(num, num, (left, comp, right, vars, context) => KeepCompare(ValToList(left), comp, right, vars));
      ComparisonKeep.AddFunction(lst, num, (left, comp, right, vars, context) => KeepCompare(left, comp, right, vars));
      ComparisonKeep.AddFunction(num, lst, (left, comp, right, vars, context) => KeepCompare(ValToList(left), comp, ListToNum(right), vars));
      ComparisonKeep.AddFunction(lst, lst, (left, comp, right, vars, context) => KeepCompare(left, comp, ListToNum(right), vars));

      // The comparison "d" operator.
      ComparisonDrop = (CLOperators.BinaryOperators.GetOrNull("d=") as CLComparisonOperator)?.Parent ?? new CLComparisonOperatorSet("d", KeepPriority, true, true);
      ComparisonDrop.AddFunction(num, num, (left, comp, right, vars, context) => KeepCompare(ValToList(left), comp.Opposite, right, vars));
      ComparisonDrop.AddFunction(lst, num, (left, comp, right, vars, context) => KeepCompare(left, comp.Opposite, right, vars));
      ComparisonDrop.AddFunction(num, lst, (left, comp, right, vars, context) => KeepCompare(ValToList(left), comp.Opposite, ListToNum(right), vars));
      ComparisonDrop.AddFunction(lst, lst, (left, comp, right, vars, context) => KeepCompare(left, comp.Opposite, ListToNum(right), vars));

      // The binary "**" operator.
      BinaryRepeat = CLOperators.BinaryOperators.GetOrNull("**") ?? new CLBinaryOperator("**", RepeatPriority, false, true);
      BinaryRepeat.AddFunction(obj, num, BinRepeat);
      BinaryRepeat.AddFunction(obj, lst, (left, right, vars, context) => BinRepeat(left, ListToNum(right), vars, context));

      // The "!die" function
      FuncDie = new CLCodeFunction("die", FunctionDie);
    }

    private static CalcValue BinDice(CalcObject left, CalcObject right, CLLocalStore vars, CLContextProvider context) {
      int limit = int.MaxValue;
      DiceContext dc = null;

      // We need to get the limits if they've been set
      if (context.ContainsDerived(typeof(DiceContext), out Type actualDiceContext)) {
        dc = (DiceContext)(context.Get(actualDiceContext));
        limit = Math.Min(dc.PerRollLimit, dc.PerFunctionLimit - dc.PerFunctionUsed);
        if (limit == 0) throw new LimitedDiceException();
      }

      CalcNumber numLeft = (CalcNumber)left;
      CalcNumber numRight = null;
      CalcList lstRight = null;
      bool list = false;

      // Now figure out how many dice to roll...
      int count = (int)(numLeft.Value);

      // ... and whether or not it's within limits (including the limitation that it must be positive)
      if (count <= 0) {
        throw new CLException("The number of dice to roll must be positive.");
      }
      else if (count > limit) {
        count = limit;
      }

      // also remember to actually UPDATE the limits! (╯°□°）╯︵ ┻━┻
      if (dc != null) dc.PerFunctionUsed += count;

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

      // ... and ensure it's at least one.
      if (sides < 1) {
        throw new CLException("Dice must have at least one side.");
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
          CalcValue val = lstRight[choice];
          if (val is CalcNumber valNum) {
            ret[i] = new DiceDie(valNum.Value, lstRight);
          }
          else if (val is CalcList valList) {
            ret[i] = new DiceDie(valList.Sum(), lstRight);
          }
          else throw new CLException("Dice must be numeric values."); // maybe I'll change this one day
        }
        else {
          ret[i] = new DiceDie(choice + 1, new CalcNumber(sides));
        }
      }

      CalcList output = new CalcList(ret);

      // Add to roll history
      if (context.ContainsDerived(typeof(List<(string, CalcList)>), out Type actual)) {
        List<(string, CalcList)> history = (List<(string, CalcList)>)context.Get(actual);
        history.Add(($"{left.ToCode()}d{right.ToCode()}", output));
      }

      return output;
    }

    private static CalcValue CompUntil(CalcObject left, CLComparison comp, CalcObject right, CLLocalStore vars, CLContextProvider context) {
      int limit = int.MaxValue;

      DiceContext dc = null;

      // We need to get the limits if they've been set
      if (context.ContainsDerived(typeof(DiceContext), out Type actualDiceContext)) {
        dc = (DiceContext)(context.Get(actualDiceContext));
        limit = Math.Min(dc.PerRollLimit, dc.PerFunctionLimit - dc.PerFunctionUsed);
        if (limit == 0) throw new LimitedDiceException();
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

      // ... and ensure it's at least one.
      if (sides < 1) {
        throw new CLException("Dice must have at least one side.");
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

      CalcList output = null;
      Type actual = null;

      for (int i = 0; i < limit; i++) {
        // First determine the value
        int choice = rand.Next(sides);
        CalcNumber value = null;
        if (list) {
          CalcValue val = lstLeft[choice];
          if (val is CalcList valList) {
            value = new DiceDie(valList.Sum(), lstLeft);
          }
          else if (val is CalcNumber valNum) {
            value = new DiceDie(valNum.Value, lstLeft);
          }
        }
        else {
          value = new DiceDie(choice + 1, new CalcNumber(sides));
        }

        // See if it satisfies the comparison
        if (comp.CompareFunction(value.Value, numRight.Value)) {
          vars["_u"] = value;

          output = new CalcList(lstRet.ToArray());

          // Add to roll history
          if (context.ContainsDerived(typeof(List<(string, CalcList)>), out actual)) {
            List<(string, CalcList)> history = (List<(string, CalcList)>)context.Get(actual);
            history.Add(($"{left.ToCode()}u{comp.PostfixSymbol}{right.ToCode()}", output));
            history.Add(($"Killed above roll:", ValToList(value)));
          }

          // also remember to actually UPDATE the limits! (╯°□°）╯︵ ┻━┻
          if (dc != null) dc.PerFunctionUsed += i + 1;

          return output;
        }
        else {
          lstRet.Add(value);
        }
      }

      vars["_u"] = new CalcNumber(0);
      output = new CalcList(lstRet.ToArray());

      // Add to roll history
      if (context.ContainsDerived(typeof(List<(string, CalcList)>), out actual)) {
        List<(string, CalcList)> history = (List<(string, CalcList)>)context.Get(actual);
        history.Add(($"{left.ToCode()}u{comp.PostfixSymbol}{right.ToCode()}", output));
      }

      // also remember to actually UPDATE the limits! (╯°□°）╯︵ ┻━┻
      if (dc != null) dc.PerFunctionUsed += limit;

      return output;
    }

    private static CalcValue CompRerolls(CalcObject left, CLComparison comp, CalcObject right, CLLocalStore vars, CLContextProvider context, bool keep = false, bool recurse = false) {
      CalcList lstLeft = (CalcList)left;
      CalcNumber numRight = (CalcNumber)right;

      List<CalcValue> output = new List<CalcValue>();

      DiceContext dc = null;
      int limit = 0;
      int limitUsed = 0;

      // We need to get the limits if they've been set
      if (context.ContainsDerived(typeof(DiceContext), out Type actualDiceContext)) {
        dc = (DiceContext)(context.Get(actualDiceContext));
        limit = Math.Min(dc.PerRollLimit, dc.PerFunctionLimit - dc.PerFunctionUsed);
        if (limit == 0) throw new LimitedDiceException();
      }

      // Go through the list
      foreach (CalcValue val in lstLeft) {
        decimal value;
        if (val is CalcNumber valNum) value = valNum.Value;
        else if (val is CalcList valList) value = valList.Sum();
        else throw new CLException("Re-rolls only work with numeric values.");

        // If it's a value we need to re-roll
        if (comp.CompareFunction(value, numRight.Value)) {
          // Keep the original value ("x" and "xr" operators)
          if (keep) output.Add(val);

          // Now make another value (or recurse)
          bool redo = true;

          // Now figure out how many sides each die has...
          int sides = 0;
          bool list = false;
          CalcValue dSides;

          CalcList lstSides = null;

          if (val is DiceDie die) {
            dSides = die.Sides;

            // (Are we using a list or a number for the sides?)
            if (dSides is CalcNumber nSides) {
              sides = (int)(nSides.Value);
            }
            else if (dSides is CalcList lSides) {
              lstSides = lSides;
              sides = lSides.Count;
              list = true;
            }
          }
          else {
            decimal valValue = 0;
            if (val is CalcNumber nVal) valValue = nVal.Value;
            else if (val is CalcList lVal) valValue = lVal.Sum();
            else throw new CLException("Reroll only works with numeric values.");

            if (valValue < 0) valValue *= -1;

            sides =
              (valValue <= 6) ? 6 :
              (valValue <= 20) ? 20 :
              (valValue <= 100) ? 100 :
              (valValue <= 1000) ? 1000 :
              (valValue <= 10000) ? 10000 :
              (valValue <= 100000) ? 100000 :
              (valValue <= 1000000) ? 1000000 :
              (valValue <= 10000000) ? 10000000 :
              (valValue <= 100000000) ? 100000000 :
              (valValue <= 1000000000) ? 1000000000 : 2147483647;

            dSides = new CalcNumber(sides);
          }

          // Now we can roll the dice!
          Random rand = null;

          if (context.ContainsDerived(typeof(Random), out Type actualRandom)) {
            rand = (Random)(context.Get(actualRandom));
          }
          else {
            rand = new Random();
          }

          while (redo && limitUsed < limit) {
            int choice = rand.Next(sides);
            limitUsed++;
            decimal cValue = 0;

            if (list) {
              CalcValue cVal = lstSides[choice];
              if (cVal is CalcNumber cNum) {
                cValue = cNum.Value;
                output.Add(new DiceDie(cValue, lstSides));
              }
              else if (cVal is CalcList cList) {
                cValue = cList.Sum();
                output.Add(new DiceDie(cValue, lstSides));
              }
            }
            else {
              cValue = choice + 1;
              output.Add(new DiceDie(cValue, new CalcNumber(sides)));
            }

            // recursion?
            if (recurse) redo = comp.CompareFunction(cValue, numRight.Value);
            else redo = false;
          }
        }
        else {
          // The reroll comparison wasn't satisfied
          output.Add(val);
        }
      }

      return new CalcList(output.ToArray());
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

      if (keep) {
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

    private static CalcValue KeepCompare(CalcObject left, CLComparison comp, CalcObject right, CLLocalStore vars) {
      CalcList lstLeft = (CalcList)left;
      CalcNumber numRight = (CalcNumber)right;

      List<CalcValue> kept = new List<CalcValue>();
      List<CalcValue> dropped = new List<CalcValue>();

      foreach (CalcValue val in lstLeft) {
        if (comp.CompareFunction(ValueOf(val), numRight.Value)) {
          kept.Add(val);
        }
        else {
          dropped.Add(val);
        }
      }

      vars["_d"] = new CalcList(dropped.ToArray());
      return new CalcList(kept.ToArray());
    }

    private static CalcValue BinRepeat(CalcObject left, CalcObject right, CLLocalStore vars, CLContextProvider context) {
      List<CalcValue> ret = new List<CalcValue>();
      CalcNumber numRight = (CalcNumber)right;
      int count = (int)numRight.Value;

      CalcObject _i = null;

      if (vars.ContainsVar("_i")) {
        _i = vars["_i"];
      }

      for (int i = 0; i < count; i++) {
        vars["_i"] = new CalcNumber(i);
        ret.Add(left.GetValue(vars, context));
      }

      if (_i != null) {
        vars["_i"] = _i;
      }

      return new CalcList(ret.ToArray());
    }

    private static DiceDie FunctionDie(CalcObject[] pars, CLLocalStore vars, CLContextProvider context) {
      if (pars.Length < 2) throw new CLException("{!die} requires two params: A number and a value.");

      CalcNumber num = NumberAt(pars, 0, "!die", vars, context);
      CalcValue val = pars[1].GetValue();

      return new DiceDie(num.Value, val);
    }
  }
}