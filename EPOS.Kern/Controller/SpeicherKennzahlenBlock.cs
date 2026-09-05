using System.Collections.Generic;
using System.Globalization;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Warnstufe einer Kennzahlzeile — der sprachneutrale Ersatz für die vier
    /// <c>Color.FromArgb</c>-Werte, mit denen <c>Form_Simulation_Detail</c> die
    /// Zyklen- und die Budgetzeile einfärbte (iU9-W11a.3).
    ///
    /// <para><b>Warum keine Farbe.</b> Eine Farbe ist <c>System.Drawing</c> und damit im
    /// Kern verboten; sie ist außerdem eine Darstellungsentscheidung. Die Stufe sagt, WAS
    /// gilt — welche Farbe (oder welches Symbol, oder welcher Kontrastmodus) daraus wird,
    /// entscheidet die Oberfläche.</para>
    ///
    /// <para><b>Warum nicht <c>WarnStufe</c>.</b> Den Namen führt seit iU9-W2 der Baustein
    /// <c>EPOS.UI.Bausteine.WarnStufe</c> (Hinweis/Warnung/Fehler/Erfolg eines
    /// Warnbanners) — eine andere Frage mit anderen Ausprägungen. Zwei gleichnamige
    /// Aufzählungen in zwei Bibliotheken, die dieselbe Razor-Datei sieht, sind eine
    /// Mehrdeutigkeit (CS0104).</para>
    /// </summary>
    public enum KennzahlStufe
    {
        /// <summary>Keine Aussage möglich — die Bezugsgröße ist nicht gepflegt.</summary>
        Unbestimmt = 0,
        /// <summary>Im grünen Bereich (bis 90 % der Schranke).</summary>
        Ok = 1,
        /// <summary>Knapp (über 90 % der Schranke).</summary>
        Knapp = 2,
        /// <summary>Schranke überschritten.</summary>
        Ueberschritten = 3
    }

    /// <summary>
    /// Die 39 Kennzahlzeilen des Stromspeicher-Ergebnisses — eine Wahrheit für
    /// Bildschirm, Bericht und Razor-Seite (iU9-W11a.3).
    ///
    /// <para><b>Woher sie kommen.</b> <c>Form_Simulation_Detail.SpKennzahlenFuellen</c>
    /// (:7287-7400) baute sie unmittelbar als <c>ListViewItem</c>: 17 Zeilen „Energie",
    /// 8 „Speicher", 14 „Wirtschaft" (die Vermessung nennt 18/40 — nachgezählt sind es
    /// 17/39; die Eigenverbrauchsquote steht in einer if/else-Verzweigung und wurde dort
    /// doppelt gezählt), dazu die Hilfsmethoden <c>Vgl</c>,
    /// <c>SpVerkaufKwh</c>, <c>SpBudgetzeilen</c>, <c>SpAmortisationstext</c>,
    /// <c>SpAmpelfarbe</c> und <c>SpBudgetfarbe</c>. Die Zeilenliste ist eine
    /// Fachaussage über den Lauf und keine Eigenschaft eines Steuerelements.</para>
    ///
    /// <para><b>Was hier bleibt und was nicht.</b> Beschriftungen kommen weiter aus
    /// <c>MyResource.Resource</c> (sie liegen im Kern); die Zahlen sind mit denselben
    /// Formatangaben und derselben Kultur formatiert wie im Vorläufer, damit die Anzeige
    /// zeichengleich bleibt. Die Farben sind <see cref="KennzahlStufe"/> geworden.</para>
    ///
    /// <para><b>Warum „…Block".</b> Die Arbeitsanweisung nennt den Typ
    /// <c>SpeicherKennzahlen</c>; der Name ist vergeben —
    /// <c>SpeicherEngine.SpeicherKennzahlen</c> ist der Kennzahlensatz der Engine, und
    /// jede Datei mit <c>using SpeicherEngine;</c> bekäme eine Namensverdeckung
    /// (CS0723/CS0029 in <c>StromspeicherSimCtrl</c>).</para>
    /// </summary>
    public static class SpeicherKennzahlenBlock
    {
        /// <summary>Gruppenschlüssel — sprachneutral und ASCII (Drei-Schichten-Regel).</summary>
        public const string GRUPPE_ENERGIE = "ENERGIE";
        public const string GRUPPE_SPEICHER = "SPEICHER";
        public const string GRUPPE_WIRTSCHAFT = "WIRTSCHAFT";

        /// <summary>
        /// Anzeige einer Kennzahl, die in DIESEM Lauf keinen Bezug hat (Abnahmebefund 2).
        /// Ein Symbol, kein Text — sprachneutral wie die Einheitenspalte.
        /// </summary>
        public const string UNBESTIMMT = "–";

        private const string KWH = "kWh/a";
        private const string EUR_A = "€/a";

        /// <summary>
        /// Eine Zeile des Kennzahlenblocks.
        /// </summary>
        /// <param name="Gruppe">Einer der drei Gruppenschlüssel.</param>
        /// <param name="Bezeichnung">Der Anzeigetext aus dem Ressourcenkatalog.</param>
        /// <param name="Wert">Der formatierte Wert des Laufs.</param>
        /// <param name="Vergleich">Der formatierte Wert des Vergleichslaufs; leer ohne Vergleich.</param>
        /// <param name="Einheit">Die Einheit; <c>"-"</c> für dimensionslose Zahlen.</param>
        /// <param name="Stufe">Warnfärbung der Zeile.</param>
        public sealed record Zeile(string Gruppe, string Bezeichnung, string Wert,
                                   string Vergleich, string Einheit, KennzahlStufe Stufe);

        /// <summary>
        /// Baut den vollständigen Kennzahlenblock.
        /// <paramref name="kv"/> und <paramref name="vergleich"/> sind <c>null</c>, wenn
        /// es keinen Vergleichslauf gibt; dann bleibt die Vergleichsspalte leer.
        /// </summary>
        public static List<Zeile> Zeilen(ErgebnisStromspeicherModel k,
                                         SpeicherErgebnis erg,
                                         StromspeicherLaufKontext kontext,
                                         ErgebnisStromspeicherModel kv = null,
                                         SpeicherErgebnis vergleich = null)
        {
            List<Zeile> zeilen = new List<Zeile>();
            if (k == null || erg == null) return zeilen;

            // ABNAHMEBEFUND 2: Zuerst die EINGANGSGRÖSSEN des Laufs. Bis dahin zeigte die
            // Seite ausschließlich Ergebnisse; ob der Speicher überhaupt eine Last und
            // eine Erzeugung vor sich hatte, war ihr nicht zu entnehmen.
            SpeicherEngine.SpeicherKennzahlen ein = erg.Kennzahlen;
            SpeicherEngine.SpeicherKennzahlen einVgl = vergleich != null ? vergleich.Kennzahlen : null;

            Zahl(zeilen, GRUPPE_ENERGIE, MyResource.Resource.SP_ERG_LAST, ein.LastKwh,
                 einVgl != null ? einVgl.LastKwh : (double?)null, "N0", KWH);
            Zahl(zeilen, GRUPPE_ENERGIE, MyResource.Resource.SP_ERG_ERZEUGUNG_PV, ein.ErzeugungPvKwh,
                 einVgl != null ? einVgl.ErzeugungPvKwh : (double?)null, "N0", KWH);
            Zahl(zeilen, GRUPPE_ENERGIE, MyResource.Resource.SP_ERG_ERZEUGUNG_BHKW, ein.ErzeugungBhkwKwh,
                 einVgl != null ? einVgl.ErzeugungBhkwKwh : (double?)null, "N0", KWH);
            Zahl(zeilen, GRUPPE_ENERGIE, MyResource.Resource.SP_ERG_DIREKTVERBRAUCH, ein.DirektverbrauchKwh,
                 einVgl != null ? einVgl.DirektverbrauchKwh : (double?)null, "N0", KWH);

            Zahl(zeilen, GRUPPE_ENERGIE, MyResource.Resource.SP_ERG_LADUNG_PV, k.Ladung_PV, Vgl(kv, x => x.Ladung_PV), "N0", KWH);
            Zahl(zeilen, GRUPPE_ENERGIE, MyResource.Resource.SP_ERG_LADUNG_BHKW, k.Ladung_BHKW, Vgl(kv, x => x.Ladung_BHKW), "N0", KWH);
            Zahl(zeilen, GRUPPE_ENERGIE, MyResource.Resource.SP_ERG_LADUNG_NETZ, k.Ladung_Netz, Vgl(kv, x => x.Ladung_Netz), "N0", KWH);
            Zahl(zeilen, GRUPPE_ENERGIE, MyResource.Resource.SP_ERG_LADUNG_GESAMT, k.Ladung_Gesamt, Vgl(kv, x => x.Ladung_Gesamt), "N0", KWH);
            Zahl(zeilen, GRUPPE_ENERGIE, MyResource.Resource.SP_ERG_ENTLADUNG, k.Entladung_Gesamt, Vgl(kv, x => x.Entladung_Gesamt), "N0", KWH);
            // Netzverkauf (AP10): Die Größe steht nicht im Ergebnismodell - sie ist dort
            // im Entladungssummenwert enthalten und wird hier eigens ausgewiesen.
            Zahl(zeilen, GRUPPE_ENERGIE, MyResource.Resource.ARB_ERG_VERKAUF, VerkaufKwh(kontext), null, "N0", KWH);
            Zahl(zeilen, GRUPPE_ENERGIE, MyResource.Resource.SP_ERG_VERLUSTE, k.Verluste_Gesamt, Vgl(kv, x => x.Verluste_Gesamt), "N0", KWH);
            Zahl(zeilen, GRUPPE_ENERGIE, MyResource.Resource.SP_ERG_NETZBEZUG_OHNE, k.Netzbezug_Ohne, Vgl(kv, x => x.Netzbezug_Ohne), "N0", KWH);
            Zahl(zeilen, GRUPPE_ENERGIE, MyResource.Resource.SP_ERG_NETZBEZUG_MIT, k.Netzbezug_Mit, Vgl(kv, x => x.Netzbezug_Mit), "N0", KWH);
            Zahl(zeilen, GRUPPE_ENERGIE, MyResource.Resource.SP_ERG_EINSPEISUNG_OHNE, k.Einspeisung_Ohne, Vgl(kv, x => x.Einspeisung_Ohne), "N0", KWH);
            Zahl(zeilen, GRUPPE_ENERGIE, MyResource.Resource.SP_ERG_EINSPEISUNG_MIT, k.Einspeisung_Mit, Vgl(kv, x => x.Einspeisung_Mit), "N0", KWH);

            // ABNAHMEBEFUND 2: Ohne Erzeugung ist die Eigenverbrauchsquote NICHT NULL,
            // sondern unbestimmt (0/0). Die Engine muss dafür 0 führen - das Feld geht so
            // in Tab_ErgebnisStromspeicher, und Access nimmt kein NaN entgegen. Auf dem
            // Bildschirm steht deshalb der Gedankenstrich.
            if (ein.ErzeugungKwh > 0.0)
                Zahl(zeilen, GRUPPE_ENERGIE, MyResource.Resource.SP_ERG_EIGENVERBRAUCH,
                     k.Eigenverbrauchsquote, Vgl(kv, x => x.Eigenverbrauchsquote), "N1", "%");
            else
                zeilen.Add(new Zeile(GRUPPE_ENERGIE, MyResource.Resource.SP_ERG_EIGENVERBRAUCH,
                                     UNBESTIMMT, "", "%", KennzahlStufe.Unbestimmt));

            Zahl(zeilen, GRUPPE_ENERGIE, MyResource.Resource.SP_ERG_AUTARKIE, k.Autarkiegrad, Vgl(kv, x => x.Autarkiegrad), "N1", "%");

            Zahl(zeilen, GRUPPE_SPEICHER, MyResource.Resource.SP_ERG_VOLLZYKLEN, k.Vollzyklen, Vgl(kv, x => x.Vollzyklen), "N1", "1/a");
            Zahl(zeilen, GRUPPE_SPEICHER, MyResource.Resource.SP_ERG_SOC_MIN, k.SoC_Min, Vgl(kv, x => x.SoC_Min), "N1", "kWh");
            Zahl(zeilen, GRUPPE_SPEICHER, MyResource.Resource.SP_ERG_SOC_MITTEL, k.SoC_Mittel, Vgl(kv, x => x.SoC_Mittel), "N1", "kWh");
            Zahl(zeilen, GRUPPE_SPEICHER, MyResource.Resource.SP_ERG_SOC_MAX, k.SoC_Max, Vgl(kv, x => x.SoC_Max), "N1", "kWh");
            Zahl(zeilen, GRUPPE_SPEICHER, MyResource.Resource.SP_ERG_ZEITANTEIL_UNTEN, k.Zeitanteil_Untergrenze, Vgl(kv, x => x.Zeitanteil_Untergrenze), "N1", "%");
            Zahl(zeilen, GRUPPE_SPEICHER, MyResource.Resource.SP_ERG_ZEITANTEIL_OBEN, k.Zeitanteil_Obergrenze, Vgl(kv, x => x.Zeitanteil_Obergrenze), "N1", "%");
            Zahl(zeilen, GRUPPE_SPEICHER, MyResource.Resource.SP_ERG_ZYKLEN_HOCHRECHNUNG,
                 k.Zyklen_Hochrechnung, Vgl(kv, x => x.Zyklen_Hochrechnung), "N0", "-",
                 Zyklenstufe(k, kontext));
            // Zugesicherte Zyklen sind ein Gerätedatum, kein Ergebnis - hier gibt es
            // nichts zu vergleichen.
            Zahl(zeilen, GRUPPE_SPEICHER, MyResource.Resource.SP_ERG_ZYKLEN_ZUGESICHERT,
                 kontext != null ? kontext.ZyklenZugesichert : 0.0, null, "N0", "-");

            Budgetzeilen(zeilen, kontext);

            Zahl(zeilen, GRUPPE_WIRTSCHAFT, MyResource.Resource.SP_ERG_ERTRAG_BEZUG, k.Ertrag_Bezugsersparnis, Vgl(kv, x => x.Ertrag_Bezugsersparnis), "N2", EUR_A);
            Zahl(zeilen, GRUPPE_WIRTSCHAFT, MyResource.Resource.SP_ERG_ERTRAG_VERGUETUNG, -k.Ertrag_Verguetung_Entgangen, Vgl(kv, x => -x.Ertrag_Verguetung_Entgangen), "N2", EUR_A);
            Zahl(zeilen, GRUPPE_WIRTSCHAFT, MyResource.Resource.SP_ERG_ERTRAG_NETZ, k.Ertrag_Netzerloes, Vgl(kv, x => x.Ertrag_Netzerloes), "N2", EUR_A);
            Zahl(zeilen, GRUPPE_WIRTSCHAFT, MyResource.Resource.SP_ERG_KOSTEN_LADUNG, k.Kosten_Ladung, Vgl(kv, x => x.Kosten_Ladung), "N2", EUR_A);
            Zahl(zeilen, GRUPPE_WIRTSCHAFT, MyResource.Resource.SP_ERG_ERTRAG_LEISTUNGSPREIS, k.Ertrag_Leistungspreis, Vgl(kv, x => x.Ertrag_Leistungspreis), "N2", EUR_A);
            Zahl(zeilen, GRUPPE_WIRTSCHAFT, MyResource.Resource.SP_ERG_VERSCHLEISS, k.Verschleisskosten, Vgl(kv, x => x.Verschleisskosten), "N2", EUR_A);
            // Investition und Annuität hängen allein an den Parametern, nicht an der
            // Betriebsstrategie - sie stehen in beiden Spalten gleich und bekommen
            // deshalb keinen Vergleichswert.
            Zahl(zeilen, GRUPPE_WIRTSCHAFT, MyResource.Resource.SP_ERG_INVESTITION, k.Investition, null, "N2", "€");
            Zahl(zeilen, GRUPPE_WIRTSCHAFT, MyResource.Resource.SP_ERG_ANNUITAET, k.Annuitaet, null, "N2", EUR_A);
            Zahl(zeilen, GRUPPE_WIRTSCHAFT, MyResource.Resource.SP_ERG_JAHRESUEBERSCHUSS, k.Jahresueberschuss, Vgl(kv, x => x.Jahresueberschuss), "N2", EUR_A);
            Zahl(zeilen, GRUPPE_WIRTSCHAFT, MyResource.Resource.SP_ERG_ERTRAG_JAHR1, k.Ertrag_Jahr1, Vgl(kv, x => x.Ertrag_Jahr1), "N2", EUR_A);
            Zahl(zeilen, GRUPPE_WIRTSCHAFT, MyResource.Resource.SP_ERG_ERTRAG_AEQUIVALENT, k.Ertrag_Aequivalent, Vgl(kv, x => x.Ertrag_Aequivalent), "N2", EUR_A);

            // Amortisation direkt aus dem Engine-Ergebnis: Es kennt die beiden Fälle
            // "nicht amortisierbar" und "> Nutzungsdauer", die der gespeicherte Satz als
            // 0 führen muss (Access nimmt kein Infinity entgegen).
            zeilen.Add(new Zeile(GRUPPE_WIRTSCHAFT, MyResource.Resource.SP_ERG_AMORT_STATISCH,
                                 AmortisationText(erg.Wirtschaftlichkeit.StatischeAmortisation),
                                 vergleich != null ? AmortisationText(vergleich.Wirtschaftlichkeit.StatischeAmortisation) : "",
                                 "a", KennzahlStufe.Unbestimmt));
            zeilen.Add(new Zeile(GRUPPE_WIRTSCHAFT, MyResource.Resource.SP_ERG_AMORT_DYNAMISCH,
                                 AmortisationText(erg.Wirtschaftlichkeit.DynamischeAmortisation),
                                 vergleich != null ? AmortisationText(vergleich.Wirtschaftlichkeit.DynamischeAmortisation) : "",
                                 "a", KennzahlStufe.Unbestimmt));
            Zahl(zeilen, GRUPPE_WIRTSCHAFT, MyResource.Resource.SP_ERG_KAPITALWERT, k.Kapitalwert, Vgl(kv, x => x.Kapitalwert), "N2", "€");

            return zeilen;
        }

        /// <summary>
        /// Zeilen der Preissteuerung (AP10, Fachkonzept 6.5): Jahres-Zyklenbudget, seine
        /// Auslastung mit Warnstufe, der Verschleiß je ausgespeicherter kWh und die Zahl
        /// der angenommenen Paarungen.
        ///
        /// <para>Sie erscheinen nur, wenn wirklich mit der Preissteuerung gerechnet wurde —
        /// ohne sie ist das Budget keine Schranke, sondern nur eine zweite Schreibweise
        /// der Zyklenhochrechnung eine Zeile weiter oben.</para>
        /// </summary>
        private static void Budgetzeilen(List<Zeile> zeilen, StromspeicherLaufKontext kontext)
        {
            ArbitrageErgebnis arb = kontext != null ? kontext.Arbitrageergebnis : null;
            if (arb == null) return;

            ArbitrageKennzahlen a = arb.Kennzahlen;

            Zahl(zeilen, GRUPPE_SPEICHER, MyResource.Resource.ARB_ERG_BUDGET, a.ZyklenbudgetDcKwhProA, null, "N0", "kWh/a");
            Zahl(zeilen, GRUPPE_SPEICHER, MyResource.Resource.ARB_ERG_BUDGET_AUSLASTUNG,
                 a.BudgetauslastungProzent, null, "N1", "%", Budgetstufe(a));
            Zahl(zeilen, GRUPPE_SPEICHER, MyResource.Resource.ARB_ERG_KVER, a.VerschleissCtKwh, null, "N3", "ct/kWh");
            Zahl(zeilen, GRUPPE_SPEICHER, MyResource.Resource.ARB_ERG_PAARE,
                 a.PaareAngenommen + a.VerkaufsslotsAngenommen, null, "N0", "-");
        }

        /// <summary>
        /// Warnstufe der Zyklenzeile (Fachkonzept 5.4/7.1): grün bis 90 % des Budgets,
        /// gelb darüber, rot bei Überschreitung, unbestimmt ohne gepflegte N_zyk.
        /// </summary>
        public static KennzahlStufe Zyklenstufe(ErgebnisStromspeicherModel k, StromspeicherLaufKontext kontext)
        {
            double budget = kontext != null ? kontext.ZyklenZugesichert : 0.0;
            if (budget <= 0.0) return KennzahlStufe.Unbestimmt;
            if (k.Zyklen_Hochrechnung > budget) return KennzahlStufe.Ueberschritten;
            if (k.Zyklen_Hochrechnung > budget * 0.9) return KennzahlStufe.Knapp;
            return KennzahlStufe.Ok;
        }

        /// <summary>Dieselbe Staffelung für die Budgetzeile der Preissteuerung.</summary>
        public static KennzahlStufe Budgetstufe(ArbitrageKennzahlen a)
        {
            if (a.ZyklenbudgetDcKwhProA <= 0.0) return KennzahlStufe.Unbestimmt;
            if (a.BudgetauslastungProzent > 100.0) return KennzahlStufe.Ueberschritten;
            if (a.BudgetauslastungProzent > 90.0) return KennzahlStufe.Knapp;
            return KennzahlStufe.Ok;
        }

        /// <summary>Ins Netz verkaufte Energie des Laufs [kWh/a]; 0 ohne Preissteuerung.</summary>
        public static double VerkaufKwh(StromspeicherLaufKontext kontext)
        {
            return kontext != null && kontext.Arbitrageergebnis != null
                ? kontext.Arbitrageergebnis.Kennzahlen.VerkaufKwh
                : 0.0;
        }

        /// <summary>
        /// Amortisationszeit als Text: die Jahre, oder der Klartext des Sonderfalls
        /// (Fachkonzept 7.1 — die V7-Mappe schrieb beides in dieselbe Zelle, die Engine
        /// trennt Zustand und Zahl).
        ///
        /// <para>Seit iU9-W11a.5 steht sie in <see cref="SpeicherAnzeigeCtrl"/> — sie
        /// gehoerte zu dritt in den Bestand (Befund W11-B42). Hier bleibt die
        /// Weiterleitung, damit der Kennzahlenblock in einer Datei lesbar bleibt.</para>
        /// </summary>
        public static string AmortisationText(Amortisation a)
        {
            return SpeicherAnzeigeCtrl.AmortisationText(a);
        }

        /// <summary>Wert des Vergleichslaufs, oder <c>null</c>, wenn es keinen gibt.</summary>
        private static double? Vgl(ErgebnisStromspeicherModel kv,
                                   System.Func<ErgebnisStromspeicherModel, double> auswahl)
        {
            return kv != null ? auswahl(kv) : (double?)null;
        }

        private static void Zahl(List<Zeile> zeilen, string gruppe, string bezeichnung,
                                 double wert, double? vergleich, string format, string einheit,
                                 KennzahlStufe stufe = KennzahlStufe.Unbestimmt)
        {
            zeilen.Add(new Zeile(
                gruppe, bezeichnung,
                wert.ToString(format, CultureInfo.CurrentCulture),
                vergleich.HasValue ? vergleich.Value.ToString(format, CultureInfo.CurrentCulture) : "",
                einheit, stufe));
        }
    }
}
