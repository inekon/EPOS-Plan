using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die EINE Leseschicht für „was kostet die im Projekt verbaute Technik" — Grundlage
    /// der Vorbelegung und der Abweichungsanzeige in der Kostenverwaltung
    /// (<see cref="Form_Kosten"/>) und auf der Kostenseite von „Berichte &amp; Kosten"
    /// (<see cref="UcBkKosten"/>).
    ///
    /// <para>
    /// <b>Warum es diese Klasse gibt.</b> Bis 18.08.2026 zog <c>Form_Kosten.GetModulKosten</c>
    /// je Gewerk genau EIN Feld (BHKW: <c>Kosten_Modul</c>, Kessel/Puffer/Solar:
    /// <c>Investitionskosten</c>, WP/PV/Speicher: <c>Modulkosten</c>). Damit fiel beim BHKW
    /// der eigentliche Kostentreiber unter den Tisch: das Beispielmodul „2G 250kw.el Gas"
    /// trägt <c>Kosten_Modul</c> = 16.666 €, aber <c>Investition_kwel</c> = 653,60 €/kWel bei
    /// 250 kWel — also 163.400 €. Beide Zahlen sind gepflegt, keine ist „richtiger"; die
    /// Entscheidung gehört dem Anwender (Nutzerentscheidung 1 vom 18.08.2026), und zwar
    /// <b>je Anlage</b>, weil ein Projekt Module mit und ohne spezifischen Preis mischt.
    /// </para>
    ///
    /// <para>
    /// <b>Je Gerät genau einmal.</b> Mehrere Zeilen in <c>Tab_Energieanlagen</c> dürfen auf
    /// dasselbe Gerät zeigen (Dublette, z. B. Projekt 1018 Zeilen 11329/11330 mit
    /// <c>ID_PUFFER</c> = 1054168). Die Abfrage entdoppelt deshalb über die Verweisspalte —
    /// derselbe Schutz wie beim Befund D2, der hierher umgezogen ist.
    /// </para>
    ///
    /// <para>
    /// <b>Gespeicherte Access-Abfragen bleiben unangetastet.</b> Die Datenbank liegt außerhalb
    /// des Repos; eine Abfrageänderung erreicht Bestandsinstallationen nur über einen
    /// Migrationsschritt. Tabellen- und Spaltennamen stammen ausschließlich aus der
    /// Landkarte <see cref="Plaene"/>, nie aus einer Eingabe; die ProjektID bleibt Parameter.
    /// </para>
    /// </summary>
    internal static class TechnikPlanwertCtrl
    {
        // ------------------------------------------------------------ Schlüssel (ASCII)

        /// <summary>Kostenbasis „Modul-/Gerätepreis" — ein absoluter Betrag am Gerät.</summary>
        internal const string BASIS_MODULPREIS = "MODULPREIS";

        /// <summary>Kostenbasis „spezifischer Preis × Baugröße" (z. B. €/kWel × kWel).</summary>
        internal const string BASIS_SPEZIFISCH = "SPEZIFISCH";

        /// <summary>Auswahlwert „diese Anlage trägt nichts bei" (nur bei Mehrdeutigkeit).</summary>
        internal const string BASIS_KEINE = "KEINE";

        // --- Bezugsgröße der Kessel-Wartungskosten (Entscheidung 18.08.2026, Punkt 1) ---
        //
        // Sprachneutrale Steuerwerte der Auswahlliste. Der GESPEICHERTE Wert steht in
        // DbWerte.KESSEL_WARTUNG_EINHEIT_* (deutsch, eingefroren), der ANZEIGETEXT in
        // MyResource.Resource.KESSEL_WARTUNG_EINH_* — Drei-Schichten-Regel, Konzept 13.6.

        /// <summary>Fester Jahresbetrag [€/a] — die Vorbelegung jedes Bestandskessels.</summary>
        internal const string WARTUNG_EUR_JAHR = "EUR_JAHR";

        /// <summary>Auf die erzeugte Wärmemenge [€/kWh] — braucht einen Simulationslauf.</summary>
        internal const string WARTUNG_EUR_KWH = "EUR_KWH";

        /// <summary>Anteil der Investition je Jahr [%/a] — braucht die Investitionsposition.</summary>
        internal const string WARTUNG_PROZENT_INV = "PROZENT_INV";

        /// <summary>Die drei Einheiten in Anzeigereihenfolge — Quelle jeder Auswahlliste.</summary>
        internal static readonly string[] WARTUNG_SCHLUESSEL =
        { WARTUNG_EUR_JAHR, WARTUNG_EUR_KWH, WARTUNG_PROZENT_INV };

        // ------------------------------------------------------------------ Datentypen

        /// <summary>Eine mögliche Kostenbasis einer Anlage.</summary>
        internal sealed class Basiswert
        {
            /// <summary>Sprachneutraler Steuerwert (<see cref="BASIS_MODULPREIS"/> …).</summary>
            public string Schluessel;

            /// <summary>Betrag in Euro.</summary>
            public double Betrag;

            /// <summary>Klartext-Herleitung für die Oberfläche (bereits lokalisiert).</summary>
            public string Herleitung = "";
        }

        /// <summary>Ein Nebenkostenposten einer Anlage (eigene Zeile in der Kostenverwaltung).</summary>
        internal sealed class Nebenposten
        {
            /// <summary>Persistenzwert für <c>Tab_Kostenfaktor.Bezeichnung</c> (deutsch, eingefroren).</summary>
            public string Bezeichnung;

            /// <summary>Betrag in Euro.</summary>
            public double Betrag;
        }

        /// <summary>Eine im Projekt verbaute Anlage mit allen ermittelbaren Planwerten.</summary>
        internal sealed class Anlage
        {
            public int GeraetID;
            public string Bezeichner = "";

            /// <summary>
            /// Verbaute STÜCKZAHL dieses Geräts im Projekt — Summe über alle Anlagenzeilen,
            /// die darauf verweisen. Nur bei den Gewerken belegt, deren Gerätepreis ein
            /// Preis JE MODUL ist (Photovoltaik, Solarthermie); sonst 0.
            /// </summary>
            public double Menge;

            /// <summary>Alle Basiswerte &gt; 0. Leer = in der Technik ist nichts gepflegt.</summary>
            public List<Basiswert> Basiswerte = new List<Basiswert>();

            /// <summary>Nebenkosten &gt; 0 dieser Anlage.</summary>
            public List<Nebenposten> Nebenkosten = new List<Nebenposten>();

            /// <summary>Mehr als eine gepflegte Basis — der Anwender muss wählen.</summary>
            public bool Mehrdeutig { get { return Basiswerte.Count > 1; } }

            /// <summary>
            /// Der Wert, der ohne Rückfrage gilt: genau eine gepflegte Basis. Bei
            /// Mehrdeutigkeit 0 — dann entscheidet der Anwender im Übernahmedialog.
            /// </summary>
            public double EindeutigerWert
            { get { return Basiswerte.Count == 1 ? Basiswerte[0].Betrag : 0.0; } }

            /// <summary>Basiswert zu einem Schlüssel, oder <c>null</c>.</summary>
            public Basiswert Basis(string schluessel)
            {
                foreach (Basiswert b in Basiswerte)
                    if (string.Equals(b.Schluessel, schluessel, StringComparison.Ordinal)) return b;
                return null;
            }
        }

        // ------------------------------------------------------------------ Landkarte

        /// <summary>Gerätetabelle, Verweisspalte und Stückzahlspalte eines Gewerks.</summary>
        private sealed class Plan
        {
            public string Tabelle;
            public string Verweis;

            /// <summary>
            /// Spalte in <c>Tab_Energieanlagen</c>, die die verbaute STÜCKZAHL trägt —
            /// gesetzt nur bei Photovoltaik und Solarthermie (Nutzerentscheidung vom
            /// 18.08.2026, Punkt 2). <c>null</c> = das Gewerk kennt keine Stückzahl, der
            /// Gerätepreis ist der Anlagenpreis.
            /// </summary>
            public string Mengenspalte;
        }

        /// <summary>
        /// Die sieben Kostenkomponenten (<c>Tab_KostenKomponente</c>) auf ihre Gerätetabelle.
        /// Schlüssel sind Persistenzwerte aus <see cref="DbWerte"/>.
        /// </summary>
        private static readonly Dictionary<string, Plan> Plaene =
            new Dictionary<string, Plan>(StringComparer.Ordinal)
        {
            { DbWerte.ERZEUGER_WAERMEPUMPE,             new Plan { Tabelle = "Tab_WP",               Verweis = "ID_WP" } },
            { DbWerte.ERZEUGER_HEIZKESSEL,              new Plan { Tabelle = "Tab_Heizkessel",       Verweis = "ID_Kessel" } },
            { DbWerte.ERZEUGER_PHOTOVOLTAIK,            new Plan { Tabelle = "Tab_PV",               Verweis = "ID_PV",     Mengenspalte = "PV_Leistung" } },
            { DbWerte.ERZEUGER_SOLARTHERMIE,            new Plan { Tabelle = "Tab_Solarkollektoren", Verweis = "ID_Solar",  Mengenspalte = "Kollektormodulanzahl" } },
            { DbWerte.ERZEUGER_STROMSPEICHER,           new Plan { Tabelle = "Tab_Stromspeicher",    Verweis = "ID_SP" } },
            { DbWerte.KOSTEN_KOMPONENTE_PUFFERSPEICHER, new Plan { Tabelle = "Tab_Pufferspeicher",   Verweis = "ID_PUFFER" } },
            { DbWerte.ERZEUGER_BHKW,                    new Plan { Tabelle = "Tab_BHKW",             Verweis = "ID_BHKW" } }
        };

        /// <summary>Führt dieses Gewerk überhaupt Technik-Planwerte?</summary>
        internal static bool Bekannt(string komponente)
        { return !string.IsNullOrEmpty(komponente) && Plaene.ContainsKey(komponente); }

        // --------------------------------------------------------------- Investition

        /// <summary>
        /// Alle im Projekt verbauten Anlagen des Gewerks mit ihren Kostenbasen und
        /// Nebenkosten. Leere Liste = kein Gerät verbaut oder Gewerk unbekannt.
        /// </summary>
        internal static List<Anlage> LiesAnlagen(int projektID, string komponente)
        {
            var liste = new List<Anlage>();

            Plan plan;
            if (!Plaene.TryGetValue(komponente ?? "", out plan)) return liste;

            DataTable dt;
            try
            {
                // Entdoppelung über die Verweisspalte (Befund D2): ein Gerät zählt einmal,
                // egal wie viele Anlagenzeilen darauf zeigen.
                //
                // FÜHRT DAS GEWERK EINE STÜCKZAHL, wird sie dabei AUFSUMMIERT statt
                // verworfen (Nutzerentscheidung 2 vom 18.08.2026). Mehrere Anlagenzeilen
                // auf dasselbe PV-Modul sind kein Fehler, sondern der Regelfall: Jede
                // Zeile ist ein eigenes Feld mit eigener Neigung und Ausrichtung und
                // eigener Modulzahl. Genau so rechnet auch die Engine — SimulationPV
                // läuft über die ANLAGENZEILEN und nimmt je Zeile deren PV_Leistung
                // (SimulationPV.cs, Modulfläche = Breite × Länge × PV_Leistung); für die
                // Solarthermie ebenso (SimulationSolarthermie: Fläche =
                // Aperturfläche × Kollektormodulanzahl). Die Kostenseite muss dieselbe
                // Anlage beschreiben wie der Rechenkern.
                string mengenAusdruck = (plan.Mengenspalte != null)
                    ? ", SUM([" + plan.Mengenspalte + "]) AS Menge" : "";

                dt = DataRepository.GetDataTable(
                    "SELECT g.*" + (plan.Mengenspalte != null ? ", a.Menge" : "") + " " +
                    "FROM (SELECT [" + plan.Verweis + "] AS Geraet" + mengenAusdruck +
                    "      FROM Tab_Energieanlagen " +
                    "      WHERE ID_Projekt = ? AND [" + plan.Verweis + "] IS NOT NULL " +
                    "      GROUP BY [" + plan.Verweis + "]) AS a " +
                    "     INNER JOIN [" + plan.Tabelle + "] AS g ON a.Geraet = g.ID",
                    new OleDbParameter("@p", (Int32)projektID));
            }
            catch { return liste; }

            if (dt == null) return liste;

            foreach (DataRow r in dt.Rows)
            {
                var a = new Anlage
                {
                    GeraetID = Ganz(r, "ID"),
                    Bezeichner = Text(r, "Bezeichner"),
                    // Ganzzahlig abgeschnitten wie in der Engine: SimulationPV castet
                    // PV_Leistung mit (long), eine eingegebene 10,5 wird dort als 10
                    // Module gerechnet. Kosten und Ertrag müssen dieselbe Anlage meinen.
                    Menge = Math.Truncate(Zahl(r, "Menge"))
                };

                BasenFuellen(komponente, r, a);
                liste.Add(a);
            }
            return liste;
        }

        /// <summary>
        /// Die Kostenbasen und Nebenkosten EINER Gerätezeile — die einzige Stelle, an der
        /// steht, welches Feld welches Gewerks welche Bedeutung hat.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>BHKW</b> ist das einzige Gewerk mit ZWEI konkurrierenden Kostenfeldern in der
        /// Gerätetabelle: <c>Kosten_Modul</c> (absolut) und <c>Investition_kwel</c>
        /// (spezifisch, €/kWel — Beschriftung des Katalogdialogs
        /// <c>Form_DBBHKW.designer.cs</c>). Nur hier entsteht eine echte Auswahl.
        /// </para>
        /// <para>
        /// <b>Stromspeicher</b> führt drei Kostenfelder, die zusammen EINE Formel bilden:
        /// <c>Modulkosten</c> ist entgegen dem Namen ein SPEZIFISCHER Preis in €/kWh
        /// (AP0-Entscheid vom 16.08.2026, umgesetzt in
        /// <c>Form_AdminStromspeicher.EinheitenBeschriftungKorrigieren</c>),
        /// <c>Leistungskosten</c> €/kW und <c>Investition_Fix</c> ein fester Anteil —
        /// genau die Aufteilung, mit der <c>StromspeicherSimCtrl</c> rechnet. Der alte Weg
        /// summierte <c>Modulkosten</c> als Euro-Betrag und war damit dimensional falsch;
        /// hier entsteht nur EINE Basis, aber die richtige.
        /// </para>
        /// <para>
        /// <b>Die übrigen fünf Gewerke</b> führen genau ein Kostenfeld und bleiben einfeldrig
        /// (Nutzerentscheidung 1: „falls ein Gewerk nur ein Feld hat, bleibt es einfeldrig").
        /// </para>
        /// </remarks>
        private static void BasenFuellen(string komponente, DataRow r, Anlage a)
        {
            switch (komponente)
            {
                case DbWerte.ERZEUGER_BHKW:
                    {
                        double modul = Zahl(r, "Kosten_Modul");
                        double spez = Zahl(r, "Investition_kwel");
                        double pel = Zahl(r, "Pel");

                        Basis(a, BASIS_MODULPREIS, modul,
                              Herleitung(MyResource.Resource.KOSTEN_PLANWERT_HERL_FELD, "Kosten_Modul"));
                        Basis(a, BASIS_SPEZIFISCH, spez * pel,
                              Herleitung(MyResource.Resource.KOSTEN_PLANWERT_HERL_BHKW,
                                         Z(spez, 2), Z(pel, 2)));

                        Neben(a, DbWerte.KOSTENPOSTEN_MONTAGE, Zahl(r, "Kosten_Montage"));
                        Neben(a, DbWerte.KOSTENPOSTEN_LIEFERUNG, Zahl(r, "Kosten_Lieferung"));
                        Neben(a, DbWerte.KOSTENPOSTEN_SCHALLSCHUTZHAUBE, Zahl(r, "Kosten_Schallschutzhaube"));
                        Neben(a, DbWerte.KOSTENPOSTEN_ABGASREINIGUNG, Zahl(r, "Kosten_Abgasreinigung"));
                        break;
                    }

                case DbWerte.ERZEUGER_STROMSPEICHER:
                    {
                        double cCap = Zahl(r, "Modulkosten");        // €/kWh
                        double energie = Zahl(r, "Energie");         // kWh
                        double cPow = Zahl(r, "Leistungskosten");    // €/kW
                        double leistung = Zahl(r, "Leistung");       // kW
                        double fix = Zahl(r, "Investition_Fix");     // €

                        Basis(a, BASIS_SPEZIFISCH, cCap * energie + cPow * leistung + fix,
                              Herleitung(MyResource.Resource.KOSTEN_PLANWERT_HERL_SPEICHER,
                                         Z(cCap, 2), Z(energie, 2), Z(cPow, 2), Z(leistung, 2), Z(fix, 2)));
                        break;
                    }

                case DbWerte.ERZEUGER_WAERMEPUMPE:
                    Basis(a, BASIS_MODULPREIS, Zahl(r, "Modulkosten"),
                          Herleitung(MyResource.Resource.KOSTEN_PLANWERT_HERL_FELD, "Modulkosten"));
                    break;

                case DbWerte.ERZEUGER_PHOTOVOLTAIK:
                    Stueckpreis(a, Zahl(r, "Modulkosten"));
                    break;

                case DbWerte.ERZEUGER_SOLARTHERMIE:
                    Stueckpreis(a, Zahl(r, "Investitionskosten"));
                    break;

                case DbWerte.ERZEUGER_HEIZKESSEL:
                case DbWerte.KOSTEN_KOMPONENTE_PUFFERSPEICHER:
                    Basis(a, BASIS_MODULPREIS, Zahl(r, "Investitionskosten"),
                          Herleitung(MyResource.Resource.KOSTEN_PLANWERT_HERL_FELD, "Investitionskosten"));
                    break;
            }
        }

        /// <summary>
        /// Kostenbasis der Gewerke, deren Gerätepreis ein Preis JE MODUL ist:
        /// Modulpreis × verbaute Stückzahl (Nutzerentscheidung 2 vom 18.08.2026).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Belegt, nicht vermutet.</b> Beide Faktoren sind aus dem Rechenkern
        /// nachweisbar: <c>Tab_Energieanlagen.PV_Leistung</c> ist trotz des Namens die
        /// MODULANZAHL — <c>SimulationPV</c> bildet damit die Modulfläche
        /// (Breite × Länge × PV_Leistung) und führt den Wert als <c>Anzahl</c> ins
        /// Ergebnis; die Maske beschriftet das Feld mit „Anzahl Module".
        /// <c>Kollektormodulanzahl</c> ebenso: <c>SimulationSolarthermie</c> multipliziert
        /// sie mit der Aperturfläche EINES Kollektors. Beide Kostenfelder stehen im
        /// jeweiligen MODUL-Katalogdialog neben Modulmaßen und Modulleistung und sind
        /// dort mit „€" beschriftet, also ein Betrag je Modul.
        /// </para>
        /// <para>
        /// <b>Warum <see cref="BASIS_SPEZIFISCH"/> und nicht <see cref="BASIS_MODULPREIS"/>.</b>
        /// Der Wert ist jetzt „spezifischer Preis × Baugröße" — dieselbe Bauform wie beim
        /// BHKW (€/kWel × kWel) und beim Stromspeicher (€/kWh × kWh). Der Anzeigetext und
        /// die Herkunftsspalte des Übernahmedialogs stimmen damit ohne Sonderfall, und die
        /// Rechnung steht im Klartext daneben („468,89 €/Modul × 20 Module").
        /// Eine ECHTE Auswahl entsteht dadurch nicht: Beide Gewerke führen weiterhin genau
        /// ein Kostenfeld, es bleibt bei einer Basis je Anlage.
        /// </para>
        /// <para>
        /// <b>Stückzahl 0 ergibt keine Basis.</b> <see cref="Basis"/> verwirft Beträge
        /// ≤ 0 — eine Anlage ohne konfigurierte Module trägt also nichts bei, statt
        /// stillschweigend ein Modul zu unterstellen. Das ist eine Änderung gegenüber dem
        /// bisherigen Verhalten (nackter Modulpreis) und wird über die Abweichungsanzeige
        /// gemeldet; überschrieben wird nie (Nutzerentscheidung 4).
        /// </para>
        /// </remarks>
        private static void Stueckpreis(Anlage a, double preisJeModul)
        {
            Basis(a, BASIS_SPEZIFISCH, preisJeModul * a.Menge,
                  Herleitung(MyResource.Resource.KOSTEN_PLANWERT_HERL_MENGE,
                             Z(preisJeModul, 2), Z(a.Menge, 0)));
        }

        private static void Basis(Anlage a, string schluessel, double betrag, string herleitung)
        {
            if (betrag <= 0.0) return;                 // 0/leer = ungepflegt, keine Scheinauswahl
            a.Basiswerte.Add(new Basiswert
            { Schluessel = schluessel, Betrag = betrag, Herleitung = herleitung });
        }

        private static void Neben(Anlage a, string bezeichnung, double betrag)
        {
            if (betrag <= 0.0) return;                 // nur Posten mit Wert (Nutzerentscheidung 2)
            a.Nebenkosten.Add(new Nebenposten { Bezeichnung = bezeichnung, Betrag = betrag });
        }

        /// <summary>
        /// Summe der Hauptposition über alle Anlagen. <paramref name="wahl"/> ordnet
        /// GerätID → Basisschlüssel zu; fehlt ein Eintrag, gilt der eindeutige Wert der
        /// Anlage (bei Mehrdeutigkeit also 0 — nie stillschweigend eine der beiden Zahlen).
        /// </summary>
        internal static double Hauptsumme(List<Anlage> anlagen, IDictionary<int, string> wahl)
        {
            double summe = 0;
            if (anlagen == null) return 0;

            foreach (Anlage a in anlagen)
            {
                string schluessel;
                if (wahl != null && wahl.TryGetValue(a.GeraetID, out schluessel))
                {
                    if (string.Equals(schluessel, BASIS_KEINE, StringComparison.Ordinal)) continue;
                    Basiswert b = a.Basis(schluessel);
                    summe += (b != null) ? b.Betrag : 0.0;
                }
                else summe += a.EindeutigerWert;
            }
            return summe;
        }

        /// <summary>Nebenkosten aller Anlagen, je Bezeichnung zusammengefasst.</summary>
        internal static List<Nebenposten> Nebensummen(List<Anlage> anlagen)
        {
            var liste = new List<Nebenposten>();
            if (anlagen == null) return liste;

            foreach (Anlage a in anlagen)
                foreach (Nebenposten n in a.Nebenkosten)
                {
                    Nebenposten treffer = null;
                    foreach (Nebenposten x in liste)
                        if (string.Equals(x.Bezeichnung, n.Bezeichnung, StringComparison.Ordinal))
                        { treffer = x; break; }

                    if (treffer == null) liste.Add(new Nebenposten
                    { Bezeichnung = n.Bezeichnung, Betrag = n.Betrag });
                    else treffer.Betrag += n.Betrag;
                }
            return liste;
        }

        /// <summary>Trägt mindestens eine Anlage zwei konkurrierende Kostenbasen?</summary>
        internal static bool Mehrdeutig(List<Anlage> anlagen)
        {
            if (anlagen == null) return false;
            foreach (Anlage a in anlagen) if (a.Mehrdeutig) return true;
            return false;
        }

        /// <summary>Anzeigename einer Kostenbasis (lokalisiert).</summary>
        internal static string BasisName(string schluessel)
        {
            switch (schluessel)
            {
                case BASIS_MODULPREIS: return MyResource.Resource.KOSTEN_PLANWERT_BASIS_MODUL;
                case BASIS_SPEZIFISCH: return MyResource.Resource.KOSTEN_PLANWERT_BASIS_SPEZ;
                case BASIS_KEINE: return MyResource.Resource.KOSTEN_PLANWERT_BASIS_KEINE;
                default: return schluessel ?? "";
            }
        }

        // -------------------------------------------------------------- Betriebskosten

        /// <summary>Ergebnis der Betriebskosten-Vorbelegung.</summary>
        internal sealed class Betriebsplanwert
        {
            /// <summary>Betrag in €/a, oder <c>null</c> = keine Vorbelegung möglich.</summary>
            public double? Betrag;

            /// <summary>Erklärung für die Oberfläche (Herleitung oder Grund, lokalisiert).</summary>
            public string Hinweis = "";
        }

        /// <summary>
        /// Betriebskosten-Vorbelegung aus den Wartungsangaben der Technik mal der
        /// <b>tatsächlich gerechneten</b> Jahresmenge des jüngsten Simulationslaufs
        /// (Nutzerentscheidung 3 vom 18.08.2026).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Ohne Simulationsergebnis gibt es keine Vorbelegung</b> — nur einen Hinweis.
        /// Es wird nichts geschätzt und keine Vollbenutzungsstundenzahl unterstellt.
        /// </para>
        /// <para>
        /// <b>Umgesetzt ist bisher nur das BHKW.</b> <c>Tab_BHKW.Wartungskosten_kwhel</c>
        /// trägt eine belegte Einheit: der Katalogdialog beschriftet das Feld mit
        /// „€ / kWhel" (<c>Views\BHKW\Form_DBBHKW.designer.cs</c>, Label19 auf Höhe von
        /// <c>textBox_Wartungskosten</c>). Bezugsgröße ist die Stromerzeugung des Laufs
        /// aus <c>Tab_ErgebnisBHKWModul.Stromproduktion</c> bzw. <c>Tab_ErgebnisBHKW</c>
        /// (Einheit MWh/a, siehe Konzept_Wirtschaftlichkeit 3.1) × 1000.
        /// </para>
        /// <para>
        /// <b>Der Heizkessel rechnet seit dem 18.08.2026 nach der GEWÄHLTEN Einheit.</b>
        /// <c>Tab_Heizkessel.Wartungskosten</c> hatte bis dahin keine Oberfläche und stand
        /// überall auf 0; die Einheit war deshalb nicht belegbar. Statt eine zu erraten, ist
        /// sie jetzt je Kessel wählbar (<c>Wartungskosten_Einheit</c>,
        /// Migrationsschritt 15) — siehe <see cref="KesselPlanwert"/>.
        /// </para>
        /// <para>
        /// Die übrigen Gewerke führen überhaupt kein Wartungsfeld in der Gerätetabelle
        /// (Wärmepumpe, PV, Solarthermie, Pufferspeicher). Der Stromspeicher führt
        /// <c>Verschleisskosten</c>; die rechnet <c>StromspeicherSimCtrl</c> bereits selbst
        /// in seine eigene Wirtschaftlichkeit ein — eine zweite, zusätzlich addierte
        /// Kostenposition wäre Doppelzählung.
        /// </para>
        /// </remarks>
        /// <param name="komponentenID">
        /// <c>Tab_KostenKomponente.ID</c> des Gewerks — nur für die Einheit „%/a" nötig,
        /// deren Bezugsgröße die erfasste Investitionsposition ist. 0 = unbekannt; dann
        /// gibt es für diese Einheit keine Vorbelegung.
        /// </param>
        internal static Betriebsplanwert LiesBetriebsplanwert(int projektID, string komponente,
                                                              int komponentenID)
        {
            var erg = new Betriebsplanwert();

            if (string.Equals(komponente, DbWerte.ERZEUGER_HEIZKESSEL, StringComparison.Ordinal))
                return KesselPlanwert(projektID, komponente, komponentenID);

            if (!string.Equals(komponente, DbWerte.ERZEUGER_BHKW, StringComparison.Ordinal))
            {
                erg.Hinweis = MyResource.Resource.KOSTEN_BETRIEB_OHNE_WARTUNGSFELD;
                return erg;
            }

            // --- Jüngster Simulationslauf des Projekts -------------------------------
            int idErgebnis = 0;
            DateTime stand = DateTime.MinValue;
            try
            {
                DataTable k = DataRepository.GetDataTable(
                    "SELECT TOP 1 ID, Zeitstempel FROM Tab_Ergebnis WHERE ID_Projekt = ? ORDER BY ID DESC",
                    new OleDbParameter("@p", (Int32)projektID));
                if (k != null && k.Rows.Count > 0)
                {
                    idErgebnis = Ganz(k.Rows[0], "ID");
                    if (k.Rows[0]["Zeitstempel"] != DBNull.Value)
                        stand = Convert.ToDateTime(k.Rows[0]["Zeitstempel"]);
                }
            }
            catch { }

            if (idErgebnis <= 0)
            { erg.Hinweis = MyResource.Resource.KOSTEN_BETRIEB_OHNE_ERGEBNIS; return erg; }

            // --- Wartungssätze je Anlagenbezeichner ----------------------------------
            // Der Modulname im Ergebnis ist Tab_Energieanlagen.Bezeichner (gesetzt in
            // SimulationControl.BHKW_Liste_Laden), nicht der Gerätename — deshalb wird
            // über die Anlagenzeile verknüpft.
            var satzNachAnlage = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var saetze = new List<double>();
            try
            {
                DataTable g = DataRepository.GetDataTable(
                    "SELECT a.Bezeichner AS Anlage, b.Wartungskosten_kwhel AS Satz " +
                    "FROM Tab_Energieanlagen AS a INNER JOIN Tab_BHKW AS b ON a.ID_BHKW = b.ID " +
                    "WHERE a.ID_Projekt = ? AND a.ID_BHKW IS NOT NULL",
                    new OleDbParameter("@p", (Int32)projektID));
                if (g != null)
                    foreach (DataRow r in g.Rows)
                    {
                        double satz = Zahl(r, "Satz");
                        satzNachAnlage[Text(r, "Anlage")] = satz;
                        saetze.Add(satz);
                    }
            }
            catch { }

            if (saetze.Count == 0)
            { erg.Hinweis = MyResource.Resource.KOSTEN_BETRIEB_OHNE_WARTUNGSFELD; return erg; }

            // --- Stromerzeugung des Laufs je Modul -----------------------------------
            DataTable module = null;
            try
            {
                module = DataRepository.GetDataTable(
                    "SELECT m.Modul, m.Stromproduktion " +
                    "FROM Tab_ErgebnisBHKW AS e INNER JOIN Tab_ErgebnisBHKWModul AS m " +
                    "     ON e.ID = m.ID_ErgebnisBHKW " +
                    "WHERE e.ID_Ergebnis = ?",
                    new OleDbParameter("@e", (Int32)idErgebnis));
            }
            catch { }

            if (module == null || module.Rows.Count == 0)
            { erg.Hinweis = MyResource.Resource.KOSTEN_BETRIEB_OHNE_ERGEBNIS; return erg; }

            double summeEuro = 0, summeKwh = 0;
            bool alleZugeordnet = true;

            foreach (DataRow r in module.Rows)
            {
                double kwh = Zahl(r, "Stromproduktion") * 1000.0;   // MWh/a → kWh/a
                double satz;

                if (!satzNachAnlage.TryGetValue(Text(r, "Modul"), out satz))
                {
                    // Rückfall NUR bei Eindeutigkeit: führt das Projekt genau einen
                    // Wartungssatz, kann das Modul zu keinem anderen gehören. Das ist
                    // keine Schätzung, sondern die einzige Möglichkeit. Sonst: Abbruch.
                    if (!EinSatz(saetze, out satz)) { alleZugeordnet = false; break; }
                }

                summeEuro += satz * kwh;
                summeKwh += kwh;
            }

            if (!alleZugeordnet)
            { erg.Hinweis = MyResource.Resource.KOSTEN_BETRIEB_NICHT_ZUORDENBAR; return erg; }

            if (summeKwh <= 0.0)
            { erg.Hinweis = MyResource.Resource.KOSTEN_BETRIEB_OHNE_MENGE; return erg; }

            erg.Betrag = summeEuro;
            erg.Hinweis = string.Format(MyResource.Resource.KOSTEN_BETRIEB_HERLEITUNG,
                                        Z(summeEuro / summeKwh, 4), Z(summeKwh, 0),
                                        stand == DateTime.MinValue ? "-" : stand.ToString("dd.MM.yyyy HH:mm"));
            return erg;
        }

        // ------------------------------------------------- Betriebskosten Heizkessel

        /// <summary>
        /// Betriebskosten-Vorbelegung des HEIZKESSELS nach der je Kessel gewählten
        /// Bezugsgröße <c>Tab_Heizkessel.Wartungskosten_Einheit</c>
        /// (Entscheidung des Anwenders vom 18.08.2026, Punkt 1).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>0 gilt als ungepflegt.</b> Trägt kein Kessel des Projekts einen Betrag &gt; 0,
        /// gibt es keine Zahl und keinen Rechenweg, sondern denselben Hinweis wie bei den
        /// Gewerken ohne Wartungsfeld. Das ist die Hausregel aus <c>605dcb8</c>
        /// („Arbeitspreis 0 gilt als ungepflegt"), und sie ist der Grund, aus dem die
        /// Vorbelegung der Einheit auf „€/a" für den Bestand folgenlos bleibt.
        /// </para>
        /// <para>
        /// <b>Eine Einheit je Projekt.</b> Die Bezugsgrößen zweier Einheiten sind
        /// GEWERKGRÖSSEN, keine Gerätegrößen: <c>Tab_ErgebnisHeizkessel</c> führt genau
        /// EINE Zeile je Lauf — die Wärme aller Kessel zusammen, nicht je Modul wie beim
        /// BHKW (<c>Tab_ErgebnisBHKWModul</c>) —, und die Investitionsposition ist ohnehin
        /// eine Zahl für das ganze Gewerk. Eine Aufteilung auf einzelne Kessel gibt die
        /// Datenlage nicht her. Führen die Kessel eines Projekts unterschiedliche
        /// Einheiten, wird deshalb nichts vorbelegt, sondern der Grund genannt — lieber
        /// kein Wert als ein geratener.
        /// </para>
        /// <para>
        /// <b>Daraus folgt die Rechenweise je Einheit:</b>
        /// <list type="bullet">
        ///   <item><description><b>€/a</b> — die Beträge der Kessel ADDIEREN sich; jeder
        ///     trägt seinen eigenen Jahresbetrag bei. Braucht weder Lauf noch
        ///     Investitionsposition.</description></item>
        ///   <item><description><b>€/kWh</b> — Satz × Wärmemenge des jüngsten Laufs.
        ///     Weil die Wärmemenge für alle Kessel zusammen gilt, muss auch der Satz
        ///     eindeutig sein; bei mehreren Sätzen wäre Σ Satzᵢ × Q die vierfache Wartung
        ///     für vier Kessel. Ohne Lauf keine Zahl (Nutzerentscheidung 3).</description></item>
        ///   <item><description><b>%/a</b> — Satz × erfasste Investitionsposition, aus
        ///     demselben Grund ebenfalls genau einmal. Ist die Position noch nicht
        ///     erfasst, fehlt die Bezugsgröße.</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        private static Betriebsplanwert KesselPlanwert(int projektID, string komponente,
                                                       int komponentenID)
        {
            var erg = new Betriebsplanwert();

            // --- Kessel des Projekts, je Gerät einmal (Befund D2) --------------------
            DataTable dt;
            try
            {
                dt = DataRepository.GetDataTable(
                    "SELECT k.Wartungskosten, k.[" + SchemaKatalog.SPALTE_KESSEL_WARTUNG_EINHEIT + "] AS Einheit " +
                    "FROM (SELECT DISTINCT [ID_Kessel] FROM Tab_Energieanlagen " +
                    "      WHERE ID_Projekt = ? AND [ID_Kessel] IS NOT NULL) AS a " +
                    "     INNER JOIN [Tab_Heizkessel] AS k ON a.[ID_Kessel] = k.ID",
                    new OleDbParameter("@p", (Int32)projektID));
            }
            catch { dt = null; }

            if (dt == null || dt.Rows.Count == 0)
            { erg.Hinweis = MyResource.Resource.KOSTEN_BETRIEB_OHNE_WARTUNGSFELD; return erg; }

            double summeJahr = 0;
            var saetze = new List<double>();
            string einheit = null;
            bool gemischt = false;

            foreach (DataRow r in dt.Rows)
            {
                double betrag = Zahl(r, "Wartungskosten");
                if (betrag <= 0.0) continue;                  // 0 = ungepflegt

                string e = HeizkesselCtrl.Einheit(Text(r, "Einheit"));
                if (einheit == null) einheit = e;
                else if (!string.Equals(einheit, e, StringComparison.Ordinal)) gemischt = true;

                summeJahr += betrag;
                saetze.Add(betrag);
            }

            if (einheit == null)
            { erg.Hinweis = MyResource.Resource.KOSTEN_BETRIEB_OHNE_WARTUNGSFELD; return erg; }

            if (gemischt)
            { erg.Hinweis = MyResource.Resource.KOSTEN_BETRIEB_EINHEIT_GEMISCHT; return erg; }

            // --- Fester Jahresbetrag: keine weitere Bezugsgröße nötig ----------------
            if (string.Equals(einheit, DbWerte.KESSEL_WARTUNG_EINHEIT_JAHR, StringComparison.Ordinal))
            {
                erg.Betrag = summeJahr;
                erg.Hinweis = string.Format(MyResource.Resource.KOSTEN_BETRIEB_HERL_KESSEL_JAHR,
                                            Z(summeJahr, 2));
                return erg;
            }

            // Beide mengenbezogenen Einheiten brauchen EINEN Satz (siehe <remarks>).
            double satz;
            if (!EinSatz(saetze, out satz))
            { erg.Hinweis = MyResource.Resource.KOSTEN_BETRIEB_NICHT_ZUORDENBAR; return erg; }

            // --- Anteil der Investition ---------------------------------------------
            if (string.Equals(einheit, DbWerte.KESSEL_WARTUNG_EINHEIT_PROZENT, StringComparison.Ordinal))
            {
                double investition = 0;
                if (komponentenID > 0)
                    investition = KostenPositionCtrl.LiesBetrag(
                        KostenPositionCtrl.FindeHauptposition(projektID, Form_Kosten.KATEGORIE_INVESTITION,
                                                              komponentenID, komponente));

                if (investition <= 0.0)
                { erg.Hinweis = MyResource.Resource.KOSTEN_BETRIEB_OHNE_INVESTITION; return erg; }

                erg.Betrag = satz / 100.0 * investition;
                erg.Hinweis = string.Format(MyResource.Resource.KOSTEN_BETRIEB_HERL_KESSEL_PROZENT,
                                            Z(satz, 2), Z(investition, 2), Z(erg.Betrag.Value, 2));
                return erg;
            }

            // --- Auf die erzeugte Wärmemenge des jüngsten Laufs ----------------------
            int idErgebnis = 0;
            DateTime stand = DateTime.MinValue;
            try
            {
                DataTable k = DataRepository.GetDataTable(
                    "SELECT TOP 1 ID, Zeitstempel FROM Tab_Ergebnis WHERE ID_Projekt = ? ORDER BY ID DESC",
                    new OleDbParameter("@p", (Int32)projektID));
                if (k != null && k.Rows.Count > 0)
                {
                    idErgebnis = Ganz(k.Rows[0], "ID");
                    if (k.Rows[0]["Zeitstempel"] != DBNull.Value)
                        stand = Convert.ToDateTime(k.Rows[0]["Zeitstempel"]);
                }
            }
            catch { }

            if (idErgebnis <= 0)
            { erg.Hinweis = MyResource.Resource.KOSTEN_BETRIEB_OHNE_ERGEBNIS; return erg; }

            // Tab_ErgebnisHeizkessel führt EINE Zeile je Lauf (die Kessel des Projekts
            // zusammen); Waermeproduktion in MWh/a wie alle Wärmegrößen dieser Tabelle
            // (Konzept_Wirtschaftlichkeit 3.1), deshalb × 1000.
            double kwh = 0;
            bool gefunden = false;
            try
            {
                DataTable w = DataRepository.GetDataTable(
                    "SELECT Waermeproduktion FROM Tab_ErgebnisHeizkessel WHERE ID_Ergebnis = ?",
                    new OleDbParameter("@e", (Int32)idErgebnis));
                if (w != null && w.Rows.Count > 0)
                {
                    gefunden = true;
                    foreach (DataRow r in w.Rows) kwh += Zahl(r, "Waermeproduktion") * 1000.0;
                }
            }
            catch { }

            if (!gefunden)
            { erg.Hinweis = MyResource.Resource.KOSTEN_BETRIEB_OHNE_ERGEBNIS; return erg; }

            if (kwh <= 0.0)
            { erg.Hinweis = MyResource.Resource.KOSTEN_BETRIEB_OHNE_MENGE; return erg; }

            erg.Betrag = satz * kwh;
            erg.Hinweis = string.Format(MyResource.Resource.KOSTEN_BETRIEB_HERL_KESSEL_ARBEIT,
                                        Z(satz, 4), Z(kwh, 0),
                                        stand == DateTime.MinValue ? "-" : stand.ToString("dd.MM.yyyy HH:mm"));
            return erg;
        }

        // ------------------------------------------------------- Wartungseinheiten

        /// <summary>
        /// Sprachneutraler Steuerwert zum gespeicherten Persistenzwert
        /// (<see cref="DbWerte.KESSEL_WARTUNG_EINHEIT_JAHR"/> &amp; Co.).
        /// Unbekanntes oder Leeres gilt als fester Jahresbetrag — dieselbe Rückfallebene
        /// wie <c>HeizkesselCtrl.Einheit</c>.
        /// </summary>
        internal static string WartungSchluessel(string dbWert)
        {
            string w = HeizkesselCtrl.Einheit(dbWert);
            if (string.Equals(w, DbWerte.KESSEL_WARTUNG_EINHEIT_ARBEIT, StringComparison.Ordinal))
                return WARTUNG_EUR_KWH;
            if (string.Equals(w, DbWerte.KESSEL_WARTUNG_EINHEIT_PROZENT, StringComparison.Ordinal))
                return WARTUNG_PROZENT_INV;
            return WARTUNG_EUR_JAHR;
        }

        /// <summary>Gespeicherter Persistenzwert zum sprachneutralen Steuerwert.</summary>
        internal static string WartungDbWert(string schluessel)
        {
            switch (schluessel)
            {
                case WARTUNG_EUR_KWH: return DbWerte.KESSEL_WARTUNG_EINHEIT_ARBEIT;
                case WARTUNG_PROZENT_INV: return DbWerte.KESSEL_WARTUNG_EINHEIT_PROZENT;
                default: return DbWerte.KESSEL_WARTUNG_EINHEIT_JAHR;
            }
        }

        /// <summary>Anzeigename einer Wartungseinheit (lokalisiert).</summary>
        internal static string WartungName(string schluessel)
        {
            switch (schluessel)
            {
                case WARTUNG_EUR_KWH: return MyResource.Resource.KESSEL_WARTUNG_EINH_ARBEIT;
                case WARTUNG_PROZENT_INV: return MyResource.Resource.KESSEL_WARTUNG_EINH_PROZENT;
                default: return MyResource.Resource.KESSEL_WARTUNG_EINH_JAHR;
            }
        }

        /// <summary>Führen alle Anlagen denselben Wartungssatz? Dann ist er eindeutig.</summary>
        private static bool EinSatz(List<double> saetze, out double satz)
        {
            satz = 0;
            if (saetze.Count == 0) return false;
            satz = saetze[0];
            for (int i = 1; i < saetze.Count; i++)
                if (Math.Abs(saetze[i] - satz) > 1e-12) return false;
            return true;
        }

        // --------------------------------------------------------------------- Helfer

        private static string Herleitung(string muster, params object[] teile)
        {
            try { return string.Format(muster, teile); }
            catch { return muster ?? ""; }
        }

        private static string Z(double wert, int stellen)
        { return wert.ToString("N" + stellen.ToString(CultureInfo.InvariantCulture), BerichtTexte.Kultur); }

        private static double Zahl(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return 0.0;
            try { return Convert.ToDouble(r[spalte]); } catch { return 0.0; }
        }

        private static int Ganz(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return 0;
            try { return Convert.ToInt32(r[spalte]); } catch { return 0; }
        }

        private static string Text(DataRow r, string spalte)
        {
            return (r.Table.Columns.Contains(spalte) && r[spalte] != DBNull.Value)
                ? r[spalte].ToString().Trim() : "";
        }
    }
}
