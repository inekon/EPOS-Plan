using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpeicherEngine;
using Xbim.Ifc4.Interfaces;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Einheiten einer IFC-Datei</b> (Umsetzungskonzept 3.4, „Einheiten"; Gegenlesen IFC):
    /// Faktoren nach Meter, m², m³ aus <c>IIfcProject.UnitsInContext.Units</c>.
    ///
    /// <para><b>Jede Größenart mit ihrem EIGENEN Prefix.</b> <c>LENGTHUNIT</c>, <c>AREAUNIT</c> und
    /// <c>VOLUMEUNIT</c> werden je für sich ausgewertet; ein fehlender Prefix heißt „ohne Prefix", nicht
    /// „wie die Länge". Revit und Archicad erklären regelmäßig <c>MILLI METRE</c> und
    /// <c>SQUARE_METRE</c> ohne Prefix — wer den Längenfaktor quadriert, rechnet die Flächen um 10⁻⁶
    /// falsch. Nur wenn eine Flächen- oder Volumeneinheit ganz FEHLT, wird sie aus der Länge
    /// abgeleitet, und das wird gemeldet. Ein Prefix an <c>SQUARE_METRE</c> gilt dem Meter, die Fläche
    /// trägt ihn im Quadrat (<c>MILLI SQUARE_METRE</c> = mm² = 10⁻⁶ m²).</para>
    ///
    /// <para><b><c>IIfcConversionBasedUnit</c></b> (Fuß, Zoll) trägt ihren Faktor in
    /// <c>ConversionFactor</c> und wird benannt gemeldet statt still als 1,0 genommen. Fehlt die
    /// Längeneinheit, gilt Meter, mit Warnung.</para>
    /// </summary>
    internal sealed class IfcEinheiten
    {
        private const string P = IfcImportProfil.MELDUNGSPRAEFIX;

        /// <summary>Faktor der Längeneinheit nach Meter.</summary>
        public double Laenge { get; private set; } = 1.0;

        /// <summary>Faktor der Flächeneinheit nach m².</summary>
        public double Flaeche { get; private set; } = 1.0;

        /// <summary>Faktor der Volumeneinheit nach m³.</summary>
        public double Volumen { get; private set; } = 1.0;

        /// <summary>Temperatureinheit: <c>true</c> = Kelvin erklärt, <c>false</c> = Grad Celsius erklärt, <c>null</c> = keine Angabe.</summary>
        public bool? TemperaturInKelvin { get; private set; }

        /// <summary>Liest die Einheiten des Projekts; Auffälligkeiten gehen als Meldung in <paramref name="meldungen"/>.</summary>
        public static IfcEinheiten Lesen(IIfcProject projekt, List<PruefMeldung> meldungen)
        {
            var e = new IfcEinheiten();
            List<IIfcNamedUnit> einheiten = projekt?.UnitsInContext?.Units?.OfType<IIfcNamedUnit>().ToList()
                                            ?? new List<IIfcNamedUnit>();

            IIfcNamedUnit laenge = einheiten.FirstOrDefault(u => u.UnitType == IfcUnitEnum.LENGTHUNIT);
            IIfcNamedUnit flaeche = einheiten.FirstOrDefault(u => u.UnitType == IfcUnitEnum.AREAUNIT);
            IIfcNamedUnit volumen = einheiten.FirstOrDefault(u => u.UnitType == IfcUnitEnum.VOLUMEUNIT);
            IIfcNamedUnit temperatur = einheiten.FirstOrDefault(u => u.UnitType == IfcUnitEnum.THERMODYNAMICTEMPERATUREUNIT);

            if (laenge == null)
                meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "EINHEIT_FEHLT", "LENGTHUNIT", "m"));
            else
                e.Laenge = Faktor(laenge, 1, "m", meldungen) ?? 1.0;

            if (flaeche == null)
            {
                e.Flaeche = e.Laenge * e.Laenge;
                meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "EINHEIT_ABGELEITET", "AREAUNIT", Zahl(e.Flaeche)));
            }
            else
                e.Flaeche = Faktor(flaeche, 2, "m²", meldungen) ?? e.Laenge * e.Laenge;

            if (volumen == null)
            {
                e.Volumen = e.Laenge * e.Laenge * e.Laenge;
                meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "EINHEIT_ABGELEITET", "VOLUMEUNIT", Zahl(e.Volumen)));
            }
            else
                e.Volumen = Faktor(volumen, 3, "m³", meldungen) ?? e.Laenge * e.Laenge * e.Laenge;

            if (temperatur is IIfcSIUnit ts)
                e.TemperaturInKelvin = ts.Name == IfcSIUnitName.KELVIN ? true
                                     : ts.Name == IfcSIUnitName.DEGREE_CELSIUS ? false : (bool?)null;
            return e;
        }

        /// <summary>
        /// Der Faktor einer Einheit nach SI in der Potenz <paramref name="potenz"/> (1 Länge, 2 Fläche,
        /// 3 Volumen). Eine umgerechnete Einheit wird gemeldet; <c>null</c> = nicht auswertbar (gemeldet).
        /// </summary>
        internal static double? Faktor(IIfcNamedUnit einheit, int potenz, string siZeichen, List<PruefMeldung> meldungen)
        {
            switch (einheit)
            {
                case IIfcSIUnit si:
                {
                    IfcSIUnitName erwartet = potenz == 1 ? IfcSIUnitName.METRE
                                           : potenz == 2 ? IfcSIUnitName.SQUARE_METRE : IfcSIUnitName.CUBIC_METRE;
                    double f = Math.Pow(PrefixFaktor(si.Prefix), potenz);
                    if (si.Name != erwartet)
                        meldungen?.Add(new PruefMeldung(PruefStufe.Warnung, P + "EINHEIT",
                            si.Name.ToString(), Zahl(f) + " " + siZeichen));
                    return f;
                }
                case IIfcConversionBasedUnit umgerechnet:
                {
                    double? f = Umrechnung(umgerechnet, potenz);
                    string name = umgerechnet.Name.ToString();
                    if (f.HasValue && f.Value > 0.0)
                    {
                        meldungen?.Add(new PruefMeldung(PruefStufe.Warnung, P + "EINHEIT", name, Zahl(f.Value) + " " + siZeichen));
                        return f;
                    }
                    meldungen?.Add(new PruefMeldung(PruefStufe.Warnung, P + "EINHEIT", name, "1 " + siZeichen));
                    return null;
                }
                default:
                    meldungen?.Add(new PruefMeldung(PruefStufe.Warnung, P + "EINHEIT",
                        einheit?.GetType().Name ?? "", "1 " + siZeichen));
                    return null;
            }
        }

        /// <summary>Der Faktor einer umgerechneten Einheit: Wert × Faktor der Bezugseinheit.</summary>
        private static double? Umrechnung(IIfcConversionBasedUnit einheit, int potenz)
        {
            IIfcMeasureWithUnit mit = einheit.ConversionFactor;
            double? wert = IfcEigenschaften.Zahl(mit?.ValueComponent);
            if (!wert.HasValue) return null;
            if (mit.UnitComponent is IIfcNamedUnit bezug)
            {
                double? b = Faktor(bezug, potenz, "", null);
                return b.HasValue ? wert.Value * b.Value : (double?)null;
            }
            return wert;
        }

        /// <summary>Der Faktor eines SI-Prefix; ohne Prefix 1.</summary>
        internal static double PrefixFaktor(IfcSIPrefix? prefix)
        {
            if (!prefix.HasValue) return 1.0;
            switch (prefix.Value)
            {
                case IfcSIPrefix.EXA: return 1e18;
                case IfcSIPrefix.PETA: return 1e15;
                case IfcSIPrefix.TERA: return 1e12;
                case IfcSIPrefix.GIGA: return 1e9;
                case IfcSIPrefix.MEGA: return 1e6;
                case IfcSIPrefix.KILO: return 1e3;
                case IfcSIPrefix.HECTO: return 1e2;
                case IfcSIPrefix.DECA: return 1e1;
                case IfcSIPrefix.DECI: return 1e-1;
                case IfcSIPrefix.CENTI: return 1e-2;
                case IfcSIPrefix.MILLI: return 1e-3;
                case IfcSIPrefix.MICRO: return 1e-6;
                case IfcSIPrefix.NANO: return 1e-9;
                case IfcSIPrefix.PICO: return 1e-12;
                case IfcSIPrefix.FEMTO: return 1e-15;
                case IfcSIPrefix.ATTO: return 1e-18;
                default: return 1.0;
            }
        }

        /// <summary>
        /// Eine Temperatur nach °C: bei erklärtem Kelvin −273,15; ohne Angabe gilt ein Wert über 150 als
        /// Kelvin (die SI-Vorgabe von IFC), sonst als °C — ein Raumsollwert liegt in keinem der beiden
        /// Bereiche des anderen.
        /// </summary>
        public double NachCelsius(double wert)
        {
            if (TemperaturInKelvin == true) return wert - 273.15;
            if (TemperaturInKelvin == false) return wert;
            return wert > 150.0 ? wert - 273.15 : wert;
        }

        private static string Zahl(double w) => w.ToString("0.############", CultureInfo.InvariantCulture);
    }
}
