using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BacASableWPF4
{
    public enum EnergyUnit
    {
        Wh,
        kWh,
        MWh,
        J,
        kJ,
        MJ,
        GJ,
    }

    public static class EnergyUnitExtension
    {
        public static EnergyUnit GetAdjustedUnit(this EnergyUnit originalUnit, ref decimal factor)
        {
            var minimalFactorLog = originalUnit.GetFactorLogForSmallestUnit(factor);
            if (Math.Round(minimalFactorLog) != minimalFactorLog)
            {
                return originalUnit;
            }
            else
            {
                switch (originalUnit)
                {
                    case EnergyUnit.Wh:
                    case EnergyUnit.kWh:
                    case EnergyUnit.MWh:
                        if (minimalFactorLog < 3)
                        {
                            factor = (decimal)Math.Pow(10, minimalFactorLog);
                            return EnergyUnit.Wh;
                        }
                        else if (minimalFactorLog < 6)
                        {
                            factor = (decimal)Math.Pow(10, minimalFactorLog - 3);
                            return EnergyUnit.kWh;
                        }
                        else
                        {
                            factor = (decimal)Math.Pow(10, minimalFactorLog - 6);
                            return EnergyUnit.MWh;
                        }

                    case EnergyUnit.J:
                    case EnergyUnit.kJ:
                    case EnergyUnit.MJ:
                    case EnergyUnit.GJ:
                        if (minimalFactorLog < 3)
                        {
                            factor = (decimal)Math.Pow(10, minimalFactorLog);
                            return EnergyUnit.J;
                        }
                        else if (minimalFactorLog < 6)
                        {
                            factor = (decimal)Math.Pow(10, minimalFactorLog - 3);
                            return EnergyUnit.kJ;
                        }
                        else if (minimalFactorLog < 9)
                        {
                            factor = (decimal)Math.Pow(10, minimalFactorLog - 6);
                            return EnergyUnit.MJ;
                        }
                        else
                        {
                            factor = (decimal)Math.Pow(10, minimalFactorLog - 9);
                            return EnergyUnit.GJ;
                        }
                    default:
                        throw new NotImplementedException("Unknown energy unit : " + originalUnit.ToString());
                }
            }

        }

        private static EnergyUnit GetSmallestUnit(EnergyUnit originalUnit)
        {
            switch (originalUnit)
            {
                case EnergyUnit.Wh:
                case EnergyUnit.kWh:
                case EnergyUnit.MWh:
                    return EnergyUnit.Wh;
                case EnergyUnit.J:
                case EnergyUnit.kJ:
                case EnergyUnit.MJ:
                case EnergyUnit.GJ:
                default:
                    return EnergyUnit.J;
            }
        }

        private static double GetFactorLogForSmallestUnit(this EnergyUnit originalUnit, decimal factor)
        {
            var factorLog = Math.Log10((double)factor);

            switch (originalUnit)
            {
                case EnergyUnit.kWh:
                case EnergyUnit.kJ:
                    return factorLog + 3;
                case EnergyUnit.MWh:
                case EnergyUnit.MJ:
                    return factorLog + 6;
                case EnergyUnit.GJ:
                    return factorLog + 9;
                case EnergyUnit.Wh:
                case EnergyUnit.J:
                default:
                    return factorLog;
            }
        }
    }
}
