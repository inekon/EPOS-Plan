using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Wirtschaftlichkeit;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HUELLE des Dialogs „BHKW-Wirtschaftlichkeit" (Etappe B5b).
    ///
    /// <para><b>Stichtag iZ5, zweite Maske.</b> Der Dialog lebt seit B5b als
    /// Razor-Komponente <see cref="BhkwWirtschaftlichkeitDialog"/> in
    /// <c>EPOS.UI</c>; die WinForms-Fassung <c>Form_BhkwWirtschaftlichkeit</c> ist
    /// mit demselben Schritt GELOESCHT (Regel M1: keine zweite Fassung derselben
    /// Maske). Vorbild dieser Klasse ist <c>Views/Kosten/Form_Kosten.cs</c>,
    /// <c>CreateNewEnergyCarrier</c> (iU8-9): Parameterwoerterbuch bauen,
    /// <c>Geschlossen</c>-Rueckruf auf <see cref="BlazorDialogForm{T}.Schliessen"/>
    /// legen, <c>ShowDialog()</c> wie bisher auswerten.</para>
    ///
    /// <para><b>Hier liegt die Datenseite.</b> Die Komponente kennt keine Datenbank
    /// (Hausregel <c>EPOS.UI/CLAUDE.md</c>). Alles, was sie zeigt, wird hier
    /// geladen — mit denselben Controllern und in derselben Reihenfolge wie zuvor
    /// im Konstruktor des Formulars — und alles, was sie schreibt, wird hier
    /// geschrieben: <c>KwkgAnlagenCtrl.Speichere(g, true)</c> (K7, elf Spalten) und
    /// <c>WirtschaftlichkeitCtrl.SpeichereParameter</c>.</para>
    ///
    /// <para><b>Der Sprung in die Tarifstruktur laeuft nachgelagert.</b> Die beiden
    /// Sprungknoepfe der Stromsteuergruppe fuehren in <c>Form_Tarifstruktur</c> —
    /// einen WinForms-Dialog. Ein Muster, mit dem eine Razor-Komponente ein zweites
    /// MODALES Fenster ueber sich oeffnet, hat das Haus (Stand iU8) nicht; die
    /// einzige Bruecke ist <c>IHilfeDienst</c>, und die zeigt ein modeloses
    /// Hilfefenster. Die Komponente meldet den Wunsch deshalb im Ergebnis, diese
    /// Huelle schliesst den Dialog, oeffnet das Ziel und bringt den Dialog danach
    /// mit frisch geladenen Daten zurueck. <b>Designfrage fuer B6</b> (siehe
    /// <c>Allgemein/Reporting/B5b_Blazor_Port_Protokoll.md</c>).</para>
    /// </summary>
    internal static class BhkwWirtschaftlichkeitHuelle
    {
        /// <summary>Innenmass des Dialogfensters. Breite wie die WinForms-Fassung
        /// (Hausmass § 5 der Feldkarte, 914); die Hoehe deckelt den Arbeitsbereich,
        /// gescrollt wird innerhalb der Komponente.</summary>
        private const int FENSTER_BREITE = 914;

        /// <summary>
        /// Zeigt den Dialog. Liefert <c>true</c>, wenn mindestens einmal gespeichert
        /// wurde — dann rechnet die Wirtschaftlichkeitsseite neu.
        /// </summary>
        /// <param name="besitzer">Besitzerfenster (fuer die mittige Lage).</param>
        /// <param name="idStamm">Stammprojekt der Vergleichsgruppe.</param>
        /// <param name="ergebnisseAusLauf">Die Ergebnisse des zuletzt gerechneten
        /// Laufs; <c>null</c> ist zulaessig. Zwei ihrer Bestandteile sind nicht
        /// persistiert und aus der Datenbank nicht zu holen: die Kohaerenzhinweise
        /// (B2-O4) und die KWKG-Modulnachweise mit der Mengenkette (E7/B3b).</param>
        internal static bool Oeffnen(IWin32Window besitzer, int idStamm,
                                     List<WirtschaftlichkeitErgebnis> ergebnisseAusLauf)
        {
            bool gespeichert = false;

            // Der Sprung in die Tarifstruktur schliesst den Dialog und bringt ihn
            // danach zurueck; deshalb eine Schleife statt eines einzelnen Aufrufs.
            while (true)
            {
                BhkwWirtschaftlichkeitErgebnis ergebnis = EinmalZeigen(besitzer, idStamm,
                                                                      ergebnisseAusLauf);
                if (ergebnis == null) return gespeichert;
                if (ergebnis.Gespeichert) gespeichert = true;
                if (ergebnis.Sprung == BhkwSprung.Keiner) return gespeichert;

                TarifOeffnen(besitzer, idStamm, ergebnis.Sprung);
            }
        }

        /// <summary>Ein Durchgang: laden, zeigen, Ergebnis melden.</summary>
        private static BhkwWirtschaftlichkeitErgebnis EinmalZeigen(
            IWin32Window besitzer, int idStamm, List<WirtschaftlichkeitErgebnis> ergebnisseAusLauf)
        {
            var ctrl = new WirtschaftlichkeitCtrl();
            var anlagenCtrl = new KwkgAnlagenCtrl();
            var katalog = new GesetzKatalog();

            WirtschaftlichkeitParameter parameter = ctrl.LadeParameter(idStamm);
            WirtschaftlichkeitCtrl.ErzeugerFlags erzeuger = ctrl.ErzeugerDerGruppe(idStamm);

            var pc = new ProjektCtrl();
            try { pc.ReadSingle(idStamm); } catch { }
            string stammName = pc.rows > 0 ? pc.m_szProjektname : "";

            List<KwkgAnlagenAngabe> anlagen = anlagenCtrl.LadeGruppe(idStamm, stammName);

            // Die laufunabhaengige Doppelpflege-Pruefung — genau der Zweig, der keinen
            // Steuerlauf braucht. Sie ist internal zum Kern und deshalb hier, nicht in
            // der Komponente.
            var doppelpflege = new List<KohaerenzHinweis>();
            try { doppelpflege.AddRange(KohaerenzPruefung.Pruefe(idStamm, null)); }
            catch { }

            BhkwWirtschaftlichkeitErgebnis ergebnis = null;
            BlazorDialogForm<BhkwWirtschaftlichkeitDialog> dlg = null;

            var werte = new Dictionary<string, object>
            {
                ["IdStamm"] = idStamm,
                ["StammName"] = stammName,
                ["Anlagen"] = anlagen,
                ["Parameter"] = parameter,
                ["HatHeizkessel"] = erzeuger != null && erzeuger.Heizkessel,
                ["Doppelpflege"] = doppelpflege,

                // Der Gesetzeskatalog als DELEGAT — dieselbe Uebergabe, die
                // KwkgSatzRechner selbst verlangt (Leitentscheidung L9). Die Komponente
                // bleibt damit datenbankfrei und rechnet trotzdem mit dem einen Katalog.
                ["Katalog"] = new Func<string, int, GesetzParameter>(katalog.WertMitHerkunft),

                ["GrenzeAusschreibungRueckfall"] = WirtschaftlichkeitCtrl.KWKG_MAX_LEISTUNG_KW,
                ["GrenzeStromsteuerRueckfall"] = 2000.0,

                ["ErgebnisseAusLauf"] = (IReadOnlyList<WirtschaftlichkeitErgebnis>)
                    (ergebnisseAusLauf ?? new List<WirtschaftlichkeitErgebnis>()),
                ["ErgebnisseLaden"] =
                    new Func<IReadOnlyList<int>, IReadOnlyList<WirtschaftlichkeitErgebnis>>(
                        ids => ctrl.LadeErgebnisse(new List<int>(ids))),

                // Der Schreibweg. Rueckgabe = Zahl der gescheiterten Saetze; die
                // Komponente macht daraus ihre Statuszeile bzw. ihr Warnbanner.
                ["Speichern"] = new Func<int>(() => Speichern(anlagenCtrl, ctrl, anlagen, parameter)),

                ["Geschlossen"] = EventCallback.Factory
                    .Create<BhkwWirtschaftlichkeitErgebnis>(new object(), e =>
                    {
                        ergebnis = e;
                        if (dlg != null) dlg.Schliessen(e != null && e.Gespeichert);
                    })
            };

            int hoehe = Math.Max(420, Screen.PrimaryScreen.WorkingArea.Height - 90);
            dlg = new BlazorDialogForm<BhkwWirtschaftlichkeitDialog>(
                Titel(stammName), new Size(FENSTER_BREITE, hoehe), werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ergebnis;
        }

        /// <summary>
        /// Schreibt den Bildschirmzustand fort: die Anlagenzeilen mit ihren ELF Spalten
        /// (K7) und die Projektvorgaben. Liefert die Zahl der gescheiterten Saetze.
        ///
        /// <para><b>Nur die Felder dieses Dialogs.</b> Alles Uebrige steht unveraendert
        /// im geladenen Parametersatz und geht wertgleich in die Zeile zurueck; das ist
        /// dieselbe Eigenschaft, mit der der Parameterdialog seine ausgeblendeten
        /// Gruppen unveraendert laesst.</para>
        /// </summary>
        private static int Speichern(KwkgAnlagenCtrl anlagenCtrl, WirtschaftlichkeitCtrl ctrl,
                                     List<KwkgAnlagenAngabe> anlagen,
                                     WirtschaftlichkeitParameter parameter)
        {
            int fehler = 0;
            foreach (KwkgAnlagenAngabe a in anlagen)
                if (!anlagenCtrl.Speichere(a, true)) fehler++;

            // K3 = a: Der Modus des § 9 Abs. 1 Nr. 3 wird NICHT gespeichert — es gibt
            // dafuer bis B6 (M-3) keine Spalte.
            try { if (!ctrl.SpeichereParameter(parameter)) fehler++; }
            catch { fehler++; }
            return fehler;
        }

        /// <summary>Der Sprung in die Tarifstruktur — nachgelagert, siehe Klassenkopf.</summary>
        private static void TarifOeffnen(IWin32Window besitzer, int idStamm, BhkwSprung sprung)
        {
            TarifSicht sicht = sprung == BhkwSprung.BhkwTarif
                             ? TarifSicht.Bhkw : TarifSicht.Strombezug;
            try
            {
                using (var dlg = new Form_Tarifstruktur(idStamm, sicht))
                {
                    if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Titel(""),
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Fenstertitel — derselbe Text wie in der Komponente.</summary>
        private static string Titel(string stammName)
        {
            string t = BhwTexte.T("BHW_TITEL", "BHKW-Wirtschaftlichkeit");
            return string.IsNullOrEmpty(stammName) ? t : t + " — " + stammName;
        }
    }
}
