using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der EINE Rechenweg der Betriebskostenpositionen nach VDI 2067 (Konzept
    /// <c>Konzept_BHKW_Kosten_Erloese.md</c> Abschnitt 4.1): der Betrag aus Menge und
    /// Satz (<see cref="Betrag"/>), die Einheitenzeichen von Satz und Bezugsmenge
    /// (<c>SatzEinheit</c>, <c>MengenEinheit</c>) und die Investitionssumme, an der sich
    /// eine Prozentposition bemisst (<c>Kaskadensummen</c>,
    /// <see cref="InvestSummeFuer"/>).
    ///
    /// <para>
    /// <b>Warum ein eigener Controller.</b> Zwei Aufrufer brauchen dieselben Regeln: der
    /// Komponenten-Kostendialog (<c>KostenKomponenteHuelle</c>, Sperren der abgeleiteten
    /// Beträge) und die Wirtschaftlichkeit (<c>WirtschaftlichkeitCtrl</c>). Eine zweite
    /// Kopie der Formel wäre genau die Sorte Doppelpflege, an der die Kostenseite schon
    /// einmal auseinandergelaufen ist (Befund D1).
    /// </para>
    ///
    /// <para>
    /// <b>Die Bezugsgröße ist PERSISTENT, nicht abgeleitet.</b> Beim Speichern schreibt
    /// der Dialog die ermittelte Menge nach <c>Tab_ProjektWerte.Menge</c> und den Satz
    /// nach <c>Einheitpreis</c>; der Betrag entsteht aus genau diesen beiden Zahlen
    /// (<see cref="Betrag"/>). Damit ist die Herleitung „0,041 €/kWh × 72.000 kWh"
    /// nachvollziehbar gespeichert (Leitentscheidung L5) — und die Wirtschaftlichkeit
    /// rechnet ohne einen einzigen zusätzlichen Datenbankzugriff denselben Wert wie die
    /// Kostenmaske.
    /// </para>
    ///
    /// <para>
    /// <b>ANWENDERENTSCHEID W5‑B‑8 (09.09.2026): „x % der Investitionssumme" rechnet auf
    /// die KASKADE.</b> Die Investitionssumme kommt aus <see cref="InvestKaskade"/> statt
    /// aus <c>SUM(EingegebenerWert)</c> — siehe <see cref="InvestSummeFuer"/>. Damit ist
    /// der VIERTE Leseweg der Kategorie-1-Zeilen geschlossen, den W5‑B‑7 auf der
    /// Investseite bereits abgeschafft hatte.
    /// </para>
    /// </summary>
    internal static class BetriebskostenCtrl
    {

        // ------------------------------------------------------------- Der Rechenweg

        /// <summary>
        /// Der EINE Rechenweg je Bemessungsart. Reine Funktion — kein Datenbankzugriff,
        /// keine Kultur, kein Zustand (Leitentscheidung L9).
        /// </summary>
        /// <param name="bemessung">Wert aus <c>DbWerte.BEMESSUNG_*</c>; leer gilt als <c>BETRAG</c>.</param>
        /// <param name="eingegeben">Der erfasste Betrag [€/a] — gilt bei <c>BETRAG</c>.</param>
        /// <param name="menge">Bezugsmenge (€, h/a, kWh/a).</param>
        /// <param name="satz">Satz (%, €/h, €/kWh).</param>
        /// <param name="istErloes">true = Erlös; das Ergebnis ist dann ≤ 0.</param>
        /// <remarks>
        /// <b>Vorzeichen.</b> Der Rückgabewert ist immer die Zahlungswirkung in €/a:
        /// positiv = Ausgabe, negativ = Einnahme. Bei einer Erlösposition wird der Betrag
        /// deshalb auf sein negatives Vorzeichen gezwungen — ein Erlös kann so nirgends
        /// als Kosten in eine Summe geraten, gleichgültig mit welchem Vorzeichen Menge
        /// und Satz erfasst wurden.
        /// </remarks>
        internal static double Betrag(string bemessung, double eingegeben,
                                      double? menge, double? satz, bool istErloes)
        {
            double wert;

            if (string.IsNullOrEmpty(bemessung) ||
                string.Equals(bemessung, DbWerte.BEMESSUNG_BETRAG, StringComparison.Ordinal) ||
                string.Equals(bemessung, DbWerte.BEMESSUNG_JAHRESBETRAG, StringComparison.Ordinal))
            {
                // JAHRESBETRAG ist die zweite ABSOLUTE Art (Etappe KD1/KD3, Konzept
                // Kostendialoge § 5.3): fester Jahresbetrag ohne Bezugsgröße — er trägt
                // seinen Wert wie BETRAG direkt in EingegebenerWert. Ohne diesen Zweig
                // fiele er unten in die "nicht gepflegt = 0"-Klammer.
                wert = eingegeben;
            }
            else if (!menge.HasValue || !satz.HasValue)
            {
                // ANWENDERENTSCHEID I-2 (30.08.2026, Paket FX2): „Wenn eine Bemessungsart
                // 0 ergibt, nimm den erfassten Wert > 0."
                //
                // Fehlt eine der beiden Zahlen, ist die Ableitung NICHT RECHENBAR. Bis
                // hierher galt dann 0 — eine abgeleitete Zeile mit erfasstem Betrag fiel
                // also still aus der Rechnung (Befund I-2 der Rechenwege-Formelkarte).
                // Sie verhält sich jetzt wie BETRAG: der erfasste Wert gilt. Das ist
                // dieselbe Klammer, die der „unbekannte Wert"-Zweig unten schon immer
                // hatte, und dieselbe Vorsicht — ein Betrag verschwindet nicht wortlos.
                //
                // NUR dieser Zweig ändert sich. Eine ECHTE Ableitung mit Menge 0 (die
                // Baugröße ist wirklich 0) läuft weiter unten durch und ergibt 0 — der
                // Unterschied ist „nicht ermittelbar" gegen „ermittelt und null", und
                // genau den unterscheidet das Datenmodell mit NULL.
                //
                // FOLGE für die Endenergie-Arten (Konzept BHKW-Wirtschaftlichkeit § 4.5,
                // „ohne Lauf keine Menge, kein Betrag"): Liefert der Auflöser null, greift
                // ab jetzt der erfasste Wert statt der 0. Das ist vom Anwenderentscheid
                // ausdrücklich gedeckt und mit ihm gemessen.
                wert = eingegeben;
            }
            else
            {
                double m = menge.Value, s = satz.Value;

                // ETAPPE H1 — die beiden Endenergie-Bemessungen rechnen wie jede andere
                // Prozentangabe. Die MENGE ist dabei ein ERGEBNISWERT aus dem
                // Simulationslauf, kein Eingabewert (Festlegung 29.08.2026); im Dialog
                // wird nur der Satz gepflegt.
                //
                // WEG B liefert eine Strommenge, keine Kosten: Der Bezugsgroessen-Aufloeser
                // uebergibt fuer PROZENT_ENDENERGIEBEDARF deshalb den BEWERTETEN Bedarf
                // (kWh x Strombezugspreis). Das ist rechnerisch dasselbe wie
                // "Menge x Satz/100 x Preis" - die Multiplikation ist kommutativ - und
                // kommt ohne zweite Formel aus. Die unbewertete Menge bleibt fuer die
                // Herleitungszeile erhalten.
                if (string.Equals(bemessung, DbWerte.BEMESSUNG_PROZENT_INVESTITION, StringComparison.Ordinal) ||
                    string.Equals(bemessung, DbWerte.BEMESSUNG_PROZENT_BRENNSTOFFKOSTEN, StringComparison.Ordinal) ||
                    string.Equals(bemessung, DbWerte.BEMESSUNG_PROZENT_ERZEUGERKOSTEN, StringComparison.Ordinal) ||
                    string.Equals(bemessung, DbWerte.BEMESSUNG_PROZENT_STROMKOSTEN, StringComparison.Ordinal) ||
                    string.Equals(bemessung, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN, StringComparison.Ordinal) ||
                    string.Equals(bemessung, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF, StringComparison.Ordinal))
                    wert = m * s / 100.0;
                else if (string.Equals(bemessung, DbWerte.BEMESSUNG_EUR_PRO_H, StringComparison.Ordinal) ||
                         string.Equals(bemessung, DbWerte.BEMESSUNG_EUR_PRO_KWH, StringComparison.Ordinal) ||
                         string.Equals(bemessung, DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH, StringComparison.Ordinal) ||
                         string.Equals(bemessung, DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH, StringComparison.Ordinal) ||
                         string.Equals(bemessung, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, StringComparison.Ordinal) ||
                         string.Equals(bemessung, DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG, StringComparison.Ordinal) ||
                         string.Equals(bemessung, DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH, StringComparison.Ordinal) ||
                         string.Equals(bemessung, DbWerte.BEMESSUNG_EUR_PRO_KWP, StringComparison.Ordinal) ||
                         string.Equals(bemessung, DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET, StringComparison.Ordinal) ||
                         string.Equals(bemessung, DbWerte.BEMESSUNG_EUR_PRO_M2_KOLLEKTOR, StringComparison.Ordinal))
                    wert = m * s;
                else
                    wert = eingegeben;      // unbekannter Wert: wie BETRAG, nie stillschweigend 0
            }

            if (istErloes && wert > 0) wert = -wert;
            return wert;
        }

        /// <summary>Kurzform für eine gelesene Position.</summary>
        internal static double Betrag(double eingegeben, KostenPositionCtrl.Zusatz z)
        {
            if (z == null) return eingegeben;
            return Betrag(z.Bemessung, eingegeben, z.Menge, z.Einheitpreis, z.IstErloes);
        }

        /// <summary>
        /// ETAPPE E8c (E8b‑Q2) — der <b>Faktor einer Bemessungsart</b>: 1 für eine Art
        /// „Satz je Einheit" (Betrag = Menge × Satz), 0,01 für eine Prozentart (Betrag =
        /// Menge × Satz / 100) und <c>null</c> für eine FESTE Art — fester Betrag, fester
        /// Jahresbetrag, eine leere Spalte und ein unbekannter Steuerwert, die
        /// <see cref="Betrag"/> alle mit dem erfassten Betrag rechnet.
        ///
        /// <para><b>Gefragt wird der EINE Rechenweg, keine zweite Liste.</b> Die Antwort ist
        /// <see cref="Betrag"/> für Menge 1, Satz 1 und einen erfassten Betrag 0. Herleitung
        /// und Formelmappe (Stufe 3) fragen hier; eine neue Art im Rechenweg kann so nicht an
        /// ihnen vorbeilaufen — genau das war der Befund E8b‑Q2: Der Bemessungstext der
        /// Berichte kannte 4 von 17 Arten und nannte die übrigen „fester Betrag".</para>
        /// </summary>
        internal static double? Bemessungsfaktor(string bemessung)
        {
            double faktor = Betrag(bemessung, 0.0, 1.0, 1.0, false);
            if (faktor == 1.0) return 1.0;
            if (Math.Abs(faktor - 0.01) < 1e-15) return 0.01;
            return null;
        }

        /// <summary>
        /// Einheitenzeichen des SATZES einer Bemessungsart („%", „€/h", „€/kWh", „€").
        /// <b>Nicht lokalisiert</b> — reine Einheitenzeichen ohne Wortbestand, in beiden
        /// Sprachen gleich; dieselbe Ausnahme wie bei den typografischen Marken
        /// (Lokalisierungskatalog).
        /// </summary>
        internal static string SatzEinheit(string bemessung)
        {
            return SatzEinheit(bemessung, 0);
        }

        /// <summary>
        /// ANWENDERENTSCHEID 15.09.2026: dieselbe Einheit, aber IN DIESEM GEWERK
        /// (<c>Tab_KostenKomponente.ID</c>; 0 = unbekannt). „je kW Leistung" trägt am
        /// Pufferspeicher „€/Ltr.", weil seine Bezugsgröße das Volumen ist — eine
        /// Herleitung „1.000,00 × 0,700 €/kW" wäre dort schlicht falsch.
        /// <inheritdoc cref="SatzEinheit(string)" path="/summary/text()[last()]"/>
        /// </summary>
        internal static string SatzEinheit(string bemessung, int komponente)
        {
            return SatzEinheit(bemessung, komponente, false);
        }

        /// <summary>
        /// U33 (18.09.2026): dieselbe Einheit, aber IM RASTER — <paramref name="betrieb"/>
        /// = true fragt das Betriebsraster. Ein Leistungssatz trägt dort sein Jahr selbst
        /// („€/kWp·a"), weil die Bezugsgröße kWp keines kennt; bei jeder anderen Art
        /// fällt die Antwort gleich aus.
        /// <inheritdoc cref="SatzEinheit(string)" path="/summary/text()[last()]"/>
        /// </summary>
        internal static string SatzEinheit(string bemessung, int komponente, bool betrieb)
        {
            // Die KD-Bemessungen (Etappe KD1+) tragen ihre Einheit im BemessungKatalog —
            // EINE Wahrheit für Alt-Dialog und Komponenten-Kostendialog.
            BemessungKatalog.Info kd = BemessungKatalog.Finde(bemessung);
            if (kd != null &&
                !string.Equals(bemessung, DbWerte.BEMESSUNG_BETRAG, StringComparison.Ordinal))
                return BemessungKatalog.Einheit(bemessung, komponente, betrieb);

            if (string.Equals(bemessung, DbWerte.BEMESSUNG_PROZENT_INVESTITION, StringComparison.Ordinal) ||
                string.Equals(bemessung, DbWerte.BEMESSUNG_PROZENT_BRENNSTOFFKOSTEN, StringComparison.Ordinal))
                return "%";
            if (string.Equals(bemessung, DbWerte.BEMESSUNG_EUR_PRO_H, StringComparison.Ordinal))
                return "€/h";
            if (string.Equals(bemessung, DbWerte.BEMESSUNG_EUR_PRO_KWH, StringComparison.Ordinal))
                return "€/kWh";
            return DbWerte.KOSTEN_EINHEIT_EURO;
        }

        /// <summary>
        /// Einheitenzeichen der BEZUGSMENGE einer Bemessungsart („€", „h/a", „kWh/a",
        /// „kW", „kWp", „Ltr.").
        /// <inheritdoc cref="SatzEinheit(string)" path="/summary/text()[last()]"/>
        ///
        /// <para><b>ETAPPE E2 (Befund B-7).</b> Bis hierher kannte diese Methode genau
        /// zwei Arten und beschriftete jede andere Bezugsmenge mit „€" — die
        /// Herleitungszeile las sich dann „500,00 € × 12,000 €/kW·a", wo eine Leistung
        /// in kW steht. Die Antwort kommt seither aus dem <see cref="BemessungKatalog"/>,
        /// genau wie bei <see cref="SatzEinheit(string,int,bool)"/>: EINE Wahrheit für
        /// Satz und Menge, samt der gewerkeigenen Bezugsgröße (Pufferspeicher „Ltr.").</para>
        /// </summary>
        /// <param name="komponente"><c>Tab_KostenKomponente.ID</c> der Zeile; 0 =
        /// unbekannt, dann gilt die allgemeine Bezugsgröße des Katalogs.</param>
        internal static string MengenEinheit(string bemessung, int komponente)
        {
            return BemessungKatalog.Mengeneinheit(bemessung, komponente);
        }

        /// <summary>
        /// <c>Tab_KostenKomponente.ID</c> der Komponenten, deren Investition als
        /// Bezugsgröße dient. Dieselben Nummern wie in <c>Tab_KostenKomponente</c>;
        /// sie stehen hier als benannte Konstanten, damit die Zuordnung im
        /// Betriebskostenpfad nicht als nackte Zahl auftaucht.
        /// </summary>
        internal const int KOMPONENTE_HEIZKESSEL = 2;
        internal const int KOMPONENTE_BHKW = 7;

        /// <summary>ANWENDERENTSCHEID 15.09.2026: der Pufferspeicher (6) — Quelle wie
        /// oben. Er bemisst sich seither an seinem VOLUMEN, und Landkarte wie
        /// Beschriftung fragen ihn beim Namen.</summary>
        internal const int KOMPONENTE_PUFFERSPEICHER = 6;

        /// <summary>
        /// ANWENDERENTSCHEID W5‑B‑8 (09.09.2026): die Kaskadensummen eines Projekts,
        /// geschlüsselt nach (Komponenten-Id, Anlagen-Id) — EINMAL gelesen, mehrfach
        /// gestaffelt. Nie <c>null</c>: eine LEERE Karte heißt „der Rechenweg hat nichts
        /// anzubieten" (Datenbank ohne die Spalten aus Schritt 19, oder keine
        /// Kategorie-1-Zeile) und schaltet in <see cref="InvestSumme"/> den
        /// dokumentierten SQL-Rückfall ein — dieselbe Vorsorge wie in
        /// <c>KostenSummenCtrl.Rechenwegsummen</c>.
        /// </summary>
        internal static Dictionary<KeyValuePair<int, int>, double> Kaskadensummen(int projektID)
        {
            return Kaskadensummen(projektID, null);
        }

        /// <summary>
        /// ANWENDERENTSCHEID W5‑B‑11 (09.09.2026, VALERI-Lücke G11): dieselben
        /// Kaskadensummen, aber im SZENARIO des Parametersatzes.
        ///
        /// <para>Der Satz trägt sein Szenario selbst mit (<see cref="SzenarioSatz.Szenario"/>);
        /// <paramref name="satz"/> = <c>null</c> heißt deshalb „Erwartungslauf ohne
        /// Ausschlag" und ist Zeichen für Zeichen der Weg von vorher. Mit Satz gelten
        /// die gepflegten Best-/Worst-Zeilenwerte UND der pauschale Ausschlag auf die
        /// nicht gepflegten Zeilen — dieselbe Vorrangregel wie in
        /// <c>WirtschaftlichkeitCtrl.LiesInvestitionen</c>, weil beide Wege dieselbe
        /// Methode fragen (<see cref="InvestKaskade.BetragImSzenario"/>).</para>
        /// </summary>
        internal static Dictionary<KeyValuePair<int, int>, double> Kaskadensummen(
            int projektID, SzenarioSatz satz)
        {
            string szenario = satz != null ? satz.Szenario : WirtschaftlichkeitSzenario.ERWARTET;
            try { return InvestKaskade.Summen(projektID, szenario, satz); }
            catch { return new Dictionary<KeyValuePair<int, int>, double>(); }
        }

        /// <summary>
        /// Summe der Investitionspositionen (Kategorie 1) aus dem RECHENWEG.
        /// <paramref name="komponentenID"/> = 0 heißt „ganzes Projekt",
        /// <paramref name="idAnlage"/> = 0 „ohne Anlagenfilter".
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>ANWENDERENTSCHEID W5‑B‑8 (09.09.2026) — die Basis ist die KASKADE.</b> Bis
        /// hierher summierte diese Methode roh <c>SUM(EingegebenerWert)</c> der
        /// Kategorie-1-Zeilen. Damit bemaß sich „x % der Investitionssumme" auf der
        /// BETRIEBSSEITE an einer Zahl, die satzbasierte Zeilen (Menge × Satz) und alle
        /// Prozentzeilen der Investseite gar nicht enthielt — der VIERTE Leseweg
        /// derselben Zeilen, den W5‑B‑7 auf der Investseite abgeschafft, hier aber
        /// bewusst stehen gelassen hatte (er verändert die Kapitalwertrechnung und
        /// gehörte deshalb vor eine Entscheidung). Seit dem Entscheid ist die
        /// Bezugsgröße die Summe der <see cref="InvestKaskade"/> — dieselbe Stufung
        /// Anlage → Komponente → Projekt wie deren Runde 3, dieselbe Zahl wie Kachel,
        /// Dialog und Anlagentabelle.
        /// </para>
        /// <para>
        /// <b>Wie SQL zählt.</b> Trifft der Filter keine einzige Gruppe, ist das Ergebnis
        /// <c>null</c> — genau wie <c>SUM(...)</c> über keine Zeile. Nur so geht
        /// <see cref="InvestSummeFuer"/> eine Stufe höher, statt eine 0 auszuweisen.
        /// </para>
        /// <para>
        /// <b>Zuschuss.</b> <see cref="InvestKaskade.Summen"/> trägt eine Zuschusszeile mit
        /// Beitrag 0, legt ihren Schlüssel aber an. Eine Komponente mit AUSSCHLIESSLICH
        /// Zuschusszeilen liefert hier deshalb 0,00 statt <c>null</c> — für die
        /// Staffelung ist das gleichwertig (auch die 0 lässt
        /// <see cref="InvestSummeFuer"/> weiterlaufen), und die K5-Regel „vor
        /// Zuschussabzug" bleibt Wort für Wort erhalten.
        /// </para>
        /// </remarks>
        private static double? InvestSumme(int projektID, int komponentenID, int idAnlage,
                                           Dictionary<KeyValuePair<int, int>, double> kaskade)
        {
            if (kaskade != null && kaskade.Count > 0)
            {
                double summe = 0;
                bool getroffen = false;
                foreach (KeyValuePair<KeyValuePair<int, int>, double> e in kaskade)
                {
                    if (komponentenID > 0 && e.Key.Key != komponentenID) continue;
                    if (idAnlage > 0 && e.Key.Value != idAnlage) continue;
                    summe += e.Value;
                    getroffen = true;
                }
                return getroffen ? summe : (double?)null;
            }
            return InvestSummeSql(projektID, komponentenID, idAnlage);
        }

        /// <summary>
        /// DER DOKUMENTIERTE RÜCKFALL (W5‑B‑8): die rohe Spaltensumme, wie sie bis zum
        /// 09.09.2026 der einzige Weg war. Sie gilt nur noch, wenn die Kaskade nichts
        /// anzubieten hat — also auf einer Datenbank ohne die Spalten aus Schritt 19.
        /// Auf so einer Datenbank rechnet die Kaskade ohnehin Zeile für Zeile
        /// <c>EingegebenerWert</c>, weil es dort weder Bemessung noch Satz noch
        /// Kostenart gibt; die beiden Wege fallen dann zusammen.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>ETAPPE K5 — ohne Zuschusspositionen.</b> Die prozentualen Bemessungen der
        /// VDI 2067 („% der Investitionssumme") rechnen ausdrücklich <b>vor</b>
        /// Zuschussabzug (Konzept § 7.4, letzter Punkt). Das ist keine Feinheit, sondern
        /// beides zusammen: fachlich richtig — instand zu halten ist die Anlage, nicht der
        /// Eigenanteil — und die Auflösung eines Alt-Widerspruchs, denn die Altanwendung
        /// war an dieser Stelle uneinheitlich (Dialog gegen Blatt, Anhang A).
        /// </para>
        /// <para>
        /// <b>Ohne den Ausschluss wäre es sogar falsch herum.</b> Ein Zuschuss steht als
        /// POSITIVER Betrag in <c>EingegebenerWert</c> (Begründung an
        /// <see cref="DbWerte.KOSTENART_ZUSCHUSS"/>). Eine ungefilterte Summe würde ihn
        /// also nicht abziehen, sondern ADDIEREN — die Instandhaltung bemäße sich an einer
        /// Investitionssumme, die es nie gab.
        /// </para>
        /// <para>
        /// <b>Rückfallebene.</b> Fehlt die Spalte <c>Kostenart</c> (nie migrierte
        /// Datenbank), läuft die Abfrage ohne die Einschränkung — also genau wie vor K5.
        /// In einer solchen Datenbank kann es keine Zuschusszeile geben.
        /// </para>
        /// </remarks>
        private static double? InvestSummeSql(int projektID, int komponentenID, int idAnlage)
        {
            bool mitKostenart = false;
            try { mitKostenart = KostenPositionCtrl.StelleSpaltenSicher(); }
            catch { }

            try
            {
                string sql = "SELECT SUM(EingegebenerWert) FROM " + SchemaKatalog.TAB_PROJEKTWERTE +
                             " WHERE ProjektID = ? AND KategorieID = ?";
                var ps = new List<DbParam>
                {
                    new DbParam("@p", projektID),
                    new DbParam("@k", DbWerte.KOSTEN_KATEGORIE_INVESTITION)
                };
                if (komponentenID > 0)
                {
                    sql += " AND KomponentenID = ?";
                    ps.Add(new DbParam("@c", komponentenID));
                }
                if (idAnlage > 0 && AnlagenSpalteVorhanden())
                {
                    sql += " AND [" + SchemaKatalog.SPALTE_PW_ID_ANLAGE + "] = ?";
                    ps.Add(new DbParam("@a", idAnlage));
                }
                if (mitKostenart)
                {
                    // NULL und Leerstring bleiben drin: Das sind die Bestandszeilen (bzw.
                    // die, die Schritt 19b nicht erreicht hat), und sie sind Investitionen.
                    sql += " AND (([" + SchemaKatalog.SPALTE_PW_KOSTENART + "] IS NULL) OR ([" +
                           SchemaKatalog.SPALTE_PW_KOSTENART + "] <> ?))";
                    ps.Add(new DbParam("@art", DbWerte.KOSTENART_ZUSCHUSS));
                }

                object o = DataRepository.ExecuteScalar(sql, ps.ToArray());
                if (o == null || o == DBNull.Value) return null;
                return Convert.ToDouble(o);
            }
            catch { return null; }
        }

        /// <summary>ETAPPE H4a: Cache der Spaltenprobe <c>Tab_ProjektWerte.ID_Anlage</c>
        /// (Muster <see cref="WirtschaftlichkeitCtrl.SpalteVorhanden"/>).</summary>
        private static bool? _anlagenSpalte;

        private static bool AnlagenSpalteVorhanden()
        {
            if (_anlagenSpalte.HasValue) return _anlagenSpalte.Value;
            _anlagenSpalte = WirtschaftlichkeitCtrl.SpalteVorhanden(
                SchemaKatalog.TAB_PROJEKTWERTE, SchemaKatalog.SPALTE_PW_ID_ANLAGE);
            return _anlagenSpalte.Value;
        }

        /// <summary>
        /// ETAPPE H4a: Bezugsgröße „% der Investition" (Konzept Kostendialoge § 5.3:
        /// Summe der Investitionskosten der Komponente VOR Zuschussabzug) — stufig:
        /// Trägt die Position eine Anlage und existieren Investitionszeilen an genau
        /// dieser Anlage, zählt deren Summe; sonst die Komponentensumme (die
        /// dokumentierte Regel), notfalls das ganze Projekt. null = nichts erfasst.
        /// <para><b>W5‑B‑8 (09.09.2026):</b> Gestaffelt wird seither über die
        /// <see cref="InvestKaskade"/> statt über <c>SUM(EingegebenerWert)</c> —
        /// dieselbe Stufung, dieselbe Reihenfolge, andere (vollständige) Basis.</para>
        /// </summary>
        internal static double? InvestSummeFuer(int projektID, int komponentenID, int idAnlage)
        {
            return InvestSummeFuer(projektID, komponentenID, idAnlage, Kaskadensummen(projektID));
        }

        /// <summary>W5‑B‑8: dieselbe Staffelung mit BEREITS GELESENER Kaskade — für
        /// Leseschleifen, die viele Positionen desselben Projekts abarbeiten
        /// (<c>WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe</c> /
        /// <c>LiesBetriebskostenPositionen</c>). Ohne diese Überladung liefe der ganze
        /// Rechenweg der Kategorie 1 je Betriebskostenzeile erneut.</summary>
        internal static double? InvestSummeFuer(int projektID, int komponentenID, int idAnlage,
                                                Dictionary<KeyValuePair<int, int>, double> kaskade)
        {
            if (idAnlage > 0)
            {
                double? anlage = InvestSumme(projektID, komponentenID, idAnlage, kaskade);
                if (anlage.HasValue && anlage.Value != 0) return anlage;
            }
            if (komponentenID > 0)
            {
                double? komponente = InvestSumme(projektID, komponentenID, 0, kaskade);
                if (komponente.HasValue && komponente.Value != 0) return komponente;
            }
            return InvestSumme(projektID, 0, 0, kaskade);
        }
    }
}
