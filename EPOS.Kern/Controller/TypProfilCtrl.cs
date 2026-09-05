using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Wie das Anlegen eines Typ-Profils ausgegangen ist (Befund W8-B-2, Windows-Abnahme
    /// 05.09.2026).
    ///
    /// <para>Bis dahin kannte <see cref="TypProfilCtrl.Neu"/> nur <c>true</c>/<c>false</c>
    /// und lief mit einem BELEGTEN Namen bis in das <c>INSERT</c>. Die Zugriffsschicht
    /// meldete den Wurf dann ueber <c>DataRepository.FehlerMelden</c> — ein modaler
    /// Kasten mit dem Wortlaut „Datenbankfehler: SQLite Error 19: 'UNIQUE constraint
    /// failed: Tab_Stromverbrauchertyp_STAMM.Typname'" samt der Anweisung, mitten aus
    /// einem Blazor-Ereignis heraus (Hausregel A-8). Der belegte Name ist jetzt ein WERT,
    /// und der Dialog macht daraus ein Warnbanner in der offenen Namensabfrage.</para>
    /// </summary>
    internal enum TypAnlageErgebnis
    {
        /// <summary>Der Typ steht im Katalog.</summary>
        Angelegt = 0,

        /// <summary>Es gibt schon einen Typ dieses Namens — es wurde nichts geschrieben.</summary>
        NameBelegt = 1,

        /// <summary>Der Kopf oder die 168 Werte kamen nicht durch.</summary>
        Fehlgeschlagen = 2
    }

    /// <summary>
    /// Die Wochen-Stundenprofile der drei Typkataloge (iU9-W8.0b) — Datenseite von
    /// <c>EPOS.UI/Dialoge/Bedarf/TypProfilDialog.razor</c>, das <c>Form_EingStromTyp</c>,
    /// <c>Form_EingProzTyp</c> und <c>Form_EingBrauchwasserTyp</c> abloest.
    ///
    /// <para><b>Eine Schnittstelle, drei Tabellen mit ZWEI Schluesselspalten.</b>
    /// <c>Tab_Stromverbrauchertyp_STAMM</c> fuehrt den Typnamen in <c>Typname</c>,
    /// <c>Tab_Prozesstyp_STAMM</c> und <c>Tab_Brauchwassertyp_STAMM</c> in
    /// <c>Bezeichner</c>. Beides kommt aus <see cref="BedarfStammCtrl.TypKatalog"/>, damit
    /// die Zuordnung an EINER Stelle steht.</para>
    ///
    /// <para><b>Die 168 Stundenspalten heissen <c>[1]</c>…<c>[168]</c></b> — das sind
    /// BEZEICHNER, keine Parameter, und sie muessen deshalb in den Anweisungstext. Die
    /// eckigen Klammern schuetzen die rein numerischen Namen (Kommentar der Vorlaeufer).
    /// Die Werte selbst sind Parameter; die Zahl kommt damit ohne Kulturformatierung an.
    /// Der Name der Spalte entsteht hier aus einer SCHLEIFE ueber 1…168 und niemals aus
    /// einer Eingabe.</para>
    ///
    /// <para><b>Gespeichert wird in EINER Transaktion.</b> <c>Form_EingProzTyp</c> tat das
    /// schon (<c>DbVorgang</c>, :168-214); Strom und Brauchwasser schickten 169
    /// Einzelanweisungen ohne Klammer los — ein Fehler in der Mitte hinterliess einen
    /// HALBEN Stand, und <c>Tab_*typ_STAMM</c> ist Simulationseingang. Der Weg des
    /// Prozesstyps gilt jetzt fuer alle drei (Verbesserung A-7, ergebnisgleich).</para>
    /// </summary>
    internal static class TypProfilCtrl
    {
        /// <summary>Sieben Tage.</summary>
        internal const int TAGE = 7;

        /// <summary>Vierundzwanzig Stunden je Tag — zusammen die 168 Wochenwerte.</summary>
        internal const int STUNDEN = 24;

        /// <summary>
        /// Die erste Stundenspalte steht an der VIERTEN Stelle der Zeile: ID, Name,
        /// Beschreibung, dann <c>[1]</c>. Wortgleich aus <c>Form_EingStromTyp.DatenEinlesen</c>:127.
        /// </summary>
        private const int ERSTE_STUNDENSPALTE = 3;

        /// <summary>Die Typliste — dieselbe Reihenfolge wie im Vorlaeufer.</summary>
        internal static IReadOnlyList<string> Typen(BedarfsArt art) => BedarfStammCtrl.Typen(art);

        /// <summary>
        /// Gibt es einen Typ dieses Namens schon? Die VORPRUEFUNG zu <see cref="Neu"/> und
        /// <see cref="SpeichernUnter"/> (Befund W8-B-2).
        ///
        /// <para>Die Namensspalte ist in allen drei Katalogen EINDEUTIG; ohne diese Frage
        /// lief ein belegter Name in den Wurf der Datenbank und der Anwender bekam dessen
        /// Wortlaut zu sehen. Sie fragt dieselbe Spalte, die auch der Schluessel ist
        /// (<see cref="BedarfStammCtrl.TypKatalog"/>) — eine zweite Namensquelle liefe
        /// beim ersten Katalog auseinander, der seine Spalte umbenennt.</para>
        /// </summary>
        internal static bool TypExists(BedarfsArt art, string name)
        {
            if (string.IsNullOrEmpty(name)) return false;

            (string tabelle, string spalte) = BedarfStammCtrl.TypKatalog(art);
            object v = DataRepository.ExecuteScalar(
                "SELECT " + spalte + " FROM " + tabelle + " WHERE " + spalte + " = ?",
                new DbParam("@typ", name));
            return v != null && v != DBNull.Value;
        }

        /// <summary>Ist das Typ-Profil Auslieferungsbestand (<c>ReadOnly</c>)?</summary>
        internal static bool IstReadOnly(BedarfsArt art, string typ)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher: return StromverbraucherStammCtrl.TypIsReadOnly(typ);
                case BedarfsArt.Prozesswaerme:    return ProzesswaermeStammCtrl.TypIsReadOnly(typ);
                default:                          return BrauchwasserStammCtrl.TypIsReadOnly(typ);
            }
        }

        /// <summary>
        /// Beschreibung und die 7 × 24 Wochenwerte eines Typs; <c>null</c>, wenn es ihn
        /// nicht (mehr) gibt. Fehlende oder leere Spalten zaehlen als 0 — wie im Vorlaeufer.
        /// </summary>
        internal static (string Beschreibung, double[,] Werte)? Lies(BedarfsArt art, string typ)
        {
            (string tabelle, string spalte) = BedarfStammCtrl.TypKatalog(art);

            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM " + tabelle + " WHERE " + spalte + " = ?",
                new DbParam("@typ", typ ?? ""));
            if (dt == null || dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            var werte = new double[TAGE, STUNDEN];
            for (int tag = 0; tag < TAGE; tag++)
                for (int stunde = 0; stunde < STUNDEN; stunde++)
                {
                    int index = tag * STUNDEN + stunde + ERSTE_STUNDENSPALTE;
                    werte[tag, stunde] = (index < dt.Columns.Count && row[index] != DBNull.Value)
                        ? Convert.ToDouble(row[index]) : 0.0;
                }

            string beschreibung = (dt.Columns.Contains("Beschreibung") && row["Beschreibung"] != DBNull.Value)
                ? row["Beschreibung"].ToString() : "";
            return (beschreibung, werte);
        }

        /// <summary>
        /// Schreibt 168 Stundenwerte und die Beschreibung eines VORHANDENEN Typs — in einer
        /// Transaktion. Die ReadOnly-Sperre prueft der Aufrufer vorher, damit die Meldung
        /// im Dialog stehen kann statt in einem modalen Kasten.
        /// </summary>
        internal static bool Speichern(BedarfsArt art, string typ, double[,] werte, string beschreibung)
        {
            if (werte == null || werte.GetLength(0) < TAGE || werte.GetLength(1) < STUNDEN) return false;

            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    if (!Schreibe(v, art, typ, werte, beschreibung)) { v.Rollback(); return false; }
                    v.Commit();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Speichern des Typ-Profils: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// „Neu": Typkopf anlegen, dann 168 Nullen und eine leere Beschreibung.
        /// <see cref="TypAnlageErgebnis.NameBelegt"/>, wenn es den Namen schon gibt;
        /// <see cref="TypAnlageErgebnis.Fehlgeschlagen"/>, wenn der Kopf nicht entstand —
        /// der Vorlaeufer brach dann ebenfalls ab.
        /// </summary>
        internal static TypAnlageErgebnis Neu(BedarfsArt art, string name)
            => Anlegen(art, name, new double[TAGE, STUNDEN], "");

        /// <summary>
        /// „Speichern unter": Typkopf anlegen und die AKTUELLEN Werte samt Beschreibung
        /// hineinschreiben. Ein belegter Name meldet sich wie bei <see cref="Neu"/>.
        /// </summary>
        internal static TypAnlageErgebnis SpeichernUnter(BedarfsArt art, string name,
                                                         double[,] werte, string beschreibung)
            => Anlegen(art, name, werte, beschreibung);

        /// <summary>Loescht ein Typ-Profil. Die ReadOnly-Sperre prueft der Aufrufer vorher.</summary>
        internal static bool Loeschen(BedarfsArt art, string typ)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher: return StromverbraucherStammCtrl.TypDelete(typ);
                case BedarfsArt.Prozesswaerme:    return ProzesswaermeStammCtrl.TypDelete(typ);
                default:                          return BrauchwasserStammCtrl.TypDelete(typ);
            }
        }

        // =================================================================================

        private static TypAnlageErgebnis Anlegen(BedarfsArt art, string name, double[,] werte,
                                                 string beschreibung)
        {
            if (string.IsNullOrEmpty(name)) return TypAnlageErgebnis.Fehlgeschlagen;
            if (werte == null || werte.GetLength(0) < TAGE || werte.GetLength(1) < STUNDEN)
                return TypAnlageErgebnis.Fehlgeschlagen;

            // VORPRUEFUNG statt Wurf (Befund W8-B-2): Die Namensspalte ist eindeutig, und
            // ein doppelter Name endete bis dahin in einem modalen Datenbankfehler.
            if (TypExists(art, name)) return TypAnlageErgebnis.NameBelegt;

            int id;
            switch (art)
            {
                case BedarfsArt.Stromverbraucher: id = StromverbraucherStammCtrl.TypNew(name); break;
                case BedarfsArt.Prozesswaerme:    id = ProzesswaermeStammCtrl.TypNew(name); break;
                default:                          id = BrauchwasserStammCtrl.TypNew(name); break;
            }
            if (id <= 0) return TypAnlageErgebnis.Fehlgeschlagen;

            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    if (!Schreibe(v, art, name, werte, beschreibung))
                    {
                        v.Rollback();
                        return TypAnlageErgebnis.Fehlgeschlagen;
                    }
                    v.Commit();
                    return TypAnlageErgebnis.Angelegt;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Anlegen des Typ-Profils: " + ex.Message);
                return TypAnlageErgebnis.Fehlgeschlagen;
            }
        }

        /// <summary>Die 168 Werte und die Beschreibung innerhalb einer laufenden Transaktion.</summary>
        private static bool Schreibe(DbVorgang v, BedarfsArt art, string typ,
                                     double[,] werte, string beschreibung)
        {
            (string tabelle, string spalte) = BedarfStammCtrl.TypKatalog(art);

            for (int tag = 0; tag < TAGE; tag++)
                for (int stunde = 0; stunde < STUNDEN; stunde++)
                {
                    // Der Spaltenname entsteht aus der Schleife, nicht aus einer Eingabe;
                    // die eckigen Klammern schuetzen den rein numerischen Bezeichner.
                    string feld = (tag * STUNDEN + stunde + 1).ToString();
                    v.Ausfuehren(
                        "UPDATE " + tabelle + " SET [" + feld + "] = ? WHERE " + spalte + " = ?",
                        new DbParam("@wert", DbParamTyp.Double) { Wert = werte[tag, stunde] },
                        new DbParam("@typ", DbParamTyp.VarWChar) { Wert = (object)(typ ?? "") });
                }

            v.Ausfuehren(
                "UPDATE " + tabelle + " SET Beschreibung = ? WHERE " + spalte + " = ?",
                new DbParam("@bes", DbParamTyp.VarWChar) { Wert = (object)(beschreibung ?? "") },
                new DbParam("@typ", DbParamTyp.VarWChar) { Wert = (object)(typ ?? "") });
            return true;
        }
    }
}
