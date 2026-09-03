using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ETAPPE B3 Paket b — <b>der eine Ort, an dem Hilfsstrom zur Menge wird</b>
    /// (Konzept <c>Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md</c> §§ 4.3/4.5, BW6).
    ///
    /// <para><b>Die Definition, in einer Zeile:</b> Hilfsenergie ist Strom für den
    /// Betrieb der Komponente, bemessen an der <b>Endenergie dieser Anlage</b>. Für BHKW
    /// und Heizkessel ist die Endenergie der Brennstoff — daher
    /// <c>Hilfsstrom = Hilfsenergie_Anteil [%] × Brennstoff [MWh]</c>
    /// (<see cref="MengeMWh"/>). Der Anteil steht je Anlage in
    /// <c>Tab_Energieanlagen.Hilfsenergie_Anteil</c> (Schema-Schritt 61);
    /// <c>NULL</c> oder 0 heißt „keine Hilfsenergie" und ist der Wert, der nichts
    /// auslöst.</para>
    ///
    /// <para><b>Warum eine eigene Klasse.</b> Dieselbe Menge wird an zwei ganz
    /// verschiedenen Stellen gebraucht: <see cref="WirtschaftlichkeitCtrl"/> mindert mit
    /// ihr die zuschlagsfähige Nettostromerzeugung des KWKG, und
    /// <see cref="ErgebnisCtrl"/> schreibt sie beim Speichern eines Laufs als
    /// Ergebnisgröße in die Modulzeile (Ausweis für Bericht und § 5.2). Beide Wege
    /// dürfen nie auseinanderlaufen — deshalb liegt die Formel hier und nirgends
    /// sonst.</para>
    ///
    /// <para><b>Die Menge ist Ergebniswert, nicht Eingabewert</b> (Festlegung
    /// 29.08.2026, Konzept § 4.5): Gepflegt wird ausschließlich der Anteil, die Menge
    /// entsteht bei jedem Lauf neu aus der Endenergie desselben Laufs. Die
    /// Ergebnisspalte <c>Hilfsenergie</c> ist deshalb Ausweis („Stand des Laufs vom …"),
    /// nicht Rechenwahrheit; die Wirtschaftlichkeit liest sie bewusst nicht, sondern
    /// rechnet frisch.</para>
    ///
    /// <para><b>Was hier NICHT steht:</b> die Wege A („% der Endenergiekosten") und C
    /// („fester Jahresbetrag") aus Konzept § 4.5. Beide sind Kostengrößen und laufen
    /// über die Betriebskostenposition <c>DbWerte.VDI_POS_HILFSENERGIE</c>; Paket b
    /// bildet allein den MENGENweg B, weil nur er auf die Strommengen des KWKG wirkt.
    /// Dass beide Pflegewege nebeneinander gesetzt sein können, ist der Grund für die
    /// Doppelpflege-Warnung in <see cref="KohaerenzPruefung"/>.</para>
    /// </summary>
    internal static class HilfsstromRechner
    {
        // =====================================================================
        // Die Formel
        // =====================================================================

        /// <summary>
        /// Hilfsstrom einer Anlage [MWh/a] — <b>die</b> Formel des Konzepts § 4.3:
        /// <c>Anteil / 100 × Endenergie</c>.
        /// </summary>
        /// <param name="anteilProzent"><c>Tab_Energieanlagen.Hilfsenergie_Anteil</c>
        /// [% des Energieeinsatzes]; <c>null</c> oder ≤ 0 = keine Hilfsenergie
        /// (Muster „<c>.HasValue &amp;&amp; &gt; 0</c>" aus Paket a).</param>
        /// <param name="endenergieMWh">Endenergie DIESER Anlage [MWh/a] — bei BHKW und
        /// Heizkessel der Brennstoffeinsatz der Ergebnis-Modulzeile.</param>
        /// <returns>0, wenn kein Anteil gepflegt ist oder keine Endenergie vorliegt.</returns>
        internal static double MengeMWh(double? anteilProzent, double endenergieMWh)
        {
            if (!anteilProzent.HasValue || anteilProzent.Value <= 0) return 0;
            if (endenergieMWh <= 0) return 0;
            return endenergieMWh * anteilProzent.Value / 100.0;
        }

        /// <summary>
        /// Mindert den Eigen-/Einspeise-Split des KWKG um den Hilfsstrom —
        /// <b>zuerst den Eigenverbrauch, erst danach die Einspeisung</b>.
        ///
        /// <para><b>Die Reihenfolge ist keine Konvention, sondern Physik.</b>
        /// Hilfsstrom ist Strom, den die Anlage selbst verbraucht; er verlässt die
        /// Kundenanlage nie und kann deshalb niemals Teil der Einspeisung sein. Ein
        /// Abzug nach Anteilen würde einen Teil der Minderung der Einspeisung
        /// zuschreiben und damit behaupten, ein Teil des Eigenbedarfs sei ins Netz
        /// geflossen. Erst wenn der gesamte bilanzielle Eigenverbrauch aufgezehrt ist —
        /// der Hilfsbedarf also größer ist als alles, was die Anlage im Jahr selbst
        /// genutzt hat —, bleibt nur noch die Einspeisung als Deckung übrig, und
        /// dann trägt sie den Rest.</para>
        ///
        /// <para>Die Summe <c>eigen + einsp</c> sinkt um genau den Hilfsstrom (bzw. auf
        /// 0, wenn er größer ist als die ganze Erzeugung); negative Mengen entstehen
        /// nicht.</para>
        /// </summary>
        internal static void NettoSplit(double hilfsstromMWh, ref double eigenMWh,
                                        ref double einspMWh)
        {
            if (hilfsstromMWh <= 0) return;

            double rest = hilfsstromMWh;
            double ab = Math.Min(Math.Max(0, eigenMWh), rest);
            eigenMWh -= ab;
            rest -= ab;
            if (rest <= 0) return;

            ab = Math.Min(Math.Max(0, einspMWh), rest);
            einspMWh -= ab;
        }

        // =====================================================================
        // Persistenzweg: Anteil je Modulzeile
        // =====================================================================

        /// <summary>
        /// Hilfsstrom [MWh/a] <b>je Ergebnis-Modulzeile</b> eines Projekts — die eine
        /// Auskunft, die der Speicherweg (<see cref="ErgebnisCtrl"/>) braucht.
        ///
        /// <para>Rückgabe ist ein Feld in der Reihenfolge von
        /// <paramref name="modulNamen"/>; überall dort 0, wo kein Anteil gepflegt ist,
        /// die Spalte fehlt oder sich Anlagen- und Modulzeilen nicht paaren lassen.
        /// <b>Eine geratene Zuordnung gibt es nicht</b> — dieselbe Haltung wie beim
        /// KWKG-Guard, der in diesem Fall auf den projektweiten Ersatzweg geht.</para>
        /// </summary>
        /// <param name="idProjekt">Projekt der Anlagen- und Modulzeilen.</param>
        /// <param name="idType"><c>WizardItemClass.BHKW_TYP</c> bzw.
        /// <c>KESSEL_TYP</c>.</param>
        /// <param name="modulNamen"><c>Modul</c>-Spalte der Ergebniszeilen, in
        /// Schreibreihenfolge.</param>
        /// <param name="endenergieJeModulMWh">Endenergie je Modulzeile [MWh/a],
        /// gleiche Reihenfolge — beim BHKW der anteilig verteilte Brennstoff, beim
        /// Kessel die Menge aus
        /// <see cref="WirtschaftlichkeitCtrl.KesselBrennstoffMWh"/>.</param>
        internal static double[] JeModul(int idProjekt, int idType, string[] modulNamen,
                                         double[] endenergieJeModulMWh)
        {
            int n = modulNamen == null ? 0 : modulNamen.Length;
            var ergebnis = new double[n];
            if (n == 0 || endenergieJeModulMWh == null || endenergieJeModulMWh.Length != n)
                return ergebnis;

            List<AnlagenAnteil> anlagen = Anteile(idProjekt, idType);
            if (anlagen.Count == 0) return ergebnis;

            bool gepflegt = false;
            foreach (AnlagenAnteil a in anlagen)
                if (a.AnteilProzent.HasValue && a.AnteilProzent.Value > 0) { gepflegt = true; break; }
            if (!gepflegt) return ergebnis;      // nichts zu tun — und keine Zuordnung nötig

            var bezeichner = new string[anlagen.Count];
            for (int i = 0; i < anlagen.Count; i++) bezeichner[i] = anlagen[i].Bezeichner;

            int[] anlageJeModul = ZuordnungModulZuAnlage(bezeichner, modulNamen);
            if (anlageJeModul == null) return ergebnis;

            for (int j = 0; j < n; j++)
            {
                int i = anlageJeModul[j];
                if (i < 0) continue;
                ergebnis[j] = MengeMWh(anlagen[i].AnteilProzent, endenergieJeModulMWh[j]);
            }
            return ergebnis;
        }

        /// <summary>Eine Anlagenzeile, soweit der Hilfsstrom sie braucht.</summary>
        internal sealed class AnlagenAnteil
        {
            /// <summary><c>Tab_Energieanlagen.ID</c>.</summary>
            public int IdAnlage;

            /// <summary><c>Tab_Energieanlagen.Bezeichner</c> — Datenwert, kein
            /// Anzeigetext; er trägt die Zuordnung zur Ergebnis-Modulzeile.</summary>
            public string Bezeichner = "";

            /// <summary><c>Tab_Energieanlagen.Hilfsenergie_Anteil</c> [%];
            /// <c>null</c> = nicht gepflegt.</summary>
            public double? AnteilProzent;
        }

        /// <summary>
        /// Bezeichner und Hilfsenergieanteil aller Anlagenzeilen eines Projekts.
        /// Leere Liste = kein Anlagenbestand, fehlende Spalte (Datenbank vor
        /// Migrationsschritt 61) oder gescheiterte Abfrage.
        ///
        /// <para><b>Kein <c>ORDER BY</c></b> — aus demselben Grund wie in
        /// <c>WirtschaftlichkeitCtrl.AnlagenTabelle</c>: Die Zuordnung Anlage ↔
        /// Ergebnismodul fällt bei nicht passenden Bezeichnern auf die REIHENFOLGE
        /// zurück, und die Modulzeilen entstehen in der Reihenfolge von
        /// <c>SimulationControl.BHKW_Liste_Laden</c> bzw. <c>SPK_Liste_Laden</c>, die
        /// beide ebenfalls ohne Sortierung lesen.</para>
        /// </summary>
        internal static List<AnlagenAnteil> Anteile(int idProjekt, int idType)
        {
            var liste = new List<AnlagenAnteil>();
            if (idProjekt <= 0) return liste;

            try
            {
                DataTable dt;
                using (DataRepository.EngineModus())
                    dt = DataRepository.GetDataTable(
                        "SELECT ID, Bezeichner, [" +
                        SchemaKatalog.SPALTE_EA_HILFSENERGIE_ANTEIL + "] " +
                        "FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = " +
                        idType.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        new DbParam("@p", idProjekt));

                // DataRepository liefert bei einem SQL-Fehler eine LEERE DataTable statt
                // zu werfen. Ob die Spalte da ist, verrät deshalb nur die Spaltenliste —
                // dieselbe Erkennung wie in WirtschaftlichkeitCtrl.LiesAnlagen.
                if (dt == null ||
                    !dt.Columns.Contains(SchemaKatalog.SPALTE_EA_HILFSENERGIE_ANTEIL))
                    return liste;

                foreach (DataRow r in dt.Rows)
                {
                    var a = new AnlagenAnteil();
                    a.IdAnlage = r["ID"] == DBNull.Value ? 0 : Convert.ToInt32(r["ID"]);
                    a.Bezeichner = r["Bezeichner"] == DBNull.Value
                                 ? "" : Convert.ToString(r["Bezeichner"]).Trim();
                    object w = r[SchemaKatalog.SPALTE_EA_HILFSENERGIE_ANTEIL];
                    a.AnteilProzent = w == DBNull.Value ? (double?)null : Convert.ToDouble(w);
                    liste.Add(a);
                }
            }
            catch { liste.Clear(); }
            return liste;
        }

        /// <summary>
        /// Ordnet jeder MODULZEILE ihre Anlagenzeile zu: Rückgabe je Modulindex der
        /// Anlagenindex, <c>-1</c> = keine. <c>null</c> = nicht zuordenbar.
        ///
        /// <para><b>Dasselbe Verfahren wie
        /// <c>WirtschaftlichkeitCtrl.ModulJeAnlage</c></b> — erst der Bezeichner
        /// (<c>SimulationRunner</c> schreibt ihn als <c>Modul</c>), dann die Reihenfolge
        /// bei gleicher Anzahl, sonst nichts. Es steht hier ein zweites Mal, weil der
        /// Speicherweg die Modulmodelle gar nicht kennt, sondern nur Namen und Mengen;
        /// eine gemeinsame typisierte Fassung hätte
        /// <c>ErgebnisBHKWModulModel</c> und <c>ErgebnisHeizkesselModulModel</c> unter
        /// eine Schnittstelle zwingen müssen, die es im Modell nicht gibt — dieselbe
        /// Abwägung, die schon <c>KesselModulJeAnlage</c> in Paket a getroffen hat.
        /// <b>Ändert sich das Verfahren, ändern es beide Stellen.</b></para>
        /// </summary>
        internal static int[] ZuordnungModulZuAnlage(string[] anlagenBezeichner,
                                                     string[] modulNamen)
        {
            if (anlagenBezeichner == null || modulNamen == null) return null;

            var treffer = new int[modulNamen.Length];
            for (int j = 0; j < treffer.Length; j++) treffer[j] = -1;

            bool[] belegt = new bool[modulNamen.Length];
            int getroffen = 0;

            for (int i = 0; i < anlagenBezeichner.Length; i++)
                for (int j = 0; j < modulNamen.Length; j++)
                {
                    if (belegt[j]) continue;
                    string name = modulNamen[j] == null ? "" : modulNamen[j].Trim();
                    string bez = anlagenBezeichner[i] == null ? "" : anlagenBezeichner[i].Trim();
                    if (!string.Equals(name, bez, StringComparison.OrdinalIgnoreCase)) continue;
                    belegt[j] = true;
                    treffer[j] = i;
                    getroffen++;
                    break;
                }
            if (getroffen == anlagenBezeichner.Length) return treffer;

            if (anlagenBezeichner.Length != modulNamen.Length) return null;
            for (int j = 0; j < modulNamen.Length; j++) treffer[j] = j;
            return treffer;
        }

        /// <summary>
        /// Bemessungsmenge des § 54 für einen Kessel [MWh/a, heizwertbezogen].
        ///
        /// <para><b>Warum sie abgeleitet und nicht gelesen wird.</b>
        /// <c>Tab_ErgebnisHeizkesselModul.Verbrauch</c> existiert seit jeher, wird vom
        /// Rechenkern aber NIE gesetzt (<c>SimulationRunner</c> füllt an der Modulzeile
        /// nur Modul, Waerme_Gas, Waerme_Oel, Jahresnutzungsgrad und carrier_id) — im
        /// ganzen Bestand steht dort 0. Gelesen wird die Spalte trotzdem zuerst: Sobald
        /// sie einmal gefüllt wird, ist sie die bessere Quelle, und diese Reihenfolge
        /// muss dann nicht noch einmal angefasst werden.</para>
        ///
        /// <para><b>Die Ableitung ist die exakte Umkehrung der Vorwärtsrechnung.</b>
        /// <c>SimulationSPK.Bilanz_und_Nutzungsgrad</c> bildet den Nutzungsgrad als
        /// <c>(Waerme_Gas + Waerme_Oel) / Brennstoffeinsatz × 100</c> — in PROZENT und
        /// über denselben Zähler. Die Rückrechnung
        /// <c>(Waerme_Gas + Waerme_Oel) / (Nutzungsgrad / 100)</c> liefert deshalb wieder
        /// den Brennstoffeinsatz des Laufs, nicht eine Näherung. Einzige Ausnahme sind
        /// die Plausibilitätsklemmen des Rechenkerns (Nutzungsgrad über 110 % wird auf
        /// 108 gesetzt, unter 1 % auf 1); in diesen Fällen weicht die Rückrechnung um
        /// genau den geklemmten Betrag ab — ein Fall, den es nur bei absurden
        /// Eingangsdaten gibt.</para>
        ///
        /// <para><b>Ohne Nutzungsgrad keine Menge:</b> 0, und die Steuerrechnung meldet
        /// „Menge unklar" mit dem Anlagennamen. Eine geratene Menge wäre hier dasselbe wie
        /// eine geratene Dichte (Leitentscheidung L3).</para>
        ///
        /// <para><b>Der Simulationspfad bleibt unberührt.</b> Die Ableitung steht
        /// bewusst in der Zuführung und nicht im <c>SimulationRunner</c>: Eine neu
        /// gefüllte Ergebnisspalte änderte gespeicherte Läufe und damit die
        /// Referenzlaufvergleiche, ohne dass die Wirtschaftlichkeit davon mehr hätte.</para>
        ///
        /// <para><b>ETAPPE B3 Paket b.</b> Der Speicherweg (<see cref="ErgebnisCtrl"/>)
        /// braucht dieselbe Menge als Bemessungsgrundlage des Hilfsstroms. Eine zweite
        /// Ableitung daneben wäre die zweite Wahrheit über genau die Frage, die dieser
        /// Kommentar beantwortet.</para>
        ///
        /// <para><b>Umsetzungskonzept iU3, Kante K5:</b> Die Methode stand bis dahin bei
        /// <see cref="WirtschaftlichkeitCtrl"/> und war die einzige Verbindung von
        /// <see cref="ErgebnisCtrl"/> dorthin. Sie liegt jetzt bei dem Rechner, der sie
        /// als Bemessungsgrundlage ohnehin nennt (siehe <see cref="JeModul"/>);
        /// <see cref="WirtschaftlichkeitCtrl.KesselBrennstoffMWh"/> leitet hierher
        /// weiter.</para>
        /// </summary>
        internal static double KesselBrennstoffMWh(ErgebnisHeizkesselModulModel m)
        {
            if (m.Verbrauch > 0) return m.Verbrauch;
            double waerme = m.Waerme_Gas + m.Waerme_Oel;
            if (waerme <= 0 || m.Jahresnutzungsgrad <= 0) return 0;
            return waerme / (m.Jahresnutzungsgrad / 100.0);
        }
    }
}
