using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Wirtschaftlichkeit;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HUELLE des Dialogs „Kapitalwert-Verlauf" (iU9-W1.6).
    ///
    /// <para><b>Hier liegt die Rechnung.</b> Die Komponente
    /// <see cref="KapitalwertVerlaufDialog"/> kennt weder Datenbank noch
    /// Renderer; sie ruft einen Delegaten und zeigt zwei PNG. Dieser Delegat
    /// macht genau das, was <c>Form_WirtschaftlichkeitVerlauf.btnZeichnen_Click</c>
    /// tat: Parameter und Tarif laden, die Simulationsdaten EINMAL sammeln
    /// (<c>BerichtsDatenSammler</c>, danach aus dem Zwischenspeicher),
    /// <c>WirtschaftlichkeitCtrl.BerechneVerlauf</c> rufen und die beiden Bilder
    /// mit <c>ChartRenderer.KapitalwertVerlauf</c> zeichnen — alles auf einem
    /// eigenen Faden (<c>Task.Run</c>), abbrechbar über den <c>CancellationToken</c>
    /// der Komponente.</para>
    ///
    /// <para><b>Was nicht mitgeht.</b> Die <c>ProgressBar</c> und der
    /// <c>IProgress</c>-Melder des Sammlers: Ein Fortschrittsbaustein entsteht in
    /// <c>EPOS.UI</c> erst in Welle 11 (Bausteinlücke 13). Der Sammler bekommt
    /// deshalb <c>null</c> als Melder — er kommt damit aus, der Weg ist
    /// derselbe.</para>
    /// </summary>
    internal static class KapitalwertVerlaufHuelle
    {
        /// <summary>Die drei Szenarien als Auswahleinträge. Die Ids sind Indizes;
        /// den Persistenzwert (<c>Tab_ErgebnisWirtschaftlichkeit.Szenario</c>)
        /// setzt <see cref="SzenarioZu"/> — Anzeigetext und Steuerwert fallen hier
        /// zusammen, genau wie in der <c>ComboBox</c> des Vorläufers.</summary>
        private static readonly string[] SZENARIEN =
        {
            WirtschaftlichkeitSzenario.ERWARTET,
            WirtschaftlichkeitSzenario.BEST,
            WirtschaftlichkeitSzenario.WORST
        };

        private static string SzenarioZu(int id)
            => (id >= 0 && id < SZENARIEN.Length) ? SZENARIEN[id] : WirtschaftlichkeitSzenario.ERWARTET;

        /// <summary>
        /// Zeigt den Dialog. Liefert <c>true</c>, wenn beim Sammeln neu simuliert
        /// wurde — der Aufrufer frischt dann seine Anzeige auf (Review Phase 11).
        /// </summary>
        /// <param name="besitzer">Besitzerfenster (für die mittige Lage).</param>
        /// <param name="idStamm">Stammprojekt der Vergleichsgruppe.</param>
        /// <param name="stammName">Name des Stammprojekts (steht im Fenstertitel).</param>
        /// <param name="variantenIds">Die angehakten Varianten.</param>
        internal static bool Oeffnen(IWin32Window besitzer, int idStamm, string stammName,
                                     List<int> variantenIds)
        {
            Func<bool> neuGesammelt;
            BlazorDialogForm<KapitalwertVerlaufDialog> dlg = null;

            var werte = new Dictionary<string, object>(
                Gaben(idStamm, stammName, variantenIds, out neuGesammelt))
            {
                ["Geschlossen"] = EventCallback.Factory.Create(new object(), () =>
                {
                    if (dlg != null) dlg.Schliessen(true);
                })
            };

            // Das Entwurfsmaß 898 x 744 des Designers, auf den Arbeitsbereich
            // gedeckelt — dasselbe tat GroesseAufArbeitsflaecheDeckeln. Die Hülle
            // klemmt zusätzlich auf 92 % des Bildschirms.
            int hoehe = Math.Max(560, Math.Min(760, Screen.PrimaryScreen.WorkingArea.Height - 90));
            dlg = new BlazorDialogForm<KapitalwertVerlaufDialog>(
                Titel(stammName ?? ""), new Size(1000, hoehe), werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return neuGesammelt();
        }

        /// <summary>
        /// Der PARAMETERSATZ des Dialogs (iU9-W5.3). Seit die
        /// Wirtschaftlichkeitsseite selbst eine Razor-Komponente ist, erscheint
        /// der Verlauf in einer <c>Ueberlagerung</c> darin — dasselbe Fenster,
        /// dieselbe WebView (Risiko R2). <c>Geschlossen</c> setzt der Wirt.
        /// </summary>
        /// <param name="neuGesammelt">
        /// Liefert nach dem Schließen, ob der Lauf neu simuliert hat — dann
        /// passen die persistierten Ergebnisse nicht mehr zum Simulationsstand
        /// (Review Phase 11).
        /// </param>
        internal static IReadOnlyDictionary<string, object> Gaben(
            int idStamm, string stammName, List<int> variantenIds, out Func<bool> neuGesammelt)
        {
            string name = stammName ?? "";
            List<int> varianten = variantenIds ?? new List<int>();

            var ctrl = new WirtschaftlichkeitCtrl();
            BerichtsDaten daten = null;              // Zwischenspeicher des Simulationsstands
            var neu = new bool[1];
            neuGesammelt = () => neu[0];

            var szenarien = new List<ValueTuple<int, string>>();
            for (int i = 0; i < SZENARIEN.Length; i++)
                szenarien.Add(new ValueTuple<int, string>(i, SZENARIEN[i]));

            // ParameterVorbelegen: der gespeicherte Betrachtungszeitraum, wenn er
            // im Bereich des Drehfeldes liegt.
            int jahreVorgabe = 20;
            try
            {
                WirtschaftlichkeitParameter p0 = ctrl.LadeParameter(idStamm);
                if (p0.Betrachtungszeitraum >= 2 && p0.Betrachtungszeitraum <= 60)
                    jahreVorgabe = p0.Betrachtungszeitraum;
            }
            catch { }

            return new Dictionary<string, object>
            {
                ["Szenarien"] = (IReadOnlyList<ValueTuple<int, string>>)szenarien,
                ["JahreVorgabe"] = jahreVorgabe,

                ["Berechnen"] = new Func<int, int, CancellationToken, Task<KapitalwertVerlaufBilder>>(
                    (jahre, szenarioId, ct) => Task.Run(() =>
                    {
                        string szenario = SzenarioZu(szenarioId);
                        WirtschaftlichkeitParameter p = ctrl.LadeParameter(idStamm);
                        TarifParameter tarif = ctrl.LadeTarif(idStamm);
                        bool mitZeitreihen = tarif.Aktiv || p.KwkgBonus > 0 || p.KwkgBonusEinspeisung > 0;

                        bool warGecacht = daten != null;
                        if (daten == null)   // Simulationsdaten nur einmal sammeln
                            daten = new BerichtsDatenSammler().Sammle(
                                idStamm, name, varianten, false, mitZeitreihen, null, ct);

                        WirtschaftlichkeitVerlauf verlauf = ctrl.BerechneVerlauf(daten, p, jahre, szenario);

                        // Exaktes Kriterium: der Sammler markiert neu simulierte (und damit
                        // neu persistierte) Projekte selbst (Review-Verifikation 11).
                        if (!warGecacht && daten != null &&
                            daten.Varianten.Any(v => v.FrischSimuliert))
                            neu[0] = true;

                        return Bilder(verlauf, p, jahre, szenario);
                    }, ct)),

                ["TitelText"] = Titel(name),
                ["LabelJahre"] = Text_("WVERL_LBL_ZEITRAUM", "Zeitraum [Jahre]:"),
                ["LabelSzenario"] = Text_("WVERL_LBL_SZENARIO", "Szenario:"),
                ["ZeichnenText"] = Text_("WVERL_BTN_ZEICHNEN", "Aktualisieren"),
                ["SchliessenText"] = Text_("WVERL_BTN_SCHLIESSEN", "Schließen"),
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN,
                ["LaeuftText"] = Text_("WVERL_STATUS_LAEUFT", "Berechnung läuft …"),
                ["AbgebrochenText"] = Text_("WVERL_STATUS_ABBRUCH", "Vorgang abgebrochen."),
                ["VorlageFehler"] = Text_("WVERL_MSG_FEHLER", "Fehler beim Berechnen des Verlaufs: {0}"),
                ["AltDifferenz"] = Text_("WVERL_BILD_DIFF", TITEL_DIFF),
                ["AltAbsolut"] = Text_("WVERL_BILD_ABS", TITEL_ABS),
                ["PlatzhalterText"] = Text_("WVERL_KEIN_BILD", "Noch kein Diagramm")
            };
        }

        // ------------------------------------------------------------------ Bilder

        private const string TITEL_DIFF = "Kapitalwert-Verlauf: Differenz zur Stamm-Referenz";
        private const string TITEL_ABS = "Kapitalwert-Verlauf: kumulierte Barwerte je Projekt";

        /// <summary>
        /// Zeichnet die beiden Bilder und baut die beiden Textzeilen — wortgleich aus
        /// <c>ZeigeDiagramme</c> und dem Statusteil von <c>btnZeichnen_Click</c>.
        /// </summary>
        private static KapitalwertVerlaufBilder Bilder(WirtschaftlichkeitVerlauf verlauf,
                                                       WirtschaftlichkeitParameter p,
                                                       int jahre, string szenario)
        {
            var kultur = BerichtTexte.Kultur;

            byte[] diff = ChartRenderer.KapitalwertVerlauf(
                TITEL_DIFF,
                ChartRenderer.VerlaufsReihen(verlauf.Differenz, false),
                "Kumulierte diskontierte Differenz-Zahlungsströme Variante − Stamm; " +
                "Schnitt mit der Nulllinie = dynamische Amortisation. Ohne Restwert.");

            byte[] abs = ChartRenderer.KapitalwertVerlauf(
                TITEL_ABS,
                ChartRenderer.VerlaufsReihen(verlauf.Absolut, true),
                "Kumulierte diskontierte Zahlungsströme (Kosten negativ). " +
                "Ohne Restwert — Nettobarwert = Endwert + Restwert-Barwert.");

            // Restwerte am gewählten Horizont ausweisen (Reihen sind ohne Restwert).
            var teile = new List<string>();
            foreach (VerlaufSerie s in verlauf.Absolut)
                if (s.Kumuliert != null && Math.Abs(s.RestwertBarwert) > 0.5)
                    teile.Add(s.Anzeige + " " + s.RestwertBarwert.ToString("N0", kultur) + " €");
            string restwert = teile.Count > 0
                ? "Restwert-Barwerte am Horizontende (nicht in den Linien enthalten): " +
                  string.Join(" · ", teile)
                : "";

            // Nicht berechenbare Projekte offen ausweisen.
            var fehler = verlauf.Absolut.Where(s => s.Fehlgrund != null).ToList();
            if (fehler.Count > 0)
                restwert = ("⚠ Ohne Reihe: " + string.Join("; ",
                    fehler.Select(s => s.Anzeige + " (" + s.Fehlgrund + ")")) + "   " +
                    restwert).Trim();

            string status = "Verlauf über " + jahre + " Jahre, Szenario „" + szenario + "“" +
                            (jahre != p.Betrachtungszeitraum
                             ? " (abweichend von T = " + p.Betrachtungszeitraum + " a — nur Anzeige, " +
                               "gespeicherte Ergebnisse unverändert" +
                               (jahre > p.Betrachtungszeitraum
                                ? "; Nulldurchgänge jenseits von T erscheinen nicht in der " +
                                  "gespeicherten Amortisationskennzahl"
                                : "") + ")."
                             : ".");

            return new KapitalwertVerlaufBilder(diff, abs, restwert, status);
        }

        /// <summary>Fenstertitel — wortgleich aus <c>TexteSetzen</c>.</summary>
        private static string Titel(string stammName)
        {
            return Text_("WVERL_TITEL", "Kapitalwert-Verlauf über den Nutzungszeitraum") +
                   " — Stamm: " + stammName;
        }

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
