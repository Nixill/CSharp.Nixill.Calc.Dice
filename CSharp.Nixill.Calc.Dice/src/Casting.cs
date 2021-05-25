using Nixill.CalcLib.Exception;
using Nixill.CalcLib.Objects;
using Nixill.CalcLib.Varaibles;

namespace Nixill.DiceLib {
  internal static class Casting {
    // Converts a number to a string.
    internal static CalcString NumToString(CalcObject num) => new CalcString((num as CalcNumber).Value.ToString("0.#####"));

    // Converts a value to a list.
    internal static CalcList ValToList(CalcObject val) => new CalcList(new CalcValue[] { (val as CalcValue) });

    // Converts a list to a number.
    internal static CalcNumber ListToNum(CalcObject lst) => new CalcNumber((lst as CalcList).Sum());

    // Converts a value to a number.
    internal static decimal ValueOf(CalcObject obj) {
      if (obj is CalcNumber num) return num.Value;
      else return (obj as CalcList).Sum();
    }

    // Gets the parameter at a given index in the params list as a number.
    internal static CalcNumber NumberAt(CalcObject[] pars, int index, string name, CLLocalStore vars, CLContextProvider context) {
      if (pars.Length <= index) throw new CLException(name + " parameter " + index + " was not specified.");
      CalcValue val = pars[index].GetValue(vars, context);
      if (!(val is CalcNumber num)) throw new CLCastException(name + " parameter " + index + " must be a number.");
      return num;
    }
  }
}