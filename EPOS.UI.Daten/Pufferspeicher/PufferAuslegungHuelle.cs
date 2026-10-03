using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten;
using EPOS.UI.Seiten.Pufferspeicher;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der ARBEITSGANG einer Pufferspeicher-Auslegung: Projekt, Zielpuffer, Einstieg und — vom
    /// Zapfprofil — das übergebene Ergebnis samt vorgewählter Klassen. Wer die Ansicht öffnet,
    /// meldet ihn an (<see cref="PufferAuslegungHuelle.Anmelden"/>); die Wurzel holt ihn beim
    /// Betreten ab.
    /// </summary>
    internal sealed class PufferAuslegungAuftrag
    {
        internal int IdProjekt;

        /// <summary>Der Projektpuffer; <c>null</c> = die Übernahme legt einen neuen an.</summary>
        internal int? IdPuffer;

        /// <summary>Woher die Ansicht kam — eine Zeile im Kopf; leer = keine.</summary>
        internal string Einstieg = "";

        /// <summary>Das übergebene Zapfprofil-Ergebnis; <c>null</c> = das gespeicherte des Projekts.</summary>
        internal PufferZapfprofil Zapfprofil;

        /// <summary>Die Herkunft des übergebenen Ergebnisses (Klartext des Kerns).</summary>
        internal string ZapfprofilText = "";

        /// <summary>Vorgewählte Klassen (Zapfprofil: {B} bzw. {H,B}); <c>null</c> = aus dem Puffer.</summary>
        internal bool? KlasseHeizung, KlasseBrauchwasser, KlasseProzess;

        /// <summary>Was nach einer Übernahme nachzuziehen ist (Simulation: Ergebnis veraltet).</summary>
        internal Action Nachzug;
    }

    /// <summary>
    /// Die NAHT der Pufferspeicher-Auslegung für Wege, die nur eine Plattform kann (Muster
    /// <see cref="Katalogwege"/>): Windows öffnet die Ansicht auf Wunsch in einem EIGENEN FENSTER —
    /// der Weg aus dem Zapfprofil, das selbst in einem Fenster steht. Ohne eingehängten Weg (iOS)
    /// lehnt die Übergabe benannt ab; die Auslegung bleibt über ① Konfiguration erreichbar.
    /// </summary>
    internal static class Pufferauslegungswege
    {
        /// <summary>Zeigt den Parametersatz der Ansicht modal in einem eigenen Fenster; <c>null</c> = keines.</summary>
        internal static Func<IReadOnlyDictionary<string, object>, Task> Fenster;
    }

    /// <summary>
    /// Die HÜLLE der Pufferspeicher-Auslegung (Konzept Pufferspeicher-Auslegung, Abschnitt 6, Stufe
    /// P2): baut aus <see cref="PufferAuslegungCtrl"/> den Parametersatz von
    /// <c>PufferAuslegungSeite</c> — Startstand mit Herkunft je Feld, Vorlagen, Bedarfsreihen als
    /// Kennzahlen (oder ihren Fehlertext, benannt), das erste Ergebnis — und die drei Wege
    /// Rechnen, Speichern, Übernehmen. Plattformfrei; die Reihen holt sie EINMAL je Arbeitsgang,
    /// jedes Rechnen danach ist datenbankfrei.
    /// </summary>
    internal sealed class PufferAuslegungHuelle
    {
        /// <summary>Der Hilfeschlüssel der Ansicht (help_mapping.txt).</summary>
        internal const string HILFE = "Form_PufferAuslegung.btn_Help";

        private static PufferAuslegungAuftrag _angemeldet;

        private readonly PufferAuslegungAuftrag _auftrag;
        private readonly PufferAuslegungReihen _reihen;
        private readonly PufferAuslegungVorbelegung _vorbelegung;
        private readonly string _fehler = "";
        private int? _idPuffer;

        // =================================================================
        //  Einstieg
        // =================================================================

        /// <summary>Meldet den Arbeitsgang an, den die Wurzel beim nächsten Betreten abholt.</summary>
        internal static void Anmelden(PufferAuslegungAuftrag auftrag) => _angemeldet = auftrag;

        /// <summary>
        /// Der Parametersatz der Ansicht beim BETRETEN (Wurzel, beide Plattformen): der angemeldete
        /// Arbeitsgang, sonst ein neuer für das Projekt der Wurzel (Hilfe-Assistent, Rückweg).
        /// <c>null</c> = kein Projekt — die Wurzel nennt dann den Grund.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> AnsichtGaben(int idProjekt)
        {
            PufferAuslegungAuftrag a = _angemeldet;
            _angemeldet = null;
            if (a == null || a.IdProjekt <= 0) a = new PufferAuslegungAuftrag { IdProjekt = idProjekt };
            if (a.IdProjekt <= 0) return null;
            return new PufferAuslegungHuelle(a).Gaben();
        }

        /// <summary>
        /// Wechselt die Wurzel auf die Ansicht (Konfiguration, Pufferverwaltung, Kachel). Ohne
        /// angemeldete Wurzel geschieht nichts — <c>false</c>, der Arbeitsgang verfällt.
        /// </summary>
        internal static bool Oeffnen(PufferAuslegungAuftrag auftrag)
        {
            if (auftrag == null || auftrag.IdProjekt <= 0) return false;
            Anmelden(auftrag);
            INavigationsZiel ziel = Navigationsziel.Aktuell;
            if (ziel != null && ziel.OeffneMaske(Seitenschluessel.PufferAuslegung)) return true;
            _angemeldet = null;
            return false;
        }

        /// <summary>
        /// Öffnet die Ansicht in einem eigenen Fenster (Naht <see cref="Pufferauslegungswege.Fenster"/>).
        /// Liefert <c>null</c>, wenn sie offen war, sonst den Grund, warum diese Plattform es nicht kann.
        /// </summary>
        internal static async Task<string> ImFensterOeffnen(PufferAuslegungAuftrag auftrag)
        {
            Func<IReadOnlyDictionary<string, object>, Task> weg = Pufferauslegungswege.Fenster;
            if (weg == null) return MyResource.Resource.PAUS_UEBERGABE_NICHT_MOEGLICH;
            await weg(new PufferAuslegungHuelle(auftrag).Gaben());
            return null;
        }

        /// <summary>
        /// Der Arbeitsgang für die Übergabe aus dem Zapfprofil (Konzept 6): Zielpuffer ist der
        /// Brauchwasserpuffer des Projekts; ohne ihn ein Heizungspuffer, wenn das Trinkwasser über
        /// eine Station am Puffer entsteht (Kombi {H,B}); sonst ein neuer Speicher {B}.
        /// </summary>
        internal static PufferAuslegungAuftrag AusZapfprofil(int idProjekt, PufferZapfprofil zp, string text)
        {
            var a = new PufferAuslegungAuftrag
            {
                IdProjekt = idProjekt,
                Zapfprofil = zp,
                ZapfprofilText = text ?? "",
                Einstieg = MyResource.Resource.PAUS_EINSTIEG_ZAPFPROFIL,
                KlasseBrauchwasser = true
            };
            Dictionary<int, PufferSpCtrl.KlassenSet> sets = PufferSpCtrl.KlassenSetsJeProjekt(idProjekt);
            foreach (KeyValuePair<int, PufferSpCtrl.KlassenSet> s in sets.OrderBy(s => s.Key))
                if (s.Value.Brauchwasser)
                {
                    a.IdPuffer = s.Key;
                    return a;
                }
            bool station = zp != null && zp.Topologie != PufferBwTopologie.Speicher;
            if (station)
                foreach (KeyValuePair<int, PufferSpCtrl.KlassenSet> s in sets.OrderBy(s => s.Key))
                    if (s.Value.Heizung)
                    {
                        a.IdPuffer = s.Key;
                        a.KlasseHeizung = true;
                        return a;
                    }
            a.KlasseHeizung = false;
            a.KlasseProzess = false;
            return a;
        }

        // =================================================================
        //  Aufbau
        // =================================================================

        internal PufferAuslegungHuelle(PufferAuslegungAuftrag auftrag)
        {
            _auftrag = auftrag ?? throw new ArgumentNullException(nameof(auftrag));
            _idPuffer = auftrag.IdPuffer;
            try
            {
                _reihen = PufferAuslegungCtrl.Reihen(auftrag.IdProjekt);
            }
            catch (Exception ex)
            {
                // Benannt weitergereicht, nie still: Die Ansicht zeigt den Grund in Schritt 2.
                _reihen = new PufferAuslegungReihen(null, null, null, ex.Message);
            }
            try
            {
                PufferAuslegungVorbelegung v = PufferAuslegungCtrl.Vorbelegen(auftrag.IdProjekt, auftrag.IdPuffer, _reihen);
                _vorbelegung = MitAuftrag(v, auftrag);
            }
            catch (Exception ex)
            {
                _fehler = ex.Message;
            }
        }

        /// <summary>Legt das übergebene Zapfprofil und die vorgewählten Klassen über die Vorbelegung.</summary>
        private static PufferAuslegungVorbelegung MitAuftrag(PufferAuslegungVorbelegung v, PufferAuslegungAuftrag a)
        {
            PufferAuslegungEingang e = v.Eingang;
            var h = new List<PufferAuslegungHerkunft>(v.Herkunft);
            if (a.KlasseHeizung.HasValue || a.KlasseBrauchwasser.HasValue || a.KlasseProzess.HasValue)
            {
                e = e with
                {
                    KlasseHeizung = a.KlasseHeizung ?? e.KlasseHeizung,
                    KlasseBrauchwasser = a.KlasseBrauchwasser ?? e.KlasseBrauchwasser,
                    KlasseProzess = a.KlasseProzess ?? e.KlasseProzess
                };
                h.Add(new PufferAuslegungHerkunft("Klassen", QUELLE_UEBERGEBEN, a.Einstieg));
            }
            if (a.Zapfprofil != null)
            {
                e = e with { Zapfprofil = a.Zapfprofil };
                h.Add(new PufferAuslegungHerkunft(nameof(PufferAuslegungEingang.Zapfprofil), QUELLE_UEBERGEBEN,
                                                  Format(MyResource.Resource.PAUS_ZAPF_UEBERGEBEN, a.ZapfprofilText)));
            }
            return v with { Eingang = e, Herkunft = h.AsReadOnly() };
        }

        /// <summary>Die Quelle „übergeben" — nur in dieser Hülle (Zapfprofil-Übergabe).</summary>
        private const string QUELLE_UEBERGEBEN = "Übergeben";

        /// <summary>Der Parametersatz von <c>PufferAuslegungSeite</c> — ohne <c>Geschlossen</c>.</summary>
        internal IReadOnlyDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                ["Daten"] = Start(),
                ["Dienste"] = new PufferAuslegungDienste(Rechnen, Speichern, Uebernehmen, ProbelaufImHintergrund),
                ["Texte"] = new PufferAuslegungTexte(),
                ["HilfeSchluessel"] = HILFE
            };
        }

        // =================================================================
        //  Startstand
        // =================================================================

        internal PufferAuslegungStartDaten Start()
        {
            var d = new PufferAuslegungStartDaten
            {
                IdProjekt = _auftrag.IdProjekt,
                IdPuffer = _idPuffer,
                Projektname = Projektname(_auftrag.IdProjekt),
                PufferBezeichner = _idPuffer.HasValue ? Puffername(_idPuffer.Value) : "",
                Einstieg = _auftrag.Einstieg ?? "",
                BezeichnerMuster = MyResource.Resource.PAUS_BEZEICHNER_MUSTER
            };
            if (_vorbelegung == null)
            {
                d.Fehler = Format(MyResource.Resource.PAUS_FEHLER_START, _fehler);
                return d;
            }

            PufferAuslegungEingang e = _vorbelegung.Eingang;
            PufferAuslegungParameter p = e.Parameter ?? PufferAuslegungParameter.Vorgabe();
            d.Eingabe = EingabeAus(e);
            d.Herkunft = _vorbelegung.Herkunft
                .Select(z => new PufferHerkunftDaten(z.Feld, z.Quelle, Marke(z.Quelle), Textbaustein.Aufloesen(z.Baustein))).ToList();
            d.Gespeichert = _vorbelegung.Gespeichert;

            PufferNutzungsprofilAbleitung np = _vorbelegung.Nutzungsprofil;
            if (np != null)
            {
                d.Nutzungsprofil = PufferAuslegungTexte.Nach("PAUS_NP_", np.Profil.ToString(), np.Profil.ToString());
                d.NutzungsprofilHerkunft = Textbaustein.Aufloesen(np.HerkunftBaustein);
                d.NutzungsprofilVorgabe = np.Vorgabe;
            }

            d.Vorlagen = PufferAuslegungCtrl.Vorlagen().Select(Vorlage).ToList();
            d.Uebergabearten = new[]
            {
                new PufferWahlDaten("", MyResource.Resource.PAUS_UEBERGABE_IDEAL),
                new PufferWahlDaten("RADIATOR", MyResource.Resource.PAUS_UEBERGABE_RADIATOR),
                new PufferWahlDaten("FLAECHE", MyResource.Resource.PAUS_UEBERGABE_FLAECHE),
                new PufferWahlDaten("KONVEKTOR", MyResource.Resource.PAUS_UEBERGABE_KONVEKTOR),
                new PufferWahlDaten("LUEFTER", MyResource.Resource.PAUS_UEBERGABE_LUEFTER)
            };
            d.Zirkulationswege = new[]
            {
                new PufferWahlDaten("", MyResource.Resource.PAUS_ZIRK_AUTOMATIK),
                new PufferWahlDaten(nameof(PufferZirkulationWeg.ZAPFPROFIL), MyResource.Resource.PAUS_ZIRK_ZAPFPROFIL),
                new PufferWahlDaten(nameof(PufferZirkulationWeg.PROJEKT), MyResource.Resource.PAUS_ZIRK_PROJEKT),
                new PufferWahlDaten(nameof(PufferZirkulationWeg.ANTEIL), MyResource.Resource.PAUS_ZIRK_ANTEIL),
                new PufferWahlDaten(nameof(PufferZirkulationWeg.JE_WE), MyResource.Resource.PAUS_ZIRK_JE_WE)
            };

            if (_reihen != null && _reihen.Vorhanden)
                d.Reihen = new[]
                {
                    Reihe(MyResource.Resource.KANAL_HEIZUNG_ANZEIGE, _reihen.Heizung),
                    Reihe(MyResource.Resource.KANAL_BRAUCHWASSER_ANZEIGE, _reihen.Brauchwasser),
                    Reihe(MyResource.Resource.KANAL_PROZESS_ANZEIGE, _reihen.Prozess)
                };
            else
                d.ReihenFehler = _reihen?.Fehlertext ?? "";

            PufferErzeuger erz = e.Erzeuger ?? new PufferErzeuger();
            d.IstWaermepumpe = erz.IstWaermepumpe;
            d.Erzeuger = _vorbelegung.Herkunft
                .Where(z => z.Feld == nameof(PufferAuslegungEingang.Erzeuger) && z.Quelle != PufferHerkunftsquelle.GESPEICHERT)
                .Where(z => z.Baustein?.Schluessel != PufferAuslegungCtrl.SCHLUESSEL_KEIN_ERZEUGER)
                .Select(z => Textbaustein.Aufloesen(z.Baustein)).Where(t => t.Length > 0)
                .ToList();
            if (erz.NennleistungKw <= 0 && erz.KollektorflaecheM2 <= 0) d.Erzeuger = Array.Empty<string>();

            d.VorlaufC = e.VorlaufC;
            d.RuecklaufC = e.RuecklaufC;
            d.SchwelleEin = e.SchwelleEin ?? p.WertOder(PufferAuslegungVorgaben.SCHWELLE_EIN, ProjektPuffer.SCHWELLE_EIN_DEFAULT / 100.0);
            d.SchwelleAus = e.SchwelleAus ?? p.WertOder(PufferAuslegungVorgaben.SCHWELLE_AUS, ProjektPuffer.SCHWELLE_AUS_DEFAULT / 100.0);

            PufferZapfprofil zp = e.Zapfprofil;
            if (zp != null)
            {
                d.ZapfVorhanden = true;
                d.ZapfTopologie = zp.Topologie.ToString();
                d.ZapfDmaxKwh = zp.DmaxKwh > 0 ? zp.DmaxKwh : (double?)null;
                d.ZapfPersonen = zp.Personen;
                d.ZapfTagesbedarfL = zp.TagesbedarfL;
            }

            d.Ergebnis = Rechnen(d.Eingabe);
            PufferProbelaufErgebnis letzter = PufferProbelaufCtrl.Letzter(_auftrag.IdProjekt, _idPuffer);
            if (letzter != null) d.Probelauf = Abbilden(letzter, AuslegungStartsJeTag(d.Ergebnis));
            return d;
        }

        private static PufferReiheDaten Reihe(string kanal, double[] werte)
        {
            if (werte == null || werte.Length == 0) return new PufferReiheDaten(kanal, 0, 0);
            return new PufferReiheDaten(kanal, werte.Max(), werte.Sum());
        }

        private static PufferVorlageDaten Vorlage(PufferVorlagenbeschreibung v)
        {
            string typ = v.Vorlage.ToString();
            var kriterien = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (string k in PufferAuslegungVorgaben.VORLAGE_SCHALTER)
                kriterien[k] = v.Kriterien.TryGetValue(k, out bool an) && an;
            string faust = v.Faustwert > 0
                ? Format(MyResource.Resource.PAUS_FAUSTWERT, v.Faustwert.ToString("0.##", CultureInfo.CurrentCulture), v.FaustwertEinheit)
                : "";
            return new PufferVorlageDaten(typ,
                PufferAuslegungTexte.Nach("PAUS_VORLAGE_", typ, typ),
                PufferAuslegungTexte.Nach("PAUS_VORLAGE_", typ + "_UNTER", ""),
                PufferAuslegungTexte.Nach("PAUS_VORLAGE_", typ + "_TEXT", ""),
                kriterien, faust);
        }

        /// <summary>Die übersetzte Marke einer Herkunftsquelle des Kerns.</summary>
        private static string Marke(string quelle)
        {
            switch (quelle)
            {
                case PufferHerkunftsquelle.PROJEKT: return MyResource.Resource.PAUS_QUELLE_PROJEKT;
                case PufferHerkunftsquelle.PUFFER: return MyResource.Resource.PAUS_QUELLE_PUFFER;
                case PufferHerkunftsquelle.KASKADE: return MyResource.Resource.PAUS_QUELLE_KASKADE;
                case PufferHerkunftsquelle.GEBAEUDE: return MyResource.Resource.PAUS_QUELLE_GEBAEUDE;
                case PufferHerkunftsquelle.ZAPFPROFIL: return MyResource.Resource.PAUS_QUELLE_ZAPFPROFIL;
                case PufferHerkunftsquelle.REIHE: return MyResource.Resource.PAUS_QUELLE_REIHE;
                case PufferHerkunftsquelle.KATALOG: return MyResource.Resource.PAUS_QUELLE_KATALOG;
                case PufferHerkunftsquelle.TEILLAST: return MyResource.Resource.PAUS_QUELLE_TEILLAST;
                case PufferHerkunftsquelle.PARAMETER: return MyResource.Resource.PAUS_QUELLE_PARAMETER;
                case PufferHerkunftsquelle.GESPEICHERT: return MyResource.Resource.PAUS_QUELLE_GESPEICHERT;
                case QUELLE_UEBERGEBEN: return MyResource.Resource.PAUS_QUELLE_UEBERGEBEN;
                default: return MyResource.Resource.PAUS_QUELLE_VORGABE;
            }
        }

        // =================================================================
        //  Arbeitsstand <-> Eingang des Kerns
        // =================================================================

        /// <summary>Der Arbeitsstand der Ansicht aus dem Eingang des Kerns.</summary>
        internal static PufferAuslegungEingabeDaten EingabeAus(PufferAuslegungEingang e)
        {
            PufferErzeuger erz = e.Erzeuger ?? new PufferErzeuger();
            var (profil, beginn, dauer) = PufferAuslegungCtrl.SperrprofilAus(e.Sperrfenster);
            return new PufferAuslegungEingabeDaten
            {
                KlasseHeizung = e.KlasseHeizung,
                KlasseBrauchwasser = e.KlasseBrauchwasser,
                KlasseProzess = e.KlasseProzess,
                Vorlage = e.Vorlage.ToString(),
                WpGeregelt = erz.Geregelt,
                ZweiterzeugerFrei = erz.ZweiterzeugerFrei,
                Uebergabeart = e.Uebergabeart ?? "",
                HeizgrenzeC = e.HeizgrenzeC,
                AuslegungsheizlastKw = e.AuslegungsheizlastKw,
                AnlagenvolumenL = e.AnlagenvolumenL,
                Sperrprofil = profil,
                SperrbeginnH = beginn,
                SperrdauerH = dauer,
                SperrzeitExpertenweg = e.SperrzeitExpertenweg,
                MindestlaufzeitMin = e.MindestlaufzeitMin,
                MindestleistungKw = erz.MindestleistungKw,
                StartzielJeTag = e.StartzielJeTag,
                DeckungszielProzent = e.Deckungsziel.HasValue ? e.Deckungsziel.Value * 100.0 : (double?)null,
                DeltaTBK = e.DeltaTBK,
                TPufferObenC = e.TPufferObenC,
                ZirkulationWeg = e.ZirkulationWeg?.ToString() ?? "",
                Wohneinheiten = e.Wohneinheiten
            };
        }

        /// <summary>
        /// Der Eingang des Kerns aus dem Arbeitsstand: die Vorbelegung (Reihen, Erzeuger, Puffer,
        /// Zapfprofil, Katalog) mit den Eingaben der Ansicht überlagert. Abweichende
        /// Kriterienschalter gehen als Überschreibung der Vorlagenschalter in den Parametersatz.
        /// Ein unbekanntes Sperrprofil wird benannt abgelehnt (<see cref="ArgumentException"/>).
        /// </summary>
        internal PufferAuslegungEingang EingangAus(PufferAuslegungEingabeDaten d)
        {
            if (_vorbelegung == null) throw new InvalidOperationException(_fehler);
            if (d == null) throw new ArgumentNullException(nameof(d));
            PufferAuslegungEingang b = _vorbelegung.Eingang;
            PufferVorlage vorlage = Enum.TryParse(d.Vorlage, out PufferVorlage v) ? v : b.Vorlage;
            PufferErzeuger erz = (b.Erzeuger ?? new PufferErzeuger()) with
            {
                Geregelt = d.WpGeregelt,
                ZweiterzeugerFrei = d.ZweiterzeugerFrei,
                MindestleistungKw = d.MindestleistungKw
            };
            PufferZirkulationWeg? zw = !string.IsNullOrEmpty(d.ZirkulationWeg) && Enum.TryParse(d.ZirkulationWeg, out PufferZirkulationWeg z)
                ? z : (PufferZirkulationWeg?)null;
            return b with
            {
                KlasseHeizung = d.KlasseHeizung,
                KlasseBrauchwasser = d.KlasseBrauchwasser,
                KlasseProzess = d.KlasseProzess,
                Vorlage = vorlage,
                Erzeuger = erz,
                Uebergabeart = string.IsNullOrEmpty(d.Uebergabeart) ? null : d.Uebergabeart,
                HeizgrenzeC = d.HeizgrenzeC,
                AuslegungsheizlastKw = d.AuslegungsheizlastKw,
                AnlagenvolumenL = d.AnlagenvolumenL,
                Sperrfenster = PufferSperrprofil.Fenster(string.IsNullOrEmpty(d.Sperrprofil) ? "KEINE" : d.Sperrprofil,
                                                         d.SperrbeginnH, d.SperrdauerH),
                SperrzeitExpertenweg = d.SperrzeitExpertenweg,
                MindestlaufzeitMin = d.MindestlaufzeitMin,
                StartzielJeTag = d.StartzielJeTag,
                Deckungsziel = d.DeckungszielProzent.HasValue ? d.DeckungszielProzent.Value / 100.0 : (double?)null,
                DeltaTBK = d.DeltaTBK,
                TPufferObenC = d.TPufferObenC,
                ZirkulationWeg = zw,
                Wohneinheiten = d.Wohneinheiten,
                Parameter = ParameterMit(b.Parameter, vorlage, d.Kriterien)
            };
        }

        /// <summary>Der Parametersatz mit den abweichenden Kriterienschaltern der Vorlage.</summary>
        private static PufferAuslegungParameter ParameterMit(PufferAuslegungParameter p, PufferVorlage vorlage,
                                                             IReadOnlyDictionary<string, bool> kriterien)
        {
            p ??= PufferAuslegungParameter.Vorgabe();
            if (kriterien == null || kriterien.Count == 0) return p;
            var werte = new Dictionary<string, double>(p.Werte, StringComparer.Ordinal);
            foreach (KeyValuePair<string, bool> k in kriterien)
                if (PufferAuslegungVorgaben.VORLAGE_SCHALTER.Contains(k.Key))
                    werte[PufferAuslegungVorgaben.VorlageSchluessel(vorlage.ToString(), k.Key)] = k.Value ? 1 : 0;
            return PufferAuslegungParameter.Mit(werte);
        }

        // =================================================================
        //  Die drei Wege
        // =================================================================

        /// <summary>Rechnet den Arbeitsstand; ohne Reihen oder bei unzulässigem Eingang benannt abgelehnt.</summary>
        internal PufferAuslegungErgebnisDaten Rechnen(PufferAuslegungEingabeDaten d)
        {
            if (_vorbelegung == null)
                return PufferAuslegungErgebnisDaten.MitFehler(Format(MyResource.Resource.PAUS_FEHLER_START, _fehler));
            if (!_reihen.Vorhanden)
                return PufferAuslegungErgebnisDaten.MitFehler(Format(MyResource.Resource.PAUS_REIHEN_FEHLER, _reihen.Fehlertext ?? ""));
            try
            {
                return Abbilden(PufferAuslegungCtrl.Rechnen(EingangAus(d)));
            }
            catch (ArgumentException ex)
            {
                return PufferAuslegungErgebnisDaten.MitFehler(Format(MyResource.Resource.PAUS_FEHLER_RECHNEN, ex.Message));
            }
        }

        // =================================================================
        //  Probelauf der Jahressimulation (Welle P4b)
        // =================================================================

        /// <summary>Der Probelauf außerhalb des Oberflächenfadens — die Jahressimulation dauert Sekunden.</summary>
        internal Task<PufferProbelaufDaten> ProbelaufImHintergrund(PufferAuslegungEingabeDaten d) => Task.Run(() => Probelauf(d));

        /// <summary>
        /// Rechnet die Auslegung des Arbeitsstands und fährt mit ihrer Empfehlung einen Probelauf der
        /// Jahressimulation (<see cref="PufferProbelaufCtrl.Probelauf"/>). Nichts wird gespeichert, nichts
        /// übernommen; was nicht geht, kommt benannt zurück (Lesemodus, Konfiguration, Klimaregion, neuer Puffer).
        /// </summary>
        internal PufferProbelaufDaten Probelauf(PufferAuslegungEingabeDaten d)
        {
            if (_vorbelegung == null) return PufferProbelaufDaten.MitFehler(Format(MyResource.Resource.PAUS_FEHLER_START, _fehler));
            if (!_reihen.Vorhanden)
                return PufferProbelaufDaten.MitFehler(Format(MyResource.Resource.PAUS_REIHEN_FEHLER, _reihen.Fehlertext ?? ""));
            PufferAuslegungEingang e;
            PufferAuslegungErgebnis r;
            try
            {
                e = EingangAus(d);
                r = PufferAuslegungCtrl.Rechnen(e);
            }
            catch (ArgumentException ex)
            {
                return PufferProbelaufDaten.MitFehler(Format(MyResource.Resource.PAUS_FEHLER_RECHNEN, ex.Message));
            }
            if (!(r.EmpfehlungL > 0)) return PufferProbelaufDaten.MitFehler(MyResource.Resource.PAUS_GRUND_KEINE_EMPFEHLUNG);

            PufferProbelaufErgebnis p;
            try
            {
                p = PufferProbelaufCtrl.Probelauf(_auftrag.IdProjekt, _idPuffer, r.EmpfehlungL, _reihen.Heizung,
                                                  PufferProbelaufCtrl.Rang1Typ(e.Vorlage));
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                // Benannt, nie still: Der Lauf ist eine Fremdrechnung, jeder Abbruch erreicht die Ansicht.
                return PufferProbelaufDaten.MitFehler(Format(MyResource.Resource.PAUS_PROBELAUF_FEHLER, ex.Message));
            }
            PufferBetriebsbild bild = r.Zone(PufferZone.Heizung)?.Betriebsbild ?? r.Zone(PufferZone.Prozess)?.Betriebsbild;
            return Abbilden(p, bild?.StartsJeTag);
        }

        /// <summary>Die Starts je Tag der Auslegung (D2) aus dem angezeigten Ergebnis; <c>null</c> = keine.</summary>
        private static double? AuslegungStartsJeTag(PufferAuslegungErgebnisDaten r)
            => (r?.Zonen.FirstOrDefault(z => z.Zone == nameof(PufferZone.Heizung))?.Betriebsbild
                ?? r?.Zonen.FirstOrDefault(z => z.Zone == nameof(PufferZone.Prozess))?.Betriebsbild)?.StartsJeTag;

        /// <summary>Der Probelauf des Kerns als Anzeige, gehalten gegen die Starts je Tag der Auslegung.</summary>
        internal static PufferProbelaufDaten Abbilden(PufferProbelaufErgebnis p, double? auslegungJeTag)
        {
            if (p == null) return null;
            if (!p.Erfolgreich)
                return PufferProbelaufDaten.MitFehler(Format(MyResource.Resource.PAUS_PROBELAUF_FEHLER, p.Fehlertext));
            var d = new PufferProbelaufDaten
            {
                Erfolg = true,
                Zeitpunkt = p.Zeitpunkt,
                DauerSekunden = p.Dauer.TotalSeconds,
                VolumenL = p.VolumenL,
                Starts = p.Starts.Select(s => new PufferProbelaufStartsDaten(Erzeugername(s.Typ), s.StartsJahr,
                                                                            s.StartsHeizperiode, s.StartsJeTag, s.Rang1,
                                                                            s.AusReihe)).ToList(),
                Deckung = p.Deckung,
                Monate = p.Monate.Select(m => new PufferFuellstandMonatDaten(m.Monat, m.Min, m.Mittel, m.Max)).ToList(),
                AuslegungStartsJeTag = auslegungJeTag
            };
            if (p.Fuellstand != null && p.KaeltesteWocheAb >= 0)
            {
                IEnumerable<double> woche = p.Fuellstand.Skip(p.KaeltesteWocheAb).Take(168);
                d.KaeltesteWocheTag = p.KaeltesteWocheAb / 24 + 1;
                d.WocheMin = woche.Min();
                d.WocheMittel = woche.Average();
                d.WocheMax = woche.Max();
            }
            double? lauf = p.Rang1?.StartsJeTag;
            d.Abweichung = PufferProbelaufCtrl.Abweichung(auslegungJeTag, lauf);
            if (d.Abweichung)
                d.AbweichungText = Textbaustein.Aufloesen(PufferProbelaufCtrl.AbweichungText(auslegungJeTag.Value, lauf.Value));
            return d;
        }

        private static string Erzeugername(int typ)
        {
            switch (typ)
            {
                case ProjektPuffer.TYP_WP: return MyResource.Resource.PAUS_HERK_TYP_WP;
                case ProjektPuffer.TYP_BHKW: return MyResource.Resource.PAUS_HERK_TYP_BHKW;
                case ProjektPuffer.TYP_KESSEL: return MyResource.Resource.PAUS_HERK_TYP_KESSEL;
                default: return typ.ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>Speichert Eingaben und Ergebnis in <c>Tab_PufferAuslegung</c>; <c>null</c> = gespeichert.</summary>
        internal string Speichern(PufferAuslegungEingabeDaten d)
        {
            if (_vorbelegung == null) return Format(MyResource.Resource.PAUS_FEHLER_START, _fehler);
            try
            {
                PufferAuslegungEingang e = EingangAus(d);
                PufferAuslegungErgebnis r = _reihen.Vorhanden ? PufferAuslegungCtrl.Rechnen(e) : null;
                int id = PufferAuslegungCtrl.Speichern(_auftrag.IdProjekt, _idPuffer, e, r);
                return id > 0 ? null : MyResource.Resource.PAUS_GRUND_SPEICHERN;
            }
            catch (ArgumentException ex)
            {
                return Format(MyResource.Resource.PAUS_FEHLER_RECHNEN, ex.Message);
            }
        }

        /// <summary>
        /// Speichert die Auslegung und übernimmt die Empfehlung in den gewählten Projektpuffer bzw.
        /// legt einen neuen an (die Auslegungszeile hängt der Kern dann an ihn). Danach zieht der
        /// Arbeitsgang seinen Nachzug (Simulation: Ergebnis veraltet).
        /// </summary>
        internal PufferUebernahmeErgebnis Uebernehmen(PufferAuslegungEingabeDaten d, PufferUebernahmeDaten u)
        {
            if (_vorbelegung == null)
                return new PufferUebernahmeErgebnis(false, Format(MyResource.Resource.PAUS_FEHLER_START, _fehler), 0);
            if (!_reihen.Vorhanden)
                return new PufferUebernahmeErgebnis(false, Format(MyResource.Resource.PAUS_REIHEN_FEHLER, _reihen.Fehlertext ?? ""), 0);
            PufferAuslegungEingang e;
            PufferAuslegungErgebnis r;
            try
            {
                e = EingangAus(d);
                r = PufferAuslegungCtrl.Rechnen(e);
            }
            catch (ArgumentException ex)
            {
                return new PufferUebernahmeErgebnis(false, Format(MyResource.Resource.PAUS_FEHLER_RECHNEN, ex.Message), 0);
            }
            if (!(r.EmpfehlungL > 0))
                return new PufferUebernahmeErgebnis(false, MyResource.Resource.PAUS_GRUND_KEINE_EMPFEHLUNG, 0);

            bool neu = u == null || u.Neu || !_idPuffer.HasValue;
            int? ziel = neu ? (int?)null : _idPuffer;
            string name = neu ? (u?.Bezeichner ?? "").Trim() : "";
            PufferAuslegungCtrl.Speichern(_auftrag.IdProjekt, ziel, e, r);
            int id = PufferAuslegungCtrl.Uebernehmen(_auftrag.IdProjekt, ziel, r, name);
            if (id <= 0)
                return new PufferUebernahmeErgebnis(false, MyResource.Resource.PAUS_GRUND_SCHREIBFEHLER, 0);

            _idPuffer = id;
            _auftrag.Nachzug?.Invoke();
            string anzeige = Puffername(id);
            return new PufferUebernahmeErgebnis(true,
                Format(MyResource.Resource.PAUS_MELDUNG_UEBERNOMMEN, anzeige.Length > 0 ? anzeige : name,
                       r.EmpfehlungL.ToString("N0", CultureInfo.CurrentCulture)), id);
        }

        // =================================================================
        //  Ergebnis -> Anzeige
        // =================================================================

        /// <summary>Das Ergebnis des Kerns als Anzeige — Texte aus den Ressourcen, Rückfall Klartext.</summary>
        internal static PufferAuslegungErgebnisDaten Abbilden(PufferAuslegungErgebnis r)
        {
            if (r == null) return new PufferAuslegungErgebnisDaten();
            var zonen = r.Zonen.Select(z => new PufferZoneDaten(
                z.Zone.ToString(),
                PufferAuslegungTexte.Nach("PAUS_ZONE_", z.Zone.ToString()),
                z.Bemessend ?? "",
                z.Bemessend == null ? "" : PufferAuslegungTexte.Nach("PAUS_KRIT_", z.Bemessend, z.Bemessend),
                z.VolumenL,
                z.KeinPuffer,
                z.Kriterien.Select(k => new PufferKriteriumDaten(
                    k.Kennung, PufferAuslegungTexte.Nach("PAUS_KRIT_", k.Kennung, k.Bezeichnung), k.VolumenL,
                    k.Aktiv, k.Gueltig, Textbaustein.Aufloesen(k.HerkunftBaustein), Textbaustein.Aufloesen(k.RechenwegBaustein))).ToList(),
                z.Betriebsbild == null ? null : new PufferBetriebsbildDaten(
                    z.Betriebsbild.VolumenL, z.Betriebsbild.StartsJeTag, z.Betriebsbild.StartsHeizperiode,
                    z.Betriebsbild.Heizstunden, z.Betriebsbild.Deckungsgrad, z.Betriebsbild.MittlereLaufzeitH))).ToList();

            PufferZonenergebnis groesste = r.Zonen.Where(z => z.Bemessend != null).OrderByDescending(z => z.VolumenL).FirstOrDefault();
            PufferKennzahlen k0 = r.Kennzahlen ?? new PufferKennzahlen();
            PufferBetriebsbild bild = r.Zone(PufferZone.Heizung)?.Betriebsbild ?? r.Zone(PufferZone.Prozess)?.Betriebsbild;
            return new PufferAuslegungErgebnisDaten
            {
                Zustand = PufferErgebnisZustand.Gerechnet,
                Zonen = zonen,
                SummeL = r.SummeL,
                EmpfehlungL = r.EmpfehlungL,
                UeberListenende = r.UeberListenende,
                AnPraxisgrenze = r.AnPraxisgrenze,
                Bemessend = groesste == null ? ""
                    : PufferAuslegungTexte.Nach("PAUS_ZONE_", groesste.Zone.ToString()) + ": " +
                      PufferAuslegungTexte.Nach("PAUS_KRIT_", groesste.Bemessend, groesste.Bemessend),
                Katalogvorschlag = r.Katalogvorschlag == null ? ""
                    : r.Katalogvorschlag.Bezeichner + " · " + r.Katalogvorschlag.VolumenL.ToString("N0", CultureInfo.CurrentCulture) + " l",
                Kennzahlen = new PufferKennzahlDaten
                {
                    Nutzanteil = k0.Nutzanteil,
                    SpreizungK = k0.SpreizungK,
                    AuslegungsheizlastKw = k0.AuslegungsheizlastKw,
                    AnlagenvolumenL = k0.AnlagenvolumenL,
                    BandMinL = k0.BandMinL,
                    BandMaxL = k0.BandMaxL,
                    BandWpMinL = k0.BandWpMinL,
                    BandWpMaxL = k0.BandWpMaxL,
                    KapazitaetKwh = k0.KapazitaetKwh,
                    VerlustKwhJeTag = k0.Verlust?.KwhJeTag,
                    VerlustWJeK = k0.Verlust?.WJeK,
                    VerlustKwhJeJahr = k0.Verlust?.KwhJeJahr,
                    VerlustAusKatalog = k0.Verlust?.AusKatalog == true,
                    VerlustExtrapoliert = k0.Verlust?.Extrapoliert == true,
                    StartsJeTag = bild?.StartsJeTag,
                    StartsHeizperiode = bild?.StartsHeizperiode,
                    LiterJePerson = k0.LiterJePerson,
                    ZonenanteilHeizung = k0.ZonenanteilHeizung,
                    SchichtenMindest = k0.SchichtenMindest,
                    FaustwertL = k0.FaustwertGegenprobeL
                },
                Warnungen = r.Warnungen.Select(w => new PufferWarnungDaten(
                    w.Code, w.Stufe == PufferStufe.Warnung,
                    Ressource(w.Ressourcenschluessel, w.Text), w.Text ?? "", Textbaustein.Aufloesen(w.HerkunftBaustein),
                    w.Zone.HasValue ? PufferAuslegungTexte.Nach("PAUS_ZONE_", w.Zone.Value.ToString()) : "")).ToList()
            };
        }

        // =================================================================
        //  Hilfen
        // =================================================================

        private static string Ressource(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel, CultureInfo.CurrentUICulture); }
            catch (Exception) { t = null; }
            return string.IsNullOrEmpty(t) ? (rueckfall ?? "") : t;
        }

        private static string Projektname(int idProjekt)
        {
            if (idProjekt <= 0) return "";
            try
            {
                var p = new ProjektCtrl();
                p.ReadSingle(idProjekt);
                return p.m_szProjektname ?? "";
            }
            catch (Exception) { return ""; }
        }

        private string Puffername(int idPuffer)
        {
            try { return PufferSpCtrl.Detail(idPuffer, _auftrag.IdProjekt)?.Bezeichner ?? ""; }
            catch (Exception) { return ""; }
        }

        private static string Format(string muster, params object[] werte)
        {
            try { return string.Format(CultureInfo.CurrentCulture, muster ?? "", werte); }
            catch (FormatException) { return muster ?? ""; }
        }
    }
}
