using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Kosten;
using Microsoft.AspNetCore.Components;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HUELLE des Dialogs „Kostenprofil" (iU9-W3.4).
    ///
    /// <para><b>Hier liegen Datenseite und Rechnung.</b> Die Komponente
    /// <see cref="KostenprofilDialog"/> kennt weder Datenbank noch Engine noch
    /// Renderer. Diese Hülle lädt und schreibt über
    /// <see cref="KostenprofilCtrl"/>, sie serialisiert die beiden
    /// <c>";"</c>-Zeichenketten (invariant, wie im Vorläufer) und sie zeichnet
    /// die Vorschau: <c>PreisModell.AusMonatsUndWochenwerten</c> — dieselbe
    /// Engine-Methode, mit der später auch die Simulation rechnet — und daraus
    /// <c>ChartRenderer.Kostenprofil</c>.</para>
    ///
    /// <para><b>Die Vorschau läuft auf einem eigenen Faden</b> (<c>Task.Run</c>,
    /// Muster <c>KapitalwertVerlaufHuelle</c>): 8 760 Stützstellen rechnen und
    /// zeichnen dauert lang genug, dass eine blockierte WebView auffiele.</para>
    /// </summary>
    internal static class KostenprofilHuelle
    {
        /// <summary>Vorbelegung eines Monatswerts [ct/kWh] — der Regelfall-Aufschlag
        /// plus 20 ct Energie (wortgleich aus <c>Form_Kostenprofil</c>).</summary>
        private const double VORGABE_MONATSWERT = 25.0;

        /// <summary>Innenmaß des Fensters. Die WinForms-Fassung maß 700 × 580 mit
        /// drei Reitern; die Blazor-Fassung stellt Monat, Woche und Grafik
        /// untereinander (Bausteinlücke 10 — den Reiter gibt es erst in Welle 5).</summary>
        private static readonly Size FENSTER = new Size(900, 860);

        /// <summary>
        /// Zeigt den Dialog. Liefert <c>true</c>, wenn gespeichert wurde.
        /// </summary>
        /// <param name="besitzer">Besitzerfenster (für die mittige Lage).</param>
        /// <param name="idProjekt">Projekt, dem das Profil gehört.</param>
        /// <param name="idProfil">Vorhandenes Profil, oder 0 für ein neues.</param>
        internal static bool Oeffnen(IWin32Window besitzer, int idProjekt, int idProfil = 0)
        {
            KostenprofilCtrl.StelleTabelleSicher();

            var ctrl = new KostenprofilCtrl();
            KostenprofilModel modell = idProfil > 0 ? ctrl.ReadSingle(idProfil) : null;
            if (modell == null)
            {
                modell = new KostenprofilModel { ID_Projekt = idProjekt };
                modell.Bezeichner = MyResource.Resource.PREIS_PROFIL_NEU;
            }

            double[] monat = MonateLesen(modell.Monatswerte);
            double[] woche = WochenwerteLesen(modell.Wochenwerte);

            bool gespeichert = false;
            BlazorDialogForm<KostenprofilDialog> dlg = null;

            var parameter = new Dictionary<string, object>
            {
                ["Bezeichner"] = modell.Bezeichner ?? "",
                ["Monatswerte"] = (IReadOnlyList<double>)new List<double>(monat),
                ["Wochenwerte"] = (IReadOnlyList<double>)new List<double>(woche),
                ["Monatsnamen"] = (IReadOnlyList<string>)Monatsnamen(),
                ["Wochentage"] = (IReadOnlyList<ValueTuple<int, string>>)Wochentage(),
                ["Einheit"] = DbWerte.PREISREIHE_EINHEIT_CT_KWH,

                // ChartAktualisieren: Engine rechnen lassen, Kern zeichnen lassen.
                ["Vorschau"] = new Func<IReadOnlyList<double>, IReadOnlyList<double>, Task<byte[]>>(
                    (m, w) => Task.Run(() =>
                    {
                        double[] mm = Feld(m, 12);
                        double[] ww = Feld(w, 168);
                        double[] profil = PreisModell.AusMonatsUndWochenwerten(mm, ww);

                        return ChartRenderer.Kostenprofil(
                            MyResource.Resource.PREIS_CHART_SERIE_KOSTENPROFIL,
                            profil,
                            DbWerte.PREISREIHE_EINHEIT_CT_KWH,
                            MyResource.Resource.CHART_ACHSE_MONAT);
                    })),

                // btnOk_Click: schreiben, dann schließen.
                ["Speichern"] = new Func<string, IReadOnlyList<double>, IReadOnlyList<double>, bool>(
                    (bezeichner, m, w) =>
                    {
                        modell.ID_Projekt = idProjekt;
                        modell.Bezeichner = bezeichner.Length > 0
                            ? bezeichner : MyResource.Resource.PREIS_PROFIL_NEU;
                        modell.Monatswerte = Kette(Feld(m, 12));
                        modell.Wochenwerte = Kette(Feld(w, 168));

                        bool ok = modell.ID > 0 ? ctrl.Update(modell) : ctrl.Insert(modell) > 0;
                        if (ok) gespeichert = true;
                        return ok;
                    }),

                ["TitelText"] = MyResource.Resource.PREIS_PROFIL_TITEL,
                ["InfoText"] = MyResource.Resource.PREIS_PROFIL_INFO,
                ["LabelBezeichner"] = MyResource.Resource.PREIS_PROFIL_LABEL_BEZEICHNER,
                ["KopfMonat"] = MyResource.Resource.PREIS_PROFIL_KOPF_MONAT,
                ["KopfWoche"] = MyResource.Resource.PREIS_PROFIL_KOPF_WOCHE,
                ["KopfGrafik"] = MyResource.Resource.PREIS_PROFIL_TAB_GRAFIK,
                ["LabelWochentag"] = MyResource.Resource.PREIS_PROFIL_LBL_WOCHENTAG,
                ["HinweisAbweichung"] = MyResource.Resource.PREIS_PROFIL_HINWEIS_ABWEICHUNG,
                ["AlleMonateText"] = MyResource.Resource.PREIS_PROFIL_BTN_ALLE_MONATE,
                ["TagKopierenText"] = MyResource.Resource.PREIS_PROFIL_BTN_TAG_KOPIEREN,
                ["TagEinfuegenText"] = MyResource.Resource.PREIS_PROFIL_BTN_TAG_EINFUEGEN,
                ["AlleTageText"] = MyResource.Resource.PREIS_PROFIL_BTN_ALLE_TAGE,
                ["TagUebernehmenText"] = MyResource.Resource.PREIS_PROFIL_BTN_UEBERNEHMEN,
                ["AktualisierenText"] = Text_("KPROF_BTN_VORSCHAU", "Vorschau aktualisieren"),
                ["BildAlt"] = Text_("KPROF_BILD_ALT",
                    "Jahresprofil der Strompreise über 8760 Stunden"),
                ["PlatzhalterText"] = Text_("KPROF_KEIN_BILD", "Noch kein Diagramm"),
                ["MeldungAlleTage"] = MyResource.Resource.PREIS_PROFIL_MSG_ALLE_TAGE,
                ["MeldungErstKopieren"] = MyResource.Resource.PREIS_PROFIL_MSG_ERST_KOPIEREN,
                ["MeldungNichtGespeichert"] = MyResource.Resource.PREIS_PROFIL_MSG_NICHT_GESPEICHERT,
                ["OkText"] = MyResource.Resource.SIM_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.SIM_BTN_ABBRECHEN,

                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), ok =>
                {
                    if (dlg != null) dlg.Schliessen(ok);
                })
            };

            dlg = new BlazorDialogForm<KostenprofilDialog>(
                MyResource.Resource.PREIS_PROFIL_TITEL, FENSTER, parameter);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return gespeichert;
        }

        // =================================================================== Ablageformat

        /// <summary>
        /// Monatswerte aus <c>"m1;…;m12"</c> — invariant gelesen, fehlende Werte
        /// auf die Vorgabe (wortgleich aus der Eigenschaft <c>Monatswerte</c>).
        /// </summary>
        private static double[] MonateLesen(string kette)
        {
            var w = new double[12];
            for (int m = 0; m < 12; m++) w[m] = VORGABE_MONATSWERT;
            if (string.IsNullOrEmpty(kette)) return w;

            string[] teile = kette.Split(';');
            for (int m = 0; m < 12 && m < teile.Length; m++)
            {
                double d;
                if (double.TryParse(teile[m], NumberStyles.Float, CultureInfo.InvariantCulture, out d))
                    w[m] = d;
            }
            return w;
        }

        /// <summary>Wochenwerte aus <c>"w1;…;w168"</c> (Montag 0 Uhr bis Sonntag 23 Uhr).</summary>
        private static double[] WochenwerteLesen(string kette)
        {
            var w = new double[168];
            if (string.IsNullOrEmpty(kette)) return w;

            string[] teile = kette.Split(';');
            for (int i = 0; i < 168 && i < teile.Length; i++)
            {
                double d;
                if (double.TryParse(teile[i], NumberStyles.Float, CultureInfo.InvariantCulture, out d))
                    w[i] = d;
            }
            return w;
        }

        /// <summary>Zurück in die Ablage — invariant, mit <c>";"</c> getrennt.</summary>
        private static string Kette(double[] werte)
        {
            var teile = new string[werte.Length];
            for (int i = 0; i < werte.Length; i++)
                teile[i] = werte[i].ToString(CultureInfo.InvariantCulture);
            return string.Join(";", teile);
        }

        private static double[] Feld(IReadOnlyList<double> werte, int laenge)
        {
            var w = new double[laenge];
            for (int i = 0; i < laenge && i < werte.Count; i++) w[i] = werte[i];
            return w;
        }

        // =================================================================== Namen

        private static List<string> Monatsnamen()
        {
            string[] namen = CultureInfo.CurrentUICulture.DateTimeFormat.MonthNames;
            var zwoelf = new List<string>(12);
            for (int m = 0; m < 12; m++) zwoelf.Add(m < namen.Length ? namen[m] : "");
            return zwoelf;
        }

        /// <summary>Die sieben Wochentage ab MONTAG — dieselbe Drehung wie im Vorläufer.</summary>
        private static List<ValueTuple<int, string>> Wochentage()
        {
            string[] tage = CultureInfo.CurrentUICulture.DateTimeFormat.DayNames;
            var abMontag = new List<ValueTuple<int, string>>(7);
            for (int t = 0; t < 7; t++)
                abMontag.Add(new ValueTuple<int, string>(t, tage[(t + 1) % 7]));
            return abMontag;
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
