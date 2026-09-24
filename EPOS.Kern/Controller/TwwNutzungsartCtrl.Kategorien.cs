using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Zapfkategorien einer Nutzungsart für den Editor der Stufe Experte (Umsetzungskonzept
    /// Zapfprofilgenerator 4.4, 5.1; Stufe Z4): die Kategorien in der Reihenfolge des Katalogs, die
    /// Summe der Anteile, der Hinweis bei Σ ≠ 1 und die Sperre der Nutzungsart — frei
    /// (<see cref="TwwKatalogAusgang.Ausgefuehrt"/>: Speichern an Ort und Stelle) oder gesperrt
    /// (Auslieferung bzw. benutzt: Speichern nur als neue Katalogversion).
    /// </summary>
    internal sealed record TwwKategorienStand(int IdNutzungsart, IReadOnlyList<Zapfkategorie> Kategorien, double SummeAnteil,
                                              ZapfSatz Hinweis, TwwKatalogAusgang Sperre)
    {
        /// <summary>Speichert <see cref="TwwNutzungsartCtrl.KategorienSpeichern"/> an Ort und Stelle?</summary>
        public bool Frei => Sperre == TwwKatalogAusgang.Ausgefuehrt;
    }

    /// <summary>
    /// Ergebnis von <see cref="TwwNutzungsartCtrl.KategorienSpeichern"/>: Ausgang, die Nutzungsart,
    /// die jetzt die Kategorien trägt (dieselbe oder die per „Speichern unter" neue — dann
    /// <see cref="NeueZeile"/>), und bei <see cref="TwwKatalogAusgang.RasterUngueltig"/> der
    /// verletzte Satz der Regeln (<see cref="Zapfkategoriensatz.Pruefen"/>).
    /// </summary>
    internal sealed record TwwKategorienErgebnis(TwwKatalogAusgang Ausgang, int IdNutzungsart, ZapfSatz Grund)
    {
        /// <summary>Ist die Aktion geschrieben (oder war nichts zu schreiben)?</summary>
        public bool Ok => Ausgang == TwwKatalogAusgang.Ausgefuehrt;

        /// <summary>Ist eine neue Nutzungsart (Katalogkopie) entstanden?</summary>
        public bool NeueZeile { get; init; }
    }

    /// <summary>
    /// <b>Kategorien als Katalogkopie</b> (Umsetzungskonzept Zapfprofilgenerator 4.4: „Im
    /// Experten-Modus sind Kategorien und σ als Katalogkopie bearbeitbar (Status EIGEN)"; 3.2; Stufe
    /// Z4, Gruppe 1). Dieselbe Sperrregel wie Tagesgang und Nutzungsart: Eine Nutzungsart der
    /// Auslieferung (auch über eine ausgelieferte Kategorie) oder eine, die eine Zone benutzt, ist
    /// unveränderlich — geänderte Kategorien ergeben dann per „Speichern unter" eine NEUE
    /// Nutzungsart (Katalogversion des Aufrufers, <c>ID_Vorlage</c> auf die alte, Status
    /// <c>EIGEN</c>, Werte und Tagesgangsatz der alten) mit den neuen Kategorien; die alte bleibt
    /// unberührt. Eine freie Nutzungsart trägt die neuen Kategorien an Ort und Stelle, ihr
    /// Freigabevermerk entfällt.
    ///
    /// <para><b>Provenienz je Kategorie</b> gegen den Bezug (die gespeicherte Kategorie gleichen
    /// Namens, sonst die des Vorgabesatzes): gleiche Werte behalten Provenienz und Beleg; geänderte
    /// Werte tragen Herkunftsart <c>EIGENKONSTRUKTION</c> mit neutraler Quelle und der
    /// Katalogversion der Zeile, solange der Entwurf die Provenienz des Bezugs unverändert
    /// mitbringt; eine neue Kategorie ohne Bezug ist Eigenkonstruktion. Alle geschriebenen
    /// Kategorien tragen Status <c>EIGEN</c> und <c>ReadOnly = 0</c>; die Reihenfolge ist die des
    /// Entwurfs.</para>
    /// </summary>
    internal static partial class TwwNutzungsartCtrl
    {
        private const string SQL_KATEGORIE_INSERT =
            "INSERT INTO " + TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM + " (ID_Nutzungsart, Kategorie, Reihenfolge, " +
            "Volumenstrom_l_min, Dauer_min, Anteil, Sigma, Kappung_l_min, Quelle, Ausgabe, Version, Herkunftsart, " +
            "Status, Beleg, ReadOnly) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 0)";

        private const string SQL_KATEGORIEN_LESEN =
            "SELECT Kategorie, Volumenstrom_l_min, Sigma, Dauer_min, Anteil, Kappung_l_min, Quelle, Ausgabe, Version, " +
            "Herkunftsart, Beleg FROM " + TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM + " WHERE ID_Nutzungsart = ? ORDER BY Reihenfolge, ID";

        /// <summary>
        /// Die Kategorien der Nutzungsart <paramref name="idNutzungsart"/> für den Editor samt Summe,
        /// Hinweis und Sperre. Ohne Tabellen (Stand vor Schritt 115) leer mit
        /// <see cref="TwwKatalogAusgang.TabellenFehlen"/>; eine fehlende Nutzungsart
        /// <see cref="TwwKatalogAusgang.NichtGefunden"/>.
        /// </summary>
        internal static TwwKategorienStand KategorienLesen(int idNutzungsart)
        {
            if (!TabellenVorhanden() || !DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM))
                return new TwwKategorienStand(idNutzungsart, new Zapfkategorie[0], 0.0, null, TwwKatalogAusgang.TabellenFehlen);
            if (Lies(idNutzungsart) == null)
                return new TwwKategorienStand(idNutzungsart, new Zapfkategorie[0], 0.0, null, TwwKatalogAusgang.NichtGefunden);

            IReadOnlyList<Zapfkategorie> k = ZapfprofilCtrl.Zapfkategorien(new[] { idNutzungsart });
            TwwKatalogAusgang sperre = IstReadOnly(idNutzungsart) ? TwwKatalogAusgang.ReadOnlyGesperrt
                                     : IstBenutzt(idNutzungsart) ? TwwKatalogAusgang.BenutztGesperrt
                                     : TwwKatalogAusgang.Ausgefuehrt;
            return new TwwKategorienStand(idNutzungsart, k, Zapfkategoriensatz.SummeAnteil(k), Zapfkategoriensatz.Summenhinweis(k), sperre);
        }

        /// <summary>
        /// <b>Der Vorgabesatz</b> (N12 (p)): die frei verfügbaren Kategorien (Herkunftsart
        /// <c>FREI</c>, der Paketteil unter <c>Referenzlaeufe/Katalogpaket_frei/</c>), wie sie beim
        /// Einspielen an die Nutzungsarten gebunden wurden — die der kleinsten Nutzungsart, die sie
        /// führt, in deren Reihenfolge und mit Provenienz, ohne Bindung (<c>IdNutzungsart</c> 0).
        /// Startwerte des Editors, wenn eine Nutzungsart keine Kategorien führt; leer, wenn der
        /// Katalog keinen Vorgabesatz trägt.
        /// </summary>
        internal static IReadOnlyList<Zapfkategorie> KategorienVorgabe()
        {
            var liste = new List<Zapfkategorie>();
            if (!DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM)) return liste;
            string frei = TwwWertemengen.Text(Herkunftsart.Frei);
            DataTable dt = DataRepository.GetDataTable(
                "SELECT Kategorie, Volumenstrom_l_min, Sigma, Dauer_min, Anteil, Kappung_l_min, Quelle, Ausgabe, Version, " +
                "Herkunftsart FROM " + TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM + " WHERE Herkunftsart = ? AND ID_Nutzungsart = " +
                "(SELECT MIN(ID_Nutzungsart) FROM " + TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM + " WHERE Herkunftsart = ?) " +
                "ORDER BY Reihenfolge, ID",
                new DbParam("@frei", frei), new DbParam("@frei2", frei));
            if (dt == null) return liste;
            foreach (DataRow r in dt.Rows) liste.Add(Kategorie(r, 0));
            return liste;
        }

        /// <summary>
        /// <b>Speichert die Kategorien</b> <paramref name="kategorien"/> (Reihenfolge = Position) an
        /// der Nutzungsart <paramref name="idNutzungsart"/> — in EINEM Vorgang, nach den Regeln von
        /// <see cref="Zapfkategoriensatz.Pruefen"/> (sonst <see cref="TwwKatalogAusgang.RasterUngueltig"/>
        /// mit dem Satz). Ist die Nutzungsart gesperrt, entsteht sie per „Speichern unter" neu in der
        /// Katalogversion <paramref name="katalogversion"/> (Pflicht, sonst
        /// <see cref="TwwKatalogAusgang.EntwurfUnvollstaendig"/>; vergeben:
        /// <see cref="TwwKatalogAusgang.NameBelegt"/>). Sind Namen, Reihenfolge und Werte gleich,
        /// wird nichts geschrieben.
        /// </summary>
        internal static TwwKategorienErgebnis KategorienSpeichern(int idNutzungsart, IReadOnlyList<Zapfkategorie> kategorien,
                                                                  string katalogversion = null)
        {
            if (!TabellenVorhanden() || !DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM))
                return new TwwKategorienErgebnis(TwwKatalogAusgang.TabellenFehlen, idNutzungsart, null);
            ZapfSatz grund = Zapfkategoriensatz.Pruefen(kategorien);
            if (grund != null) return new TwwKategorienErgebnis(TwwKatalogAusgang.RasterUngueltig, idNutzungsart, grund);

            string neueVersion = string.IsNullOrWhiteSpace(katalogversion) ? null : katalogversion.Trim();
            IReadOnlyList<Zapfkategorie> vorgabe = KategorienVorgabe();
            int ziel = idNutzungsart;
            bool neu = false;

            TwwKatalogErgebnis erg = Ausfuehren(idNutzungsart, v =>
            {
                TwwNutzungsartEntwurf bezug = Bezugszeile(v, idNutzungsart);
                if (bezug == null) return TwwKatalogAusgang.NichtGefunden;
                List<(Zapfkategorie Kategorie, string Beleg)> alt = KategorienImVorgang(v, idNutzungsart);
                if (Unveraendert(alt, kategorien)) return TwwKatalogAusgang.Ausgefuehrt;

                bool gesperrt = Sperre(v, idNutzungsart) != TwwKatalogAusgang.Ausgefuehrt;
                if (gesperrt && neueVersion == null) return TwwKatalogAusgang.EntwurfUnvollstaendig;
                string version = gesperrt ? neueVersion : bezug.Katalogversion;

                if (gesperrt)
                {
                    if (NameVergeben(v, bezug.Bezeichner, neueVersion, null)) return TwwKatalogAusgang.NameBelegt;
                    List<DbParam> p = Fachwerte(bezug with { Katalogversion = neueVersion });
                    p.Add(new DbParam("@vorlage", idNutzungsart));
                    p.Add(new DbParam("@status", TwwWertemengen.Text(ZapfKatalogstatus.Eigen)));
                    // Die Werte der Nutzungsart sind unverändert — ihr interner Beleg kommt mit.
                    p.Add(new DbParam("@beleg", idNutzungsart));
                    ziel = v.EinfuegenUndId(SQL_INSERT, p.ToArray());
                    neu = true;
                }
                else
                {
                    v.Ausfuehren("DELETE FROM " + TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM + " WHERE ID_Nutzungsart = ?",
                                 new DbParam("@id", idNutzungsart));
                    v.Ausfuehren("UPDATE " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + " SET Freigabe = NULL WHERE ID = ?",
                                 new DbParam("@id", idNutzungsart));
                }

                for (int i = 0; i < kategorien.Count; i++)
                {
                    Zapfkategorie k = kategorien[i];
                    (Provenienz herkunft, string beleg) = KategorieHerkunft(k, alt, vorgabe, version);
                    v.Ausfuehren(SQL_KATEGORIE_INSERT,
                        new DbParam("@n", ziel), new DbParam("@k", k.Name.Trim()), new DbParam("@r", i + 1),
                        new DbParam("@v", k.VolumenstromLJeMin), new DbParam("@d", k.DauerMin), new DbParam("@a", k.Anteil),
                        new DbParam("@s", k.StreuungLJeMin), new DbParam("@kap", k.KappungLJeMin.HasValue ? (object)k.KappungLJeMin.Value : null),
                        new DbParam("@q", herkunft.Quelle), new DbParam("@aus", herkunft.Ausgabe), new DbParam("@ver", herkunft.Version),
                        new DbParam("@art", TwwWertemengen.Text(herkunft.Art)),
                        new DbParam("@st", TwwWertemengen.Text(ZapfKatalogstatus.Eigen)), new DbParam("@beleg", beleg));
                }
                return TwwKatalogAusgang.Ausgefuehrt;
            });

            return erg.Ok
                ? new TwwKategorienErgebnis(TwwKatalogAusgang.Ausgefuehrt, ziel, null) { NeueZeile = neu }
                : new TwwKategorienErgebnis(erg.Ausgang, idNutzungsart, null);
        }

        // =================================================================================

        /// <summary>Die gespeicherten Kategorien samt internem Beleg — im laufenden Vorgang gelesen.</summary>
        private static List<(Zapfkategorie Kategorie, string Beleg)> KategorienImVorgang(DbVorgang v, int idNutzungsart)
        {
            var liste = new List<(Zapfkategorie, string)>();
            DataTable dt = v.Lese(SQL_KATEGORIEN_LESEN, new DbParam("@id", idNutzungsart));
            if (dt == null) return liste;
            foreach (DataRow r in dt.Rows)
                liste.Add((Kategorie(r, idNutzungsart), r["Beleg"] == DBNull.Value ? null : Convert.ToString(r["Beleg"], CultureInfo.InvariantCulture)));
            return liste;
        }

        private static Zapfkategorie Kategorie(DataRow r, int idNutzungsart)
            => new Zapfkategorie(idNutzungsart, ZapfprofilCtrl.Text(r, "Kategorie"), ZapfprofilCtrl.Zahl(r, "Volumenstrom_l_min"),
                                 ZapfprofilCtrl.Zahl(r, "Sigma"), ZapfprofilCtrl.Ganz(r, "Dauer_min"), ZapfprofilCtrl.Zahl(r, "Anteil"),
                                 ZapfprofilCtrl.Herkunft(r, ""))
            {
                KappungLJeMin = ZapfprofilCtrl.ZahlOderNull(r, "Kappung_l_min")
            };

        /// <summary>Gleiche Namen in gleicher Reihenfolge mit gleichen Werten — dann ist nichts zu schreiben.</summary>
        private static bool Unveraendert(List<(Zapfkategorie Kategorie, string Beleg)> alt, IReadOnlyList<Zapfkategorie> neu)
        {
            if (alt.Count != neu.Count) return false;
            for (int i = 0; i < alt.Count; i++)
                if (!string.Equals(alt[i].Kategorie.Name, neu[i].Name.Trim(), StringComparison.Ordinal)
                    || !WerteGleich(alt[i].Kategorie, neu[i])) return false;
            return true;
        }

        private static bool WerteGleich(Zapfkategorie a, Zapfkategorie b)
            => a.VolumenstromLJeMin.Equals(b.VolumenstromLJeMin) && a.StreuungLJeMin.Equals(b.StreuungLJeMin)
               && a.DauerMin == b.DauerMin && a.Anteil.Equals(b.Anteil) && Nullable.Equals(a.KappungLJeMin, b.KappungLJeMin);

        /// <summary>
        /// Provenienz und Beleg einer geschriebenen Kategorie gegen ihren Bezug — die gespeicherte
        /// Kategorie gleichen Namens, sonst die des Vorgabesatzes (Klassenkommentar).
        /// </summary>
        private static (Provenienz, string) KategorieHerkunft(Zapfkategorie k, List<(Zapfkategorie Kategorie, string Beleg)> alt,
                                                               IReadOnlyList<Zapfkategorie> vorgabe, string version)
        {
            string name = k.Name.Trim();
            Zapfkategorie bezug = null;
            string beleg = null;
            foreach ((Zapfkategorie a, string b) in alt)
                if (string.Equals(a.Name, name, StringComparison.Ordinal)) { bezug = a; beleg = b; break; }
            if (bezug == null && vorgabe != null)
                foreach (Zapfkategorie a in vorgabe)
                    if (string.Equals(a.Name, name, StringComparison.Ordinal)) { bezug = a; break; }

            if (bezug == null)
                return (Belegt(k.Herkunft) ? k.Herkunft with { Version = version } : Eigenkonstruktion(version), null);
            bool unberuehrt = k.Herkunft == null
                              || (k.Herkunft.Art == bezug.Herkunft.Art
                                  && string.Equals(k.Herkunft.Quelle, bezug.Herkunft.Quelle, StringComparison.Ordinal)
                                  && string.Equals(k.Herkunft.Ausgabe, bezug.Herkunft.Ausgabe, StringComparison.Ordinal));
            if (WerteGleich(bezug, k)) return (unberuehrt ? bezug.Herkunft : k.Herkunft, unberuehrt ? beleg : null);
            return (unberuehrt ? Eigenkonstruktion(version) : k.Herkunft with { Version = version }, null);
        }
    }
}
