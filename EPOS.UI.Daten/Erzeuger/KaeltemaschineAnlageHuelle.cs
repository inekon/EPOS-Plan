using System;
using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Dialoge.Erzeuger;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Hülle des Erzeugerdialogs „Kältemaschinen im Projekt"</b> (KU3-4c) — sie baut aus
    /// <see cref="KaeltemaschineAnlageCtrl"/> den Parametersatz der Razor-Komponente
    /// <c>KaeltemaschineAnlageDialog</c> und bildet zwischen <see cref="KaeltemaschineAnlageModel"/> und
    /// <see cref="KaeltemaschineAnlageDaten"/> ab. Plattformfrei: Unter Windows reicht die Hauptfensterhülle den
    /// Satz an die <c>AppWurzel</c> (Seitenschlüssel <c>KAELTEMASCHINE_ANLAGE</c>), auf iOS die Projektquelle.
    ///
    /// <para><b>Anlegen</b> nimmt den Weg des Kerns (<see cref="KaeltemaschineAnlageCtrl.Anlegen"/>: Projektkopie
    /// über <c>AusKatalogUebernehmen</c> und Anlagenzeile Typ 13); <b>Speichern</b> liest die Anlage neu, setzt die
    /// Eingaben und schreibt Anlagenzeile und Kühleingaben der Kopie in einem Vorgang.</para>
    /// </summary>
    internal static class KaeltemaschineAnlageHuelle
    {
        /// <summary>Der Parametersatz für ein Projekt; <c>null</c> ohne Projekt.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben(int projektId)
        {
            if (projektId <= 0) return null;
            var traeger = Kaeltestromabrechnung.StromtraegerDesProjekts(projektId)
                .Select(t => (t.Key, t.Value)).ToList();
            return new Dictionary<string, object>
            {
                ["Anlagen"] = new Func<IReadOnlyList<KaeltemaschineAnlageDaten>>(() => Liste(projektId)),
                ["Katalogzeilen"] = new Func<IReadOnlyList<Katalogfilterzeile>>(KaeltemaschineStammCtrl.Katalogfilterzeilen),
                ["Katalogprofil"] = Katalogfilterprofil.Finde(Anlagenart.Kaeltemaschine, Katalogtexte.Fuer),
                ["Katalogwerte"] = new Func<int, KaeltemaschineGeraetwerte>(id => Werte(KaeltemaschineStammCtrl.Laden(id))),
                ["Stromtraeger"] = (IReadOnlyList<(int Id, string Text)>)traeger,
                ["ProjektStromtraeger"] = Kaeltestromabrechnung.Projekttraeger(projektId),
                ["Pruefen"] = new Func<KaeltemaschineAnlageDaten, string>(Pruefen),
                ["Anlegen"] = new Func<int, string, int>((stammId, name) => KaeltemaschineAnlageCtrl.Anlegen(projektId, stammId, name)),
                ["Speichern"] = new Func<KaeltemaschineAnlageDaten, string>(Speichern),
                ["Loeschen"] = new Action<int>(KaeltemaschineAnlageCtrl.Loeschen),
                // Block „Wärmepumpen im Kühlbetrieb“: Bestand und Schreibweg wie der Kühlschalter der
                // Wärmepumpen-Konfiguration (WaermepumpeGeraeteCtrl.KuehlbetriebUmschalten), geschrieben beim OK.
                ["Waermepumpen"] = new Func<IReadOnlyList<EPOS.UI.Seiten.Start.KuehlWaermepumpe>>(
                    () => KuehlungKachelBau.Waermepumpen(projektId)),
                ["KuehlbetriebSchreiben"] = new Func<int, bool, string>(
                    (idWp, an) => KuehlungKachelBau.KuehlbetriebSchreiben(projektId, idWp, an))
            };
        }

        /// <summary>Die Anlagen des Projekts samt Kennwerten ihrer Projektkopie.</summary>
        internal static IReadOnlyList<KaeltemaschineAnlageDaten> Liste(int projektId)
            => KaeltemaschineAnlageCtrl.Liste(projektId).Select(AlsDaten).ToList();

        /// <summary>Die Prüfregeln des Kerns (<see cref="KaeltemaschineAnlageCtrl.Pruefen"/>).</summary>
        internal static string Pruefen(KaeltemaschineAnlageDaten d)
            => KaeltemaschineAnlageCtrl.Pruefen(d == null ? null : AlsModell(d, null));

        /// <summary>Schreibt eine gespeicherte Anlage; liefert den Prüftext oder <c>null</c>.</summary>
        internal static string Speichern(KaeltemaschineAnlageDaten d)
        {
            KaeltemaschineAnlageModel gelesen = d == null ? null : KaeltemaschineAnlageCtrl.Laden(d.AnlagenId);
            if (gelesen == null) return MyResource.Resource.KM_ANLAGE_FEHLT;
            return KaeltemaschineAnlageCtrl.Speichern(AlsModell(d, gelesen));
        }

        /// <summary>Modell → Feldsatz samt Kennwerten der Projektkopie.</summary>
        internal static KaeltemaschineAnlageDaten AlsDaten(KaeltemaschineAnlageModel m)
        {
            if (m == null) return null;
            return new KaeltemaschineAnlageDaten
            {
                AnlagenId = m.AnlagenId,
                GeraetId = m.IdKaeltemaschine,
                Bezeichner = m.Bezeichner ?? "",
                Anzahl = m.Anzahl,
                KuehlVorlauf = m.KuehlVorlauf,
                KuehlHilfsstromanteil = m.KuehlHilfsstromanteil,
                KuehlCarrierId = m.KuehlIdCarrier,
                KuehlEigenerZaehler = m.KuehlEigenerZaehler == true,
                Geraet = m.IdKaeltemaschine.HasValue ? Werte(KaeltemaschineCtrl.Laden(m.IdKaeltemaschine.Value)) : null
            };
        }

        /// <summary>
        /// Feldsatz → Modell; Kennung, Projekt und Gerät kommen aus dem gelesenen Stand, sonst aus dem Feldsatz.
        /// Ein leeres Anzahlfeld wird 0 — die Prüfung lehnt es ab.
        /// </summary>
        internal static KaeltemaschineAnlageModel AlsModell(KaeltemaschineAnlageDaten d, KaeltemaschineAnlageModel gelesen)
        {
            return new KaeltemaschineAnlageModel
            {
                AnlagenId = gelesen?.AnlagenId ?? d.AnlagenId,
                IdProjekt = gelesen?.IdProjekt ?? 0,
                IdKaeltemaschine = gelesen != null ? gelesen.IdKaeltemaschine : d.GeraetId,
                Bezeichner = d.Bezeichner ?? "",
                Anzahl = d.Anzahl ?? 0,
                KuehlVorlauf = d.KuehlVorlauf,
                KuehlHilfsstromanteil = d.KuehlHilfsstromanteil,
                KuehlIdCarrier = d.KuehlCarrierId,
                KuehlEigenerZaehler = d.KuehlEigenerZaehler ? true : (bool?)null
            };
        }

        /// <summary>Die Kennwerte eines Geräts für die Anzeige; <c>null</c> bleibt <c>null</c>.</summary>
        internal static KaeltemaschineGeraetwerte Werte(KaeltemaschineModel m)
        {
            if (m == null) return null;
            return new KaeltemaschineGeraetwerte
            {
                Bezeichner = m.Bezeichner ?? "",
                Firma = m.Firma ?? "",
                Typ = m.Typ ?? "",
                Rueckkuehlart = m.Rueckkuehlart == null ? "" : KaeltemaschineStammCtrl.RueckkuehlartText(m.Rueckkuehlart),
                Nennkaelteleistung = m.Nennkaelteleistung_kW,
                NennEer = m.Nenn_EER,
                Mindestteillast = m.Mindestteillast_Prozent,
                KaltwasserMin = m.Kaltwasser_Vorlauf_Min,
                Kaltwasserstuetzstellen = (m.Kennlinie ?? new List<KaeltemaschineKenndatenModel>())
                    .Select(k => k.Kaltwassertemperatur).Distinct().OrderBy(t => t).ToList()
            };
        }
    }
}
