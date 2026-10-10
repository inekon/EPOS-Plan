using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Bausteine;
using EPOS.UI.Seiten.Simulation;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Hülle des Bereichs „Kälte“</b> der Simulationskonfiguration (Entwurf Kältebereich 3, Welle KB-A):
    /// baut aus dem Kernleser <see cref="Kaeltefolge.Lesen"/> das DTO <see cref="KaeltebereichDaten"/> — dieselbe Folge,
    /// in der der Lauf die Kälteerzeuger rechnet. Plattformfrei: Lesen und die Schreibwege (Projektschalter
    /// <c>KonfigurationCtrl.KuehlbetriebSetzen</c>, Kühlbetrieb der Wärmepumpe
    /// <c>WaermepumpeGeraeteCtrl.KuehlbetriebUmschalten</c>, Folge der Erzeuger <c>KaeltefolgeCtrl</c>, Welle KB-D) sind reine Kernwege und gelten auf beiden Plattformen
    /// gleich; eine Plattformnaht braucht der Bereich nicht.
    /// </summary>
    internal static class KaeltebereichBau
    {
        /// <summary>
        /// Der Bereich für ein Projekt; ohne Projekt der leere Bereich. <paramref name="speicherkachel"/> baut die
        /// Speicherkachel eines Kältespeichers wie in der Speicherspalte (<c>SimulationKonfigHuelle.SpeicherKarteDaten</c>);
        /// <c>null</c> = schlichte Kachel aus Name, Volumen und Temperaturpaar.
        /// </summary>
        internal static KaeltebereichDaten Daten(int idProjekt, Func<WaermesenkeClass.PufferInfo, SpeicherKachelDaten> speicherkachel = null)
        {
            var d = new KaeltebereichDaten();
            if (idProjekt <= 0) return d;

            KaeltefolgeStand stand = Kaeltefolge.Lesen(idProjekt);
            d.Kuehlbetrieb = stand.Kuehlbetrieb;
            d.Folge = stand.Stufen.Select(Stufe).ToList();

            var erzeuger = new List<KaelteerzeugerZeile>();
            int nummer = 0;
            foreach (KaelteerzeugerEintrag e in stand.Erzeuger)
            {
                KaelteerzeugerZeile z = Zeile(e, ++nummer);
                z.NachVornMoeglich = nummer > 1;
                z.NachHintenMoeglich = nummer < stand.Erzeuger.Count;
                erzeuger.Add(z);
            }
            d.Erzeuger = erzeuger;
            // KB-D: die Folge der Erzeuger ist pflegbar; die Herleitungszeile sagt, welche gilt.
            d.FolgeGepflegt = stand.Gepflegt;
            d.FolgeHinweis = stand.Gepflegt ? MyResource.Resource.KAELTEFOLGE_HINWEIS_GEPFLEGT
                                            : MyResource.Resource.KAELTEFOLGE_HINWEIS_VORGABE;

            d.Kaeltespeicher = stand.Kaeltespeicher
                .Select(p => speicherkachel != null ? speicherkachel(p) : SchlichteKachel(p))
                .ToList();

            d.FreieKuehlung = stand.FreieKuehlung
                .Select(f => new FreieKuehlungZeile(Stufe(f.Art), f.AnlagenId, f.Bezeichner, f.VorAllenErzeugern))
                .ToList();
            return d;
        }

        /// <summary>Die Stufe des Kerns als Aufzählung der Seite.</summary>
        internal static KaelteStufe Stufe(KaeltefolgeStufe s) => s switch
        {
            KaeltefolgeStufe.FreieKuehlung => KaelteStufe.FreieKuehlung,
            KaeltefolgeStufe.Kaeltespeicher => KaelteStufe.Kaeltespeicher,
            KaeltefolgeStufe.Waermepumpe => KaelteStufe.Waermepumpe,
            _ => KaelteStufe.Kaeltemaschine
        };

        /// <summary>Eine Erzeugerzeile samt Kachel; die Chips nehmen die Texte der Kältebahn des Schemas.</summary>
        private static KaelteerzeugerZeile Zeile(KaelteerzeugerEintrag e, int nummer)
        {
            bool maschine = e.Art == KaeltefolgeStufe.Kaeltemaschine;
            string rueckkuehlart = maschine ? KaeltemaschineStammCtrl.RueckkuehlartText(e.Rueckkuehlart) ?? "" : "";

            var chips = new List<ChipDaten>();
            chips.Add(new ChipDaten(maschine ? MyResource.Resource.KONF_KS_KAELTEMASCHINE : MyResource.Resource.KONF_KS_WP_KUEHLBETRIEB));
            if (e.NennleistungKw is double kw && kw > 0)
                chips.Add(new ChipDaten(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KONF_KS_ANZAHL_LEISTUNG,
                                                      Math.Max(1, e.Anzahl), kw)));
            if (e.KuehlVorlaufC is double vorlauf)
                chips.Add(new ChipDaten(string.Format(CultureInfo.CurrentCulture,
                    maschine ? MyResource.Resource.KONF_KS_KALTWASSERVORLAUF : MyResource.Resource.KONF_KS_KUEHLVORLAUF, vorlauf)));
            if (!string.IsNullOrEmpty(e.KuehltraegerName))
                chips.Add(new ChipDaten(e.KuehltraegerName));
            if (rueckkuehlart.Length > 0)
                chips.Add(new ChipDaten(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KONF_KS_RUECKKUEHLUNG, rueckkuehlart),
                                        ChipStil.Quelle));

            return new KaelteerzeugerZeile
            {
                Kachel = new ErzeugerKachelDaten
                {
                    Schluessel = (maschine ? "kaelte-km-" : "kaelte-wp-") + e.AnlagenId.ToString(CultureInfo.InvariantCulture),
                    Rang = nummer.ToString(CultureInfo.CurrentCulture),
                    Titel = e.Bezeichner ?? "",
                    Chips = chips,
                    // Die Wärmepumpe: aufgenommen = Kühlbetrieb an; die Kältemaschine ist immer aufgenommen.
                    Zustand = e.Kuehlbetrieb ? Kachelzustand.Aufgenommen : Kachelzustand.Verfuegbar,
                    Umschaltbar = !maschine,
                    Editierbar = true
                },
                Art = Stufe(e.Art),
                Nummer = nummer,
                IdAnlage = e.AnlagenId,
                IdGeraet = e.IdGeraet,
                Bezeichner = e.Bezeichner ?? "",
                Anzahl = e.Anzahl,
                NennleistungKw = e.NennleistungKw,
                Kuehlbetrieb = e.Kuehlbetrieb,
                Sperrgrund = e.Sperrgrund,
                InWaermekaskade = e.InWaermekaskade,
                KuehlVorlaufC = e.KuehlVorlaufC,
                Hilfsstromanteil = e.Hilfsstromanteil,
                KuehltraegerId = e.KuehltraegerId,
                KuehltraegerName = e.KuehltraegerName ?? "",
                EigenerZaehler = e.EigenerZaehler,
                FreieKuehlung = e.FreieKuehlung,
                Rueckkuehlart = rueckkuehlart,
                AnlagenJeKopie = e.AnlagenJeKopie,
                KaelteRang = e.KaelteRang
            };
        }

        /// <summary>Die schlichte Kachel eines Kältespeichers — nur ohne Speicherkachel des Wirts.</summary>
        private static SpeicherKachelDaten SchlichteKachel(WaermesenkeClass.PufferInfo p) => new SpeicherKachelDaten
        {
            IdPuffer = p.ID,
            Bezeichner = string.IsNullOrEmpty(p.Bezeichner) ? MyResource.Resource.PSP_BEZEICHNER_ERSATZ : p.Bezeichner,
            Verwendung = p.Verwendung ?? "",
            Volumen = p.Gesamtvolumen > 0
                ? string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PSP_KARTE_VOLUMEN, p.Gesamtvolumen) : "",
            Temperaturpaar = p.Vorlauf > 0 && p.Ruecklauf > 0
                ? string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIM_KARTE_TEMPERATURPAAR, p.Vorlauf, p.Ruecklauf) : ""
        };
    }
}
