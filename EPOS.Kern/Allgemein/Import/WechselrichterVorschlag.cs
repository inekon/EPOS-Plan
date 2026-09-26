using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>„Wechselrichter vorschlagen"</b> — die Bewertung JE KATALOGGERÄT für ein Modulfeld
    /// (Anwenderwunsch vom 26.09.2026). Sie sitzt auf der Auslegungshilfe
    /// <see cref="StrangAuslegung"/> und rechnet deren Regeln nicht nach: Die Aufteilung
    /// kommt aus <see cref="StrangAuslegung.Vorschlagen(PhotovoltaikModel, WechselrichterModel, int, double, double)"/>,
    /// die Grenzen aus <see cref="StrangAuslegung.Reihe(PhotovoltaikModel, WechselrichterModel, double, double)"/>
    /// und <see cref="StrangAuslegung.ModuleJeGeraet"/>, die Spannungen und Ströme aus
    /// <see cref="StrangPlausibilitaet"/>. Hier kommt nur hinzu, was die Klappliste nicht
    /// sagen konnte: eine BEWERTUNG in drei Stufen mit Grund und eine Rangfolge.
    ///
    /// <para><b>Die Stufen.</b></para>
    /// <list type="bullet">
    ///   <item><description><b>ungeeignet</b> — es gibt keine Aufteilung: Werte fehlen, keine
    ///     Reihenlänge passt ins Spannungsfenster (P1 bis P3), das Gerät ist zu klein (schon
    ///     die kürzeste zulässige Reihe überschreitet DC/AC 1,5 oder <c>P_DC_Max</c>) oder zu
    ///     groß (alle Module an einem Gerät bleiben unter DC/AC 1,0), schon ein Strang
    ///     überschreitet die Stromgrenze je Tracker (P4), oder das Feld lässt sich
    ///     auch mit Restmodulen nicht in gleich lange Stränge teilen.</description></item>
    ///   <item><description><b>bedingt</b> — es gibt eine Aufteilung, aber mit Abstrich:
    ///     DC/AC über <see cref="DCAC_GEEIGNET_MAX"/> (bis 1,5 lässt die Ampel es zu),
    ///     Restmodule, die keinen Strang finden, ein unbekanntes DC/AC (AC-Nennleistung fehlt)
    ///     oder Spannungsgrenzen des Geräts, die nicht alle gepflegt sind.</description></item>
    ///   <item><description><b>geeignet</b> — alles andere: alle Module untergebracht, alle
    ///     drei Spannungsgrenzen geprüft, DC/AC im Fenster
    ///     <see cref="DCAC_GEEIGNET_MIN"/>…<see cref="DCAC_GEEIGNET_MAX"/>.</description></item>
    /// </list>
    ///
    /// <para><b>Die Rangfolge</b> (deterministisch): geeignet vor bedingt vor ungeeignet;
    /// innerhalb einer Stufe DC/AC nahe dem Zielband
    /// <see cref="DCAC_ZIEL_MIN"/>…<see cref="DCAC_ZIEL_MAX"/> (Abstand 0 im Band), dann
    /// wenige Geräte, dann wenige Restmodule, zuletzt Name und Id (ordinal).</para>
    ///
    /// <para><b>Restmodule.</b> Geht die Modulzahl nicht in gleich lange Stränge und gleich
    /// belegte Geräte auf, sucht die Bewertung die GRÖSSTE Modulzahl darunter, die aufgeht —
    /// höchstens eine Reihenlänge weniger (mehr Module fallen nie weg: Mit einem Strang je
    /// Gerät geht jedes Vielfache einer zulässigen Reihe auf). Die fehlenden Module nennt der
    /// Grund; die Strangtabelle, die „Auslegung vorschlagen" danach füllt, rechnet mit der
    /// ganzen Modulzahl und meldet dann selbst.</para>
    ///
    /// <para>Ohne Datenbank: Die Kandidaten liefert die Hülle (Katalog, nach Hersteller
    /// gefiltert), die Temperaturen das Projekt.</para>
    /// </summary>
    public static class WechselrichterVorschlag
    {
        /// <summary>Untere Grenze des DC/AC-Fensters, in dem ein Gerät „geeignet" heißt (= Band der Ampel).</summary>
        public const double DCAC_GEEIGNET_MIN = StrangPlausibilitaet.DCAC_MIN;

        /// <summary>Obere Grenze des DC/AC-Fensters „geeignet"; darüber bis 1,5 „bedingt".</summary>
        public const double DCAC_GEEIGNET_MAX = 1.3;

        /// <summary>Unteres Ende des Zielbands, an dem die Rangfolge misst.</summary>
        public const double DCAC_ZIEL_MIN = 1.1;

        /// <summary>Oberes Ende des Zielbands.</summary>
        public const double DCAC_ZIEL_MAX = 1.2;

        /// <summary>Die drei Stufen der Bewertung, in der Reihenfolge des Rangs.</summary>
        public enum Eignung { Geeignet = 0, Bedingt = 1, Ungeeignet = 2 }

        /// <summary>Warum ein Gerät nicht (ganz) geeignet ist.</summary>
        public enum Grund
        {
            /// <summary>Kein Abstrich.</summary>
            Keiner = 0,
            // --- ungeeignet ---
            /// <summary>Modul oder Modulzahl fehlt.</summary>
            ModulFehlt,
            /// <summary>Weder Spannungswerte des Moduls noch Grenzen des Geräts prüfbar.</summary>
            WerteFehlen,
            /// <summary>Keine Reihenlänge hält U_oc(kalt) ≤ U_max und das MPP-Fenster ein.</summary>
            Spannungsfenster,
            /// <summary>Schon die kürzeste zulässige Reihe überschreitet DC/AC 1,5 oder P_DC_Max.</summary>
            GeraetZuKlein,
            /// <summary>Alle Module an einem Gerät bleiben unter DC/AC 1,0.</summary>
            GeraetZuGross,
            /// <summary>Schon EIN Strang überschreitet die Stromgrenze je Tracker (P4).</summary>
            StromZuHoch,
            /// <summary>Keine Aufteilung in gleich lange Stränge, auch nicht mit Restmodulen.</summary>
            KeineAufteilung,
            // --- bedingt ---
            /// <summary>DC/AC über <see cref="DCAC_GEEIGNET_MAX"/>.</summary>
            DcAcHoch,
            /// <summary>Module bleiben ohne Strang.</summary>
            Restmodule,
            /// <summary>AC-Nennleistung fehlt — kein DC/AC.</summary>
            DcAcUnbekannt,
            /// <summary>Nicht alle drei Spannungsgrenzen (U_max, U_mpp,min, U_mpp,max) gepflegt.</summary>
            GrenzenUnvollstaendig
        }

        /// <summary>Ein Gerät des Katalogs mit seiner Bewertung und seinen Kennzahlen.</summary>
        public sealed class Kandidat
        {
            /// <summary>Das Katalogmodell (Id = Stamm-Id).</summary>
            public WechselrichterModel Geraet;
            /// <summary>Die Stufe.</summary>
            public Eignung Stufe = Eignung.Ungeeignet;
            /// <summary>Alle Gründe, der gewichtigste zuerst; leer = keiner.</summary>
            public List<Grund> Gruende = new List<Grund>();
            /// <summary>Die Aufteilung (nur bei „geeignet"/„bedingt" möglich).</summary>
            public StrangAuslegung.Vorschlag Vorschlag;
            /// <summary>Module des Feldes.</summary>
            public int Modulzahl;
            /// <summary>Module, die keinen Strang finden.</summary>
            public int Restmodule;
            /// <summary>MPP-Tracker je Gerät (fehlt die Angabe: 1).</summary>
            public int Mppts = 1;
            /// <summary>Leerlaufspannung der Reihe im kalten Fall [V].</summary>
            public double? UocKalt;
            /// <summary>Grenze U_max des Geräts [V].</summary>
            public double? UDcMax;
            /// <summary>MPP-Spannung der Reihe im heißen Fall [V].</summary>
            public double? MppHeiss;
            /// <summary>MPP-Spannung der Reihe im kalten Fall [V].</summary>
            public double? MppKalt;
            /// <summary>MPP-Fenster des Geräts, unten [V].</summary>
            public double? UMppMin;
            /// <summary>MPP-Fenster des Geräts, oben [V].</summary>
            public double? UMppMax;
            /// <summary>Kurzschlussstrom am am stärksten belegten Tracker im heißen Fall [A].</summary>
            public double? StromJeMppt;
            /// <summary>Stromgrenze je Tracker (I_Sc_Max, sonst I_Dc_Max) [A].</summary>
            public double? IMaxJeMppt;
            /// <summary>Kürzeste zulässige Reihe (für den Grund „zu klein").</summary>
            public int ReiheMin;

            /// <summary>Der gewichtigste Grund; <see cref="Grund.Keiner"/> ohne Abstrich.</summary>
            public Grund Hauptgrund { get { return Gruende.Count > 0 ? Gruende[0] : Grund.Keiner; } }

            /// <summary>Das DC/AC-Verhältnis der Aufteilung; <c>null</c> ohne Aufteilung oder ohne AC-Nennleistung.</summary>
            public double? DcAc
            {
                get { return Vorschlag != null && Vorschlag.Moeglich && Vorschlag.DcAc > 0.0 ? Vorschlag.DcAc : (double?)null; }
            }

            /// <summary>Geräte der Aufteilung; 0 ohne Aufteilung.</summary>
            public int Geraete { get { return Vorschlag != null && Vorschlag.Moeglich ? Vorschlag.Geraete : 0; } }
        }

        // -----------------------------------------------------------------
        //  Bewerten
        // -----------------------------------------------------------------

        /// <summary>
        /// Alle Kandidaten für ein Modulfeld, beste zuerst (Rangfolge siehe Klasse).
        /// <c>null</c>-Einträge fallen weg, doppelte Ids zählen einmal.
        /// </summary>
        public static List<Kandidat> Bewerten(PhotovoltaikModel modul, int anzahlModule,
                                              double tKalt, double tHeiss,
                                              IEnumerable<WechselrichterModel> kandidaten)
        {
            var liste = new List<Kandidat>();
            if (kandidaten == null) return liste;
            var gesehen = new HashSet<int>();
            foreach (WechselrichterModel g in kandidaten)
            {
                if (g == null || !gesehen.Add(g.m_ID)) continue;
                liste.Add(Bewerte(modul, g, anzahlModule, tKalt, tHeiss));
            }
            liste.Sort(Vergleiche);
            return liste;
        }

        /// <summary>Die Bewertung EINES Geräts.</summary>
        public static Kandidat Bewerte(PhotovoltaikModel modul, WechselrichterModel geraet, int anzahlModule,
                                       double tKalt, double tHeiss)
        {
            var k = new Kandidat { Geraet = geraet, Modulzahl = anzahlModule };
            if (geraet == null) return k;
            k.Mppts = geraet.m_Anzahl_Mppt.HasValue && geraet.m_Anzahl_Mppt.Value >= 1 ? geraet.m_Anzahl_Mppt.Value : 1;
            k.UDcMax = Gesetzt(geraet.m_U_Dc_Max) ? geraet.m_U_Dc_Max : null;
            k.UMppMin = Gesetzt(geraet.m_U_Mpp_Min) ? geraet.m_U_Mpp_Min : null;
            k.UMppMax = Gesetzt(geraet.m_U_Mpp_Max) ? geraet.m_U_Mpp_Max : null;
            k.IMaxJeMppt = Gesetzt(geraet.m_I_Sc_Max) ? geraet.m_I_Sc_Max
                         : Gesetzt(geraet.m_I_Dc_Max) ? geraet.m_I_Dc_Max : null;

            if (modul == null || anzahlModule <= 0) return Ungeeignet(k, Grund.ModulFehlt);

            // --- Die Vorprüfungen: sie benennen den Grund genauer, als es die
            //     Aufteilung allein könnte.
            StrangAuslegung.Reihenbereich rb = StrangAuslegung.Reihe(modul, geraet, tKalt, tHeiss);
            if (!rb.Pruefbar) return Ungeeignet(k, Grund.WerteFehlen);
            if (!rb.Moeglich) return Ungeeignet(k, Grund.Spannungsfenster);
            k.ReiheMin = rb.Min;

            StrangAuslegung.Modulbereich mb = StrangAuslegung.ModuleJeGeraet(modul, geraet);
            if (mb.Max.HasValue && mb.Max.Value < rb.Min) return Ungeeignet(k, Grund.GeraetZuKlein);
            if (mb.Min.HasValue && anzahlModule < mb.Min.Value) return Ungeeignet(k, Grund.GeraetZuGross);

            // P4: Verträgt ein Tracker nicht einmal EINEN Strang, gibt es keine Aufteilung -
            // und der Grund ist der Strom, nicht die Teilbarkeit der Modulzahl.
            int? jeTrackerMax = StrangAuslegung.ParallelJeMppt(modul, geraet, tHeiss);
            if (jeTrackerMax.HasValue && jeTrackerMax.Value < 1)
            {
                k.StromJeMppt = StrangPlausibilitaet.StromJeStrang(modul, tHeiss);
                return Ungeeignet(k, Grund.StromZuHoch);
            }

            // --- Die Aufteilung: erst die ganze Modulzahl, dann höchstens eine
            //     Reihenlänge weniger.
            int reiheMax = rb.Max ?? anzahlModule;
            int tiefste = Math.Max(1, anzahlModule - Math.Max(0, reiheMax - 1));
            StrangAuslegung.Vorschlag v = null;
            for (int m = anzahlModule; m >= tiefste; m--)
            {
                StrangAuslegung.Vorschlag versuch = StrangAuslegung.Vorschlagen(modul, geraet, m, tKalt, tHeiss);
                if (versuch.Moeglich) { v = versuch; break; }
            }
            if (v == null) return Ungeeignet(k, Grund.KeineAufteilung);

            k.Vorschlag = v;
            k.Restmodule = anzahlModule - v.Reihe * v.Parallel * v.Geraete;

            // --- Die Kennzahlen der gewählten Reihe.
            k.UocKalt = StrangPlausibilitaet.UocKaltReihe(v.Reihe, modul.m_U_Leerlauf, modul.m_beta_OC, tKalt);
            k.MppHeiss = StrangPlausibilitaet.SpannungReihe(v.Reihe, modul.m_U_Mpp, modul.m_beta_OC, tHeiss);
            k.MppKalt = StrangPlausibilitaet.SpannungReihe(v.Reihe, modul.m_U_Mpp, modul.m_beta_OC, tKalt);
            int tracker = Math.Min(k.Mppts, Math.Max(1, v.Parallel));
            int jeTracker = (v.Parallel + tracker - 1) / tracker;
            double? jeStrang = StrangPlausibilitaet.StromJeStrang(modul, tHeiss);
            k.StromJeMppt = jeStrang.HasValue ? jeStrang.Value * jeTracker : (double?)null;

            // --- Die Abstriche.
            if (k.DcAc.HasValue && k.DcAc.Value > DCAC_GEEIGNET_MAX + 1e-9) k.Gruende.Add(Grund.DcAcHoch);
            if (k.Restmodule > 0) k.Gruende.Add(Grund.Restmodule);
            if (!k.DcAc.HasValue) k.Gruende.Add(Grund.DcAcUnbekannt);
            if (!rb.MaxUoc.HasValue || !rb.MinMpp.HasValue || !rb.MaxMpp.HasValue)
                k.Gruende.Add(Grund.GrenzenUnvollstaendig);

            k.Stufe = k.Gruende.Count == 0 ? Eignung.Geeignet : Eignung.Bedingt;
            return k;
        }

        private static Kandidat Ungeeignet(Kandidat k, Grund grund)
        {
            k.Stufe = Eignung.Ungeeignet;
            k.Gruende.Add(grund);
            return k;
        }

        /// <summary>Abstand des DC/AC-Verhältnisses zum Zielband; 0 im Band, unbekannt = sehr groß.</summary>
        public static double Zielabstand(Kandidat k)
        {
            if (k == null || !k.DcAc.HasValue) return double.MaxValue;
            double d = k.DcAc.Value;
            if (d < DCAC_ZIEL_MIN) return DCAC_ZIEL_MIN - d;
            if (d > DCAC_ZIEL_MAX) return d - DCAC_ZIEL_MAX;
            return 0.0;
        }

        /// <summary>Die Rangfolge (siehe Klasse) — deterministisch, ohne Kultur.</summary>
        public static int Vergleiche(Kandidat a, Kandidat b)
        {
            int c = ((int)a.Stufe).CompareTo((int)b.Stufe);
            if (c != 0) return c;
            if (a.Stufe != Eignung.Ungeeignet)
            {
                double da = Math.Round(Zielabstand(a), 6), db = Math.Round(Zielabstand(b), 6);
                c = da.CompareTo(db);
                if (c != 0) return c;
                c = a.Geraete.CompareTo(b.Geraete);
                if (c != 0) return c;
                c = a.Restmodule.CompareTo(b.Restmodule);
                if (c != 0) return c;
            }
            c = string.Compare(a.Geraet?.m_szName ?? "", b.Geraet?.m_szName ?? "", StringComparison.OrdinalIgnoreCase);
            if (c != 0) return c;
            c = string.CompareOrdinal(a.Geraet?.m_szName ?? "", b.Geraet?.m_szName ?? "");
            if (c != 0) return c;
            return (a.Geraet?.m_ID ?? 0).CompareTo(b.Geraet?.m_ID ?? 0);
        }

        // -----------------------------------------------------------------
        //  Die Texte der Vorschlagsliste (Kultur des Anwenders)
        // -----------------------------------------------------------------

        /// <summary>„geeignet", „bedingt" oder „ungeeignet".</summary>
        public static string StufeText(Eignung stufe)
        {
            switch (stufe)
            {
                case Eignung.Geeignet: return MyResource.Resource.PVS_WRV_GEEIGNET;
                case Eignung.Bedingt: return MyResource.Resource.PVS_WRV_BEDINGT;
                default: return MyResource.Resource.PVS_WRV_UNGEEIGNET;
            }
        }

        /// <summary>Alle Gründe als ein Satz, mit „ · " getrennt; leer ohne Abstrich.</summary>
        public static string GrundText(Kandidat k)
        {
            if (k == null) return "";
            var teile = new List<string>();
            foreach (Grund g in k.Gruende) teile.Add(GrundText(k, g));
            return string.Join(MyResource.Resource.PVS_TRENNER, teile);
        }

        private static string GrundText(Kandidat k, Grund g)
        {
            CultureInfo c = CultureInfo.CurrentCulture;
            switch (g)
            {
                case Grund.ModulFehlt: return MyResource.Resource.PVS_WRV_GRUND_MODUL;
                case Grund.WerteFehlen: return MyResource.Resource.PVS_WRV_GRUND_WERTE;
                case Grund.Spannungsfenster: return MyResource.Resource.PVS_WRV_GRUND_SPANNUNG;
                case Grund.GeraetZuKlein:
                    return string.Format(c, MyResource.Resource.PVS_WRV_GRUND_KLEIN, Ganz(k.ReiheMin));
                case Grund.GeraetZuGross:
                    return string.Format(c, MyResource.Resource.PVS_WRV_GRUND_GROSS, Ganz(k.Modulzahl));
                case Grund.StromZuHoch:
                    if (!k.StromJeMppt.HasValue || !k.IMaxJeMppt.HasValue)
                        return MyResource.Resource.PVS_WRV_GRUND_STROM_OHNE;
                    return string.Format(c, MyResource.Resource.PVS_WRV_GRUND_STROM,
                                         Einstellig(k.StromJeMppt.Value), Einstellig(k.IMaxJeMppt.Value));
                case Grund.KeineAufteilung: return MyResource.Resource.PVS_WRV_GRUND_AUFTEILUNG;
                case Grund.DcAcHoch:
                    return string.Format(c, MyResource.Resource.PVS_WRV_GRUND_DCAC_HOCH,
                                         Komma(k.DcAc ?? 0.0), Komma(DCAC_GEEIGNET_MAX));
                case Grund.Restmodule:
                    return string.Format(c, MyResource.Resource.PVS_WRV_GRUND_REST, Ganz(k.Restmodule));
                case Grund.DcAcUnbekannt: return MyResource.Resource.PVS_WRV_GRUND_OHNE_DCAC;
                case Grund.GrenzenUnvollstaendig: return MyResource.Resource.PVS_WRV_GRUND_GRENZEN;
                default: return "";
            }
        }

        /// <summary>„1,15" — leer ohne DC/AC.</summary>
        public static string DcAcText(Kandidat k)
        {
            return k != null && k.DcAc.HasValue ? Komma(k.DcAc.Value) : "";
        }

        /// <summary>„2 × (1 × 10)" = Geräte × (Stränge je Gerät × Module in Reihe); leer ohne Aufteilung.</summary>
        public static string AufteilungText(Kandidat k)
        {
            if (k == null || k.Geraete == 0) return "";
            return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_WRV_AUFTEILUNG,
                                 Ganz(k.Vorschlag.Geraete), Ganz(k.Vorschlag.Parallel), Ganz(k.Vorschlag.Reihe));
        }

        /// <summary>„548 ≤ 600 V" — U_oc der Reihe im kalten Fall gegen U_max; leer ohne Wert.</summary>
        public static string UocText(Kandidat k)
        {
            if (k == null || !k.UocKalt.HasValue) return "";
            if (!k.UDcMax.HasValue) return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_WRV_VOLT, Volt(k.UocKalt.Value));
            return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_WRV_UOC,
                                 Volt(k.UocKalt.Value), Volt(k.UDcMax.Value));
        }

        /// <summary>„363…471 V in 175…500 V" — die MPP-Spannung heiß…kalt im Fenster des Geräts.</summary>
        public static string MppText(Kandidat k)
        {
            if (k == null || !k.MppHeiss.HasValue || !k.MppKalt.HasValue) return "";
            string lage = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_WRV_SPANNE,
                                        Volt(k.MppHeiss.Value), Volt(k.MppKalt.Value));
            if (!k.UMppMin.HasValue && !k.UMppMax.HasValue) return lage;
            string fenster = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_WRV_SPANNE,
                                           k.UMppMin.HasValue ? Volt(k.UMppMin.Value) : "–",
                                           k.UMppMax.HasValue ? Volt(k.UMppMax.Value) : "–");
            return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_WRV_MPP, lage, fenster);
        }

        private static bool Gesetzt(double? wert) { return wert.HasValue && wert.Value > 0.0; }
        private static string Ganz(int wert) { return wert.ToString("N0", CultureInfo.CurrentCulture); }
        private static string Komma(double wert) { return wert.ToString("N2", CultureInfo.CurrentCulture); }
        private static string Volt(double wert) { return wert.ToString("N0", CultureInfo.CurrentCulture); }
        private static string Einstellig(double wert) { return wert.ToString("N1", CultureInfo.CurrentCulture); }
    }
}
