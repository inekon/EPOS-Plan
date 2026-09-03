using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
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
        /// „Neu": Typkopf anlegen, dann 168 Nullen und eine leere Beschreibung. Rueckgabe
        /// <c>false</c>, wenn der Kopf nicht entstand — der Vorlaeufer brach dann ebenfalls ab.
        /// </summary>
        internal static bool Neu(BedarfsArt art, string name)
            => Anlegen(art, name, new double[TAGE, STUNDEN], "");

        /// <summary>
        /// „Speichern unter": Typkopf anlegen und die AKTUELLEN Werte samt Beschreibung
        /// hineinschreiben.
        /// </summary>
        internal static bool SpeichernUnter(BedarfsArt art, string name, double[,] werte, string beschreibung)
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

        private static bool Anlegen(BedarfsArt art, string name, double[,] werte, string beschreibung)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (werte == null || werte.GetLength(0) < TAGE || werte.GetLength(1) < STUNDEN) return false;

            int id;
            switch (art)
            {
                case BedarfsArt.Stromverbraucher: id = StromverbraucherStammCtrl.TypNew(name); break;
                case BedarfsArt.Prozesswaerme:    id = ProzesswaermeStammCtrl.TypNew(name); break;
                default:                          id = BrauchwasserStammCtrl.TypNew(name); break;
            }
            if (id <= 0) return false;

            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    if (!Schreibe(v, art, name, werte, beschreibung)) { v.Rollback(); return false; }
                    v.Commit();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Anlegen des Typ-Profils: " + ex.Message);
                return false;
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
