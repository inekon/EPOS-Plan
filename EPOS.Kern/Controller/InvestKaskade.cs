using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// DER EINE RECHENWEG DER INVESTITIONSPOSITIONEN (Kategorie 1) — die
    /// ETAPPE-H4b-Kaskade des Konzepts Kostendialoge § 5.3, herausgelöst aus
    /// <see cref="WirtschaftlichkeitCtrl.LiesInvestitionen"/>
    /// (Anwenderbefund W5‑B‑7 vom 08.09.2026).
    ///
    /// <para><b>Warum es diese Klasse gibt.</b> Bis zum Befund lasen DREI Stellen
    /// dieselben <c>Tab_ProjektWerte</c>-Zeilen auf DREI Arten: die
    /// Kapitalwertrechnung mit der vollen Kaskade, der Dialog Kostenverwaltung nur
    /// über die GESPEICHERTE Menge (für „% der Investition" wird nie eine
    /// gespeichert — <see cref="WirtschaftlichkeitCtrl.MengeAusweisen"/> schließt
    /// die Kombination ausdrücklich aus, also stand dort 0), und die Seite
    /// „Berichte &amp; Kosten" als reine SQL-Summe über <c>EingegebenerWert</c>
    /// (also ohne Menge × Satz und ohne jede Prozentzeile). Dieselbe
    /// Photovoltaik-Anlage zeigte damit 6.961,80 € in der Wirtschaftlichkeit,
    /// 5.660,00 € im Dialog und 1.500,00 € in der Anlagentabelle. Seit W5‑B‑7
    /// fragen alle drei hier.</para>
    ///
    /// <para><b>Die Kaskade ist Wort für Wort die der Kapitalwertrechnung</b> —
    /// sie ist hierher GEZOGEN, nicht nachgebaut, damit das Herauslösen keine
    /// Fachänderung ist:</para>
    /// <list type="number">
    ///   <item><description><b>Runde 1</b> — alle direkten Zeilen: <c>BETRAG</c>
    ///   unverändert, abgeleitete Arten Menge × Satz mit frischer Baugröße aus
    ///   <see cref="TechnikPlanwertCtrl.BaugroesseSumme"/> (Rückfall: gespeicherte
    ///   Menge).</description></item>
    ///   <item><description><b>Runde 2</b> — „% der Erzeugerkosten": Basis ist der
    ///   abgeleitete Betrag der Hauptposition(en) derselben Komponente.</description></item>
    ///   <item><description><b>Runde 3</b> — „% der Investition": Basis ist die
    ///   eingefrorene Summe der direkten Zeilen (ohne Zuschuss), stufig
    ///   Anlage → Komponente → Projekt. Anwenderentscheid I‑3: %-Zeilen zählen
    ///   einander nie mit.</description></item>
    /// </list>
    ///
    /// <para><b>Zuschuss.</b> Zuschusszeilen (Kostenart
    /// <see cref="DbWerte.KOSTENART_ZUSCHUSS"/>) tragen ihren Betrag wie jede
    /// andere Zeile, sind aber als solche gekennzeichnet: Der Aufrufer entscheidet,
    /// ob er sie abzieht (Kapitalwertrechnung: I₀-seitig, K5) oder aus einer
    /// Anzeigesumme heraushält.</para>
    /// </summary>
    internal static class InvestKaskade
    {
        /// <summary>Eine Kategorie-1-Zeile mit ihrem WIRKSAMEN Betrag.</summary>
        internal sealed class Zeile
        {
            /// <summary><c>Tab_ProjektWerte.ID</c> — der Schlüssel, unter dem Dialog
            /// und Kostenseite ihre Zeile wiederfinden.</summary>
            public int Id;

            public int Komponente;
            public int Anlage;

            /// <summary>Bemessungsart, Steuerwert <c>DbWerte.BEMESSUNG_*</c>.</summary>
            public string Bem = "";

            /// <summary>Satz (<c>Einheitpreis</c>); null = nicht gepflegt.</summary>
            public double? Satz;

            /// <summary>GESPEICHERTE Menge (Ausweisgröße „Stand des Laufs").</summary>
            public double? Menge;

            /// <summary>Szenariowert aus <c>EingegebenerWert</c>/<c>BestCase</c>/<c>WorstCase</c>.</summary>
            public double Wert;

            /// <summary>
            /// ETAPPE W5‑B‑9 (09.09.2026): Kam <see cref="Wert"/> aus der Best- bzw.
            /// Worst-Spalte, oder ist er auf den Erwartungswert zurückgefallen?
            ///
            /// <para><b>Wozu.</b> Der Szenario-Parametersatz trägt einen pauschalen
            /// Investitionsausschlag. Er darf NUR dort greifen, wo keine Zeile gepflegt
            /// ist — sonst zählte er doppelt: Wer 55.000 € als Worst-Case erfasst hat,
            /// meint diese Zahl, nicht diese Zahl plus 10 %. Die Vorrangregel steht in
            /// <c>Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md</c> § 2.2.</para>
            ///
            /// <para>Im Szenario ERWARTET ist das Feld immer <c>false</c> — dort gibt es
            /// keine Szenariospalte und auch keinen Ausschlag.</para>
            /// </summary>
            internal bool WertGepflegt;

            /// <inheritdoc cref="WertGepflegt"/>
            internal bool DauerGepflegt;

            /// <summary><c>EingegebenerWert</c> (VALERI-Vergleichsbasis).</summary>
            public double Erwartet;

            /// <summary>Nutzungsdauer im gewählten Szenario.</summary>
            public double Dauer;

            /// <summary>Startjahr der Position (KD6 § 11); 0 = t0.</summary>
            public int Start;

            /// <summary>Kostenart „Zuschuss" (K5).</summary>
            public bool Zuschuss;

            /// <summary><c>Tab_Kostenfaktor.IsMainComponent</c>.</summary>
            public bool Haupt;

            /// <summary>Der wirksame Betrag nach der Ableitung [€].</summary>
            public double Betrag;

            /// <summary>Die BEZUGSGRÖSSE, mit der der Betrag entstanden ist
            /// (Kaskadenbasis, Baugröße oder Konserve); null bei <c>BETRAG</c> und
            /// bei gepflegtem Szenariowert. Reine Auskunft für den Werkzeugtipp
            /// des Dialogs („3 % von 5.660,00 €") — sie wird nirgends
            /// zurückgeschrieben.</summary>
            public double? Basis;

            internal bool Abgeleitet;
        }

        /// <summary>Alle Kategorie-1-Zeilen eines Projekts mit ihrem wirksamen
        /// Betrag — in der Lesereihenfolge der Datenbank.</summary>
        internal static List<Zeile> Lies(int idProjekt, string szenario)
        {
            var puffer = new List<Zeile>();

            bool mitKostenart = false;
            try { mitKostenart = KostenPositionCtrl.StelleSpaltenSicher(); }
            catch { }

            try
            {
                string felder = "w.ID, w.EingegebenerWert, w.BestCase, w.WorstCase, w.Nutzungsdauer, " +
                                "w.BestCase_Nutzungsdauer, w.WorstCase_Nutzungsdauer, " +
                                "w.KomponentenID, f.IsMainComponent";
                if (mitKostenart)
                    felder += ", w.[" + SchemaKatalog.SPALTE_PW_KOSTENART + "]" +
                              ", w.[" + SchemaKatalog.SPALTE_PW_BEMESSUNG + "]" +
                              ", w.[" + SchemaKatalog.SPALTE_PW_MENGE + "]" +
                              ", w.[" + SchemaKatalog.SPALTE_PW_EINHEITPREIS + "]";
                // ETAPPE KD6 (§ 11, FK10): das Startjahr je Position — die Spalte kommt
                // mit Migrationsschritt 38. Sie wird nur ANGEFRAGT, wenn sie existiert:
                // Ein SELECT auf eine fehlende Spalte würde die GANZE Abfrage kippen
                // und die Investitionsliste still leeren (der catch unten schluckt).
                if (WirtschaftlichkeitCtrl.StartjahrSpalteVorhanden())
                    felder += ", w.[" + SchemaKatalog.SPALTE_PW_STARTJAHR + "]";
                if (WirtschaftlichkeitCtrl.AnlagenSpalteVorhanden())
                    felder += ", w.[" + SchemaKatalog.SPALTE_PW_ID_ANLAGE + "]";

                DataTable dt = DataRepository.GetDataTable(
                    "SELECT " + felder +
                    " FROM Tab_ProjektWerte AS w LEFT JOIN Tab_Kostenfaktor AS f " +
                    "ON w.StammID = f.StammID " +
                    "WHERE w.ProjektID = ? AND w.KategorieID = 1",
                    new DbParam("@p", idProjekt));
                if (dt == null) return puffer;

                // ETAPPE H4b: erst puffern, dann in DREI Runden ableiten — die
                // §-5.3-Kaskade des Kostendialoge-Konzepts: Hauptposition →
                // „% der Erzeugerkosten" → Summe → „% der Investition". Zeilen mit
                // Bemessung BETRAG (der GESAMTE Bestand nach Schritt 19b) laufen in
                // Runde 1 exakt den alten Weg; abgeleitete Arten rechnen
                // Menge × Satz, die Menge notfalls frisch aus der Gerätewelt
                // (TechnikPlanwertCtrl.BaugroesseSumme) — eine gepflegte Menge und
                // gepflegte Szenariowerte behalten Vorrang (VALERI-Muster wie E3).
                foreach (DataRow r in dt.Rows)
                {
                    var z = new Zeile();
                    z.Id = r.Table.Columns.Contains("ID") && r["ID"] != DBNull.Value
                        ? Convert.ToInt32(r["ID"]) : 0;
                    z.Wert = WirtschaftlichkeitCtrl.Szenariowert(
                        r, szenario, "EingegebenerWert", "BestCase", "WorstCase",
                        out z.WertGepflegt);
                    z.Erwartet = WirtschaftlichkeitCtrl.D(r, "EingegebenerWert") ?? 0;
                    z.Dauer = WirtschaftlichkeitCtrl.Szenariowert(
                        r, szenario, "Nutzungsdauer",
                        "BestCase_Nutzungsdauer", "WorstCase_Nutzungsdauer",
                        out z.DauerGepflegt);
                    z.Start = WirtschaftlichkeitCtrl.StartJahrDerZeile(r);
                    z.Zuschuss = mitKostenart && WirtschaftlichkeitCtrl.IstZuschuss(r);
                    z.Haupt = WirtschaftlichkeitCtrl.B(r, "IsMainComponent");
                    WirtschaftlichkeitCtrl.KomponenteUndAnlage(r, out z.Komponente, out z.Anlage);
                    if (mitKostenart)
                    {
                        z.Bem = WirtschaftlichkeitCtrl.Text(r, SchemaKatalog.SPALTE_PW_BEMESSUNG);
                        z.Satz = WirtschaftlichkeitCtrl.D(r, SchemaKatalog.SPALTE_PW_EINHEITPREIS);
                        z.Menge = WirtschaftlichkeitCtrl.D(r, SchemaKatalog.SPALTE_PW_MENGE);
                    }
                    puffer.Add(z);
                }

                // Runde 1 — alles außer den beiden %-Kaskadenarten.
                foreach (Zeile z in puffer)
                {
                    if (IstProzentErzeuger(z.Bem) ||
                        WirtschaftlichkeitCtrl.IstProzentInvest(z.Bem)) continue;
                    z.Betrag = InvestBetrag(z, idProjekt, null, out z.Basis);
                    z.Abgeleitet = true;
                }

                // Runde 2 — „% der Erzeugerkosten": Basis ist der abgeleitete Betrag
                // der Hauptposition(en) derselben Komponente.
                foreach (Zeile z in puffer)
                {
                    if (!IstProzentErzeuger(z.Bem)) continue;
                    double basis = 0; bool da = false;
                    foreach (Zeile h in puffer)
                        if (h.Abgeleitet && h.Haupt && !h.Zuschuss && h.Komponente == z.Komponente)
                        { basis += h.Betrag; da = true; }
                    z.Betrag = InvestBetrag(z, idProjekt, da && basis != 0 ? basis : (double?)null,
                                            out z.Basis);
                    z.Abgeleitet = true;
                }

                // Runde 3 — „% der Investition": Basis ist die Summe der in den Runden 1
                // und 2 abgeleiteten Beträge (ohne Zuschüsse), stufig Anlage → Komponente
                // → Projekt (dieselbe Semantik wie InvestSummeFuer der H4a).
                //
                // ANWENDERENTSCHEID I-3 (30.08.2026, Paket FX2) — ZWEI PHASEN.
                // Die Basiszeilen werden VOR der Zuweisungsschleife eingefroren. Bis
                // hierher setzte die Schleife jede fertige Zeile sofort auf
                // Abgeleitet = true; eine ZWEITE „% der Investition"-Zeile rechnete
                // deshalb die ERSTE in ihre Basis ein, und weil die Leseabfrage kein
                // ORDER BY trägt, entschied die Datenbank über das Ergebnis (Befund I-3).
                //
                // Der Entscheid: Jede Investition ist eine eigene Position mit eigener
                // Nutzungsdauer — das bleibt. %-Zeilen bemessen sich aber ausschließlich
                // an den DIREKTEN Zeilen der Runden 1 und 2 und zählen einander nie mit.
                // Damit ist das Ergebnis deterministisch und reihenfolgeunabhängig.
                var basisZeilen = new List<Zeile>();
                foreach (Zeile h in puffer)
                    if (h.Abgeleitet && !h.Zuschuss) basisZeilen.Add(h);

                foreach (Zeile z in puffer)
                {
                    if (z.Abgeleitet) continue;
                    double sAnlage = 0, sKomponente = 0, sProjekt = 0;
                    bool aDa = false, kDa = false;
                    foreach (Zeile h in basisZeilen)
                    {
                        sProjekt += h.Betrag;
                        if (z.Komponente > 0 && h.Komponente == z.Komponente)
                        {
                            sKomponente += h.Betrag; kDa = true;
                            if (z.Anlage > 0 && h.Anlage == z.Anlage) { sAnlage += h.Betrag; aDa = true; }
                        }
                    }
                    double basis = (aDa && sAnlage != 0) ? sAnlage
                                 : (kDa && sKomponente != 0) ? sKomponente : sProjekt;
                    z.Betrag = InvestBetrag(z, idProjekt, basis != 0 ? basis : (double?)null,
                                            out z.Basis);
                    z.Abgeleitet = true;
                }
            }
            catch { }
            return puffer;
        }

        /// <summary>Dieselben Zeilen, geschlüsselt nach <c>Tab_ProjektWerte.ID</c> —
        /// die Form, in der Dialog und Kostenseite ihre Zeile nachschlagen.
        /// Zeilen ohne Id (theoretisch: Abfrage ohne Schlüsselspalte) fallen weg.</summary>
        internal static Dictionary<int, Zeile> NachId(int idProjekt, string szenario)
        {
            var karte = new Dictionary<int, Zeile>();
            foreach (Zeile z in Lies(idProjekt, szenario))
                if (z.Id > 0) karte[z.Id] = z;
            return karte;
        }

        /// <summary>
        /// ETAPPE W5‑B‑11 (Anwenderentscheid 09.09.2026, VALERI-Lücke G11): der
        /// WIRKSAME Betrag einer Zeile im Szenario — der pauschale
        /// Investitionsausschlag des Parametersatzes, nach der Vorrangregel.
        ///
        /// <para><paramref name="satz"/> = <c>null</c> heißt „kein Ausschlag" und liefert
        /// <see cref="Zeile.Betrag"/> unverändert — der Weg des Szenarios ERWARTET und
        /// jeder Anzeige, die erfasste Zahlen zeigt. Eine Zeile mit gepflegtem Best-/
        /// Worst-Wert (<see cref="Zeile.WertGepflegt"/>) bleibt ebenfalls unangetastet:
        /// Wer 7.000 € als Worst-Case erfasst hat, meint diese Zahl, nicht diese Zahl
        /// plus 10 % (§ 2.2 des Szenarienkonzepts).</para>
        ///
        /// <para><b>Warum es diese Methode gibt.</b> Bis W5‑B‑10 stand die Regel nur in
        /// <c>WirtschaftlichkeitCtrl.LiesInvestitionen</c>. Seit G11 braucht sie auch
        /// <see cref="Summen"/> — die Bemessungsbasis der Betriebskostenzeilen
        /// „x % der Investitionssumme". Zwei Stellen mit derselben Regel wären zwei
        /// Stellen, an denen sie auseinanderlaufen kann.</para>
        /// </summary>
        internal static double BetragImSzenario(Zeile z, SzenarioSatz satz)
        {
            if (z == null) return 0.0;
            if (satz == null || z.WertGepflegt) return z.Betrag;
            return z.Betrag * satz.InvestFaktor;
        }

        /// <summary>
        /// Summen je Komponente und Anlage [€] — die Zahl, die die Anlagenzeile der
        /// Kostenseite und die Kostenzeile der Anlagendialoge zeigen.
        /// <para>Zuschusszeilen bleiben AUSSEN VOR, genau wie in
        /// <see cref="WirtschaftlichkeitCtrl.LiesInvestitionen"/> — sonst zeigte die
        /// Tabelle eine andere Investition als die Kachel darüber. Der Schlüssel ist
        /// (Komponenten-Id, Anlagen-Id); Anlage 0 = ohne (gültige) Anlagenzuordnung.
        /// Ein Schlüssel entsteht für JEDE Kategorie-1-Zeile — auch für eine reine
        /// Zuschusszeile, deren Betrag dann 0 beiträgt; daran entscheidet die
        /// Kostenseite „hat Positionen" gegen „—".</para>
        /// </summary>
        internal static Dictionary<KeyValuePair<int, int>, double> Summen(
            int idProjekt, string szenario)
        {
            return Summen(idProjekt, szenario, null);
        }

        /// <summary>
        /// ETAPPE W5‑B‑11 (Anwenderentscheid 09.09.2026, G11): dieselben Summen, aber
        /// mit dem pauschalen Investitionsausschlag des Szenarios.
        ///
        /// <para><b>Wozu.</b> Diese Karte ist die Bemessungsbasis der Betriebskosten-
        /// zeilen „x % der Investitionssumme" (H4a). Bis W5‑B‑10 wurde sie IMMER
        /// aus dem Erwartungslauf gebildet: Eine Anlage, die im Worst-Fall 10 % mehr
        /// kostet, hatte dort dieselbe Wartung wie im Erwartungsfall — anders als in
        /// der Sensitivität, die den Ausschlag längst mitzieht (FX5‑a). Der Anwender
        /// hat am 09.09.2026 entschieden, ihn auch im Szenario mitzuziehen.</para>
        ///
        /// <para><b>Die Vorrangregel gilt hier genauso</b>
        /// (<see cref="BetragImSzenario"/>): Eine Zeile mit gepflegtem Worst-Wert von
        /// 7.000 € trägt 7.000 € zur Basis bei, nicht 7.700 €. Ohne
        /// <paramref name="satz"/> ist diese Fassung Zeichen für Zeichen die von vorher.</para>
        /// </summary>
        internal static Dictionary<KeyValuePair<int, int>, double> Summen(
            int idProjekt, string szenario, SzenarioSatz satz)
        {
            var summen = new Dictionary<KeyValuePair<int, int>, double>();
            foreach (Zeile z in Lies(idProjekt, szenario))
            {
                var schluessel = new KeyValuePair<int, int>(z.Komponente, z.Anlage);
                double alt;
                summen.TryGetValue(schluessel, out alt);
                // Der SCHLUESSEL entsteht auch fuer eine reine Zuschusszeile - die
                // Kostenseite entscheidet an der Gruppe "hat Positionen" gegen "-";
                // nur ihr BETRAG bleibt aussen vor.
                summen[schluessel] = alt + (z.Zuschuss ? 0.0 : BetragImSzenario(z, satz));
            }
            return summen;
        }

        private static bool IstProzentErzeuger(string bem)
        {
            return string.Equals(bem, DbWerte.BEMESSUNG_PROZENT_ERZEUGERKOSTEN, StringComparison.Ordinal);
        }

        /// <summary>
        /// ETAPPE H4b: wirksamer Betrag einer Investitionszeile. BETRAG/leer = der
        /// Bestandsweg (Szenariowert unverändert); abgeleitete Arten rechnen
        /// Menge × Satz über <see cref="BetriebskostenCtrl.Betrag"/>. Gepflegte
        /// Best-/Worst-Beträge schlagen die Ableitung (VALERI).
        /// ETAPPE H2-1: Mengenreihenfolge FRISCH vor Konserve — erst
        /// <paramref name="kaskadenBasis"/> (Runden 2/3), dann die Gerätewelt,
        /// zuletzt die Menge-Spalte. Die ist nur Ausweisgröße („Stand des Laufs",
        /// Konzept BHKW-Wirtschaftlichkeit § 4.5), sonst rechnete die Kaskade nach
        /// einer Geräteänderung stillschweigend mit der alten Baugröße weiter.
        /// <para><b>W5‑B‑7:</b> <paramref name="verwendet"/> gibt die tatsächlich
        /// angesetzte Bezugsgröße heraus — dieselbe Zahl, mit der gerechnet wurde,
        /// für den Werkzeugtipp des Dialogs. Die Rechnung selbst ist unverändert.</para>
        /// </summary>
        private static double InvestBetrag(Zeile z, int idProjekt, double? kaskadenBasis,
                                           out double? verwendet)
        {
            verwendet = null;

            if (string.IsNullOrEmpty(z.Bem) ||
                string.Equals(z.Bem, DbWerte.BEMESSUNG_BETRAG, StringComparison.Ordinal))
                return z.Wert;

            bool szenarioGepflegt = Math.Abs(z.Wert - z.Erwartet) > 1e-9;
            if (szenarioGepflegt) return z.Wert;

            double? menge = kaskadenBasis;
            if (!menge.HasValue)
                menge = TechnikPlanwertCtrl.BaugroesseSumme(idProjekt, z.Komponente, z.Bem, z.Anlage);
            if (!menge.HasValue) menge = z.Menge;

            verwendet = menge;
            return BetriebskostenCtrl.Betrag(z.Bem, z.Erwartet, menge, z.Satz, false);
        }
    }
}
