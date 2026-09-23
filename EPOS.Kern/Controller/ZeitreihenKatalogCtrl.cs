using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Zeilen der drei ZEITREIHENKATALOGE</b> (Anwenderentscheid
    /// <b>W14a-E-10</b> vom 07.09.2026, Konzept_Katalogfilter 2.10 und 4.10, Stufe
    /// <b>S3.2</b>) — Waermebedarf-Lastgang (4), Stromganglinie (3) und
    /// Solarthermieganglinie (1).
    ///
    /// <para><b>Warum EIN Controller fuer drei Kataloge.</b> Die drei Kopftabellen
    /// unterscheiden sich in genau zwei Spalten — <c>Zeitinterval</c> fuehrt nur die
    /// Stromganglinie, <c>Beschreibung</c> nur die Solarganglinie —, und ihre
    /// Wertetabellen sind Zwillinge. Drei Fassungen derselben Schleife liefen beim
    /// ersten Schemawechsel auseinander; die Ausprägung ist deshalb
    /// <see cref="Zeitreihenart"/> und ihre Tabellen stehen als
    /// <see cref="GanglinienQuelle"/> daneben — dasselbe Muster wie
    /// <see cref="BedarfStammCtrl"/> fuer die drei Bedarfskataloge.</para>
    ///
    /// <para><b>ZWEI Abfragen je Liste, nicht zwei je Zeile.</b> Der Kopfsatz kommt aus
    /// einem <c>SELECT * … ORDER BY Bezeichner</c>, Jahresarbeit und Spitze aus der
    /// EINEN Gruppenabfrage <see cref="GanglinienAuswertungCtrl.Kennzahlen"/>. Bis
    /// hierher standen die zwei Kennzahlen ueberhaupt nicht in der Liste; wer sie
    /// sehen wollte, klickte eine Zeile an und liess ihre 8 760 bzw. 35 040 Wertzeilen
    /// einzeln lesen.</para>
    ///
    /// <para><b>Gelesen, nicht gerechnet</b> — der Rechenweg der Simulation ist
    /// unberuehrt.</para>
    /// </summary>
    internal static class ZeitreihenKatalogCtrl
    {
        /// <summary>
        /// Die Zeilen eines Zeitreihenkatalogs samt ihren Parameterspalten:
        /// Bezeichner, Zeitintervall (nur Stromganglinie), Beschreibung (nur
        /// Solarganglinie), Jahresarbeit [MWh] und Spitze [kW].
        ///
        /// <para><b>Eine Reihe, die nicht ins Raster passt, zeigt Leerwerte</b> statt
        /// einer erfundenen Zahl (W6-E-1): Was weder 8 760 noch 35 040 Werte hat, gilt
        /// dem Rechenkern als unbrauchbar, und die Anzeige sagt das mit dem
        /// Halbgeviertstrich — sichtbar bleibt der Satz trotzdem.</para>
        /// </summary>
        internal static IReadOnlyList<Katalogfilterzeile> Katalogfilterzeilen(Zeitreihenart art)
        {
            var liste = new List<Katalogfilterzeile>();

            GanglinienQuelle quelle = GanglinienQuelle.Zu(art);
            IReadOnlyDictionary<int, GanglinienKennzahl> kennzahlen =
                GanglinienAuswertungCtrl.Kennzahlen(quelle);

            // Der Tabellenname kommt aus GanglinienQuelle und nicht aus einer Eingabe.
            // SELECT * mit Bedacht: Die drei Kopftabellen fuehren verschiedene Spalten,
            // und eine namentliche Liste scheiterte auf der jeweils anderen mit
            // "no such column" (Muster ParameterUebersichtCtrl.Satz).
            DataTable dt = StilleDb.Tabelle(
                "SELECT * FROM [" + quelle.KopfStamm + "] ORDER BY Bezeichner");
            if (dt == null) return liste;

            foreach (DataRow r in dt.Rows)
            {
                int id = Katalogfeld.Ganzzahl(r, "ID");
                string bezeichner = Katalogfeld.Text(r, "Bezeichner");

                var zeile = new Katalogfilterzeile(id, bezeichner)
                {
                    Geschuetzt = Katalogfeld.Kennzeichen(r, "ReadOnly")
                };

                zeile.MitText(Katalogfilterprofil.SpBezeichner, bezeichner);

                if (art == Zeitreihenart.Stromganglinie)
                    zeile.MitZahl(Katalogfilterprofil.SpZeitintervall,
                                  Katalogfeld.Zahl(r, "Zeitinterval"), 0);

                if (art == Zeitreihenart.Solarganglinie)
                    zeile.MitText(Katalogfilterprofil.SpBeschreibung,
                                  Katalogfeld.Text(r, "Beschreibung"));

                GanglinienKennzahl k;
                bool da = kennzahlen.TryGetValue(id, out k) && k.Brauchbar;

                zeile.MitZahl(Katalogfilterprofil.SpJahresarbeitMwh,
                              da ? k.JahresarbeitMwh : (double?)null, 1);
                zeile.MitZahl(Katalogfilterprofil.SpSpitzeKw,
                              da ? k.SpitzeKw : (double?)null, 1);

                liste.Add(zeile);
            }
            return liste;
        }

        // =====================================================================
        // Die VERWENDUNG in Projekten (Neuordnung der Administrationsdialoge,
        // Stufe 4: Loeschen weich gesperrt mit dem Projektnamen)
        // =====================================================================

        /// <summary>
        /// <b>Welche Projekte einen Katalogsatz verwenden</b> — je Bezeichner die
        /// Projektnamen aus der Zuordnungstabelle (<c>Z_Projekt…</c>), alphabetisch und ohne
        /// Doppel.
        ///
        /// <para><b>Wozu.</b> „Löschen…" in der Auswahlleiste der drei Zeitreihen-
        /// verwaltungen ist WEICH gesperrt, solange ein Projekt den Satz führt, und sein
        /// Kurztext nennt die Projekte (Konzept Administrationsdialoge 3.4). Bis hierher
        /// meldete erst der Klick „Es existiert eine Projektzuordnung" — ohne zu sagen,
        /// welche.</para>
        ///
        /// <para><b>EINE Abfrage je Liste, nicht eine je Zeile</b>: Die Auswahlleiste
        /// fragt den Sperrgrund bei jedem Zeichnen, und das darf keine Datenbank kosten.
        /// Die Zuordnung läuft über den BEZEICHNER — dieselbe Bedingung wie
        /// <c>HatProjektzuordnung</c> der drei Stamm-Controller (die Zuordnung zeigt mit
        /// <c>ID_Ganglinie</c> auf die Projektkopie, nicht auf den Katalogsatz). Eine
        /// neue Beziehung entsteht nicht; sie wird gelesen.</para>
        ///
        /// <para>Fehlt eine Tabelle (nie migrierte Datenbank), bleibt die Karte leer —
        /// dann sperrt nur noch der Löschweg des Controllers.</para>
        /// </summary>
        internal static IReadOnlyDictionary<string, IReadOnlyList<string>> Projektverwendung(Zeitreihenart art)
        {
            var karte = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            DataTable dt = StilleDb.Tabelle(VerwendungSql(art));
            if (dt != null)
            {
                foreach (DataRow r in dt.Rows)
                {
                    string bezeichner = Katalogfeld.Text(r, "Bezeichner");
                    string projekt = Katalogfeld.Text(r, "Projektname");
                    if (bezeichner.Length == 0 || projekt.Length == 0) continue;

                    if (!karte.TryGetValue(bezeichner, out List<string> projekte))
                        karte[bezeichner] = projekte = new List<string>();
                    if (!projekte.Contains(projekt)) projekte.Add(projekt);
                }
            }

            var ergebnis = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, List<string>> e in karte) ergebnis[e.Key] = e.Value;
            return ergebnis;
        }

        /// <summary>
        /// Die Abfrage der Verwendung je Ausprägung — drei feste Texte statt eines
        /// zusammengesetzten, damit der <c>SqlDialektPruefer</c> jeden von ihnen hält.
        /// </summary>
        private static string VerwendungSql(Zeitreihenart art)
        {
            switch (art)
            {
                case Zeitreihenart.Stromganglinie:
                    return "SELECT Z.Bezeichner, P.Projektname FROM Z_ProjektStromganglinie Z " +
                           "INNER JOIN Tab_Projekt P ON P.ID = Z.ID_Projekt " +
                           "ORDER BY Z.Bezeichner, P.Projektname";
                case Zeitreihenart.Solarganglinie:
                    return "SELECT Z.Bezeichner, P.Projektname FROM Z_ProjektSolarganglinie Z " +
                           "INNER JOIN Tab_Projekt P ON P.ID = Z.ID_Projekt " +
                           "ORDER BY Z.Bezeichner, P.Projektname";
                default:
                    return "SELECT Z.Bezeichner, P.Projektname FROM Z_ProjektWaermebedarf Z " +
                           "INNER JOIN Tab_Projekt P ON P.ID = Z.ID_Projekt " +
                           "ORDER BY Z.Bezeichner, P.Projektname";
            }
        }
    }
}
