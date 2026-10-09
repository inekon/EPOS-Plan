using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Lesewerte der Teillastrechnung einer Kältemaschine</b> (Fachkonzept Teillast und Takten 5.4, 7.2, 7.3; KM3‑E3‑b):
    /// Weg samt Herkunft der wirksamen Kurve, Verdichterregelung, Taktverlustfaktor C_d und Kennfeldrand — roh aus dem Satz
    /// (Projektkopie oder Katalog), die Texte erst in der verlangten Sprache. Anlagendialog, KI-Sicht und Berichtstafel lesen
    /// dieselben Werte; keine Rechenwirkung.
    /// </summary>
    public sealed class KaeltemaschineTeillastLesewerte
    {
        /// <summary>Die Herkunft der wirksamen Kurve wie im Lauf (<see cref="Kaeltemaschinenteillast.AusModell"/>).</summary>
        public KaeltemaschinenKurvenherkunft Herkunft { get; init; }
        /// <summary>Die gepflegte Verdichterregelung (Persistenzwert); <c>null</c> = keine Angabe.</summary>
        public string Verdichterregelung { get; init; }
        /// <summary>Der gepflegte Taktverlustfaktor; <c>null</c> = Vorgabe.</summary>
        public double? Cd { get; init; }
        /// <summary>Der gepflegte Kennfeldrand (Persistenzwert); <c>null</c> = Vorgabe (Randwert).</summary>
        public string Randweg { get; init; }

        /// <summary>Ist ein Teillastweg wirksam (nicht der Bestandsweg)? Dann rechnet der Lauf Teillast und Takten.</summary>
        public bool Gesetzt => Herkunft != KaeltemaschinenKurvenherkunft.Bestand;

        /// <summary>Die Lesewerte eines Satzes; <c>null</c> bleibt <c>null</c>.</summary>
        public static KaeltemaschineTeillastLesewerte Aus(KaeltemaschineModel m)
        {
            if (m == null) return null;
            return new KaeltemaschineTeillastLesewerte
            {
                Herkunft = Kaeltemaschinenteillast.AusModell(m).Herkunft,
                Verdichterregelung = string.IsNullOrWhiteSpace(m.Verdichterregelung) ? null : m.Verdichterregelung.Trim(),
                Cd = m.Taktverlustfaktor_Cd,
                Randweg = string.IsNullOrWhiteSpace(m.Kennfeld_Randweg) ? null : m.Kennfeld_Randweg.Trim(),
            };
        }

        /// <summary>Der Weg mit Herkunft als Text: wie bisher · linear · Kurve · Kurve (Vorgabekurve) · linear (Kurve verworfen).</summary>
        public string WegText(CultureInfo kultur) => T(Herkunft switch
        {
            KaeltemaschinenKurvenherkunft.Linear => nameof(R.KM_TEILLAST_WEG_LINEAR),
            KaeltemaschinenKurvenherkunft.Kurve => nameof(R.KM_TEILLAST_WEG_KURVE),
            KaeltemaschinenKurvenherkunft.Vorgabekurve => nameof(R.KM_TT_WEG_VORGABEKURVE),
            KaeltemaschinenKurvenherkunft.Verworfen => nameof(R.KM_TT_WEG_VERWORFEN),
            _ => nameof(R.KM_TEILLAST_WEG_BESTAND)
        }, kultur);

        /// <summary>Die Verdichterregelung als Text; ohne Angabe „keine Angabe“.</summary>
        public string RegelungText(CultureInfo kultur) => T(Verdichterregelung switch
        {
            KaeltemaschineTeillastSchema.REGELUNG_EIN_AUS => nameof(R.KM_REGELUNG_EIN_AUS),
            KaeltemaschineTeillastSchema.REGELUNG_STUFEN => nameof(R.KM_REGELUNG_STUFEN),
            KaeltemaschineTeillastSchema.REGELUNG_DREHZAHL => nameof(R.KM_REGELUNG_DREHZAHL),
            _ => nameof(R.KM_REGELUNG_KEINE)
        }, kultur);

        /// <summary>C_d als Text: gepflegt mit zwei Stellen, sonst „0,9 (Vorgabe)“.</summary>
        public string CdText(CultureInfo kultur)
            => Cd is double cd ? cd.ToString("0.00", kultur ?? CultureInfo.CurrentCulture) : T(nameof(R.KM_PH_CD_VORGABE), kultur);

        /// <summary>Der Kennfeldrand als Text; ohne Angabe „Randwert (Vorgabe)“.</summary>
        public string RandwegText(CultureInfo kultur) => T(Randweg switch
        {
            KaeltemaschineTeillastSchema.RANDWEG_RANDWERT => nameof(R.KM_RANDWEG_RANDWERT),
            KaeltemaschineTeillastSchema.RANDWEG_GUETEGRAD => nameof(R.KM_RANDWEG_GUETEGRAD),
            _ => nameof(R.KM_PH_RANDWEG_VORGABE)
        }, kultur);

        private static string T(string schluessel, CultureInfo kultur)
            => R.ResourceManager.GetString(schluessel, kultur ?? CultureInfo.CurrentUICulture) ?? schluessel;
    }

    /// <summary>
    /// <b>Die Kennzahlen „Teillast und Takten“ einer Kältemaschine</b> (Fachkonzept 5.3; KM3‑E3‑b) aus ihrem Ergebnissatz —
    /// eine Formel für Ergebnisreiter, Kennzahlenkatalog, Tafel, Export und KI-Sicht.
    /// </summary>
    public static class KaeltemaschineTeillastKennzahlen
    {
        /// <summary>Trägt der Satz die Werte des Teillastwegs (Maschine mit Weg)?</summary>
        public static bool MitWeg(ErgebnisKaeltemaschineModel k) => k != null && k.Taktstrom_MWh.HasValue;

        /// <summary>Die Kälte über den Verdichter [MWh/a] = Kälteproduktion − freie Kühlung.</summary>
        public static double VerdichterkaelteMwh(ErgebnisKaeltemaschineModel k)
            => k == null ? 0.0 : Math.Max(0.0, k.Kaelteproduktion_MWh - k.FreieKuehlung_MWh);

        /// <summary>Der Verdichterstrom [MWh/a] = Strom − Hilfsstrom (samt Zuschlag und Rückkühlung).</summary>
        public static double VerdichterstromMwh(ErgebnisKaeltemaschineModel k)
            => k == null ? 0.0 : Math.Max(0.0, k.Stromverbrauch_MWh - k.Hilfsstrom_MWh);

        /// <summary>
        /// Jahres-EER ohne Hilfsstrom (<c>kaelte.km.jaz_verdichter</c>) = Verdichterkälte / Verdichterstrom; <c>null</c>
        /// ohne Verdichterstrom.
        /// </summary>
        public static double? JazVerdichter(ErgebnisKaeltemaschineModel k)
        {
            double strom = VerdichterstromMwh(k);
            return strom > 0 ? VerdichterkaelteMwh(k) / strom : (double?)null;
        }

        /// <summary>
        /// Teillastanteil [%] (<c>kaelte.km.teillastanteil</c>) = Teillaststunden / Verdichterstunden · 100; <c>null</c> ohne
        /// eine der beiden Zahlen oder ohne Verdichterstunde.
        /// </summary>
        public static double? TeillastanteilProzent(int? teillaststunden, int? verdichterstunden)
            => teillaststunden.HasValue && verdichterstunden is int n && n > 0
                ? 100.0 * teillaststunden.Value / n : (double?)null;

        /// <summary>
        /// Die Kennzahlen eines Satzes unter den Spaltennamen des Ergebnisses als Schlüssel (CSV-Export, KI-Sicht):
        /// <c>Taktstrom_kWh</c>, <c>Starts</c>, <c>Teillastanteil_Prozent</c>, <c>Lastgrad_Mittel</c>,
        /// <c>EER_ohne_Hilfsstrom</c>; leer ohne Weg, ein nicht erhobener Wert fehlt.
        /// </summary>
        public static IReadOnlyList<KeyValuePair<string, double>> Schluesselwerte(ErgebnisKaeltemaschineModel k, int? verdichterstunden)
        {
            var l = new List<KeyValuePair<string, double>>();
            if (!MitWeg(k)) return l;
            l.Add(new KeyValuePair<string, double>("Taktstrom_kWh", k.Taktstrom_MWh.Value * 1000.0));
            if (k.Starts is int s) l.Add(new KeyValuePair<string, double>("Starts", s));
            if (TeillastanteilProzent(k.Teillaststunden, verdichterstunden) is double a)
                l.Add(new KeyValuePair<string, double>("Teillastanteil_Prozent", a));
            if (k.Lastgrad_Mittel is double g) l.Add(new KeyValuePair<string, double>("Lastgrad_Mittel", g));
            if (JazVerdichter(k) is double j) l.Add(new KeyValuePair<string, double>("EER_ohne_Hilfsstrom", j));
            return l;
        }
    }

    /// <summary>
    /// <b>Die Quelle der Lesewerte für den Bericht</b> (KM3‑E3‑b): die Projektkopien der Kältemaschinen eines Projekts,
    /// gelesen wie der Anlagendialog. Die Quellen des Vorlagenkatalogs lesen die Datenbank nie.
    /// </summary>
    internal static class KaeltemaschineTeillastBerichtsquelle
    {
        /// <summary>Die Lesewerte je Projektkopie (<c>ID_Kaeltemaschine</c>); leer ohne Kältemaschine. Wirft nicht.</summary>
        internal static Dictionary<int, KaeltemaschineTeillastLesewerte> Lade(int idProjekt)
        {
            var d = new Dictionary<int, KaeltemaschineTeillastLesewerte>();
            if (idProjekt <= 0) return d;
            try
            {
                foreach (int id in KaeltemaschineCtrl.IdsImProjektStill(idProjekt))
                {
                    KaeltemaschineTeillastLesewerte w = KaeltemaschineTeillastLesewerte.Aus(KaeltemaschineCtrl.LadenStill(id));
                    if (w != null) d[id] = w;
                }
            }
            catch (Exception)
            {
                d.Clear();
            }
            return d;
        }
    }
}
