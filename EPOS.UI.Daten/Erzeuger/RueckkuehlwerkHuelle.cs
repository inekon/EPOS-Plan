using System;
using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Erzeuger;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die plattformfreie Hülle der Verwaltung „Rückkühlwerke"</b> (K-F5a) — sie baut aus
    /// <see cref="RueckkuehlwerkStammCtrl"/> den Parametersatz des <see cref="RueckkuehlwerkDialog"/> und bildet zwischen
    /// <see cref="RueckkuehlwerkModel"/> und <see cref="RueckkuehlwerkDaten"/> ab. Unter Windows trägt sie das Fenster
    /// <c>RueckkuehlwerkFenster</c>, auf iOS <c>IosProjektQuelle.RueckkuehlwerkGaben</c> in die <c>AppWurzel</c>.
    ///
    /// <para><b>Die Regeln stehen im Kern</b> (Auslieferungssatz gesperrt, Name eindeutig, Löschen nur ohne Verwendung,
    /// Wertemengen und Bereiche mit benannter Meldung); die Hülle übersetzt nur. Die Wertemengen gehen als Persistenzwert
    /// samt Anzeigetext hinaus, die Vorgaben eines leeren Feldes rechnet der Rechenweg (<see cref="Rueckkuehlwerk.AusModell"/>).</para>
    /// </summary>
    public sealed class RueckkuehlwerkHuelle
    {
        /// <summary>Fensterbreite unter Windows.</summary>
        public const int FENSTER_BREITE = 1180;

        /// <summary>Fensterhöhe unter Windows.</summary>
        public const int FENSTER_HOEHE = 820;

        /// <summary>Der Fenstertitel.</summary>
        public static string Titel() => MyResource.Resource.RKW_TITEL;

        /// <summary>Hat einer der Schreibwege etwas geschrieben?</summary>
        public bool Geaendert { get; private set; }

        /// <summary>Der Parametersatz des Dialogs (ohne <c>Geschlossen</c>, das setzt der Wirt).</summary>
        public IReadOnlyDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                ["Katalogzeilen"] = new Func<IReadOnlyList<Katalogfilterzeile>>(RueckkuehlwerkStammCtrl.Katalogfilterzeilen),
                ["Katalogprofil"] = Katalogfilterprofil.FuerRueckkuehlwerk(Katalogtexte.Fuer),
                ["Bauarten"] = Bauarten(),
                ["Wege"] = Wahl(RueckkuehlwerkSchema.ANNAEHERUNG_WEGE, RueckkuehlwerkStammCtrl.WegText),
                ["Regelungen"] = Wahl(RueckkuehlwerkSchema.VENTILATOR_REGELUNGEN, RueckkuehlwerkStammCtrl.RegelungText),
                ["Schaltungen"] = Wahl(RueckkuehlwerkSchema.FREIKUEHLUNG_SCHALTUNGEN, RueckkuehlwerkStammCtrl.SchaltungText),
                ["Lies"] = new Func<int, RueckkuehlwerkDaten>(id => AlsDaten(RueckkuehlwerkStammCtrl.Laden(id))),
                ["Pruefen"] = new Func<RueckkuehlwerkDaten, string>(d => RueckkuehlwerkStammCtrl.Pruefen(AlsModell(d))),
                ["Speichern"] = new Func<RueckkuehlwerkDaten, RueckkuehlwerkSpeicherErgebnis>(Speichern),
                ["Loeschen"] = new Func<int, RueckkuehlwerkSpeicherErgebnis>(Loeschen),
                ["Duplizieren"] = new Func<int, string, RueckkuehlwerkSpeicherErgebnis>(Duplizieren),
                ["Schloss"] = Schlosswege.Aus((ids, gesperrt) =>
                {
                    Auslieferungskennzeichen.Ergebnis e = RueckkuehlwerkStammCtrl.SchlossSetzen(ids, gesperrt);
                    if (e != null && e.Ok) Geaendert = true;
                    return e;
                }),
                ["Verwendung"] = new Func<IReadOnlyDictionary<int, int>>(RueckkuehlwerkStammCtrl.KatalogVerwendung),
                ["Vorgaben"] = new Func<RueckkuehlwerkDaten, RueckkuehlwerkVorgaben>(Vorgaben),
            };
        }

        // =================================================================== Wertemengen

        /// <summary>Die Bauarten in Schemafolge — Persistenzwert, Anzeigetext und Einordnung (nass, befeuchtend).</summary>
        public static IReadOnlyList<RueckkuehlwerkBauart> Bauarten() =>
            RueckkuehlwerkSchema.BAUARTEN.Select(b =>
            {
                Rueckkuehlwerk r = Rueckkuehlwerk.AusModell(new RueckkuehlwerkModel { Bezeichner = b, Bauart = b });
                return new RueckkuehlwerkBauart(b, RueckkuehlwerkStammCtrl.BauartText(b), r.Verdunstungskuehlanlage, r.Befeuchtend);
            }).ToList();

        private static IReadOnlyList<RueckkuehlwerkWahl> Wahl(IReadOnlyList<string> menge, Func<string, string> text)
            => menge.Select(w => new RueckkuehlwerkWahl(w, text(w))).ToList();

        /// <summary>
        /// Die Vorgaben der leeren Felder für die Bauart des Arbeitsstands — die Rechenklasse aus einem Satz, der nur die
        /// Bauart trägt; ihre wirksamen Werte sind dann genau die Vorgaben.
        /// </summary>
        public static RueckkuehlwerkVorgaben Vorgaben(RueckkuehlwerkDaten d)
        {
            Rueckkuehlwerk r = Rueckkuehlwerk.AusModell(new RueckkuehlwerkModel { Bezeichner = "-", Bauart = Leer(d?.Bauart) });
            return new RueckkuehlwerkVorgaben(r.AnnaeherungK, r.Stufen, r.DrehzahlMin, r.BefeuchtungWirkungsgrad, r.BefeuchtungAbC,
                                              r.VerdunstungFaktor, r.Eindickung, r.DriftAnteil, r.Verdunstungskuehlanlage);
        }

        // =================================================================== Schreibwege

        private RueckkuehlwerkSpeicherErgebnis Speichern(RueckkuehlwerkDaten d)
        {
            if (d == null) return new RueckkuehlwerkSpeicherErgebnis(false, MyResource.Resource.RKW_MSG_NAME_LEER, 0);
            RueckkuehlwerkSpeicherErgebnis e = Abbild(RueckkuehlwerkStammCtrl.Speichern(AlsModell(d)));
            if (e.Ok) Geaendert = true;
            return e;
        }

        private RueckkuehlwerkSpeicherErgebnis Loeschen(int id)
        {
            RueckkuehlwerkSpeicherErgebnis e = Abbild(RueckkuehlwerkStammCtrl.Loeschen(id));
            if (e.Ok) Geaendert = true;
            return e;
        }

        private RueckkuehlwerkSpeicherErgebnis Duplizieren(int id, string name)
        {
            Katalogkopie.Ergebnis k = RueckkuehlwerkStammCtrl.Duplizieren(id, name);
            if (k == null) return new RueckkuehlwerkSpeicherErgebnis(false, "", 0);
            if (k.Ok) Geaendert = true;
            return new RueckkuehlwerkSpeicherErgebnis(k.Ok, k.Meldung ?? "", k.Id);
        }

        private static RueckkuehlwerkSpeicherErgebnis Abbild(RueckkuehlwerkStammCtrl.SpeicherErgebnis e)
            => e == null ? new RueckkuehlwerkSpeicherErgebnis(false, "", 0) : new RueckkuehlwerkSpeicherErgebnis(e.Ok, e.Meldung ?? "", e.Id);

        // =================================================================== Abbildung

        /// <summary>Das Kernmodell als DTO der Oberfläche; <c>null</c> bleibt <c>null</c>.</summary>
        public static RueckkuehlwerkDaten AlsDaten(RueckkuehlwerkModel m)
        {
            if (m == null) return null;
            return new RueckkuehlwerkDaten
            {
                Id = m.Id,
                Bezeichner = m.Bezeichner ?? "",
                Beschreibung = m.Beschreibung ?? "",
                Bauart = m.Bauart ?? "",
                NennleistungKw = m.Nennleistung_kW,
                AnnaeherungNennK = m.Annaeherung_Nenn_K,
                AnnaeherungWeg = m.Annaeherung_Weg ?? "",
                VentilatorNennKw = m.Ventilator_Nenn_kW,
                VentilatorRegelung = m.Ventilator_Regelung ?? "",
                VentilatorStufen = m.Ventilator_Stufen,
                VentilatorDrehzahlMin = m.Ventilator_Drehzahl_Min,
                BefeuchtungWirkungsgrad = m.Befeuchtung_Wirkungsgrad,
                BefeuchtungAbC = m.Befeuchtung_Ab_C,
                VerdunstungFaktor = m.Verdunstung_Faktor,
                Eindickung = m.Eindickung,
                DriftAnteil = m.Drift_Anteil,
                FreikuehlungSchaltung = m.Freikuehlung_Schaltung ?? "",
                Modulkosten = m.Modulkosten,
                Auslieferung = m.ReadOnly
            };
        }

        /// <summary>Das DTO als Kernmodell — leere Texte werden NULL (Vorgabe); der Gerätepreis reist unverändert mit.</summary>
        public static RueckkuehlwerkModel AlsModell(RueckkuehlwerkDaten d)
        {
            if (d == null) return null;
            return new RueckkuehlwerkModel
            {
                Id = d.Id,
                Bezeichner = d.Bezeichner ?? "",
                Beschreibung = Leer(d.Beschreibung),
                Bauart = Leer(d.Bauart),
                Nennleistung_kW = d.NennleistungKw,
                Annaeherung_Nenn_K = d.AnnaeherungNennK,
                Annaeherung_Weg = Leer(d.AnnaeherungWeg),
                Ventilator_Nenn_kW = d.VentilatorNennKw,
                Ventilator_Regelung = Leer(d.VentilatorRegelung),
                Ventilator_Stufen = d.VentilatorStufen,
                Ventilator_Drehzahl_Min = d.VentilatorDrehzahlMin,
                Befeuchtung_Wirkungsgrad = d.BefeuchtungWirkungsgrad,
                Befeuchtung_Ab_C = d.BefeuchtungAbC,
                Verdunstung_Faktor = d.VerdunstungFaktor,
                Eindickung = d.Eindickung,
                Drift_Anteil = d.DriftAnteil,
                Freikuehlung_Schaltung = Leer(d.FreikuehlungSchaltung),
                Modulkosten = d.Modulkosten,
                ReadOnly = d.Auslieferung
            };
        }

        private static string Leer(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }
}
