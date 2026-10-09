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
                ["Schloss"] = Schlosswege.Aus(KaeltemaschineStammCtrl.SchlossSetzen),
                // KM1: die eingebauten Typkennfelder - auf beiden Plattformen (keine Dateiwahl noetig).
                ["TypkennfelderLaden"] = new Func<KaeltemaschineTypkennfelderErgebnis>(TypkennfelderLaden),
                ["TypkennfelderAnzahl"] = TypkennfelderAnzahl(),
                // KM3-E3-a: Teillast und Takten - Lesezeile, Kurve, Schnellwahlen und Auskunft aus dem Kern.
                ["Typkennfeldnamen"] = KaeltemaschineTeillastDialogrechnung.Typkennfeldnamen(),
                ["KurveAusTypkennfeld"] = new Func<string, KaeltemaschineTypkurve>(KurveAusTypkennfeld),
                ["Skalieren"] = new Func<string, double?, double?, KaeltemaschineSkalierErgebnis>(Skalieren),
                ["Auskunft"] = new Func<KaeltemaschineDaten, IReadOnlyList<KaeltemaschineAuskunftEingabe>, KaeltemaschineAuskunftErgebnis>(Auskunft),
                ["Lesezeile"] = new Func<KaeltemaschineDaten, KaeltemaschineTeillastLesestand>(Lesezeile),
                ["NennEerHinweis"] = new Func<KaeltemaschineDaten, string>(d => KaeltemaschineStammCtrl.NennEerHinweis(AlsModell(d))),
                ["Teillastbild"] = new Func<KaeltemaschineDaten, WindowsFormsApplication1.Zeichnung.Zeichenmodell>(
                    d => KaeltemaschineTeillastbild.Modell(AlsModell(d)))
            };
        }

        /// <summary>
        /// Die Gaben samt Importweg der Plattform (Windows: <c>KatalogImportHuelle.Gaben(KatalogImportArt.Kaeltemaschine)</c>).
        /// Ohne Weg (<see cref="Gaben()"/>) lehnt der Knopf „Import…" benannt ab.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(Func<IReadOnlyDictionary<string, object>> import)
        {
            var g = new Dictionary<string, object>(Gaben());
            if (import != null) g["ImportGaben"] = import;
            return g;
        }

        /// <summary>Lädt die eingebauten Typkennfelder (<see cref="KaeltemaschinenTypkennfelder.Einspielen"/>).</summary>
        internal static KaeltemaschineTypkennfelderErgebnis TypkennfelderLaden()
        {
            KaeltemaschinenTypkennfelder.Einspielergebnis e = KaeltemaschinenTypkennfelder.Einspielen();
            return new KaeltemaschineTypkennfelderErgebnis(e.Ok, e.Neu, e.Uebersprungen, e.Fehler ?? "");
        }

        /// <summary>Die Zahl der eingebauten Typkennfelder; 0, wenn die Ressource nicht lesbar ist.</summary>
        internal static int TypkennfelderAnzahl()
        {
            try { return KaeltemaschinenTypkennfelder.Lesen().Count; }
            catch (Exception) { return 0; }
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

        // =====================================================================
        //  Teillast und Takten (KM3-E3-a)
        // =====================================================================

        /// <summary>Die Lesezeile des Arbeitsstands (<see cref="KaeltemaschineTeillastDialogrechnung.Lesezeile"/>).</summary>
        internal static KaeltemaschineTeillastLesestand Lesezeile(KaeltemaschineDaten d)
        {
            KaeltemaschineTeillastDialogrechnung.Lesestand l = KaeltemaschineTeillastDialogrechnung.Lesezeile(AlsModell(d));
            return new KaeltemaschineTeillastLesestand(l.G25, l.G50, l.G75, l.Hinweis ?? "");
        }

        /// <summary>Die Teillastfelder eines Typkennfelds als Listenplätze; <c>null</c> bei unbekanntem Namen.</summary>
        internal static KaeltemaschineTypkurve KurveAusTypkennfeld(string name)
        {
            KaeltemaschineTeillastDialogrechnung.Typkurve k = KaeltemaschineTeillastDialogrechnung.KurveAusTypkennfeld(name);
            if (k == null) return null;
            return new KaeltemaschineTypkurve(k.Bezeichner, Platz(KaeltemaschineTeillastSchema.TEILLAST_WEGE, k.TeillastWeg),
                                              k.A, k.B, k.C, k.LastgradMin,
                                              Platz(KaeltemaschineTeillastSchema.VERDICHTERREGELUNGEN, k.Verdichterregelung));
        }

        /// <summary>„Typkennfeld auf Datenblatt skalieren…": der neue Satz als Feldsatz (Id 0, nicht gespeichert) oder der Grund.</summary>
        internal static KaeltemaschineSkalierErgebnis Skalieren(string name, double? nennleistungKw, double? nennEer)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new KaeltemaschineSkalierErgebnis(null, MyResource.Resource.KM_MSG_TYPKENNFELD_WAEHLEN);
            KaeltemaschineTeillastDialogrechnung.Skalierergebnis e = KaeltemaschineTeillastDialogrechnung.AufDatenblattSkalieren(
                name, nennleistungKw ?? double.NaN, nennEer ?? double.NaN);
            return e.Ok ? new KaeltemaschineSkalierErgebnis(AlsDaten(e.Satz), "")
                        : new KaeltemaschineSkalierErgebnis(null, e.Meldung ?? "");
        }

        /// <summary>
        /// „Teillastpunkte prüfen…": die Paare mit beiden Werten (Lastgrad in %), gerechnet am Arbeitsstand; ohne
        /// vollständiges Paar die benannte Ablehnung des Kerns. Nichts wird gespeichert.
        /// </summary>
        internal static KaeltemaschineAuskunftErgebnis Auskunft(KaeltemaschineDaten d, IReadOnlyList<KaeltemaschineAuskunftEingabe> eingaben)
        {
            var punkte = (eingaben ?? Array.Empty<KaeltemaschineAuskunftEingabe>())
                .Where(e => e != null && (e.AussenC.HasValue || e.LastgradProzent.HasValue))
                .Select(e => new KaeltemaschineTeillastDialogrechnung.Auskunftspunkt(e.Name ?? "", e.AussenC ?? double.NaN,
                                                                                    (e.LastgradProzent ?? double.NaN) / 100.0))
                .ToList();
            KaeltemaschineTeillastDialogrechnung.Auskunftsergebnis r =
                KaeltemaschineTeillastDialogrechnung.TeillastpunkteAuskunft(AlsModell(d), punkte);
            if (!r.Ok) return new KaeltemaschineAuskunftErgebnis(null, r.Meldung ?? "");
            return new KaeltemaschineAuskunftErgebnis(r.Zeilen.Select(z => new KaeltemaschineAuskunftZeile(
                z.Name, z.AussenC, z.RueckkuehlC, z.KaelteKw, z.LastgradMaschine, z.LeistungsaufnahmeKw, z.Eer, z.Takt,
                z.Randwert)).ToList(), "");
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
                TeillastWegIndex = Platz(KaeltemaschineTeillastSchema.TEILLAST_WEGE, m.Teillast_Weg),
                KurveA = m.Teillastkurve_a,
                KurveB = m.Teillastkurve_b,
                KurveC = m.Teillastkurve_c,
                KurveLastgradMin = m.Teillastkurve_Lastgrad_Min,
                Cd = m.Taktverlustfaktor_Cd,
                VerdichterregelungIndex = Platz(KaeltemaschineTeillastSchema.VERDICHTERREGELUNGEN, m.Verdichterregelung),
                RandwegIndex = Platz(KaeltemaschineTeillastSchema.RANDWEGE, m.Kennfeld_Randweg),
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
                Teillast_Weg = Wert(KaeltemaschineTeillastSchema.TEILLAST_WEGE, d.TeillastWegIndex),
                Teillastkurve_a = d.KurveA,
                Teillastkurve_b = d.KurveB,
                Teillastkurve_c = d.KurveC,
                Teillastkurve_Lastgrad_Min = d.KurveLastgradMin,
                Taktverlustfaktor_Cd = d.Cd,
                Verdichterregelung = Wert(KaeltemaschineTeillastSchema.VERDICHTERREGELUNGEN, d.VerdichterregelungIndex),
                Kennfeld_Randweg = Wert(KaeltemaschineTeillastSchema.RANDWEGE, d.RandwegIndex),
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

        /// <summary>Persistenzwert → Listenplatz; leer oder unbekannt = <c>null</c>.</summary>
        internal static int? Platz(IReadOnlyList<string> liste, string wert)
        {
            if (string.IsNullOrWhiteSpace(wert)) return null;
            for (int i = 0; i < liste.Count; i++)
                if (string.Equals(liste[i], wert.Trim(), StringComparison.OrdinalIgnoreCase)) return i;
            return null;
        }

        /// <summary>Listenplatz → Persistenzwert; <c>null</c> oder außerhalb der Liste = <c>null</c>.</summary>
        internal static string Wert(IReadOnlyList<string> liste, int? platz)
            => platz is int p && p >= 0 && p < liste.Count ? liste[p] : null;

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
