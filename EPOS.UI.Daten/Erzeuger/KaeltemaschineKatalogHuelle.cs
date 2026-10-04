using System;
using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Erzeuger;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Hülle der Verwaltung „Kältemaschinen"</b> (KU3-1, Teil 2) — sie baut aus
    /// <see cref="KaeltemaschineStammCtrl"/> den Parametersatz der Razor-Komponente
    /// <c>KaeltemaschineKatalogDialog</c> und bildet zwischen <see cref="KaeltemaschineModel"/> und
    /// <see cref="KaeltemaschineDaten"/> ab. Plattformfrei: Unter Windows reicht die Hauptfensterhülle den
    /// Satz an die <c>AppWurzel</c> (Seitenschlüssel <c>KAELTEMASCHINE_KATALOG</c>), auf iOS die Projektquelle.
    ///
    /// <para><b>Die Rückkühlart</b> geht als Listenplatz in die Oberfläche und kommt als Persistenzwert aus
    /// <see cref="KaeltemaschineSchema.RUECKKUEHLARTEN"/> zurück; die Anzeigetexte liefert der Kern
    /// (<see cref="KaeltemaschineStammCtrl.RueckkuehlartText"/>) in derselben Reihenfolge.</para>
    /// </summary>
    internal static class KaeltemaschineKatalogHuelle
    {
        /// <summary>Der Parametersatz der Verwaltung — ein Satz je Betreten.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                ["Katalogzeilen"] = new Func<IReadOnlyList<Katalogfilterzeile>>(KaeltemaschineStammCtrl.Katalogfilterzeilen),
                ["Katalogprofil"] = Katalogfilterprofil.Finde(Anlagenart.Kaeltemaschine, Katalogtexte.Fuer),
                ["Rueckkuehlarten"] = Rueckkuehlarten(),
                ["Lies"] = new Func<int, KaeltemaschineDaten>(id => AlsDaten(KaeltemaschineStammCtrl.Laden(id))),
                ["Pruefen"] = new Func<KaeltemaschineDaten, string>(Pruefen),
                ["Speichern"] = new Func<KaeltemaschineDaten, KaeltemaschineSpeicherErgebnis>(Speichern),
                ["Loeschen"] = new Func<int, KaeltemaschineSpeicherErgebnis>(id => Abbild(KaeltemaschineStammCtrl.Loeschen(id))),
                ["Duplizieren"] = new Func<int, string, KaeltemaschineSpeicherErgebnis>(Duplizieren),
                ["Schloss"] = Schlosswege.Aus(KaeltemaschineStammCtrl.SchlossSetzen)
            };
        }

        /// <summary>Die Anzeigetexte der Rückkühlarten in der Reihenfolge der Persistenzwerte.</summary>
        internal static IReadOnlyList<string> Rueckkuehlarten()
            => KaeltemaschineSchema.RUECKKUEHLARTEN.Select(KaeltemaschineStammCtrl.RueckkuehlartText).ToList();

        /// <summary>
        /// Die Prüfung vor dem Schreiben: ein Kennlinienpunkt ohne Temperaturpaar, dann die Regeln des Kerns
        /// (<see cref="KaeltemaschineStammCtrl.Pruefen"/>, darunter das eindeutige Temperaturpaar).
        /// </summary>
        internal static string Pruefen(KaeltemaschineDaten d)
        {
            if (d != null && d.Kennlinie.Any(p => !p.Rueckkuehltemperatur.HasValue || !p.Kaltwassertemperatur.HasValue))
                return MyResource.Resource.KM_MSG_KENNLINIE_TEMPERATUR_LEER;
            return KaeltemaschineStammCtrl.Pruefen(AlsModell(d));
        }

        /// <summary>Schreibt Kopf und Kennlinie in EINEM Vorgang (Id 0 legt an).</summary>
        internal static KaeltemaschineSpeicherErgebnis Speichern(KaeltemaschineDaten d)
        {
            if (d == null) return new KaeltemaschineSpeicherErgebnis(false, MyResource.Resource.KM_MSG_NAME_LEER, 0);
            string grund = Pruefen(d);
            if (grund != null) return new KaeltemaschineSpeicherErgebnis(false, grund, d.Id);
            return Abbild(KaeltemaschineStammCtrl.Speichern(AlsModell(d)));
        }

        /// <summary>Kopiert einen Satz samt Kennlinie als eigenen Anwendersatz (ohne Schloss).</summary>
        internal static KaeltemaschineSpeicherErgebnis Duplizieren(int id, string name)
        {
            Katalogkopie.Ergebnis e = KaeltemaschineStammCtrl.Duplizieren(id, name);
            return e == null
                ? new KaeltemaschineSpeicherErgebnis(false, "", 0)
                : new KaeltemaschineSpeicherErgebnis(e.Ok, e.Meldung ?? "", e.Id);
        }

        /// <summary>Modell → Feldsatz; <c>null</c> bleibt <c>null</c>.</summary>
        internal static KaeltemaschineDaten AlsDaten(KaeltemaschineModel m)
        {
            if (m == null) return null;
            int platz = m.Rueckkuehlart == null ? -1 : IndexVon(m.Rueckkuehlart);
            return new KaeltemaschineDaten
            {
                Id = m.Id,
                Bezeichner = m.Bezeichner ?? "",
                Firma = m.Firma ?? "",
                Typ = m.Typ ?? "",
                Beschreibung = m.Beschreibung ?? "",
                Nennkaelteleistung = m.Nennkaelteleistung_kW,
                NennEer = m.Nenn_EER,
                Kaeltemittel = m.Kaeltemittel ?? "",
                RueckkuehlartIndex = platz >= 0 ? platz : null,
                Mindestteillast = m.Mindestteillast_Prozent,
                Hilfsstrom = m.Hilfsstrom_Rueckkuehlung_kW,
                KaltwasserMin = m.Kaltwasser_Vorlauf_Min,
                Modulkosten = m.Modulkosten,
                Auslieferung = m.ReadOnly,
                Kennlinie = (m.Kennlinie ?? new List<KaeltemaschineKenndatenModel>())
                    .Select(k => new KaeltemaschinePunktDaten
                    {
                        Rueckkuehltemperatur = k.Rueckkuehltemperatur,
                        Kaltwassertemperatur = k.Kaltwassertemperatur,
                        Eer = k.EER,
                        Kaelteleistung = k.Kaelteleistung_kW
                    }).ToList()
            };
        }

        /// <summary>
        /// Feldsatz → Modell. Ein Punkt ohne Temperatur wird hier nicht geschrieben — <see cref="Pruefen"/>
        /// lehnt ihn vorher ab. Ein unbekannter Listenplatz ergibt <c>null</c> (keine Angabe).
        /// </summary>
        internal static KaeltemaschineModel AlsModell(KaeltemaschineDaten d)
        {
            if (d == null) return null;
            int? platz = d.RueckkuehlartIndex;
            return new KaeltemaschineModel
            {
                Id = d.Id,
                Bezeichner = (d.Bezeichner ?? "").Trim(),
                Firma = Leer(d.Firma),
                Typ = Leer(d.Typ),
                Beschreibung = Leer(d.Beschreibung),
                Nennkaelteleistung_kW = d.Nennkaelteleistung,
                Nenn_EER = d.NennEer,
                Kaeltemittel = Leer(d.Kaeltemittel),
                Rueckkuehlart = platz is int p && p >= 0 && p < KaeltemaschineSchema.RUECKKUEHLARTEN.Count
                    ? KaeltemaschineSchema.RUECKKUEHLARTEN[p] : null,
                Mindestteillast_Prozent = d.Mindestteillast,
                Hilfsstrom_Rueckkuehlung_kW = d.Hilfsstrom,
                Kaltwasser_Vorlauf_Min = d.KaltwasserMin,
                Modulkosten = d.Modulkosten,
                ReadOnly = d.Auslieferung,
                Kennlinie = d.Kennlinie
                    .Where(p => p.Rueckkuehltemperatur.HasValue && p.Kaltwassertemperatur.HasValue)
                    .Select(p => new KaeltemaschineKenndatenModel
                    {
                        Rueckkuehltemperatur = p.Rueckkuehltemperatur.Value,
                        Kaltwassertemperatur = p.Kaltwassertemperatur.Value,
                        EER = p.Eer,
                        Kaelteleistung_kW = p.Kaelteleistung
                    }).ToList()
            };
        }

        private static int IndexVon(string persistenzwert)
        {
            for (int i = 0; i < KaeltemaschineSchema.RUECKKUEHLARTEN.Count; i++)
                if (KaeltemaschineSchema.RUECKKUEHLARTEN[i] == persistenzwert) return i;
            return -1;
        }

        private static KaeltemaschineSpeicherErgebnis Abbild(KaeltemaschineStammCtrl.SpeicherErgebnis e)
            => e == null ? new KaeltemaschineSpeicherErgebnis(false, "", 0)
                         : new KaeltemaschineSpeicherErgebnis(e.Ok, e.Meldung ?? "", e.Id);

        private static string Leer(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }
}
