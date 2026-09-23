using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Eingang des Laufs</b> (Umsetzungskonzept Zapfprofilgenerator 2.3, 3.3, Stufe Z1,
    /// Gruppe 2): Aus dem gespeicherten Arbeitsstand — oder dem Arbeitsstand eines Dialogs —,
    /// dem Kalender der Klimaregion, dem Parametersatz der aktuellen Katalogversion, der
    /// Belegung je Raumzahl aus <c>Tab_TwwDin4708Wert_STAMM</c>, den eigenen Tagesgangsätzen
    /// der Zonen und den Angaben der gebundenen Gebäude (A8) entsteht der
    /// <see cref="Zapfprofileingang"/>; <see cref="Rechnen(int, ZapfprofilStand, int, bool[])"/>
    /// rechnet ihn mit dem Katalog.
    ///
    /// <para><b>Benannte Ablehnung statt Rückfall (2.2, 3.3).</b> Fehlen die Tww-Tabellen,
    /// wirft der Eingang <see cref="ZapfprofilEingabeException"/> mit
    /// <see cref="ZapfEingabefehler.NichtVerfuegbar"/>; fehlt die Katalogversion der Parameter,
    /// <see cref="ParametersatzException"/> (<see cref="Parameter()"/>). Was eine einzelne
    /// Zone nicht rechenbar macht (Nutzungsart, Parameter, Bezugsmenge …), lehnt der
    /// Rechenweg je Zone benannt ab.</para>
    ///
    /// <para><b>Gebundenes Gebäude (A8): nur vorbelegen.</b> Eine Zone mit <c>ID_Gebaeude</c>
    /// übernimmt die Ferienzeiten des Gebäudes nur, wenn sie selbst keine trägt und das
    /// Gebäude seine Ferien führt (<c>Ferien</c> über der Schwelle des Gebäudemodells), und
    /// seine Fläche (<c>Wohnflaeche_gesamt</c>, sonst <c>Nutzflaeche</c>) nur, wenn sie
    /// selbst keine trägt (<see cref="Mengengeruest.HatEigeneFlaeche"/>). Mehrere solche Zonen
    /// an einem Gebäude teilen seine Fläche zu gleichen Teilen — sie zählt einmal. Gespeichert
    /// wird davon nichts.</para>
    /// </summary>
    internal static partial class ZapfprofilCtrl
    {
        /// <summary>Die Tage, die eine Ferienangabe als „keine Angabe" kennzeichnen (Bestand, <c>Ferienzeit</c>).</summary>
        private const int FERIEN_KEINE_ANGABE_NULL = 0;
        private const int FERIEN_KEINE_ANGABE_366 = 366;

        /// <summary>Der Eingang des Laufs aus dem GESPEICHERTEN Arbeitsstand (<see cref="Lies"/>).</summary>
        internal static Zapfprofileingang Eingang(int idProjekt, int wochentagJan1, bool[] we)
            => Eingang(idProjekt, Lies(idProjekt), wochentagJan1, we, Katalog());

        /// <summary>
        /// Der Eingang aus einem Arbeitsstand — dem gespeicherten oder dem eines Dialogs
        /// (Vorschau gleich Lauf, 2.4). <paramref name="katalog"/> wird für die Flächenregel
        /// des gebundenen Gebäudes gebraucht; <c>null</c> liest ihn.
        /// </summary>
        internal static Zapfprofileingang Eingang(int idProjekt, ZapfprofilStand stand, int wochentagJan1, bool[] we,
                                                  IReadOnlyList<Nutzungsart> katalog = null)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));

            ZapfVerfuegbarkeit verfuegbar = Verfuegbar();
            if (!verfuegbar.Ja && verfuegbar.Grund == ZapfVerfuegbarkeitsgrund.TabellenFehlen)
                throw new ZapfprofilEingabeException(ZapfEingabefehler.NichtVerfuegbar, "", verfuegbar.Klartext);

            // Fehlt die Katalogversion, wirft Parameter() benannt (KeineKatalogversion).
            Parametersatz ps = Parameter();
            katalog ??= Katalog();

            IReadOnlyList<ZonenStand> zonen = MitGebaeude(stand.Zonen ?? new ZonenStand[0], katalog);

            return new Zapfprofileingang
            {
                Zonen = zonen,
                Projekt = stand.Projekt ?? ProjektVorgabe(),
                WochentagJan1 = wochentagJan1,
                We = we,
                Parameter = ps,
                Tagesgangsaetze = EigeneSaetze(zonen),
                BelegungJeRaumzahl = Belegung(ps.Katalogversion)
            };
        }

        /// <summary>
        /// Rechnet den Generatorweg eines Projekts über einen Arbeitsstand — derselbe Aufruf im
        /// Lauf (<c>SimulationWaermebedarf</c>, gespeicherter Stand) und in der Vorschau
        /// (Stand des Dialogs). Der Katalog wird einmal gelesen.
        /// </summary>
        internal static ZapfprofilErgebnis Rechnen(int idProjekt, ZapfprofilStand stand, int wochentagJan1, bool[] we)
        {
            IReadOnlyList<Nutzungsart> katalog = Katalog();
            Zapfprofileingang e = Eingang(idProjekt, stand, wochentagJan1, we, katalog);
            return ZapfprofilRechner.Rechnen(e, katalog);
        }

        // =================================================================================
        // Projektgrößen ohne gespeicherte Zeile: die Vorgaben der DDL
        // =================================================================================

        /// <summary>
        /// Die Projektgrößen, wie sie eine neue Zeile in <c>Tab_TwwProjekt</c> bekäme — gelesen
        /// aus den DDL-Vorgaben (<c>pragma_table_info</c>), damit es keine zweite Abschrift der
        /// Vorgabewerte im Quelltext gibt (<see cref="ZapfprofilStand"/>). Spalten ohne Vorgabe
        /// bleiben <c>null</c> (= Vorgabe des Verfahrens). <c>null</c> ohne Tabelle.
        /// </summary>
        internal static ProjektStand ProjektVorgabe()
        {
            if (!DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_PROJEKT)) return null;

            DataTable info = DataRepository.GetDataTable(
                "SELECT name, dflt_value FROM pragma_table_info(?) ORDER BY cid",
                new DbParam("@tabelle", TwwSchema.TAB_TWW_PROJEKT));
            if (info == null || info.Rows.Count == 0) return null;

            var zeile = new DataTable();
            foreach (DataRow r in info.Rows) zeile.Columns.Add(Text(r, "name"), typeof(object));
            DataRow vorgabe = zeile.NewRow();
            foreach (DataRow r in info.Rows)
            {
                string d = TextOderNull(r, "dflt_value");
                vorgabe[Text(r, "name")] = d == null ? DBNull.Value : (object)VorgabeWert(d);
            }
            return ProjektAus(vorgabe);
        }

        /// <summary>Ein DDL-Vorgabewert: <c>'TEXT'</c> ohne Anführung, sonst die Zahl als Text (invariant gelesen).</summary>
        private static string VorgabeWert(string d)
        {
            d = d.Trim();
            if (d.Length >= 2 && d[0] == '\'' && d[d.Length - 1] == '\'')
                return d.Substring(1, d.Length - 2).Replace("''", "'");
            return d;
        }

        // =================================================================================
        // Gebundenes Gebäude (A8)
        // =================================================================================

        /// <summary>
        /// Die Zonen mit den Vorbelegungen ihres gebundenen Gebäudes: Ferien, wenn die Zone
        /// keine trägt, und die Fläche, geteilt unter den Zonen ohne eigene Fläche.
        /// </summary>
        private static IReadOnlyList<ZonenStand> MitGebaeude(IReadOnlyList<ZonenStand> zonen,
                                                             IReadOnlyList<Nutzungsart> katalog)
        {
            var gebaeude = new Dictionary<int, GebaeudeAngaben>();
            var flaechenteiler = new Dictionary<int, int>();
            foreach (ZonenStand z in zonen)
            {
                if (z?.IdGebaeude == null) continue;
                int g = z.IdGebaeude.Value;
                if (!gebaeude.ContainsKey(g)) gebaeude[g] = GebaeudeLesen(g);
                if (!Mengengeruest.HatEigeneFlaeche(z, Suchen(katalog, z.IdNutzungsart)))
                    flaechenteiler[g] = (flaechenteiler.TryGetValue(g, out int n) ? n : 0) + 1;
            }
            if (gebaeude.Count == 0) return zonen;

            var ergebnis = new List<ZonenStand>(zonen.Count);
            foreach (ZonenStand z in zonen)
            {
                if (z?.IdGebaeude == null || !gebaeude.TryGetValue(z.IdGebaeude.Value, out GebaeudeAngaben a) || a == null)
                {
                    ergebnis.Add(z);
                    continue;
                }
                ZonenStand neu = z;
                if (a.FerienAktiv && !FerienGesetzt(z))
                    neu = neu with { Ferienbeginn = (int?[])a.Ferienbeginn.Clone(), Ferienende = (int?[])a.Ferienende.Clone() };
                if (a.FlaecheM2.HasValue && flaechenteiler.TryGetValue(z.IdGebaeude.Value, out int teiler) && teiler > 0
                    && !Mengengeruest.HatEigeneFlaeche(z, Suchen(katalog, z.IdNutzungsart)))
                    neu = neu with { GebaeudeflaecheM2 = a.FlaecheM2.Value / teiler };
                ergebnis.Add(neu);
            }
            return ergebnis;
        }

        private sealed class GebaeudeAngaben
        {
            internal double? FlaecheM2;
            internal bool FerienAktiv;
            internal int?[] Ferienbeginn = new int?[4];
            internal int?[] Ferienende = new int?[4];
        }

        /// <summary>Fläche und Ferien eines Gebäudes aus <c>Tab_Gebaeude</c>; <c>null</c>, wenn es die Zeile nicht gibt.</summary>
        private static GebaeudeAngaben GebaeudeLesen(int idGebaeude)
        {
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM Tab_Gebaeude WHERE ID = ?",
                                                       new DbParam("@id", idGebaeude));
            if (dt == null || dt.Rows.Count == 0) return null;
            DataRow r = dt.Rows[0];

            var a = new GebaeudeAngaben();
            double? wohn = dt.Columns.Contains("Wohnflaeche_gesamt") ? ZahlOderNull(r, "Wohnflaeche_gesamt") : null;
            double? nutz = dt.Columns.Contains("Nutzflaeche") ? ZahlOderNull(r, "Nutzflaeche") : null;
            if (wohn.HasValue && wohn.Value > 0 && !double.IsInfinity(wohn.Value)) a.FlaecheM2 = wohn.Value;
            else if (nutz.HasValue && nutz.Value > 0 && !double.IsInfinity(nutz.Value)) a.FlaecheM2 = nutz.Value;

            double? ferien = dt.Columns.Contains("Ferien") ? ZahlOderNull(r, "Ferien") : null;
            a.FerienAktiv = ferien.HasValue && ferien.Value > GebaeudeFestwerte.FERIEN_FLAG_SCHWELLE;
            for (int i = 0; i < 4; i++)
            {
                string n = (i + 1).ToString(CultureInfo.InvariantCulture);
                a.Ferienbeginn[i] = Jahrestag(dt, r, "Ferienbeginn_" + n);
                a.Ferienende[i] = Jahrestag(dt, r, "Ferienende_" + n);
            }
            return a;
        }

        /// <summary>Ein Jahrestag des Gebäudes, gerundet und auf 0 … 366 begrenzt (Wertemenge der Zonenspalten); fehlend = <c>null</c>.</summary>
        private static int? Jahrestag(DataTable dt, DataRow r, string spalte)
        {
            if (!dt.Columns.Contains(spalte)) return null;
            double? v = ZahlOderNull(r, spalte);
            if (!v.HasValue || double.IsNaN(v.Value) || double.IsInfinity(v.Value)) return null;
            double t = Math.Round(v.Value, MidpointRounding.AwayFromZero);
            return (int)Math.Min(FERIEN_KEINE_ANGABE_366, Math.Max(FERIEN_KEINE_ANGABE_NULL, t));
        }

        /// <summary>Trägt die Zone eine eigene Ferienangabe (ein Wert außer null, 0 und 366)?</summary>
        private static bool FerienGesetzt(ZonenStand z)
        {
            foreach (int?[] reihe in new[] { z.Ferienbeginn, z.Ferienende })
                if (reihe != null)
                    foreach (int? t in reihe)
                        if (t.HasValue && t.Value != FERIEN_KEINE_ANGABE_NULL && t.Value != FERIEN_KEINE_ANGABE_366)
                            return true;
            return false;
        }

        private static Nutzungsart Suchen(IReadOnlyList<Nutzungsart> katalog, int id)
        {
            if (katalog == null) return null;
            foreach (Nutzungsart n in katalog) if (n != null && n.Id == id) return n;
            return null;
        }

        // =================================================================================
        // Tagesgangsätze und Belegung
        // =================================================================================

        /// <summary>Die eigenen Tagesgangsätze der Zonen (<c>Tab_TwwZone.ID_Tagesgangsatz</c>), je Satz einmal gelesen.</summary>
        private static IReadOnlyList<Tagesgangsatz> EigeneSaetze(IReadOnlyList<ZonenStand> zonen)
        {
            var ids = new SortedSet<int>();
            foreach (ZonenStand z in zonen)
                if (z?.IdTagesgangsatz != null) ids.Add(z.IdTagesgangsatz.Value);
            var saetze = new List<Tagesgangsatz>();
            if (ids.Count == 0 || !KatalogtabellenVorhanden()) return saetze;
            foreach (int id in ids)
                if (SaetzeNachId(id).TryGetValue(id, out Tagesgangsatz s)) saetze.Add(s);
            return saetze;
        }

        /// <summary>
        /// Die Belegung (Personen) je Raumzahl aus <c>Tab_TwwDin4708Wert_STAMM</c> (Art
        /// <c>BELEGUNG</c>) der Katalogversion des Parametersatzes; <c>null</c>, wenn sie keine trägt.
        /// </summary>
        internal static IReadOnlyDictionary<string, double> Belegung(string katalogversion)
        {
            if (string.IsNullOrEmpty(katalogversion)
                || !DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_DIN4708_WERT_STAMM)) return null;

            DataTable dt = DataRepository.GetDataTable(
                "SELECT Schluessel, Wert FROM " + TwwSchema.TAB_TWW_DIN4708_WERT_STAMM +
                " WHERE Art = ? AND Katalogversion = ? ORDER BY Schluessel",
                new DbParam("@art", TwwSchema.DIN4708_ART_BELEGUNG), new DbParam("@version", katalogversion));
            if (dt == null || dt.Rows.Count == 0) return null;

            var belegung = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (DataRow r in dt.Rows) belegung[Text(r, "Schluessel")] = Zahl(r, "Wert");
            return belegung;
        }
    }
}
