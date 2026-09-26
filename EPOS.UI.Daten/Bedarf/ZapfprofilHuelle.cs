using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Bedarf;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Hülle des Dialogs „Brauchwasser-Zapfprofil"</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 5.1–5.5; Stufe Z1, Gruppe 3) — plattformfrei, Muster
    /// <see cref="BedarfErgebnisHuelle"/>.
    ///
    /// <para><b>Die einzige Stelle, die übersetzt.</b> Nur hier wird der Arbeitsstand des Kerns
    /// (<see cref="ZapfprofilStand"/>) zu den DTO aus <c>ZapfprofilDaten.cs</c> und zurück
    /// (<see cref="AlsEingabe"/>, <see cref="AlsStand"/>); der Dialog kennt keinen Kern-Typ.
    /// Die Angaben der Stufen Erweitert und Experte gehen je Zone als
    /// <see cref="ZapfprofilZonenangabenDaten"/> und fürs Gebäude als
    /// <see cref="ZapfprofilGebaeudeDaten"/> hin und zurück (Stufe Z4); fehlen sie im DTO, bleibt
    /// der Arbeitsstand des Kerns stehen — die Stufe blendet nur ein und aus.</para>
    ///
    /// <para><b>Die Vorschau ruft den Lauf.</b> <see cref="Vorschau"/> geht über
    /// <see cref="BedarfsVorschauCtrl.ProjektVorschau"/> mit dem Arbeitsstand, also über
    /// denselben Generatoraufruf, den der Lauf nimmt (2.4) — live auf dem deterministischen Pfad
    /// (5.1); die stochastische Jahresreihe zieht allein <see cref="Jahresreihe"/>, nebenläufig
    /// mit Abbruchmarke (Delegat <c>Jahresreihe</c>). Die Reihen für Tagesgang und Woche
    /// wertet der Kern aus (<see cref="Zapfauswertung"/>), die Bilder zeichnen die
    /// Zeichenbausteine des Kerns (<see cref="ZapfprofilBilder"/>). Die Hülle rechnet keinen
    /// Bedarf.</para>
    ///
    /// <para><b>Benannt statt still.</b> Jede Ablehnung — des Rechenwegs, des Schreibwegs, der
    /// Verfügbarkeit, der Plattform — kommt als <see cref="ZapfprofilMeldung"/> mit
    /// Ressourcenschlüssel als Kennung und Text in der Oberflächensprache; der deutsche Wortlaut
    /// des Kerns steht daneben (<c>Klartext</c>).</para>
    ///
    /// <para><b>Der Parametersatz</b> (<see cref="Gaben"/>) trägt die Schlüssel
    /// <c>Daten</c>, <c>Texte</c>, <c>Vorschau</c>, <c>Jahresreihe</c>, <c>Pruefen</c>, <c>AuslegungGaben</c>,
    /// <c>HilfeSchluessel</c> und <c>HilfeRechenweg</c> — die <c>[Parameter]</c> der Komponente
    /// <c>ZapfprofilDialog.razor</c>. <c>AuslegungGaben</c> baut je Öffnen den Parametersatz der
    /// Überlagerung „Auslegung" zum Arbeitsstand des Dialogs (<see cref="AuslegungGaben"/>).</para>
    /// </summary>
    internal static partial class ZapfprofilHuelle
    {
        /// <summary>Der Hilfeschlüssel des Dialogs (5.8).</summary>
        internal const string HILFE_DIALOG = "Form_Zapfprofil.btn_Help";

        /// <summary>Der Hilfeschlüssel des Rechenwegs der Jahresreihe (5.8).</summary>
        internal const string HILFE_RECHENWEG = "Form_Zapfprofil_Berechnung";

        /// <summary>Der Hilfeschlüssel des Rechenwegs der Auslegung (5.8).</summary>
        internal const string HILFE_AUSLEGUNG_RECHENWEG = "Form_Zapfprofil.grp_Auslegung";

        private const string TRENNER = " · ";

        // =================================================================================
        // Einstieg und Parametersatz
        // =================================================================================

        /// <summary>
        /// Der Einstieg aus dem Bedarfsprofil-Dialog (5.2): mit Projekt und angebotenem Weg die
        /// zwei Delegaten, sonst der benannte Grund — ohne gespeichertes Projekt (ZU10) oder
        /// wenn die Schale den Dialog nicht anbietet (A11).
        /// </summary>
        internal static ZapfprofilEinstieg Einstieg(int idProjekt, Zapfprofilwege wege)
        {
            if (wege == null || wege.Uebernehmen == null)
                return ZapfprofilEinstieg.Ohne(string.IsNullOrEmpty(wege?.Sperrgrund)
                                                   ? Text_("ZPG_MSG_PLATTFORM", "Der Zapfprofilgenerator ist auf dieser Plattform noch nicht erreichbar.")
                                                   : wege.Sperrgrund);
            if (idProjekt <= 0)
                return ZapfprofilEinstieg.Ohne(Text_("ZPG_MSG_OHNE_PROJEKT", "Das Zapfprofil braucht ein gespeichertes Projekt."));

            Zapfprofilwege w = wege;
            return new ZapfprofilEinstieg(
                true, "",
                () => Gaben(idProjekt, w.Arbeitsstand?.Invoke()),
                ergebnis =>
                {
                    if (ergebnis == null) return;
                    ZapfprofilStand basis = w.Arbeitsstand?.Invoke() ?? ZapfprofilCtrl.Lies(idProjekt);
                    w.Uebernehmen(Uebernahme(ergebnis, basis));
                });
        }

        /// <summary>
        /// Der Parametersatz des Dialogs zu einem Projekt und Arbeitsstand (<c>null</c> = der
        /// gespeicherte). Die Delegaten rechnen gegen den Arbeitsstand, mit dem der Dialog
        /// öffnete — er trägt die Größen der höheren Stufen.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(int idProjekt, ZapfprofilStand arbeitsstand)
        {
            ZapfprofilStand basis = arbeitsstand ?? ZapfprofilCtrl.Lies(idProjekt);
            ZapfprofilDaten daten = Laden(idProjekt, basis);
            return new Dictionary<string, object>
            {
                ["Daten"] = daten,
                ["Texte"] = Texte(),
                ["Vorschau"] = new Func<ZapfprofilEingabeDaten, ZapfprofilVorschauDaten>(e => Vorschau(idProjekt, e, basis)),
                // Die stochastische Jahresreihe nebenläufig (5.1): auf einem Arbeitsfaden mit der Kultur
                // des Aufrufers, abbrechbar — nie im Zeichenfaden.
                ["Jahresreihe"] = new Func<ZapfprofilEingabeDaten, CancellationToken, Task<ZapfprofilVorschauDaten>>(
                    (e, abbruch) => Kulturweitergabe.Starten(() => Jahresreihe(idProjekt, e, basis, abbruch), abbruch)),
                // Der Katalog kommt aus dem beim Öffnen geladenen Stand — kein neuer Datenbankzugriff je Tastendruck.
                ["Pruefen"] = new Func<ZapfprofilEingabeDaten, IReadOnlyList<ZapfprofilMeldung>>(e => Pruefen(e, daten.Katalog)),
                // Der Schalter „Stochastisch rechnen" ist eine Laufangabe: aus bei „Auslegung…", an beim Fußknopf.
                // Die Stufe des Dialogs reist als Laufangabe im Arbeitsstand mit (N11 (c): „Schnellauslegung"
                // nur in der Stufe Einfach).
                ["AuslegungGaben"] = new Func<ZapfprofilEingabeDaten, bool, IReadOnlyDictionary<string, object>>(
                    (e, stochastisch) => AuslegungGaben(idProjekt, e, basis, e?.Stufe ?? ZapfprofilStufe.Einfach, stochastisch)),
                // Die Editoren der Stufe Experte (Z4, Gruppe 2b) gehören zum Katalog: Ihr OK schreibt sofort,
                // danach liest der Dialog den Katalog neu. Der Ladeleistungs-Vorschlag rechnet die Auslegung
                // deterministisch zum Arbeitsstand (entprellt wie die Vorschau).
                ["TagesgangGaben"] = new Func<int, int?, IReadOnlyDictionary<string, object>>(TagesgangGaben),
                ["KategorienGaben"] = new Func<int, IReadOnlyDictionary<string, object>>(KategorienGaben),
                ["Katalogstand"] = new Func<ZapfprofilKatalogstandDaten>(Katalogstand),
                // Der Dialog "VDI-4655-Typtage" (Stufe Z4b): Einspielen und Loeschen schreiben
                // sofort; danach liest der Dialog den Katalogstand samt Typtagstand neu.
                ["TyptagGaben"] = new Func<IReadOnlyDictionary<string, object>>(TyptagGaben),
                // Der Dialog „Messdaten" (Stufe Z5): Einspielen und Loeschen schreiben sofort;
                // danach liest der Dialog die Messreihen des Projekts neu.
                ["MessreihenGaben"] = new Func<IReadOnlyDictionary<string, object>>(() => MessreihenGaben(idProjekt)),
                // Der Vergleich mit einer Messreihe und die Kalibrierung daraus (Stufe Z5, Gruppe 3).
                // Vergleich UND Kalibrierung rechnen DENSELBEN Weg wie der Lauf und laufen deshalb
                // nebenlaeufig auf einem Arbeitsfaden mit der Kultur des Aufrufers, abbrechbar; der
                // Vorschlag und seine Uebernahme rechnen nur aus der Messreihe und laufen im
                // Verteiler. Die Kalibrierung rechnet nur bei einem Teiljahr (Hochrechnung ueber den
                // Jahresgang) - bei einer Volljahresreihe kehrt sie ohne Lauf zurueck.
                ["Messreihen"] = new Func<TwwMessreihenstandDaten>(() => Messreihenstand(idProjekt)),
                ["Messvergleich"] = new Func<ZapfprofilEingabeDaten, string, CancellationToken, Task<ZapfprofilMessvergleichDaten>>(
                    (e, r, abbruch) => Kulturweitergabe.Starten(() => Vergleichsbericht(idProjekt, e, basis, r, abbruch), abbruch)),
                ["Messkalibrierung"] = new Func<ZapfprofilEingabeDaten, string, int, CancellationToken, Task<ZapfprofilMesskalibrierungDaten>>(
                    (e, r, zone, abbruch) => Kulturweitergabe.Starten(
                        () => MesswertAusReihe(idProjekt, e, basis, r, zone, abbruch), abbruch)),
                ["Kalibriervorschlag"] = new Func<ZapfprofilEingabeDaten, string, int, ZapfprofilVorschlagDaten>(
                    (e, r, zone) => Kalibriervorschlag(idProjekt, e, basis, r, zone)),
                ["VorschlagUebernehmen"] = new Func<ZapfprofilEingabeDaten, string, int, ZapfprofilVorschlagErgebnisDaten>(
                    (e, r, zone) => VorschlagUebernehmen(idProjekt, e, basis, r, zone)),
                ["Ladevorschlag"] = new Func<ZapfprofilEingabeDaten, ZapfprofilSchaetzhilfeDaten>(e => Ladevorschlag(idProjekt, e, basis)),
                ["HilfeSchluessel"] = HILFE_DIALOG,
                ["HilfeRechenweg"] = HILFE_RECHENWEG
            };
        }

        /// <summary>
        /// Der Stand des Dialogs beim Öffnen: Kontext, Katalog, Arbeitsstand, Verfügbarkeit und
        /// die erste Vorschau. Ist der Generator nicht verfügbar, bleibt der Katalog leer und
        /// <see cref="ZapfprofilDaten.Sperrgrund"/> nennt den Grund.
        /// </summary>
        internal static ZapfprofilDaten Laden(int idProjekt, ZapfprofilStand arbeitsstand)
        {
            ZapfprofilStand stand = arbeitsstand ?? ZapfprofilCtrl.Lies(idProjekt);
            var daten = new ZapfprofilDaten
            {
                IdProjekt = idProjekt,
                Eingabe = AlsEingabe(stand),
                MitPunkt = stand?.Projekt?.AuslegungVolumenL != null || stand?.Projekt?.AuslegungLeistungKw != null,
                Stufe = ZapfprofilStufe.Einfach
            };

            ZapfVerfuegbarkeit verfuegbar = ZapfprofilCtrl.Verfuegbar();
            daten.Verfuegbar = verfuegbar.Ja;
            daten.Sperrgrund = verfuegbar.Ja ? "" : Verfuegbarkeitsgrund(verfuegbar);

            var projekt = new ProjektCtrl();
            if (idProjekt > 0) projekt.ReadSingle(idProjekt);
            daten.Kontext = Kontext(projekt);
            if (!verfuegbar.Ja) return daten;

            StochastikRahmen(daten);
            daten.Typtagstand = TyptagStand();
            daten.Katalog = Katalog();
            HoehereStufen(daten, idProjekt);
            daten.Vorschau = Vorschau(idProjekt, daten.Eingabe, stand);
            return daten;
        }

        /// <summary>
        /// Was die Stufen Erweitert und Experte zum Öffnen brauchen (Stufe Z4): die gebäudeweiten
        /// Größen einer Projektzeile ohne Eingabe (DDL, <see cref="ZapfprofilCtrl.ProjektVorgabe()"/>)
        /// — trägt der Arbeitsstand keine Projektzeile, beginnt der Dialog mit ihnen —, die Vorgaben des
        /// Parametersatzes für die Platzhalter, die Tagesgangsätze, die Ausstattungsklassen der
        /// Wohnungstabelle (neutraler Name, nie ein Wert) und die Gebäude des Projekts (A8).
        /// </summary>
        private static void HoehereStufen(ZapfprofilDaten daten, int idProjekt)
        {
            daten.GebaeudeVorgabe = AlsGebaeude(ZapfprofilCtrl.ProjektVorgabe());
            daten.Eingabe.Gebaeude ??= daten.GebaeudeVorgabe?.Kopie();
            daten.Vorgaben = Vorgaben();
            daten.Monatsnamen = Enumerable.Range(1, 12).Select(Monatsname).ToList();
            daten.Tagesgangsaetze = ZapfprofilCtrl.Tagesgangsaetze().Select(AlsTagesgangsatz).ToList();
            string version = ZapfprofilCtrl.AktuelleKatalogversion();
            daten.Ausstattungen = ZapfprofilCtrl.Ausstattungen(version)
                .Select(a => new ZapfprofilKatalogeintragDaten { Id = a.Id, Name = a.Schluessel ?? "" }).ToList();
            daten.Gebaeude = ZapfprofilCtrl.GebaeudeDesProjekts(idProjekt)
                .Select(g => new ZapfprofilKatalogeintragDaten
                {
                    Id = g.Id,
                    Name = string.IsNullOrWhiteSpace(g.Name) ? "#" + g.Id.ToString(CultureInfo.InvariantCulture) : g.Name.Trim()
                }).ToList();
        }

        /// <summary>Ein Tagesgangsatz als Eintrag der Auswahl: Name · Katalogversion, Herkunft als Status; unvollständig = gesperrt mit Grund.</summary>
        internal static ZapfprofilKatalogeintragDaten AlsTagesgangsatz(Tagesgangsatz s)
            => new ZapfprofilKatalogeintragDaten
            {
                Id = s.Id,
                Name = string.IsNullOrEmpty(s.Katalogversion) ? s.Bezeichner ?? "" : (s.Bezeichner ?? "") + TRENNER + s.Katalogversion,
                Herkunft = Status(s.Status),
                Waehlbar = s.Vollstaendig,
                Sperrgrund = s.Vollstaendig ? "" : Text_("ZPG_KAT_SPERRE_TAGESGANG", "Der Tagesgangsatz dieser Nutzungsart ist unvollständig.")
            };

        /// <summary>
        /// Die Vorgaben des Parametersatzes für die Platzhalter der höheren Stufen — nur Werte, die
        /// der Parametersatz führt; ohne Katalogversion bleiben alle leer (der Rechenweg nennt dann
        /// den Grund).
        /// </summary>
        internal static ZapfprofilVorgabenDaten Vorgaben()
        {
            var v = new ZapfprofilVorgabenDaten();
            Parametersatz ps;
            try { ps = ZapfprofilCtrl.Parameter(); }
            catch (ParametersatzException) { return v; }

            double? W(string schluessel) => ps.Enthaelt(schluessel) ? ps.Wert(schluessel) : (double?)null;
            v.KaltwasserMittelC = W(ZapfParameter.KALTWASSER_MITTEL);
            v.KaltwasserAmplitudeK = W(ZapfParameter.KALTWASSER_AMPLITUDE);
            v.WohnflaecheJeWeM2 = W(ZapfParameter.WOHNEN_FLAECHE_JE_WE);
            v.ZirkLaufzeitH = W(ZapfParameter.ZIRKULATION_LAUFZEIT);
            v.ZirkAnteil = W(ZapfParameter.ZIRKULATION_ANTEIL);
            v.ZirkVerlustWJeM = W(ZapfParameter.ZIRKULATION_VERLUST_JE_METER);
            double? lage = W(ZapfParameter.ZIRKULATION_LAGE);
            v.ZirkLage = lage.HasValue ? (int)Math.Round(lage.Value) : (int?)null;
            v.ZirkKennwertLage1 = W(ZapfParameter.ZIRKULATION_KENNWERT_LAGE1);
            v.ZirkKennwertLage2 = W(ZapfParameter.ZIRKULATION_KENNWERT_LAGE2);
            v.KaltwasserAuslegungC = W(ZapfAuslegungParameter.KALTWASSER_AUSLEGUNG);
            v.SpeicherC = W(ZapfAuslegungParameter.SPEICHERTEMPERATUR_VORGABE);
            v.LadefensterH = W(ZapfAuslegungParameter.LADEFENSTER_LAENGE);
            v.LadefensterBeginnH = W(ZapfAuslegungParameter.LADEFENSTER_BEGINN);
            v.AnzeigetemperaturC = W(ZapfParameter.ANZEIGETEMPERATUR);
            v.SchwelleKw = W(ZapfParameter.STUNDENSCHWELLE);
            return v;
        }

        // =================================================================================
        // Abbildung Kern <-> DTO
        // =================================================================================

        /// <summary>Der Weg des Kerns als DTO.</summary>
        internal static ZapfprofilWeg AlsWeg(BrauchwasserWeg weg)
            => weg == BrauchwasserWeg.Generator ? ZapfprofilWeg.Generator : ZapfprofilWeg.Bestand;

        /// <summary>Der Weg des DTO für den Kern.</summary>
        internal static BrauchwasserWeg AlsWeg(ZapfprofilWeg weg)
            => weg == ZapfprofilWeg.Generator ? BrauchwasserWeg.Generator : BrauchwasserWeg.Bestand;

        /// <summary>
        /// Der Arbeitsstand des Kerns als DTO: Weg, Zonen samt den Angaben der höheren Stufen, die
        /// gebäudeweiten Größen und die Stochastik der Jahresreihe (Rechenweg, Seed, Realisierungen).
        /// Ohne Projektzeile bleiben Seed, Realisierungen und Gebäude <c>null</c> — dann gilt die
        /// Vorgabe der DDL.
        /// </summary>
        internal static ZapfprofilEingabeDaten AlsEingabe(ZapfprofilStand stand)
        {
            var e = new ZapfprofilEingabeDaten { Weg = AlsWeg(stand?.Weg ?? BrauchwasserWeg.Bestand) };
            if (stand?.Zonen != null)
                foreach (ZonenStand z in stand.Zonen) e.Zonen.Add(AlsZone(z));
            ProjektStand p = stand?.Projekt;
            if (p != null)
            {
                e.JahresreiheStochastisch = p.JahresreiheStochastisch;
                e.Seed = p.Seed;
                e.Realisierungen = p.Realisierungen;
                e.Gebaeude = AlsGebaeude(p);
                // Die Wahl des Typtagwegs (Stufe Z4b, Schritt 131) - eine Groesse des Projekts.
                e.TyptageAktiv = p.TyptageAktiv;
                e.TyptageKlimazone = p.TyptageKlimazone;
                e.TyptageGebaeudeart = p.TyptageGebaeudeart ?? "";
            }
            e.AnzeigetemperaturC = stand?.Anzeige?.AnzeigetemperaturC;
            e.SchwelleKw = stand?.Anzeige?.SchwelleKw;
            return e;
        }

        /// <summary>
        /// Die Vorgaben und Grenzen der Stochastik für den Dialog: Seed und Realisierungen aus der
        /// DDL (<see cref="ZapfprofilCtrl.ProjektVorgabe()"/>, keine zweite Abschrift im Quelltext),
        /// die Untergrenze des Schemas und die Obergrenze der Jahresreihe aus dem Kern.
        /// </summary>
        private static void StochastikRahmen(ZapfprofilDaten daten)
        {
            ProjektStand vorgabe = ZapfprofilCtrl.ProjektVorgabe();
            daten.SeedVorgabe = vorgabe?.Seed;
            daten.RealisierungenVorgabe = vorgabe?.Realisierungen;
            daten.RealisierungenMindestens = TwwSchema.RealisierungenMindestens;
            daten.RealisierungenHoechstens = Jahresensemble.HOECHSTENS;
        }

        /// <summary>
        /// Die Projektgrößen mit der Stochastik der Jahresreihe aus dem Dialog: Rechenweg, Seed und
        /// Realisierungen (<c>null</c> = der Wert der Basis bleibt). Weicht nichts ab, bleibt die
        /// Basis dieselbe Instanz; ohne Projektzeile entsteht eine aus den Vorgaben der DDL nur, wenn
        /// der Dialog etwas anderes will als sie.
        /// </summary>
        internal static ProjektStand MitStochastik(ProjektStand p, ZapfprofilEingabeDaten e)
        {
            if (e == null) return p;
            ProjektStand basis = p;
            if (basis == null)
            {
                if (!e.JahresreiheStochastisch && !e.Seed.HasValue && !e.Realisierungen.HasValue) return null;
                basis = ZapfprofilCtrl.ProjektVorgabe();
                if (basis == null) return null;
            }
            int seed = e.Seed ?? basis.Seed;
            int realisierungen = e.Realisierungen ?? basis.Realisierungen;
            if (basis.JahresreiheStochastisch == e.JahresreiheStochastisch && basis.Seed == seed
                && basis.Realisierungen == realisierungen)
                return p;
            return basis with { JahresreiheStochastisch = e.JahresreiheStochastisch, Seed = seed, Realisierungen = realisierungen };
        }

        /// <summary>
        /// <b>Die Projektgrößen mit der Wahl des Typtagwegs aus dem Dialog</b> (Stufe Z4b, Gruppe 2;
        /// Schritt 131): Typtagweg ja/nein, Klimazone und Gebäudeart. Weicht nichts ab, bleibt die
        /// Basis dieselbe Instanz; ohne Projektzeile entsteht eine aus den Vorgaben der DDL nur,
        /// wenn der Dialog den Typtagweg überhaupt will (Muster <see cref="MitStochastik"/>).
        ///
        /// <para><b>Gespeichert wird die WAHL</b>, nie ein Wert der Typtage. Eine leere Gebäudeart
        /// ist „keine Wahl" und wird als <c>null</c> weitergegeben.</para>
        /// </summary>
        internal static ProjektStand MitTyptagwahl(ProjektStand p, ZapfprofilEingabeDaten e)
        {
            if (e == null) return p;
            string art = string.IsNullOrWhiteSpace(e.TyptageGebaeudeart) ? null : e.TyptageGebaeudeart.Trim();
            ProjektStand basis = p;
            if (basis == null)
            {
                if (!e.TyptageAktiv && !e.TyptageKlimazone.HasValue && art == null) return null;
                basis = ZapfprofilCtrl.ProjektVorgabe();
                if (basis == null) return null;
            }
            if (basis.TyptageAktiv == e.TyptageAktiv && basis.TyptageKlimazone == e.TyptageKlimazone
                && string.Equals(basis.TyptageGebaeudeart ?? "", art ?? "", StringComparison.Ordinal))
                return p;
            return basis with
            {
                TyptageAktiv = e.TyptageAktiv,
                TyptageKlimazone = e.TyptageKlimazone,
                TyptageGebaeudeart = art
            };
        }

        /// <summary>
        /// Eine Zone des Kerns als DTO samt den Angaben der höheren Stufen; die Bezugsmenge 0 gilt
        /// als „nicht eingegeben".
        /// </summary>
        internal static ZapfprofilZoneDaten AlsZone(ZonenStand z)
        {
            ZapfprofilZonenangabenDaten angaben = AlsAngaben(z);
            return new ZapfprofilZoneDaten
            {
                Id = z.Id,
                Name = z.Name ?? "",
                IdNutzungsart = z.IdNutzungsart,
                Bezugsmenge = z.Bezugsmenge > 0 ? z.Bezugsmenge : null,
                Niveau = Enum.IsDefined(typeof(ZapfprofilNiveau), (int)z.Niveau) ? (ZapfprofilNiveau)(int)z.Niveau
                                                                                  : ZapfprofilNiveau.Mittel,
                Ueberschrieben = angaben.Ueberschrieben(),
                Angaben = angaben
            };
        }

        /// <summary>
        /// Die Angaben der Stufen Erweitert und Experte einer Zone des Kerns (5.3): Jahrestage der
        /// Ferien als Tag und Monat des Rechenjahrs (0, 366 und leer = keine Angabe), Wohnungstabelle
        /// in ihrer Reihenfolge, Texte leer statt <c>null</c>.
        /// </summary>
        internal static ZapfprofilZonenangabenDaten AlsAngaben(ZonenStand z)
        {
            var a = new ZapfprofilZonenangabenDaten
            {
                IdGebaeude = z.IdGebaeude,
                IdTagesgangsatz = z.IdTagesgangsatz,
                PersonenJeWe = z.PersonenJeWe,
                WohnflaecheJeWeM2 = z.WohnflaecheJeWeM2,
                Topologie = Enum.IsDefined(typeof(ZapfprofilTopologie), (int)z.Topologie)
                    ? (ZapfprofilTopologie)(int)z.Topologie : ZapfprofilTopologie.Speicher,
                Zirkulation = z.Zirkulation,
                Jahresmesswert = z.Jahresmesswert,
                JahresmesswertEinheit = z.JahresmesswertEinheit.HasValue
                    ? (ZapfprofilMesswerteinheit)(int)z.JahresmesswertEinheit.Value : (ZapfprofilMesswerteinheit?)null,
                JahresmesswertBilanzgrenze = z.JahresmesswertBilanzgrenze.HasValue
                    ? (ZapfprofilBilanzgrenze)(int)z.JahresmesswertBilanzgrenze.Value : (ZapfprofilBilanzgrenze?)null,
                JahresmesswertQuelle = z.JahresmesswertQuelle ?? "",
                JahresmesswertZeitraum = z.JahresmesswertZeitraum ?? "",
                SpeicherverlustKwhJeJahr = z.SpeicherverlustKwhJeJahr,
                TagesbedarfAuto = z.TagesbedarfAuto,
                TagesbedarfManuellKwh = z.TagesbedarfManuellKwh,
                BedarfSpezKwhJeEinheitTag = z.BedarfSpezKwhJeEinheitTag,
                ZapftemperaturC = z.ZapftemperaturC,
                KaltwasserMittelC = z.KaltwasserMittelC,
                KaltwasserAmplitudeK = z.KaltwasserAmplitudeK
            };
            for (int i = 0; i < ZapfprofilZonenangabenDaten.FERIENZEITRAEUME; i++)
            {
                int? b = z.Ferienbeginn != null && i < z.Ferienbeginn.Length ? z.Ferienbeginn[i] : null;
                int? e = z.Ferienende != null && i < z.Ferienende.Length ? z.Ferienende[i] : null;
                (int? bt, int? bm) = AlsDatum(b);
                (int? et, int? em) = AlsDatum(e);
                a.Ferien[i] = new ZapfprofilFerienDaten { BeginnTag = bt, BeginnMonat = bm, EndeTag = et, EndeMonat = em };
            }
            for (int m = 0; m < ZapfprofilZonenangabenDaten.MONATE; m++)
                a.Auslastung[m] = z.Auslastung != null && m < z.Auslastung.Length ? z.Auslastung[m] : null;
            foreach (WohnungstypStand w in (z.Wohnungen ?? new WohnungstypStand[0]).OrderBy(w => w.Reihenfolge))
                a.Wohnungen.Add(new ZapfprofilWohnungDaten
                {
                    Id = w.Id,
                    Anzahl = w.Anzahl > 0 ? w.Anzahl : (int?)null,
                    Raumzahl = w.Raumzahl,
                    Personen = w.Personen,
                    IdAusstattung = w.IdAusstattung
                });
            return a;
        }

        /// <summary>
        /// Die Zone des Kerns mit den Angaben des Dialogs: jede Größe der höheren Stufen aus dem DTO
        /// (Ferien als Jahrestag des Rechenjahrs, unvollständig = keine Angabe; Wohnungstabelle in
        /// der Reihenfolge des Dialogs, eine Zeile ohne Anzahl trägt 0 — der Schreibweg lehnt sie
        /// benannt ab). Was das DTO nicht führt (die Fläche des gebundenen Gebäudes), bleibt.
        /// </summary>
        internal static ZonenStand MitAngaben(ZonenStand z, ZapfprofilZonenangabenDaten a)
        {
            if (z == null || a == null) return z;
            var beginn = new int?[ZapfprofilZonenangabenDaten.FERIENZEITRAEUME];
            var ende = new int?[ZapfprofilZonenangabenDaten.FERIENZEITRAEUME];
            for (int i = 0; i < ZapfprofilZonenangabenDaten.FERIENZEITRAEUME; i++)
            {
                ZapfprofilFerienDaten f = a.Ferien != null && i < a.Ferien.Count ? a.Ferien[i] : null;
                beginn[i] = Jahrestag(f?.BeginnTag, f?.BeginnMonat);
                ende[i] = Jahrestag(f?.EndeTag, f?.EndeMonat);
            }
            var auslastung = new double?[ZapfprofilZonenangabenDaten.MONATE];
            for (int m = 0; m < auslastung.Length; m++)
                auslastung[m] = a.Auslastung != null && m < a.Auslastung.Length ? a.Auslastung[m] : null;
            var wohnungen = new List<WohnungstypStand>();
            for (int i = 0; i < (a.Wohnungen?.Count ?? 0); i++)
            {
                ZapfprofilWohnungDaten w = a.Wohnungen[i];
                if (w == null) continue;
                wohnungen.Add(new WohnungstypStand
                {
                    Id = w.Id,
                    Anzahl = w.Anzahl ?? 0,
                    Raumzahl = w.Raumzahl,
                    Personen = w.Personen,
                    IdAusstattung = w.IdAusstattung,
                    Reihenfolge = wohnungen.Count + 1
                });
            }
            return z with
            {
                IdGebaeude = a.IdGebaeude,
                IdTagesgangsatz = a.IdTagesgangsatz,
                PersonenJeWe = a.PersonenJeWe,
                WohnflaecheJeWeM2 = a.WohnflaecheJeWeM2,
                Topologie = (ZapfTopologie)(int)a.Topologie,
                Zirkulation = a.Zirkulation,
                Ferienbeginn = beginn,
                Ferienende = ende,
                Jahresmesswert = a.Jahresmesswert,
                JahresmesswertEinheit = a.JahresmesswertEinheit.HasValue
                    ? (ZapfMesswerteinheit)(int)a.JahresmesswertEinheit.Value : (ZapfMesswerteinheit?)null,
                JahresmesswertBilanzgrenze = a.JahresmesswertBilanzgrenze.HasValue
                    ? (ZapfBilanzgrenze)(int)a.JahresmesswertBilanzgrenze.Value : (ZapfBilanzgrenze?)null,
                JahresmesswertQuelle = string.IsNullOrWhiteSpace(a.JahresmesswertQuelle) ? null : a.JahresmesswertQuelle.Trim(),
                JahresmesswertZeitraum = string.IsNullOrWhiteSpace(a.JahresmesswertZeitraum) ? null : a.JahresmesswertZeitraum.Trim(),
                SpeicherverlustKwhJeJahr = a.SpeicherverlustKwhJeJahr,
                TagesbedarfAuto = a.TagesbedarfAuto,
                TagesbedarfManuellKwh = a.TagesbedarfManuellKwh,
                BedarfSpezKwhJeEinheitTag = a.BedarfSpezKwhJeEinheitTag,
                ZapftemperaturC = a.ZapftemperaturC,
                KaltwasserMittelC = a.KaltwasserMittelC,
                KaltwasserAmplitudeK = a.KaltwasserAmplitudeK,
                Auslastung = auslastung,
                Wohnungen = wohnungen.AsReadOnly()
            };
        }

        /// <summary>
        /// Ein Jahrestag des Kerns (1 … 365) als Tag und Monat des Rechenjahrs ohne Schaltjahr;
        /// <c>null</c>, 0, 366 und alles außerhalb = keine Angabe.
        /// </summary>
        internal static (int? Tag, int? Monat) AlsDatum(int? jahrestag)
        {
            if (!jahrestag.HasValue || jahrestag.Value < 1 || jahrestag.Value > Zapfkalender.TAGE) return (null, null);
            (int tag, int monat) = TagUndMonat(jahrestag.Value);
            return (tag, monat);
        }

        /// <summary>
        /// Tag und Monat als Jahrestag des Rechenjahrs (1 … 365, kein Schaltjahr); fehlt eines von
        /// beiden oder gibt es den Tag nicht (30. Februar), keine Angabe (<c>null</c>) — die
        /// Pflichtprüfung nennt den Zeitraum vorher.
        /// </summary>
        internal static int? Jahrestag(int? tag, int? monat)
        {
            if (!tag.HasValue || !monat.HasValue) return null;
            if (monat.Value < 1 || monat.Value > 12 || tag.Value < 1 || tag.Value > Zapfkalender.TageJeMonat[monat.Value - 1])
                return null;
            int vorher = 0;
            for (int m = 0; m < monat.Value - 1; m++) vorher += Zapfkalender.TageJeMonat[m];
            return vorher + tag.Value;
        }

        /// <summary>Die gebäudeweiten Größen einer Projektzeile als DTO; <c>null</c> ohne Projektzeile.</summary>
        internal static ZapfprofilGebaeudeDaten AlsGebaeude(ProjektStand p)
            => p == null ? null : new ZapfprofilGebaeudeDaten
            {
                ZirkAuto = p.ZirkAuto,
                // Wertgetreu hin und zurück — eine Zahl außerhalb der Wertemenge lehnt der Schreibweg benannt ab.
                ZirkMethode = (ZapfprofilZirkulationsmethode)(int)p.ZirkMethode,
                ZirkLage = p.ZirkLage.HasValue ? (ZapfprofilLeitungslage)(int)p.ZirkLage.Value : (ZapfprofilLeitungslage?)null,
                ZirkLaengeM = p.ZirkLaengeM,
                ZirkVerlustWJeM = p.ZirkVerlustWJeM,
                ZirkAnteil = p.ZirkAnteil,
                ZirkKennwert = p.ZirkKennwert,
                ZirkFlaecheM2 = p.ZirkFlaecheM2,
                ZirkLaufzeitH = p.ZirkLaufzeitH,
                ZirkManuellKw = p.ZirkManuellKw,
                LeitungsinhaltL = p.LeitungsinhaltL,
                LadeAuto = p.LadeAuto,
                LadeManuellKw = p.LadeManuellKw,
                LadefensterH = p.LadefensterH,
                LadefensterBeginnH = p.LadefensterBeginnH,
                SpeicherC = p.SpeicherC,
                KaltwasserAuslegungC = p.KaltwasserAuslegungC
            };

        /// <summary>
        /// Die Projektgrößen mit den gebäudeweiten Größen des Dialogs (<c>null</c> = die Projektzeile
        /// bleibt). Weicht nichts ab, bleibt die Projektzeile dieselbe Instanz; ohne Projektzeile
        /// entsteht eine aus den Vorgaben der DDL nur, wenn der Dialog etwas anderes will als sie
        /// (Muster <see cref="MitStochastik"/>).
        /// </summary>
        internal static ProjektStand MitGebaeude(ProjektStand p, ZapfprofilGebaeudeDaten g)
        {
            if (g == null) return p;
            if (p == null)
            {
                ProjektStand vorgabe = ZapfprofilCtrl.ProjektVorgabe();
                if (vorgabe == null) return null;
                ProjektStand neu = Gebaeudegroessen(vorgabe, g);
                return neu == vorgabe ? null : neu;
            }
            ProjektStand mit = Gebaeudegroessen(p, g);
            return mit == p ? p : mit;
        }

        private static ProjektStand Gebaeudegroessen(ProjektStand p, ZapfprofilGebaeudeDaten g) => p with
        {
            ZirkAuto = g.ZirkAuto,
            ZirkMethode = (ZapfZirkulationsmethode)(int)g.ZirkMethode,
            ZirkLage = g.ZirkLage.HasValue ? (ZapfLeitungslage)(int)g.ZirkLage.Value : (ZapfLeitungslage?)null,
            ZirkLaengeM = g.ZirkLaengeM,
            ZirkVerlustWJeM = g.ZirkVerlustWJeM,
            ZirkAnteil = g.ZirkAnteil,
            ZirkKennwert = g.ZirkKennwert,
            ZirkFlaecheM2 = g.ZirkFlaecheM2,
            ZirkLaufzeitH = g.ZirkLaufzeitH,
            ZirkManuellKw = g.ZirkManuellKw,
            LeitungsinhaltL = g.LeitungsinhaltL,
            LadeAuto = g.LadeAuto,
            LadeManuellKw = g.LadeManuellKw,
            LadefensterH = g.LadefensterH,
            LadefensterBeginnH = g.LadefensterBeginnH,
            SpeicherC = g.SpeicherC,
            KaltwasserAuslegungC = g.KaltwasserAuslegungC
        };

        /// <summary>
        /// Der Arbeitsstand des Dialogs für den Kern. Je Zone gilt als Grundlage: die Zone
        /// derselben Id im <paramref name="basis"/>-Stand, sonst die Vorlage eines Duplikats
        /// (mit neuen Ids), sonst eine neue Zone mit den Vorgaben. Gesetzt werden die Felder der
        /// Stufe Einfach, die Reihenfolge und — trägt die Zone sie — die Angaben der höheren Stufen
        /// (<see cref="MitAngaben"/>); ohne Angaben bleibt alles Übrige der Grundlage. Die
        /// gebäudeweiten Größen gehen nach der Auslegung in die Projektzeile: Der Dialog hat sie mit
        /// dem OK der Überlagerung angeglichen, sie sind der jüngere Stand.
        /// </summary>
        internal static ZapfprofilStand AlsStand(ZapfprofilEingabeDaten eingabe, ZapfprofilStand basis)
        {
            if (eingabe == null) throw new ArgumentNullException(nameof(eingabe));
            IReadOnlyList<ZonenStand> alt = basis?.Zonen ?? new ZonenStand[0];

            var zonen = new List<ZonenStand>();
            for (int i = 0; i < eingabe.Zonen.Count; i++)
            {
                ZapfprofilZoneDaten d = eingabe.Zonen[i];
                ZonenStand grund = d.Id != 0 ? alt.FirstOrDefault(z => z.Id == d.Id) : null;
                if (grund == null && d.IdVorlage != 0)
                {
                    ZonenStand vorlage = alt.FirstOrDefault(z => z.Id == d.IdVorlage);
                    if (vorlage != null)
                        grund = vorlage with
                        {
                            Id = 0,
                            Ferienbeginn = (int?[])vorlage.Ferienbeginn?.Clone() ?? new int?[4],
                            Ferienende = (int?[])vorlage.Ferienende?.Clone() ?? new int?[4],
                            Auslastung = (double?[])vorlage.Auslastung?.Clone() ?? new double?[12],
                            Wohnungen = (vorlage.Wohnungen ?? new WohnungstypStand[0])
                                        .Select(w => w with { Id = 0 }).ToArray()
                        };
                }
                grund ??= new ZonenStand();

                ZonenStand zone = grund with
                {
                    Id = grund.Id,
                    Name = (d.Name ?? "").Trim(),
                    IdNutzungsart = d.IdNutzungsart,
                    Bezugsmenge = d.Bezugsmenge ?? 0.0,
                    Niveau = (ZapfNiveau)(int)d.Niveau,
                    Reihenfolge = i + 1
                };
                // Die Angaben der Stufen Erweitert und Experte (Z4): Trägt die Zone sie, gelten sie —
                // bei einem Duplikat ohne Ids der Vorlage (neue Wohnungstypen).
                if (d.Angaben != null)
                {
                    zone = MitAngaben(zone, d.Angaben);
                    if (zone.Id == 0) zone = zone with { Wohnungen = zone.Wohnungen.Select(w => w with { Id = 0 }).ToArray() };
                }
                zonen.Add(zone);
            }

            // Die Stochastik der Jahresreihe (Z3, Stufe Experte): Rechenweg, Seed, Realisierungen —
            // was der Dialog nicht ändert, bleibt die Projektzeile der Basis.
            ProjektStand projekt = MitStochastik(basis?.Projekt, eingabe);

            // Die Wahl des Typtagwegs (Z4b, Stufe Experte): Typtagweg ja/nein, Klimazone und
            // Gebaeudeart - eine Groesse des Projekts; die eingespielten Werte selbst reisen nie.
            projekt = MitTyptagwahl(projekt, eingabe);

            // Die Auslegung (Z2): Mit OK der Überlagerung trägt der Arbeitsstand ihre Eingaben samt
            // Punkt — sie gehen in die Projektgrößen, ein konstruierter Tag als Entwurf mit; ohne
            // sie bleiben Projektgrößen und Entwurf der Basis, wie sie sind.
            BedarfstagKatalogzeile entwurf = basis?.BedarfstagEntwurf;
            IReadOnlyList<KonstruktorzeileStand> zeilen = basis?.Konstruktorzeilen ?? new KonstruktorzeileStand[0];
            if (eingabe.Auslegung != null)
            {
                projekt = MitAuslegung(projekt ?? ZapfprofilCtrl.ProjektVorgabe(), eingabe.Auslegung);
                entwurf = EntwurfAus(eingabe.Auslegung);
                zeilen = Konstruktorzeilen(eingabe.Auslegung);
            }
            // Die gebäudeweiten Größen der Stufen Erweitert und Experte (Z4) — nach der Auslegung:
            // Ladeleistung, Ladefenster und Speichertemperatur hat der Dialog mit ihrem OK angeglichen.
            projekt = MitGebaeude(projekt, eingabe.Gebaeude);
            // Haben sich die Zonen nach der Übernahme geändert, ist der Punkt überholt: verworfen,
            // nicht gespeichert — auch ein Punkt, den schon der Stand beim Öffnen trug.
            if (eingabe.PunktUeberholt) projekt = OhnePunkt(projekt);
            // Die Laufangaben der Anzeige (N9 (h)) gehen mit dem Arbeitsstand, nicht in die Datenbank.
            ZapfAnzeige anzeige = eingabe.AnzeigetemperaturC.HasValue || eingabe.SchwelleKw.HasValue
                ? new ZapfAnzeige(eingabe.AnzeigetemperaturC, eingabe.SchwelleKw) : null;
            return new ZapfprofilStand(AlsWeg(eingabe.Weg), zonen.AsReadOnly(), projekt)
            {
                BedarfstagEntwurf = entwurf,
                Konstruktorzeilen = zeilen,
                Anzeige = anzeige
            };
        }

        /// <summary>
        /// Das Ergebnis des Dialogs als Stand des Kerns: Das OK des Zapfprofils stellt die Weiche
        /// auf den Generator (ZU4); zurück stellt nur die Optionsgruppe des Bedarfsprofil-Dialogs.
        /// </summary>
        internal static ZapfprofilStand Uebernahme(ZapfprofilErgebnisDaten ergebnis, ZapfprofilStand basis)
        {
            if (ergebnis == null) throw new ArgumentNullException(nameof(ergebnis));
            ZapfprofilEingabeDaten e = ergebnis.Eingabe.Kopie();
            e.Weg = ZapfprofilWeg.Generator;
            return AlsStand(e, basis);
        }

        /// <summary>
        /// Wie viele Größen der höheren Stufen eine Zone überschreibt — gezählt an EINER Stelle,
        /// den Angaben des DTO (<see cref="ZapfprofilZonenangabenDaten.Ueberschrieben"/>): jede nullbare
        /// Größe mit Wert, jeder Schalter abseits seiner Vorgabe, jeder Ferienzeitraum und jeder
        /// Auslastungsmonat mit Wert, eine gepflegte Wohnungstabelle. Die Bindung an ein Gebäude ist
        /// keine Überschreibung; ein Ferientag 0 oder 366 ist keine Angabe.
        /// </summary>
        internal static int Ueberschrieben(ZonenStand z) => z == null ? 0 : AlsAngaben(z).Ueberschrieben();

        // =================================================================================
        // Katalog
        // =================================================================================

        /// <summary>Die Nutzungsarten des Katalogs als DTO — ohne Tabellen eine leere Liste.</summary>
        internal static List<ZapfprofilNutzungsartDaten> Katalog()
            => ZapfprofilCtrl.Katalog().Select(AlsNutzungsart).ToList();

        /// <summary>Eine Nutzungsart als DTO: Herkunft als Kurztext, nie ein Beleg (5.3).</summary>
        internal static ZapfprofilNutzungsartDaten AlsNutzungsart(Nutzungsart n)
        {
            bool vollstaendig = n.Tagesgaenge != null && n.Tagesgaenge.Vollstaendig;
            return new ZapfprofilNutzungsartDaten
            {
                Id = n.Id,
                Name = n.Name ?? "",
                Bezugsart = (int)n.Bezug,
                Bezugsgroesse = Bezugsgroesse(n.Bezug),
                Einheit = Einheit(n.Bezug),
                BedarfJeNiveauKwhJeEinheitTag = (double[])(n.BedarfJeNiveauKwhJeEinheitTag ?? new double[3]).Clone(),
                Herkunft = Herkunft(n.Herkunft?.Bedarf),
                Status = Status(n.Status),
                Katalogversion = n.Katalogversion ?? "",
                Auslieferung = n.ReadOnly,
                Waehlbar = vollstaendig,
                Sperrgrund = vollstaendig ? "" : Text_("ZPG_KAT_SPERRE_TAGESGANG", "Der Tagesgangsatz dieser Nutzungsart ist unvollständig."),
                // Die Vorgaben der höheren Stufen (Z4) — Bezug und Faktoren des Katalogs, nie ein Beleg.
                Kalenderart = (int)n.Kalender,
                Kalender = Kalendername(n.Kalender),
                // Führt die Nutzungsart eine Wohnungstabelle? EIN Kriterium mit dem Kern
                // (Mengengeruest.WohnungstabelleWirksam) — die Bezugsart allein, nicht der
                // Kalender: Andernfalls wäre eine Wohnungstabelle bei abweichendem Kalender
                // verdeckt, obwohl der Kern sie noch rechnet und prüft (Z4, Gruppe 2a Punkt 6).
                Wohnen = Mengengeruest.WohnungstabelleWirksam(n.Bezug),
                Bilanzgrenze = (int)n.Grenze,
                ZapftemperaturC = n.Bezugstemperaturen?.ZapftemperaturC,
                Monatsfaktoren = (double[])(n.Monatsfaktoren ?? new double[NutzungsartRaster.MONATE]).Clone(),
                IdTagesgangsatz = n.Tagesgaenge?.Id
            };
        }

        /// <summary>Die Kalenderart als Text („Wohnen") — Schlüssel <c>ZPG_KALENDER_…</c>.</summary>
        internal static string Kalendername(ZapfKalenderart art)
            => Text_("ZPG_KALENDER_" + Gross(art.ToString()), art.ToString());

        /// <summary>Die Herkunft als Kurztext: Art, Quelle und Ausgabe — nie Zahlen, nie ein Beleg.</summary>
        internal static string Herkunft(Provenienz p)
        {
            if (p == null) return Text_("ZPG_HERKUNFT_OHNE", "ohne Angabe");
            var teile = new List<string> { HerkunftsartText(p.Art) };
            if (!string.IsNullOrWhiteSpace(p.Quelle))
                teile.Add(string.IsNullOrWhiteSpace(p.Ausgabe) ? p.Quelle.Trim() : p.Quelle.Trim() + ", " + p.Ausgabe.Trim());
            return string.Join(TRENNER, teile);
        }

        private static string HerkunftsartText(Herkunftsart art)
        {
            switch (art)
            {
                case WindowsFormsApplication1.Herkunftsart.Verfahren: return Text_("ZPG_HERKUNFT_VERFAHREN", "Verfahren");
                case WindowsFormsApplication1.Herkunftsart.Eigenkonstruktion: return Text_("ZPG_HERKUNFT_EIGENKONSTRUKTION", "Eigenkonstruktion");
                case WindowsFormsApplication1.Herkunftsart.Frei: return Text_("ZPG_HERKUNFT_FREI", "frei verfügbar");
                case WindowsFormsApplication1.Herkunftsart.Import: return Text_("ZPG_HERKUNFT_IMPORT", "Import");
                case WindowsFormsApplication1.Herkunftsart.Fiktiv: return Text_("ZPG_HERKUNFT_FIKTIV", "fiktiv (Testdaten)");
                default: return Text_("ZPG_HERKUNFT_OHNE", "ohne Angabe");
            }
        }

        private static string Status(ZapfKatalogstatus status)
        {
            switch (status)
            {
                case ZapfKatalogstatus.Auslieferung: return Text_("ZPG_STATUS_AUSLIEFERUNG", "Auslieferung");
                case ZapfKatalogstatus.Import: return Text_("ZPG_STATUS_IMPORT", "Import");
                default: return Text_("ZPG_STATUS_EIGEN", "eigen");
            }
        }

        /// <summary>Die Bezugsart als Text („Wohneinheiten") — Schlüssel <c>ZPG_BEZUG_…</c>.</summary>
        internal static string Bezugsgroesse(ZapfBezugsart art)
            => Text_("ZPG_BEZUG_" + Gross(art.ToString()), art.ToString());

        /// <summary>Die Einheit je Bezug als Kurztext („WE") — Schlüssel <c>ZPG_EINHEIT_…</c>.</summary>
        internal static string Einheit(ZapfBezugsart art)
            => Text_("ZPG_EINHEIT_" + Gross(art.ToString()), art.ToString());

        // =================================================================================
        // Vorschau
        // =================================================================================

        /// <summary>
        /// Die Vorschau zu einem Arbeitsstand (5.1: „live über den deterministischen Pfad"): immer
        /// über den Generatorweg, auch wenn die Weiche noch auf den Bestandsprofilen steht —
        /// sie zeigt, was das Zapfprofil rechnen würde —, und immer deterministisch, auch bei
        /// Rechenweg „stochastisch" (<see cref="BedarfsVorschauCtrl.ProjektVorschau"/> zieht kein
        /// Jahresensemble; die stochastische Reihe entsteht erst im Lauf). Ohne Zone keine
        /// Rechnung; kann der Generator für das Projekt nicht rechnen, der benannte Grund.
        /// </summary>
        internal static ZapfprofilVorschauDaten Vorschau(int idProjekt, ZapfprofilEingabeDaten eingabe,
                                                         ZapfprofilStand basis)
        {
            if (eingabe == null || eingabe.Zonen.Count == 0)
                return OhneVorschau(ZapfprofilVorschauZustand.NichtGerechnet, "ZPG_MSG_KEINE_ZONE",
                                    Text_("ZPG_MSG_KEINE_ZONE", "Es ist keine Zone angelegt."), "");

            ZapfVerfuegbarkeit verfuegbar = ZapfprofilCtrl.Verfuegbar();
            if (!verfuegbar.Ja)
                return OhneVorschau(ZapfprofilVorschauZustand.Abgebrochen, VerfuegbarkeitsKennung(verfuegbar),
                                    Verfuegbarkeitsgrund(verfuegbar), verfuegbar.Klartext);

            var projekt = new ProjektCtrl();
            projekt.ReadSingle(idProjekt);
            if (projekt.m_ID_Klimaregion <= 0)
                return OhneVorschau(ZapfprofilVorschauZustand.Abgebrochen, "ZPG_MSG_KEINE_KLIMAREGION",
                                    Text_("ZPG_MSG_KEINE_KLIMAREGION", "Das Projekt hat keine Klimaregion — ohne Kalender keine Vorschau."), "");

            BedarfsVorschau v;
            ZapfprofilStand stand = AlsStand(eingabe, basis) with { Weg = BrauchwasserWeg.Generator };
            try
            {
                v = BedarfsVorschauCtrl.ProjektVorschau(BedarfsArt.Brauchwasser, idProjekt, null, stand);
            }
            catch (Exception ex) { return Unerwartet(ex.Message); }

            if (v == null || !v.Erfolgreich || v.Waerme?.Zapfprofil == null)
                return Unerwartet(OhnePraefix(v?.Meldung ?? v?.Waerme?.Fehlertext ?? ""));

            try
            {
                return AlsVorschau(v.Waerme.Zapfprofil, v.Waerme.WochentagJan1, v.Waerme.WochenendkennzeichenKopie(), eingabe);
            }
            catch (Exception ex) { return Unerwartet(ex.Message); }
        }

        /// <summary>
        /// <b>Die stochastische Jahresreihe</b> zum Arbeitsstand (4.4; „Stochastisch rechnen" des
        /// Dialogs, 5.1): derselbe Generatorweg wie der Lauf (<see cref="ZapfprofilCtrl.Rechnen(int, ZapfprofilStand, int, bool[], CancellationToken)"/>)
        /// mit dem Rechenweg, dem Seed und den Realisierungen des Arbeitsstands — in der Bilanz das
        /// Jahr zum Seed, je Zone die Konsistenzprobe aus den R Jahren. Läuft nebenläufig (der
        /// Delegat <c>Jahresreihe</c> legt sie auf einen Arbeitsfaden); <paramref name="abbruch"/>
        /// beendet sie mit <see cref="OperationCanceledException"/>, jede andere Ablehnung kommt
        /// benannt als Zustand <see cref="ZapfprofilVorschauZustand.Abgebrochen"/> zurück — auch die
        /// Schranke der Einheitentage, mit Kennung und Werten in der Oberflächensprache.
        /// </summary>
        internal static ZapfprofilVorschauDaten Jahresreihe(int idProjekt, ZapfprofilEingabeDaten eingabe, ZapfprofilStand basis,
                                                            CancellationToken abbruch)
        {
            if (eingabe == null || eingabe.Zonen.Count == 0)
                return OhneJahresreihe(ZapfprofilVorschauZustand.NichtGerechnet, "ZPG_MSG_KEINE_ZONE",
                                       Text_("ZPG_MSG_KEINE_ZONE", "Es ist keine Zone angelegt."), "");

            ZapfVerfuegbarkeit verfuegbar = ZapfprofilCtrl.Verfuegbar();
            if (!verfuegbar.Ja)
                return OhneJahresreihe(ZapfprofilVorschauZustand.Abgebrochen, VerfuegbarkeitsKennung(verfuegbar),
                                       Verfuegbarkeitsgrund(verfuegbar), verfuegbar.Klartext);
            if (!ZapfprofilCtrl.KalenderLesen(idProjekt, out int jan1, out bool[] we))
                return OhneJahresreihe(ZapfprofilVorschauZustand.Abgebrochen, "ZPG_MSG_KEINE_KLIMAREGION",
                                       Text_("ZPG_MSG_KEINE_KLIMAREGION", "Das Projekt hat keine Klimaregion — ohne Kalender keine Vorschau."), "");

            ZapfprofilStand stand = AlsStand(eingabe, basis) with { Weg = BrauchwasserWeg.Generator };
            ZapfprofilErgebnis e;
            try
            {
                e = ZapfprofilCtrl.Rechnen(idProjekt, stand, jan1, we, abbruch);
            }
            catch (OperationCanceledException) { throw; }
            catch (ZapfprofilEingabeException ex)
            {
                return OhneJahresreihe(ZapfprofilVorschauZustand.Abgebrochen, Satzkennung(ex.Satz, "ZPG_MSG_JAHRESREIHE_UNERWARTET"),
                                       Satztext(ex.Satz), ex.Message);
            }
            catch (ParametersatzException ex)
            {
                return OhneJahresreihe(ZapfprofilVorschauZustand.Abgebrochen, Satzkennung(ex.Satz, "ZPG_MSG_JAHRESREIHE_UNERWARTET"),
                                       Satztext(ex.Satz), ex.Message);
            }
            catch (Exception ex) { return JahresreiheUnerwartet(ex.Message); }

            try
            {
                ZapfprofilVorschauDaten d = AlsVorschau(e, jan1, we, eingabe);
                if (d.Stochastisch)
                {
                    // Derselbe Seed und dieselbe Zahl der Jahre wie im Eingang der Rechnung.
                    ProjektStand p = stand.Projekt ?? ZapfprofilCtrl.ProjektVorgabe();
                    d.Seed = p?.Seed;
                    d.Realisierungen = p?.Realisierungen;
                    d.Status = Format(Text_("ZPG_STATUS_VORSCHAU_STOCHASTISCH", "Stochastik gerechnet · Seed {0} · {1} Jahre"),
                                      d.Seed ?? 0, d.Realisierungen ?? 0);
                }
                return d;
            }
            catch (Exception ex) { return JahresreiheUnerwartet(ex.Message); }
        }

        /// <summary>Die Jahresreihe ohne Ergebnis: Zustand, Grund und Meldung wie die Vorschau, der Status nennt die Stochastik.</summary>
        private static ZapfprofilVorschauDaten OhneJahresreihe(ZapfprofilVorschauZustand zustand, string kennung, string grund,
                                                              string klartext)
        {
            ZapfprofilVorschauDaten d = OhneVorschau(zustand, kennung, grund, klartext);
            d.Status = Format(Text_("ZPG_STATUS_OHNE_JAHRESREIHE", "Stochastik nicht gerechnet — {0}"), grund ?? "");
            return d;
        }

        private static ZapfprofilVorschauDaten JahresreiheUnerwartet(string klartext)
            => OhneJahresreihe(ZapfprofilVorschauZustand.Abgebrochen, "ZPG_MSG_JAHRESREIHE_UNERWARTET",
                   Format(Text_("ZPG_MSG_JAHRESREIHE_UNERWARTET", "Die Jahresreihe konnte nicht gerechnet werden: {0}"), klartext ?? ""),
                   klartext);

        /// <summary>
        /// Das Ergebnis des Generators als Vorschau-DTO: die Summe und je Zone eine Ansicht, die
        /// Jahreswerte je Zone, die Meldungen und der Statustext. Die Tagtypen einer Zone sind ihr
        /// wirksamer Kalender aus dem Kern (<see cref="ZonenErgebnis.Kalender"/>, samt Ferien);
        /// die Summe mittelt über den Kalender der Klimaregion ohne die Ruhetage irgendeiner Zone
        /// (<see cref="Zapfauswertung.OhneRuhetage"/>). Ferientage gehen in keinen Tagesgang ein.
        /// </summary>
        internal static ZapfprofilVorschauDaten AlsVorschau(ZapfprofilErgebnis e, int wochentagJan1, bool[] we,
                                                            ZapfprofilEingabeDaten eingabe)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            ZapfTagtyp[] grund = Zapfkalender.Bilden(wochentagJan1, we, null);
            ZapfTagtyp[] kalender = Zapfauswertung.OhneRuhetage(grund, e.JeZone.Where(z => !z.Abgelehnt).Select(z => z.Kalender));
            ZapfprofilBildtexte bildtexte = Bildtexte();
            IReadOnlyDictionary<int, string> einheiten = Katalogeinheiten();

            var vorschau = new ZapfprofilVorschauDaten
            {
                Zustand = ZapfprofilVorschauZustand.Gerechnet,
                Stochastisch = e.Stochastisch,
                Status = Text_("ZPG_STATUS_VORSCHAU", "Vorschau aktuell · deterministisch · Stochastik noch nicht gerechnet")
            };

            string summe = Text_("ZPG_ANSICHT_SUMME", "Summe aller Zonen");
            ZapfprofilAnsichtDaten gesamt = Ansicht(0, summe, false, e.Zapfung, e.Zirkulation, kalender, wochentagJan1, bildtexte);
            gesamt.Kennzahlen = Kennzahlen(e.Kennzahlen);
            gesamt.Dauerlinie = AlsDauerlinie(e.Dauerlinie, bildtexte);
            vorschau.Ansichten.Add(gesamt);
            vorschau.Zirkulation = AlsSchaetzhilfe(e.SchaetzhilfeZirkulation);

            for (int i = 0; i < e.JeZone.Count; i++)
            {
                ZonenErgebnis z = e.JeZone[i];
                ZapfprofilZoneDaten d = eingabe != null && i < eingabe.Zonen.Count ? eingabe.Zonen[i] : null;
                string name = string.IsNullOrEmpty(z.Zone) ? d?.Name ?? "" : z.Zone;

                ZapfprofilAnsichtDaten a = Ansicht(z.IdZone, name, z.Abgelehnt, z.Zapfung, z.Zirkulation,
                                                   z.Kalender ?? grund, wochentagJan1, bildtexte);
                a.Kennzahlen = Kennzahlen(z, d != null && einheiten.TryGetValue(d.IdNutzungsart, out string eh) ? eh : "");
                if (!z.Abgelehnt)
                {
                    a.Tagesbedarf = AlsSchaetzhilfe(z.SchaetzhilfeTagesbedarf);
                    a.Auslastung = AlsAuslastung(z.Auslastung);
                    a.Auslastungsgang = AlsAuslastungsgang(z.Auslastungsgang);
                    a.Dauerlinie = AlsDauerlinie(Zapfauswertung.Dauerlinie(z.Zapfung, z.Zirkulation, e.Dauerlinie?.SchwelleKw), bildtexte);
                }
                // Die Konsistenzprobe der stochastischen Jahresreihe (4.4): an der Zone und in der Summe.
                if (!z.Abgelehnt && z.Konsistenz != null)
                {
                    ZapfprofilKonsistenzDaten probe = Konsistenz(z.Konsistenz, name, i);
                    a.Konsistenzen.Add(probe);
                    gesamt.Konsistenzen.Add(probe);
                }
                vorschau.Ansichten.Add(a);

                double summeZapfung = e.Kennzahlen?.JahresbedarfZapfungKwh ?? 0.0;
                vorschau.Zonen.Add(new ZapfprofilZonenwertDaten
                {
                    IdZone = z.IdZone,
                    Position = i,
                    Zone = name,
                    JahresbedarfZapfungKwh = z.JahresbedarfZapfungKwh,
                    Abgelehnt = z.Abgelehnt,
                    // Die Spalten der Zonenliste ab Erweitert (Z4): wirksame Menge (bei Wohnungstabelle aus
                    // ihr), Anteil an der Zapfung und wie die Menge entsteht — alles aus dem Kern.
                    BezugsmengeWirksam = z.Abgelehnt || !(z.Bezugsmenge > 0) ? null : z.Bezugsmenge,
                    Anteil = summeZapfung > 0 ? z.JahresbedarfZapfungKwh / summeZapfung : (double?)null,
                    Rechenweg = z.Abgelehnt ? ZapfprofilZonenrechenweg.Abgelehnt
                              : z.SchaetzhilfeTagesbedarf?.Kalibriert == true || z.Kalibrierfaktor.HasValue ? ZapfprofilZonenrechenweg.Messwert
                              : z.SchaetzhilfeTagesbedarf?.IstManuell == true ? ZapfprofilZonenrechenweg.Manuell
                              : ZapfprofilZonenrechenweg.Katalog
                });
            }

            // Die Meldungen tragen die Position ihrer Zone, wo der Name sie eindeutig bestimmt —
            // der Dialog ordnet sie darüber zu, nicht über den Namen.
            IReadOnlyDictionary<string, int> position = EindeutigePositionen(e.JeZone.Select(z => z.Zone));
            foreach (ZapfAblehnung a in e.Ablehnungen) vorschau.Meldungen.Add(MitPosition(Meldung(a), position));
            foreach (ZapfHinweis h in e.Hinweise) vorschau.Meldungen.Add(MitPosition(Meldung(h), position));
            // Die Warnliste der Bilanz (Warnlogik Z4, N9 (g)): jeder Hinweis mit Titel und Stufe.
            foreach (ZapfHinweis h in e.Hinweise) vorschau.Warnliste.Add(Warnung(h));
            // Das Herkunftsprotokoll (Karte "Herkunft", N19) in der Reihenfolge des Rechenwegs.
            vorschau.Herkunft.AddRange(Herkunftszeilen(e.Herkunft));
            return vorschau;
        }

        /// <summary>
        /// Die Kennungen der Hinweise des Bilanzrechenwegs — je eine Ressource <c>ZPG_WARN_…</c> als
        /// Titel der Warnliste (Stufe Z4). Die Wache hält die Liste gegen den Quelltext des Kerns.
        /// </summary>
        internal static readonly string[] BILANZHINWEISE =
        {
            ZapfHinweis.PARAMETER_FEHLT, "TAGESGANG_LEER", "TAGESGANG_SUMME", "WOCHENFAKTOREN_SUMME",
            Mengengeruest.HINWEIS_BANDBREITE, Mengengeruest.HINWEIS_WOHNUNGSTABELLE, "MESSWERT_SPEICHERVERLUST",
            "MESSWERT_ABWEICHUNG", ZapfprofilRechner.HINWEIS_NETZVERLUST, ZapfprofilRechner.HINWEIS_ZIRKULATION_GROSS,
            ZapfprofilRechner.HINWEIS_STOCHASTISCH, ZapfprofilRechner.HINWEIS_ENERGIEPROBE, "ZIRKULATION_ZONE_OHNE_FLAECHE",
            "ZIRKULATION_OHNE_FLAECHE", "ZIRKULATION_OHNE_ZONE", "ZIRKULATION_NICHT_IN_Z1", ZapfprofilCtrl.HINWEIS_EINSTELLUNG_UNGUELTIG,
            ZapfprofilRechner.HINWEIS_ANZEIGETEMPERATUR, ZapfprofilRechner.HINWEIS_STUNDENSCHWELLE,
            // Stufe Z4b: der Jahresgang über die eingespielten Typtage (N14).
            Typtagzuordnung.HINWEIS_ANZAHL, Typtagzuordnung.HINWEIS_FAKTOR_NULL, Typtagzuordnung.HINWEIS_SKALIERUNG,
            Typtagzuordnung.HINWEIS_FERIEN, Typtagzuordnung.HINWEIS_OHNE_GANG
        };

        /// <summary>Ein Hinweis der Bilanz als Eintrag der Warnliste: Titel aus <c>ZPG_WARN_…</c>, sonst „Hinweis"; Satz des Kerns; Stufe nach der Warnlogik.</summary>
        internal static ZapfprofilWarnDaten Warnung(ZapfHinweis h)
        {
            string kennung = "ZPG_WARN_" + (h?.Code ?? "");
            string titel = Text_(kennung, null) ?? Text_("ZPG_AUS_HINWEIS", "Hinweis");
            return new ZapfprofilWarnDaten(kennung, titel, Satztext(h?.Satz),
                                           h != null && h.Warnung ? ZapfprofilWarnstufe.Warnung : ZapfprofilWarnstufe.Hinweis);
        }

        /// <summary>Eine Schätzhilfe des Kerns als DTO — Vorschlag <c>null</c> ohne Verfahren, Rechenweg in der Oberflächensprache.</summary>
        internal static ZapfprofilSchaetzhilfeDaten AlsSchaetzhilfe(Schaetzhilfe s)
            => s == null ? null : new ZapfprofilSchaetzhilfeDaten
            {
                Auto = s.Auto,
                Vorschlag = s.HatVorschlag ? s.Vorschlag : (double?)null,
                Manuell = s.Manuell,
                Angesetzt = s.Angesetzt,
                Kalibriert = s.Kalibriert,
                Einheit = s.Einheit ?? "",
                Rechenweg = Satztext(s.Rechenweg)
            };

        private static ZapfprofilAuslastungDaten AlsAuslastung(Zapfauslastung a)
            => a == null ? null : new ZapfprofilAuslastungDaten
            {
                Monate = a.Monate.ToArray(), Wochentage = a.Wochentage.ToArray(), Stunden = a.Stunden.ToArray()
            };

        private static ZapfprofilAuslastungsgangDaten AlsAuslastungsgang(Auslastungsgang g)
            => g == null ? null : new ZapfprofilAuslastungsgangDaten
            {
                Wirksam = g.Wirksam.ToArray(), Katalog = g.Katalog.ToArray(), Ueberschrieben = g.JeMonatUeberschrieben.ToArray()
            };

        /// <summary>Die Dauerlinie des Kerns als DTO samt Bild (Perzentilmarken an ihrem Rang).</summary>
        internal static ZapfprofilDauerlinieDaten AlsDauerlinie(Zapfdauerlinie d, ZapfprofilBildtexte bild)
        {
            if (d == null) return null;
            var x = new ZapfprofilDauerlinieDaten
            {
                GesamtKw = d.GesamtKw.ToArray(),
                SchwelleKw = d.SchwelleKw,
                StundenUeberSchwelle = d.StundenUeberSchwelle
            };
            foreach (Dauerlinienmarke m in d.Marken) x.Marken.Add(new ZapfprofilDauerlinienmarkeDaten(m.Perzentil, m.LeistungKw, m.Rang));
            x.Modell = ZapfprofilBilder.DauerlinieModell(x.GesamtKw, x.Marken.Select(m => m.Perzentil).ToArray(),
                x.Marken.Select(m => m.Rang).ToArray(), x.Marken.Select(m => m.LeistungKw).ToArray(), null, null, bild);
            return x;
        }

        /// <summary>Die Konsistenzprobe des Kerns als DTO — Zahlen in ihrer Quelleneinheit, die Anzeige formatiert.</summary>
        internal static ZapfprofilKonsistenzDaten Konsistenz(Jahreskonsistenz k, string zone, int position)
            => new ZapfprofilKonsistenzDaten
            {
                Zone = zone ?? "",
                Position = position,
                DeterministischKwh = k.DeterministischKwh,
                MittelKwh = k.MittelKwh,
                StandardabweichungKwh = k.StandardabweichungKwh,
                Realisierungen = k.Realisierungen,
                ToleranzKwh = k.ToleranzKwh,
                Erfuellt = k.Erfuellt,
                Faktor = k.Faktor,
                Abweichung = k.Abweichung
            };

        /// <summary>Zonenname → Position, nur für Namen, die genau einmal vorkommen.</summary>
        private static IReadOnlyDictionary<string, int> EindeutigePositionen(IEnumerable<string> namen)
        {
            var d = new Dictionary<string, int>(StringComparer.Ordinal);
            var doppelt = new HashSet<string>(StringComparer.Ordinal);
            int i = 0;
            foreach (string n in namen)
            {
                string name = n ?? "";
                if (name.Length > 0 && !d.TryAdd(name, i)) doppelt.Add(name);
                i++;
            }
            foreach (string n in doppelt) d.Remove(n);
            return d;
        }

        private static ZapfprofilMeldung MitPosition(ZapfprofilMeldung m, IReadOnlyDictionary<string, int> position)
            => m.Zone.Length > 0 && position.TryGetValue(m.Zone, out int p) ? m with { Position = p } : m;

        private static ZapfprofilAnsichtDaten Ansicht(int idZone, string titel, bool abgelehnt, Bilanzreihe zapfung,
                                                     Bilanzreihe zirkulation, IReadOnlyList<ZapfTagtyp> kalender, int wochentagJan1,
                                                     ZapfprofilBildtexte bildtexte)
        {
            int monat = Zapfauswertung.GroessterMonat(zapfung);
            Tagesgangmittel tag = Zapfauswertung.Tagesgang(zapfung, zirkulation, monat, kalender);
            Wochenausschnitt woche = Zapfauswertung.Woche(zapfung, zirkulation, wochentagJan1);

            var a = new ZapfprofilAnsichtDaten
            {
                IdZone = idZone,
                Titel = titel ?? "",
                Abgelehnt = abgelehnt,
                Monat = monat,
                WerktagKw = tag.WerktagKw?.ToArray(),
                SamstagKw = tag.SamstagKw?.ToArray(),
                SonnFeiertagKw = tag.SonnFeiertagKw?.ToArray(),
                TageJeTagtyp = tag.TageJeTagtyp.ToArray(),
                ZirkulationTagKw = tag.ZirkulationKw.ToArray(),
                WochenStarttag = woche.Starttag,
                WocheZapfungKw = woche.ZapfungKw.ToArray(),
                WocheZirkulationKw = woche.ZirkulationKw.ToArray(),
                MonateZapfungKwh = zapfung.MonatssummenKwh.ToArray(),
                MonateZirkulationKwh = zirkulation.MonatssummenKwh.ToArray()
            };

            string monatsname = Monatsname(monat);
            a.TagesgangModell = ZapfprofilBilder.TagesgangModell(monatsname, a.WerktagKw, a.SamstagKw, a.SonnFeiertagKw,
                                                                 a.ZirkulationTagKw, bildtexte);
            a.WochenprofilModell = ZapfprofilBilder.WochenprofilModell(a.WocheZapfungKw, a.WocheZirkulationKw, bildtexte);
            a.JahresgangModell = ZapfprofilBilder.JahresgangModell(InMwh(a.MonateZapfungKwh), InMwh(a.MonateZirkulationKwh),
                                                                   Energieeinheit.MWh.Text, bildtexte);

            a.UnterschriftTagesgang = Format(Text_("ZPG_UNTERSCHRIFT_TAGESGANG",
                "Zapfung in kW (kWh je Stunde), {0}, {1}; Mittel der Tage je Tagtyp, ohne Ferientage."), a.Titel, monatsname);
            (int tagImMonat, int monatDerWoche) = TagUndMonat(woche.Starttag);
            a.UnterschriftWoche = Format(Text_("ZPG_UNTERSCHRIFT_WOCHE",
                "168 Wochenstunden ab {0}, {1}. {2} — die Woche mit dem größten Tagesbedarf des Jahres."),
                Wochentagsname(woche.WochentagStarttag), tagImMonat.ToString(CultureInfo.CurrentCulture),
                Monatsname(monatDerWoche));
            a.UnterschriftJahresgang = Text_("ZPG_UNTERSCHRIFT_JAHRESGANG",
                "Zwölf Monatssäulen in MWh, gestapelt aus Zapfung und Zirkulation.");
            return a;
        }

        /// <summary>Die Kennzahlen der Summe (4.6) — wie der Kern sie ausweist, in ihrer Quelleneinheit.</summary>
        internal static ZapfprofilKennzahlenDaten Kennzahlen(Zapfkennzahlen k)
        {
            if (k == null) return new ZapfprofilKennzahlenDaten();
            return new ZapfprofilKennzahlenDaten
            {
                JahresbedarfZapfungKwh = k.JahresbedarfZapfungKwh,
                JahresverlustZirkulationKwh = k.JahresverlustZirkulationKwh,
                JahresbedarfGesamtKwh = k.JahresbedarfGesamtKwh,
                Zirkulationsanteil = k.Zirkulationsanteil,
                TagesmittelZapfungKwh = k.TagesmittelZapfungKwh,
                ZapfungLiterJeTag = k.ZapfungLiterJeTag,
                GroessterStundenwertKw = k.GroessterStundenwertKw,
                VermerkGroessterStundenwert = Text_("ZPG_KZ_VERMERK_STUNDENWERT", "Bilanzwert, keine Auslegungsgröße"),
                VolllaststundenH = k.VolllaststundenH,
                StundenUeberSchwelle = k.StundenUeberSchwelle,
                SchwelleKw = k.SchwelleKw
            };
        }

        /// <summary>
        /// Die Kennzahlen einer Zone: was der Kern je Zone ausweist (Zapfung, Anteil der
        /// Zirkulation, spezifischer Wert, Liter). Größter Stundenwert und Volllaststunden weist
        /// er nur für die Summe aus — sie bleiben hier <c>null</c>.
        /// </summary>
        internal static ZapfprofilKennzahlenDaten Kennzahlen(ZonenErgebnis z, string einheit)
        {
            double gesamt = z.JahresbedarfZapfungKwh + z.JahresverlustZirkulationKwh;
            return new ZapfprofilKennzahlenDaten
            {
                JahresbedarfZapfungKwh = z.JahresbedarfZapfungKwh,
                JahresverlustZirkulationKwh = z.JahresverlustZirkulationKwh,
                JahresbedarfGesamtKwh = gesamt,
                Zirkulationsanteil = gesamt > 0 ? z.JahresverlustZirkulationKwh / gesamt : 0.0,
                TagesmittelZapfungKwh = z.JahresbedarfZapfungKwh / Zapfkalender.TAGE,
                ZapfungLiterJeTag = z.ZapfungLiterJeTag,
                SpezifischKwhJeEinheitJahr = z.Abgelehnt ? null : z.SpezifischKwhJeEinheitJahr,
                SpezifischEinheit = einheit ?? "",
                VermerkGroessterStundenwert = Text_("ZPG_KZ_VERMERK_STUNDENWERT", "Bilanzwert, keine Auslegungsgröße")
            };
        }

        private static ZapfprofilVorschauDaten OhneVorschau(ZapfprofilVorschauZustand zustand, string kennung,
                                                           string grund, string klartext)
        {
            var v = new ZapfprofilVorschauDaten
            {
                Zustand = zustand,
                Grund = grund,
                Status = Format(Text_("ZPG_STATUS_OHNE_VORSCHAU", "Keine Vorschau — {0}"), grund)
            };
            v.Meldungen.Add(new ZapfprofilMeldung(kennung, "", grund,
                zustand == ZapfprofilVorschauZustand.Abgebrochen ? ZapfprofilMeldungsart.Fehler : ZapfprofilMeldungsart.Hinweis,
                klartext ?? ""));
            return v;
        }

        private static ZapfprofilVorschauDaten Unerwartet(string klartext)
        {
            string grund = Format(Text_("ZPG_MSG_UNERWARTET", "Die Vorschau konnte nicht gerechnet werden: {0}"), klartext ?? "");
            return OhneVorschau(ZapfprofilVorschauZustand.Abgebrochen, "ZPG_MSG_UNERWARTET", grund, klartext);
        }

        /// <summary>Der Protokollvorsatz des Kerns fällt in der Oberfläche weg — der Dialog sagt schon, wo er ist.</summary>
        private static string OhnePraefix(string text)
        {
            text = text ?? "";
            return text.StartsWith(SimulationWaermebedarf.ZAPFPROFIL_PRAEFIX, StringComparison.Ordinal)
                ? text.Substring(SimulationWaermebedarf.ZAPFPROFIL_PRAEFIX.Length)
                : text;
        }

        private static IReadOnlyDictionary<int, string> Katalogeinheiten()
        {
            var d = new Dictionary<int, string>();
            foreach (Nutzungsart n in ZapfprofilCtrl.Katalog()) d[n.Id] = Einheit(n.Bezug);
            return d;
        }

        private static double[] InMwh(double[] kwh)
            => kwh?.Select(w => Energieeinheit.MWh.AusKWh(w)).ToArray();

        // =================================================================================
        // Prüfen (OK des Dialogs, 5.2)
        // =================================================================================

        /// <summary>
        /// Die Pflichtprüfung des OK (5.2): mindestens eine Zone, jede mit Name, Nutzungsart und
        /// Bezugsmenge größer 0, und kein Name doppelt — das Laufprotokoll nennt die Zonen beim
        /// Namen. Leer = in Ordnung. Dieselbe Prüfung für jeden Weg, der übernimmt.
        ///
        /// <para><paramref name="katalog"/> ist der beim Öffnen geladene Katalog (<see cref="Gaben"/>
        /// reicht ihn als Abschluss durch, KEIN neuer Datenbankzugriff je Tastendruck): Er entscheidet
        /// je Zone, ob ihre Wohnungstabelle wirksam ist (<see cref="Mengengeruest.WohnungstabelleWirksam(ZapfBezugsart)"/>,
        /// Z4, Gruppe 2a Punkt 6) — ohne Katalog gilt keine als wirksam.</para>
        /// </summary>
        internal static IReadOnlyList<ZapfprofilMeldung> Pruefen(ZapfprofilEingabeDaten eingabe,
                                                                 IReadOnlyList<ZapfprofilNutzungsartDaten> katalog = null)
        {
            var m = new List<ZapfprofilMeldung>();
            if (eingabe == null || eingabe.Zonen.Count == 0)
            {
                m.Add(Fehler("ZPG_MSG_KEINE_ZONE", "", Text_("ZPG_MSG_KEINE_ZONE", "Es ist keine Zone angelegt.")));
                return m;
            }
            // Bezugsart je Nutzungsart — EINE Stelle entscheidet, ob die Wohnungstabelle einer Zone
            // wirksam ist (Mengengeruest.WohnungstabelleWirksam, Z4, Gruppe 2a Punkt 6); der
            // Kalenderart „Wohnen" kommt dabei keine Rolle zu.
            Dictionary<int, ZapfBezugsart> bezugJeNutzungsart = (katalog ?? Array.Empty<ZapfprofilNutzungsartDaten>())
                .ToDictionary(n => n.Id, n => (ZapfBezugsart)n.Bezugsart);
            foreach (ZapfprofilZoneDaten z in eingabe.Zonen)
            {
                string name = (z.Name ?? "").Trim();
                if (name.Length == 0)
                    m.Add(Fehler("ZPG_MSG_ZONE_OHNE_NAME", "", Text_("ZPG_MSG_ZONE_OHNE_NAME", "Eine Zone hat keinen Namen.")));
                if (z.IdNutzungsart <= 0)
                    m.Add(Fehler("ZPG_MSG_ZONE_OHNE_NUTZUNGSART", name,
                        Format(Text_("ZPG_MSG_ZONE_OHNE_NUTZUNGSART", "Zone „{0}“: Bitte eine Nutzungsart wählen."), name)));
                if (!(z.Bezugsmenge > 0) || double.IsInfinity(z.Bezugsmenge.Value))
                    m.Add(Fehler("ZPG_MSG_ZONE_OHNE_BEZUGSMENGE", name,
                        Format(Text_("ZPG_MSG_ZONE_OHNE_BEZUGSMENGE", "Zone „{0}“: Bitte eine Bezugsgröße größer 0 eingeben."), name)));
                bool wohnungstabelleWirksam = bezugJeNutzungsart.TryGetValue(z.IdNutzungsart, out ZapfBezugsart bezug)
                                              && Mengengeruest.WohnungstabelleWirksam(bezug);
                if (z.Angaben != null) AngabenPruefen(z.Angaben, name, wohnungstabelleWirksam, m);
            }
            foreach (string doppelt in eingabe.Zonen.Select(z => (z.Name ?? "").Trim())
                                                    .Where(n => n.Length > 0)
                                                    .GroupBy(n => n, StringComparer.OrdinalIgnoreCase)
                                                    .Where(g => g.Count() > 1)
                                                    .Select(g => g.First()))
                m.Add(Fehler("ZPG_MSG_ZONE_NAME_DOPPELT", doppelt,
                    Format(Text_("ZPG_MSG_ZONE_NAME_DOPPELT", "Zone „{0}“: Der Name ist mehrfach vergeben — bitte jeder Zone einen eigenen Namen geben."), doppelt)));

            // Zirkulation manuell (gebäudeweit, Stufen Erweitert/Experte): dieselbe Regel wie der
            // Rechenweg (Zirkulationskanal.ManuellGueltig, Z4, Gruppe 2a Punkt 7).
            if (eingabe.Gebaeude is { ZirkAuto: false } gebaeude && !Zirkulationskanal.ManuellGueltig(gebaeude.ZirkManuellKw))
                m.Add(Fehler("ZPG_MSG_ZIRKULATION_MANUELL_UNGUELTIG", "",
                    Text_("ZPG_MSG_ZIRKULATION_MANUELL_UNGUELTIG", "Bitte die manuelle Zirkulationsleistung (kW, ≥ 0) angeben.")));

            // Die Stochastik der Jahresreihe (Stufe Experte): Seed ganz und ≥ 0, Realisierungen im
            // Bereich von Schema (Untergrenze) und Kern (Obergrenze der Jahresreihe).
            if (eingabe.Seed is int seed && seed < 0)
                m.Add(Fehler("ZPG_MSG_SEED_UNGUELTIG", "", Text_("ZPG_MSG_SEED_UNGUELTIG", "Der Seed muss eine ganze Zahl ab 0 sein.")));
            if (eingabe.Realisierungen is int r && (r < TwwSchema.RealisierungenMindestens || r > Jahresensemble.HOECHSTENS))
                m.Add(Fehler("ZPG_MSG_REALISIERUNGEN_UNGUELTIG", "",
                    Format(Text_("ZPG_MSG_REALISIERUNGEN_UNGUELTIG", "Die Zahl der Realisierungen muss zwischen {0} und {1} liegen."),
                           TwwSchema.RealisierungenMindestens, Jahresensemble.HOECHSTENS)));
            return m;
        }

        /// <summary>
        /// Die Pflichtprüfung der Angaben einer Zone (Stufen Erweitert und Experte): jeder Wohnungstyp
        /// mit einer Anzahl größer 0 — SOLANGE die Wohnungstabelle wirksam ist
        /// (<paramref name="wohnungstabelleWirksam"/>, <see cref="Mengengeruest.WohnungstabelleWirksam(ZapfBezugsart)"/>;
        /// außerhalb bleibt eine verdeckte Zeile ungeprüft, Z4, Gruppe 2a Punkt 6) —, jeder begonnene
        /// Ferienzeitraum vollständig und ein Datum des Rechenjahrs, ein Jahresmesswert größer 0 mit
        /// Einheit, bei kWh mit Bilanzgrenze und bei Grenze 3 mit Speicherverlust (4.1), kein
        /// negativer Auslastungsfaktor. Dieselben Regeln, die Rechenweg und Schreibweg benannt
        /// ablehnen — hier schon am OK des Zapfprofils.
        /// </summary>
        private static void AngabenPruefen(ZapfprofilZonenangabenDaten a, string name, bool wohnungstabelleWirksam,
                                           List<ZapfprofilMeldung> m)
        {
            if (wohnungstabelleWirksam)
                for (int i = 0; i < (a.Wohnungen?.Count ?? 0); i++)
                    if (!(a.Wohnungen[i]?.Anzahl > 0))
                        m.Add(Fehler("ZPG_MSG_WOHNUNG_OHNE_ANZAHL", name,
                            Format(Text_("ZPG_MSG_WOHNUNG_OHNE_ANZAHL", "Zone „{0}“: Wohnungstyp {1} braucht eine Anzahl größer 0."),
                                   name, i + 1)));

            for (int i = 0; i < (a.Ferien?.Count ?? 0); i++)
            {
                ZapfprofilFerienDaten f = a.Ferien[i];
                if (f == null || !f.Belegt) continue;
                bool beginn = f.BeginnTag.HasValue || f.BeginnMonat.HasValue;
                bool beginnGut = !beginn || Jahrestag(f.BeginnTag, f.BeginnMonat).HasValue;
                bool endeGut = Jahrestag(f.EndeTag, f.EndeMonat).HasValue;
                if (!beginnGut || !endeGut)
                    m.Add(Fehler("ZPG_MSG_FERIEN_UNGUELTIG", name,
                        Format(Text_("ZPG_MSG_FERIEN_UNGUELTIG",
                                     "Zone „{0}“: Ferienzeitraum {1} braucht ein Ende und für Beginn und Ende je einen gültigen Tag und Monat."),
                               name, i + 1)));
            }

            if (a.Jahresmesswert.HasValue)
            {
                double w = a.Jahresmesswert.Value;
                if (!(w > 0) || double.IsInfinity(w))
                    m.Add(Fehler("ZPG_MSG_MESSWERT_NICHT_POSITIV", name,
                        Format(Text_("ZPG_MSG_MESSWERT_NICHT_POSITIV", "Zone „{0}“: Der Jahresmesswert muss größer 0 sein."), name)));
                if (!a.JahresmesswertEinheit.HasValue)
                    m.Add(Fehler("ZPG_MSG_MESSWERT_OHNE_EINHEIT", name,
                        Format(Text_("ZPG_MSG_MESSWERT_OHNE_EINHEIT", "Zone „{0}“: Bitte die Einheit des Jahresmesswerts wählen."), name)));
                else if (a.JahresmesswertEinheit == ZapfprofilMesswerteinheit.KwhJeJahr && !a.JahresmesswertBilanzgrenze.HasValue)
                    m.Add(Fehler("ZPG_MSG_MESSWERT_OHNE_GRENZE", name,
                        Format(Text_("ZPG_MSG_MESSWERT_OHNE_GRENZE", "Zone „{0}“: Bitte die Bilanzgrenze des Jahresmesswerts wählen."), name)));
                if (a.JahresmesswertEinheit == ZapfprofilMesswerteinheit.KwhJeJahr
                    && a.JahresmesswertBilanzgrenze == ZapfprofilBilanzgrenze.MitSpeicher && !(a.SpeicherverlustKwhJeJahr >= 0))
                    m.Add(Fehler("ZPG_MSG_MESSWERT_OHNE_SPEICHERVERLUST", name,
                        Format(Text_("ZPG_MSG_MESSWERT_OHNE_SPEICHERVERLUST",
                                     "Zone „{0}“: Ein Messwert mit Speicherverlust braucht den Speicherverlust in kWh/a."), name)));
            }

            if (a.Auslastung != null && a.Auslastung.Any(x => x.HasValue && (x.Value < 0 || double.IsNaN(x.Value) || double.IsInfinity(x.Value))))
                m.Add(Fehler("ZPG_MSG_AUSLASTUNG_NEGATIV", name,
                    Format(Text_("ZPG_MSG_AUSLASTUNG_NEGATIV", "Zone „{0}“: Ein Faktor des Auslastungsgangs ist kleiner als 0."), name)));

            // Tagesbedarf manuell: dieselbe Regel wie der Rechenweg (Mengengeruest.TagesbedarfManuellGueltig,
            // Z4, Gruppe 2a Punkt 7) — keine zweite Regelsammlung.
            if (!a.TagesbedarfAuto && !Mengengeruest.TagesbedarfManuellGueltig(a.TagesbedarfManuellKwh))
                m.Add(Fehler("ZPG_MSG_TAGESBEDARF_MANUELL_UNGUELTIG", name,
                    Format(Text_("ZPG_MSG_TAGESBEDARF_MANUELL_UNGUELTIG", "Zone „{0}“: Bitte den manuellen Tagesbedarf (kWh/d, ≥ 0) angeben."), name)));
        }

        private static ZapfprofilMeldung Fehler(string kennung, string zone, string text)
            => new ZapfprofilMeldung(kennung, zone ?? "", text, ZapfprofilMeldungsart.Fehler);

        // =================================================================================
        // Der Einstieg im Bedarfsprofil-Dialog (5.2; ZU4, ZU6, ZU10)
        // =================================================================================

        /// <summary>
        /// Hängt den Zapfprofil-Einstieg in den Parametersatz des Bedarfsprofil-Dialogs der
        /// Ausprägung Brauchwasser: die zwei Delegaten aus <see cref="Einstieg"/> (oder den
        /// benannten Grund), die Optionsgruppe „Rechenweg Brauchwasser" über den
        /// <paramref name="behaelter"/>, die Zahl der Zonen und die Beschriftungen. Ohne
        /// gespeichertes Projekt (ZU10) gibt es nur den Grund; die Schale entscheidet das über
        /// <paramref name="projektGespeichert"/>.
        /// </summary>
        internal static void Einhaengen(IDictionary<string, object> gaben, int idProjekt, bool projektGespeichert,
                                        ZapfprofilBehaelter behaelter)
        {
            if (gaben == null) throw new ArgumentNullException(nameof(gaben));
            ZapfprofilEinstieg einstieg = behaelter == null
                ? Einstieg(idProjekt, null)
                : Einstieg(projektGespeichert ? idProjekt : 0, behaelter.Wege());

            gaben["ZapfprofilEinstiegTexte"] = EinstiegTexte();
            gaben["ZapfprofilGaben"] = einstieg.Gaben;
            gaben["ZapfprofilUebernommen"] = einstieg.Uebernommen;
            gaben["ZapfprofilSperrgrund"] = einstieg.Grund ?? "";
            if (!einstieg.Angeboten) return;

            gaben["RechenwegBrauchwasser"] = behaelter.Weg;
            gaben["RechenwegGesetzt"] = new Action<ZapfprofilWeg>(behaelter.WegSetzen);
            gaben["ZapfprofilZonen"] = (behaelter.Arbeitsstand ?? ZapfprofilCtrl.Lies(idProjekt)).Zonen?.Count ?? 0;
        }

        /// <summary>Die Beschriftungen des Einstiegs in der Oberflächensprache; fehlt ein Schlüssel, bleibt der deutsche Rückfall.</summary>
        internal static ZapfprofilEinstiegTexte EinstiegTexte()
        {
            var t = new ZapfprofilEinstiegTexte();
            t.Knopf = Text_("BPF_BTN_ZAPFPROFIL_BW", t.Knopf);
            t.Titel = Text_("ZPG_TITEL", t.Titel);
            t.LabelRechenweg = Text_("BPF_LBL_RECHENWEG_BW", t.LabelRechenweg);
            t.OptionBestand = Text_("BPF_OPT_BESTANDSPROFILE", t.OptionBestand);
            t.OptionZapfprofil = Text_("BPF_OPT_ZAPFPROFIL", t.OptionZapfprofil);
            t.HinweisRechenweg = Text_("BPF_HINW_RECHENWEG_BW", t.HinweisRechenweg);
            t.HinweisZapfprofilweg = Text_("BPF_HINW_ZAPFPROFILWEG", t.HinweisZapfprofilweg);
            t.HinweisOhneZonen = Text_("BPF_HINW_ZAPFPROFIL_OHNE_ZONEN", t.HinweisOhneZonen);
            t.LeisteZapfprofil = Text_("BPF_LBL_LEISTE_ZAPFPROFIL", t.LeisteZapfprofil);
            return t;
        }

        /// <summary>
        /// Die Meldung der Leiste „Simulation · monatlicher Verlauf" (5.2, N8 b/g) zu einer
        /// Vorschau des Bedarfsprofil-Dialogs: auf dem Zapfprofilweg der Grund eines Abbruchs
        /// oder je abgelehnter Zone ihr Satz (<see cref="Meldung(ZapfAblehnung)"/>) in der Oberflächensprache;
        /// auf dem Bestandsweg leer.
        /// </summary>
        internal static string Leistenmeldung(BedarfsVorschau v)
        {
            if (v == null || !v.Zapfprofilweg) return "";
            if (v.Erfolgreich && v.Waerme?.Zapfprofil != null)
                return string.Join(Environment.NewLine, v.Waerme.Zapfprofil.Ablehnungen.Select(a => Meldung(a).Text));

            ZapfVerfuegbarkeit verfuegbar = ZapfprofilCtrl.Verfuegbar();
            if (!verfuegbar.Ja) return Verfuegbarkeitsgrund(verfuegbar);
            if (v.Waerme == null)
                return Text_("ZPG_MSG_KEINE_KLIMAREGION", "Das Projekt hat keine Klimaregion — ohne Kalender keine Vorschau.");
            return Format(Text_("ZPG_MSG_UNERWARTET", "Die Vorschau konnte nicht gerechnet werden: {0}"),
                          OhnePraefix(string.IsNullOrEmpty(v.Meldung) ? v.Waerme.Fehlertext : v.Meldung));
        }

        // =================================================================================
        // Der gemeinsame Schreibweg des Bedarfsprofil-Dialogs (5.2)
        // =================================================================================

        /// <summary>
        /// <b>Ein Vorgang für beides</b> (5.2): Löschen und Neuanlegen der Brauchwasser-
        /// Zuordnungen des Projekts (<c>Del/Add_Projekt_Brauchwasser</c>) und der Arbeitsstand
        /// des Zapfprofils (<see cref="ZapfprofilBehaelter.Schreiben"/> →
        /// <c>ZapfprofilCtrl.Speichern(id, stand, v)</c>) in EINEM <see cref="DbVorgang"/>.
        /// Scheitert ein Schritt, rollt der Vorgang alles zurück: ein Fehler der Zuordnungen hat
        /// sich schon selbst gemeldet (<c>DataRepository.FehlerMelden</c>, Meldung <c>null</c>),
        /// eine Ablehnung des Zapfprofils kommt als Meldung zurück. Nach dem Commit gilt der
        /// Behälter wieder als unverändert. Aufrufer: <see cref="Schreibweg"/> im OK des
        /// Bedarfsprofil-Dialogs (Startseite und Gebäudekatalog).
        ///
        /// <para><b>Nur bei echter Änderung.</b> Gleicht die Liste dem gespeicherten Stand
        /// (<see cref="Z_ProjektBrauchwasserCtrl.GleichGespeichert"/>), bleiben die Zuordnungen
        /// stehen; ein unveränderter Behälter schreibt ohnehin nichts. Ein OK ohne Änderung lässt
        /// damit auch das Änderungsdatum des Projekts stehen — gesetzt wird es nur von den
        /// Schreibwegen, die tatsächlich schreiben, im selben Vorgang.</para>
        /// </summary>
        internal static ZapfprofilSpeicherergebnis BrauchwasserSchreiben(int idProjekt,
                                                                         List<Z_ProjektBrauchwasserModel> liste,
                                                                         ZapfprofilBehaelter behaelter)
        {
            var wizctrl = new WizardCtrl();
            liste ??= new List<Z_ProjektBrauchwasserModel>();
            bool zuordnungenGleich = Z_ProjektBrauchwasserCtrl.GleichGespeichert(idProjekt, liste);
            ZapfprofilSpeicherergebnis e;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                if (!zuordnungenGleich
                    && (!wizctrl.Del_Projekt_Brauchwasser(idProjekt, 0, v)
                        || !wizctrl.Add_Projekt_Brauchwasser(idProjekt, liste, v)))
                    return new ZapfprofilSpeicherergebnis(false, null, null);

                e = behaelter?.Schreiben(v) ?? new ZapfprofilSpeicherergebnis(true, null, null);
                if (!e.Erfolg) return e;
                v.Commit();
            }
            behaelter?.Geschrieben();
            return e;
        }

        /// <summary>
        /// <b>Der Schreibweg im OK des Bedarfsprofil-Dialogs</b> (5.2; Parameter <c>Speichern</c>
        /// von <c>BedarfsProfileDialog</c>): die Projektzeilen des Dialogs als Brauchwasser-
        /// Zuordnungen und der <paramref name="behaelter"/> (auch <c>null</c>: nur die Zuordnungen)
        /// in EINEM Vorgang (<see cref="BrauchwasserSchreiben"/>), gerufen, BEVOR der Dialog
        /// schließt. Leer = geschrieben. Sonst der Grund in der Oberflächensprache — die Ablehnung
        /// des Zapfprofils oder, wenn die Zuordnungen scheitern, deren Datenbankmeldung; der Dialog
        /// bleibt dann offen. Die Datenbankmeldung wird gesammelt statt als Plattformfenster
        /// gezeigt: Der Rückruf kommt aus einem Ereignis der Oberfläche (Hüllenregel b).
        /// </summary>
        internal static string Schreibweg(int idProjekt, IEnumerable<BedarfsProfilZeile> zeilen,
                                          ZapfprofilBehaelter behaelter)
        {
            var liste = new List<Z_ProjektBrauchwasserModel>();
            foreach (BedarfsProfilZeile z in zeilen ?? Enumerable.Empty<BedarfsProfilZeile>())
                liste.Add(new Z_ProjektBrauchwasserModel
                {
                    ID_Z = z.IdZ, ID_Projekt = idProjekt, ID_Brauchwasser = z.IdStamm,
                    szBezeichner = z.Name, Summe = z.Summe
                });

            ZapfprofilSpeicherergebnis e;
            string[] datenbank;
            using (DataRepository.EngineModus())
            {
                e = BrauchwasserSchreiben(idProjekt, liste, behaelter);
                datenbank = DataRepository.StilleFehlerAbholen();
            }
            if (e.Erfolg) return "";
            if (e.Meldung != null) return e.Meldung.Text;
            return Format(Text_("ZPG_MSG_ZUORDNUNG_NICHT_GESPEICHERT",
                                "Die Brauchwasserprofile des Projekts wurden nicht gespeichert — {0}"),
                          string.Join(" ", datenbank));
        }

        // =================================================================================
        // Speichern (5.2: im DbVorgang des Aufrufers)
        // =================================================================================

        /// <summary>
        /// Schreibt den Arbeitsstand im übergebenen Vorgang (<c>ZapfprofilCtrl.Speichern</c>, kein
        /// Commit) und liefert ihn mit den Ids der Datenbank; eine benannte Ablehnung des
        /// Schreibwegs kommt als Meldung zurück — der Aufrufer rollt seinen Vorgang zurück.
        /// </summary>
        internal static ZapfprofilSpeicherergebnis Speichern(int idProjekt, ZapfprofilStand stand, DbVorgang v)
        {
            try
            {
                ZapfprofilStand geschrieben = ZapfprofilCtrl.Speichern(idProjekt, stand, v);
                return new ZapfprofilSpeicherergebnis(true, geschrieben, null);
            }
            catch (ZapfprofilSpeicherException ex)
            {
                return new ZapfprofilSpeicherergebnis(false, null, Meldung(ex));
            }
        }

        // =================================================================================
        // Meldungen: Kern -> Ressource
        // =================================================================================

        /// <summary>
        /// <b>Der Satz des Kerns in der Oberflächensprache</b> (N11 (k)): das Muster
        /// <c>ZPG_SATZ_</c> + Kennung aus der Ressource, die Werte in der Kultur der Oberfläche
        /// eingesetzt, verschachtelte Begriffe ebenso. Fehlt die Ressource, gilt das deutsche Muster,
        /// dann die Kennung mit ihren Werten — benannt statt still. Leer ohne Satz.
        /// </summary>
        internal static string Satztext(ZapfSatz satz)
            => satz == null ? "" : satz.Text(k => Text_(k, null), CultureInfo.CurrentCulture);

        /// <summary>Die Kennung einer Meldung zu einem Satz: sein Ressourcenschlüssel <c>ZPG_SATZ_…</c>; ohne Satz die Rückfallkennung.</summary>
        internal static string Satzkennung(ZapfSatz satz, string rueckfall) => satz?.Schluessel ?? rueckfall ?? "";

        // =================================================================================
        //  Das Herkunftsprotokoll als Karte (N19)
        // =================================================================================

        /// <summary>
        /// <b>Das Herkunftsprotokoll des Kerns als Zeilen der Oberflächensprache</b> (Karte
        /// „Herkunft", N19): je Eintrag eine Zeile, in der Reihenfolge des Protokolls — sie ist die
        /// Reihenfolge, in der der Rechenweg die Werte festlegt, und wird nicht sortiert.
        ///
        /// <para>Übersetzt werden der <b>Vermerk</b> (der Satz des Kerns, wie bei jedem Hinweis),
        /// der <b>Stand</b> (<see cref="Wertstatus"/>, vier Ausprägungen) und die <b>Quelle</b>
        /// (<see cref="Herkunftsart"/>, fünf Ausprägungen; Regelwerk, Ausgabe und Katalogfassung sind
        /// Daten). Die <b>Größe</b> bleibt der Feldname des Protokolls — der Bezeichner des
        /// Rechenwegs, in beiden Sprachen derselbe (siehe <see cref="ZapfprofilHerkunftZeile"/>).</para>
        ///
        /// <para>Ein Eintrag ohne Wert trägt eine leere Wertspalte (der Vermerk sagt dann, was
        /// geschah); die Einheit „-" steht für dimensionslos und wird nicht angehängt. Ohne Provenienz
        /// hat der Anwender den Wert gesetzt — das sagt die Quellspalte benannt.</para>
        /// </summary>
        internal static List<ZapfprofilHerkunftZeile> Herkunftszeilen(IReadOnlyList<Herkunftseintrag> eintraege)
        {
            var zeilen = new List<ZapfprofilHerkunftZeile>();
            if (eintraege == null) return zeilen;
            string projekt = Text_("ZPG_HERKUNFT_ZONE_PROJEKT", "Projekt");
            foreach (Herkunftseintrag e in eintraege)
            {
                if (e == null) continue;
                zeilen.Add(new ZapfprofilHerkunftZeile(
                    Groessenname(e.Feld),
                    string.IsNullOrEmpty(e.Zone) ? projekt : e.Zone,
                    Herkunftswert(e.Wert, e.Einheit),
                    Standname(e.Status),
                    Quellentext(e.Quelle),
                    Satztext(e.Vermerk)));
            }
            return zeilen;
        }

        /// <summary>
        /// <b>Die Beschriftung einer Größe</b> des Herkunftsprotokolls (ZU25, Nachtrag N21): der
        /// Ressourcentext ihres Schlüssels <c>ZPG_GROESSE_…</c> in der Sprache der Oberfläche.
        /// Kennt der Kern den Namen nicht als Größe oder fehlt der Text, steht der <b>Feldname</b>
        /// da — ein benannter Rückfall, keine leere Zelle. Die Wache
        /// <c>ZapfprofilGroessennamenWacheTests</c> hält jede Größe des Kerns gegen die Ressourcen.
        /// </summary>
        internal static string Groessenname(string groesse)
        {
            if (string.IsNullOrEmpty(groesse)) return "";
            string schluessel = ZapfFeld.Ressourcenschluessel(groesse);
            return schluessel == null ? groesse : Text_(schluessel, null) ?? groesse;
        }

        /// <summary>Wert und Einheit einer Protokollzeile; leer ohne Wert, ohne Einheit bei „-" (dimensionslos).</summary>
        private static string Herkunftswert(double? wert, string einheit)
        {
            if (!wert.HasValue) return "";
            string zahl = wert.Value.ToString("0.###", CultureInfo.CurrentCulture);
            return string.IsNullOrEmpty(einheit) || einheit == "-" ? zahl : zahl + " " + einheit;
        }

        /// <summary>Der Stand eines Werts in der Oberflächensprache (<see cref="Wertstatus"/>).</summary>
        private static string Standname(Wertstatus stand)
        {
            switch (stand)
            {
                case Wertstatus.Ueberschrieben: return Text_("ZPG_HERKUNFT_STAND_UEBERSCHRIEBEN", "überschrieben");
                case Wertstatus.Kalibriert: return Text_("ZPG_HERKUNFT_STAND_KALIBRIERT", "kalibriert");
                case Wertstatus.Umgerechnet: return Text_("ZPG_HERKUNFT_STAND_UMGERECHNET", "umgerechnet");
                default: return Text_("ZPG_HERKUNFT_STAND_VORGABE", "Vorgabe");
            }
        }

        /// <summary>
        /// Die Provenienz einer Wertgruppe als Text: Regelwerk, Ausgabe und Katalogfassung als Daten,
        /// die Herkunftsart übersetzt, verbunden mit „ · ". Ohne Provenienz hat der Anwender den Wert
        /// gesetzt. Die interne Spalte <c>Beleg</c> steht hier nie (Konzept Kapitel 6 (e)).
        /// </summary>
        private static string Quellentext(Provenienz quelle)
        {
            if (quelle == null) return Text_("ZPG_HERKUNFT_QUELLE_ANWENDER", "Eingabe des Anwenders");
            var teile = new List<string>();
            string regelwerk = string.Join(" ", new[] { quelle.Quelle, quelle.Ausgabe }
                                                   .Where(s => !string.IsNullOrWhiteSpace(s)));
            if (regelwerk.Length > 0) teile.Add(regelwerk);
            if (!string.IsNullOrWhiteSpace(quelle.Version))
                teile.Add(Format(Text_("ZPG_HERKUNFT_FASSUNG", "Katalogfassung {0}"), quelle.Version));
            teile.Add(Artname(quelle.Art));
            return string.Join(" · ", teile);
        }

        /// <summary>Die Herkunftsart in der Oberflächensprache (<see cref="Herkunftsart"/>).</summary>
        private static string Artname(Herkunftsart art)
        {
            switch (art)
            {
                case Herkunftsart.Verfahren: return Text_("ZPG_HERKUNFT_ART_VERFAHREN", "aus einem Verfahren gerechnet");
                case Herkunftsart.Eigenkonstruktion: return Text_("ZPG_HERKUNFT_ART_EIGENKONSTRUKTION", "Eigenkonstruktion");
                case Herkunftsart.Frei: return Text_("ZPG_HERKUNFT_ART_FREI", "frei verfügbare Quelle");
                case Herkunftsart.Import: return Text_("ZPG_HERKUNFT_ART_IMPORT", "eingespielt");
                case Herkunftsart.Fiktiv: return Text_("ZPG_HERKUNFT_ART_FIKTIV", "erfundener Wert");
                default: return "";
            }
        }

        /// <summary>Der Satz einer benannten Ausnahme des Kerns; <c>null</c> bei einer fremden Ausnahme.</summary>
        internal static ZapfSatz SatzAus(Exception ex)
        {
            switch (ex)
            {
                case ZapfprofilEingabeException e: return e.Satz;
                case ParametersatzException p: return p.Satz;
                case ZapfAuslegungException a: return a.Satz;
                case ZapfprofilSpeicherException s: return s.Satz;
                default: return null;
            }
        }

        /// <summary>
        /// Eine benannte Ausnahme des Kerns als Text der Oberflächensprache; eine fremde Ausnahme
        /// behält ihren Wortlaut.
        /// </summary>
        internal static string Ausnahmetext(Exception ex)
        {
            ZapfSatz satz = SatzAus(ex);
            return satz != null ? Satztext(satz) : ex?.Message ?? "";
        }

        /// <summary>
        /// Die Ablehnung des Schreibwegs als Meldung: „Das Zapfprofil wurde nicht gespeichert — Grund"
        /// mit dem Grund als Satz des Kerns in der Oberflächensprache; die Kennung ist die des Grundes.
        /// </summary>
        internal static ZapfprofilMeldung Meldung(ZapfprofilSpeicherException ex)
            => new ZapfprofilMeldung(Satzkennung(ex.Grund, "ZPG_MSG_NICHT_GESPEICHERT"), ex.Zone ?? "",
                                     Format(Text_("ZPG_MSG_NICHT_GESPEICHERT", "Das Zapfprofil wurde nicht gespeichert — {0}"),
                                            Satztext(ex.Grund)),
                                     ZapfprofilMeldungsart.Fehler, ex.Message ?? "");

        /// <summary>
        /// Eine Ablehnung des Rechenwegs mit dem Satz des Kerns als Grund in der Oberflächensprache;
        /// die Kennung ist die des Satzes. Nennt der Satz seine Zone schon, steht er allein — sonst
        /// trägt er den Vorsatz „Zone „…“ trägt 0: …" bzw. ohne Zone „Die Zirkulation trägt 0: …"
        /// (<see cref="MitZone"/>). „Nicht rechenbar" sagt der Titel des Banners, nicht der Satz.
        /// </summary>
        internal static ZapfprofilMeldung Meldung(ZapfAblehnung a)
        {
            string text = string.IsNullOrEmpty(a.Zone)
                ? Format(Text_("ZPG_MSG_ANTEIL_TRAEGT_NULL", "Die Zirkulation trägt 0: {0}"), Satztext(a.Satz))
                : MitZone(a.Zone, a.Satz, "ZPG_MSG_ZONE_TRAEGT_NULL", "Zone „{0}“ trägt 0: {1}");
            return new ZapfprofilMeldung(Satzkennung(a.Satz, "ZPG_MSG_ZONE_TRAEGT_NULL"), a.Zone ?? "", text,
                                         ZapfprofilMeldungsart.Ablehnung, a.Klartext ?? "");
        }

        /// <summary>
        /// Der Satz einer Zone in der Oberflächensprache: allein, wenn er die Zone
        /// <paramref name="zone"/> schon nennt (<see cref="ZapfSatz.Nennt"/>) — sonst mit dem Vorsatz
        /// <paramref name="schluessel"/> (Platzhalter {0} Zone, {1} Satz). Ohne Zone der Satz allein.
        /// </summary>
        internal static string MitZone(string zone, ZapfSatz satz, string schluessel, string rueckfall)
        {
            string text = Satztext(satz);
            if (string.IsNullOrEmpty(zone) || (satz != null && satz.Nennt(zone))) return text;
            return Format(Text_(schluessel, rueckfall), zone, text);
        }

        /// <summary>Ein Hinweis des Rechenwegs: der Satz des Kerns in der Oberflächensprache, Kennung des Satzes.</summary>
        internal static ZapfprofilMeldung Meldung(ZapfHinweis h)
            => new ZapfprofilMeldung(Satzkennung(h.Satz, "ZPG_SATZ_" + h.Code), h.Zone ?? "", Satztext(h.Satz),
                                     ZapfprofilMeldungsart.Hinweis, h.Text ?? "");

        /// <summary>Die Kennung der Nichtverfügbarkeit: die des Satzes.</summary>
        private static string VerfuegbarkeitsKennung(ZapfVerfuegbarkeit v) => Satzkennung(v?.Satz, "ZPG_SATZ_VERFUEGBAR_TABELLEN_FEHLEN");

        /// <summary>Der Grund der Nichtverfügbarkeit in der Oberflächensprache (Satz des Kerns samt Tabellen bzw. Tabelle); leer, wenn verfügbar.</summary>
        internal static string Verfuegbarkeitsgrund(ZapfVerfuegbarkeit v)
            => v == null || v.Ja ? "" : Satztext(v.Satz);

        // =================================================================================
        // Kontext, Texte, Kalendernamen
        // =================================================================================

        private static ZapfprofilKontextDaten Kontext(ProjektCtrl projekt)
        {
            var k = new ZapfprofilKontextDaten
            {
                Projekt = projekt?.m_szProjektname ?? "",
                Bilanzgrenze = Text_("ZPG_KONTEXT_BILANZGRENZE",
                    "Bilanzgrenze: Zapfenergie an der Zapfstelle; Zirkulation als eigene Teilreihe")
            };
            if (projekt == null || projekt.m_ID_Klimaregion <= 0) return k;

            var regionen = new KlimaregionCtrl();
            regionen.ReadAll();
            string region = regionen.items.FirstOrDefault(r => r.m_ID_Klimaregion == projekt.m_ID_Klimaregion)?.m_szName ?? "";
            k.Klimaregion = Format(Text_("ZPG_KONTEXT_KLIMAREGION", "Klimaregion {0}"), region);
            var sim = new SimulationWaermebedarf { m_ID_Projekt = projekt.m_ID };
            sim.ZapfprofilKalenderLesen(projekt.m_ID_Klimaregion);
            k.Kalender = Format(Text_("ZPG_KONTEXT_KALENDER", "Kalender: 1. Januar = {0}, 365 Tage"),
                                Wochentagsname(sim.WochentagJan1));
            return k;
        }

        /// <summary>Das Textbündel des Dialogs in der Oberflächensprache; fehlt ein Schlüssel, bleibt der deutsche Rückfall.</summary>
        internal static ZapfprofilTexte Texte()
        {
            var t = new ZapfprofilTexte();
            t.Titel = Text_("ZPG_TITEL", t.Titel);
            t.TitelProjekt = Text_("ZPG_TITEL_PROJEKT", t.TitelProjekt);
            t.InfoBedienung = Text_("ZPG_INFO_BEDIENUNG", t.InfoBedienung);
            t.InfoRechenweg = Text_("ZPG_INFO_RECHENWEG", t.InfoRechenweg);
            t.KontextKlimaregion = Text_("ZPG_KONTEXT_KLIMAREGION", t.KontextKlimaregion);
            t.KontextKalender = Text_("ZPG_KONTEXT_KALENDER", t.KontextKalender);
            t.KontextBilanzgrenze = Text_("ZPG_KONTEXT_BILANZGRENZE", t.KontextBilanzgrenze);
            t.KontextUeberschrieben = Text_("ZPG_KONTEXT_UEBERSCHRIEBEN", t.KontextUeberschrieben);
            t.LabelStufe = Text_("ZPG_LBL_STUFE", t.LabelStufe);
            t.StufeEinfach = Text_("ZPG_STUFE_EINFACH", t.StufeEinfach);
            t.StufeErweitert = Text_("ZPG_STUFE_ERWEITERT", t.StufeErweitert);
            t.StufeExperte = Text_("ZPG_STUFE_EXPERTE", t.StufeExperte);
            t.GrundNochNicht = Text_("ZPG_GRUND_NOCH_NICHT", t.GrundNochNicht);

            t.GruppeZonen = Text_("ZPG_GRP_ZONEN", t.GruppeZonen);
            t.GruppeZonenSumme = Text_("ZPG_GRP_ZONEN_SUMME", t.GruppeZonenSumme);
            t.GruppeZone = Text_("ZPG_GRP_ZONE", t.GruppeZone);
            t.SpalteZone = Text_("ZPG_SP_ZONE", t.SpalteZone);
            t.SpalteNutzungsart = Text_("ZPG_SP_NUTZUNGSART", t.SpalteNutzungsart);
            t.SpalteBezugsgroesse = Text_("ZPG_SP_BEZUGSGROESSE", t.SpalteBezugsgroesse);
            t.SpalteJahresbedarf = Text_("ZPG_SP_JAHRESBEDARF", t.SpalteJahresbedarf);
            t.SummeZonen = Text_("ZPG_SUMME_ZONEN", t.SummeZonen);
            t.KnopfZoneNeu = Text_("ZPG_BTN_ZONE_NEU", t.KnopfZoneNeu);
            t.KnopfZoneDuplizieren = Text_("ZPG_BTN_ZONE_DUPLIZIEREN", t.KnopfZoneDuplizieren);
            t.KnopfZoneEntfernen = Text_("ZPG_BTN_ZONE_ENTFERNEN", t.KnopfZoneEntfernen);
            t.ZoneNameVorgabe = Text_("ZPG_ZONE_NAME_VORGABE", t.ZoneNameVorgabe);
            t.ZoneKopie = Text_("ZPG_ZONE_KOPIE", t.ZoneKopie);

            t.LabelZonenname = Text_("ZPG_LBL_ZONENNAME", t.LabelZonenname);
            t.LabelNutzungsart = Text_("ZPG_LBL_NUTZUNGSART", t.LabelNutzungsart);
            t.LabelBezugsmenge = Text_("ZPG_LBL_BEZUGSMENGE", t.LabelBezugsmenge);
            t.LabelNiveau = Text_("ZPG_LBL_NIVEAU", t.LabelNiveau);
            t.NiveauNiedrig = Text_("ZPG_NIVEAU_NIEDRIG", t.NiveauNiedrig);
            t.NiveauMittel = Text_("ZPG_NIVEAU_MITTEL", t.NiveauMittel);
            t.NiveauHoch = Text_("ZPG_NIVEAU_HOCH", t.NiveauHoch);
            t.HinweisBezugsmengeEinheit = Text_("ZPG_HINW_BEZUGSMENGE_EINHEIT", t.HinweisBezugsmengeEinheit);
            t.HinweisNiveauVorgabe = Text_("ZPG_HINW_NIVEAU_VORGABE", t.HinweisNiveauVorgabe);
            t.HinweisWeitereVorgabe = Text_("ZPG_HINW_WEITERE_VORGABE", t.HinweisWeitereVorgabe);
            t.HinweisWeitereErweitert = Text_("ZPG_HINW_WEITERE_ERWEITERT", t.HinweisWeitereErweitert);
            t.HinweisNutzungsartKatalog = Text_("ZPG_HINW_NUTZUNGSART_KATALOG", t.HinweisNutzungsartKatalog);
            t.KatalogKeineAuswahl = Text_("ZPG_KAT_KEINE_AUSWAHL", t.KatalogKeineAuswahl);

            t.SpalteBezugsart = Text_("ZPG_SP_BEZUGSART", t.SpalteBezugsart);
            t.SpalteHerkunft = Text_("ZPG_SP_HERKUNFT", t.SpalteHerkunft);
            t.SpalteStatus = Text_("ZPG_SP_STATUS", t.SpalteStatus);
            t.SpalteKatalogversion = Text_("ZPG_SP_KATALOGVERSION", t.SpalteKatalogversion);

            t.GruppeVorschau = Text_("ZPG_GRP_VORSCHAU", t.GruppeVorschau);
            t.VorschauLive = Text_("ZPG_VORSCHAU_LIVE", t.VorschauLive);
            t.LabelAnzeigenFuer = Text_("ZPG_LBL_ANZEIGEN_FUER", t.LabelAnzeigenFuer);
            t.AnsichtSumme = Text_("ZPG_ANSICHT_SUMME", t.AnsichtSumme);
            t.ReiterTagesgang = Text_("ZPG_REITER_TAGESGANG", t.ReiterTagesgang);
            t.ReiterWochenprofil = Text_("ZPG_REITER_WOCHENPROFIL", t.ReiterWochenprofil);
            t.ReiterJahresgang = Text_("ZPG_REITER_JAHRESGANG", t.ReiterJahresgang);
            t.ReiterDauerlinie = Text_("ZPG_REITER_DAUERLINIE", t.ReiterDauerlinie);
            t.ReiterKennzahlen = Text_("ZPG_REITER_KENNZAHLEN", t.ReiterKennzahlen);
            t.TagtypWerktag = Text_("ZPG_TAGTYP_WERKTAG", t.TagtypWerktag);
            t.TagtypSamstag = Text_("ZPG_TAGTYP_SAMSTAG", t.TagtypSamstag);
            t.TagtypSonntag = Text_("ZPG_TAGTYP_SONNTAG", t.TagtypSonntag);
            t.ReiheZapfung = Text_("ZPG_REIHE_ZAPFUNG", t.ReiheZapfung);
            t.ReiheZirkulation = Text_("ZPG_REIHE_ZIRKULATION", t.ReiheZirkulation);

            t.KennzahlBilanz = Text_("ZPG_KZ_BILANZ", t.KennzahlBilanz);
            t.KennzahlStochastik = Text_("ZPG_KZ_STOCHASTIK", t.KennzahlStochastik);
            t.KennzahlStochastikErklaerung = Text_("ZPG_KZ_STOCHASTIK_ERKLAERUNG", t.KennzahlStochastikErklaerung);
            t.SpalteKennzahl = Text_("ZPG_SP_KENNZAHL", t.SpalteKennzahl);
            t.SpalteWert = Text_("ZPG_SP_WERT", t.SpalteWert);
            t.SpalteVermerk = Text_("ZPG_SP_VERMERK", t.SpalteVermerk);
            t.KennzahlZapfung = Text_("ZPG_KZ_ZAPFUNG", t.KennzahlZapfung);
            t.KennzahlZirkulation = Text_("ZPG_KZ_ZIRKULATION", t.KennzahlZirkulation);
            t.KennzahlZirkulationVermerk = Text_("ZPG_KZ_ZIRKULATION_VERMERK", t.KennzahlZirkulationVermerk);
            t.KennzahlGesamt = Text_("ZPG_KZ_GESAMT", t.KennzahlGesamt);
            t.KennzahlZirkulationsanteil = Text_("ZPG_KZ_ZIRKULATIONSANTEIL", t.KennzahlZirkulationsanteil);
            t.KennzahlTagesmittel = Text_("ZPG_KZ_TAGESMITTEL", t.KennzahlTagesmittel);
            t.KennzahlLiterJeTag = Text_("ZPG_KZ_LITER_JE_TAG", t.KennzahlLiterJeTag);
            t.KennzahlSpezifisch = Text_("ZPG_KZ_SPEZIFISCH", t.KennzahlSpezifisch);
            t.KennzahlVolllast = Text_("ZPG_KZ_VOLLLAST", t.KennzahlVolllast);
            t.KennzahlVolllastVermerk = Text_("ZPG_KZ_VOLLLAST_VERMERK", t.KennzahlVolllastVermerk);
            t.KennzahlGroessterStundenwert = Text_("ZPG_KZ_GROESSTER_STUNDENWERT", t.KennzahlGroessterStundenwert);
            t.KennzahlVermerkStundenwert = Text_("ZPG_KZ_VERMERK_STUNDENWERT", t.KennzahlVermerkStundenwert);
            t.KennzahlStundenUeberSchwelle = Text_("ZPG_KZ_STUNDEN_UEBER_SCHWELLE", t.KennzahlStundenUeberSchwelle);
            t.KennzahlGleichzeitigkeit = Text_("ZPG_KZ_GLEICHZEITIGKEIT", t.KennzahlGleichzeitigkeit);
            t.KennzahlGleichzeitigkeitVermerk = Text_("ZPG_KZ_GLEICHZEITIGKEIT_VERMERK", t.KennzahlGleichzeitigkeitVermerk);
            t.KennzahlAbgelehnt = Text_("ZPG_KZ_ABGELEHNT", t.KennzahlAbgelehnt);

            t.GruppeStochastik = Text_("ZPG_GRP_STOCHASTIK", t.GruppeStochastik);
            t.LabelRechenwegJahresreihe = Text_("ZPG_LBL_RECHENWEG_JAHRESREIHE", t.LabelRechenwegJahresreihe);
            t.OptionDeterministisch = Text_("ZPG_OPT_DETERMINISTISCH", t.OptionDeterministisch);
            t.OptionStochastisch = Text_("ZPG_OPT_STOCHASTISCH", t.OptionStochastisch);
            t.HinweisRechenwegJahresreihe = Text_("ZPG_HINW_RECHENWEG_JAHRESREIHE", t.HinweisRechenwegJahresreihe);
            t.LabelSeed = Text_("ZPG_LBL_SEED", t.LabelSeed);
            t.HinweisSeed = Text_("ZPG_HINW_SEED", t.HinweisSeed);
            t.LabelRealisierungen = Text_("ZPG_LBL_REALISIERUNGEN", t.LabelRealisierungen);
            t.EinheitJahre = Text_("ZPG_EINHEIT_JAHRE", t.EinheitJahre);
            t.HinweisRealisierungen = Text_("ZPG_HINW_REALISIERUNGEN", t.HinweisRealisierungen);
            t.HinweisKonsistenzOrt = Text_("ZPG_HINW_KONSISTENZ_ORT", t.HinweisKonsistenzOrt);
            t.HinweisVorschauDeterministisch = Text_("ZPG_HINW_VORSCHAU_DETERMINISTISCH", t.HinweisVorschauDeterministisch);
            t.KennzahlStochastikGerechnet = Text_("ZPG_KZ_STOCHASTIK_GERECHNET", t.KennzahlStochastikGerechnet);
            t.KennzahlStochastikLaeuft = Text_("ZPG_KZ_STOCHASTIK_LAEUFT", t.KennzahlStochastikLaeuft);
            t.StatusJahresreiheLaeuft = Text_("ZPG_STATUS_JAHRESREIHE_LAEUFT", t.StatusJahresreiheLaeuft);
            t.HinweisJahresreiheAbgebrochen = Text_("ZPG_HINW_JAHRESREIHE_ABGEBROCHEN", t.HinweisJahresreiheAbgebrochen);
            t.HinweisJahresreiheDeterministisch = Text_("ZPG_HINW_JAHRESREIHE_DETERMINISTISCH", t.HinweisJahresreiheDeterministisch);
            t.KennzahlStochastikJahresreihe = Text_("ZPG_KZ_STOCHASTIK_JAHRESREIHE", t.KennzahlStochastikJahresreihe);
            t.KennzahlKonsistenz = Text_("ZPG_KZ_KONSISTENZ", t.KennzahlKonsistenz);
            t.KennzahlKonsistenzVermerk = Text_("ZPG_KZ_KONSISTENZ_VERMERK", t.KennzahlKonsistenzVermerk);
            t.KonsistenzErfuellt = Text_("ZPG_KZ_KONSISTENZ_ERFUELLT", t.KonsistenzErfuellt);
            t.KonsistenzAbweichend = Text_("ZPG_KZ_KONSISTENZ_ABWEICHEND", t.KonsistenzAbweichend);
            t.MeldungFehleingabe = Text_("ZPG_MSG_FEHLEINGABE", t.MeldungFehleingabe);

            t.KnopfStochastik = Text_("ZPG_BTN_STOCHASTIK", t.KnopfStochastik);
            t.KnopfStochastikTitel = Text_("ZPG_BTN_STOCHASTIK_TITEL", t.KnopfStochastikTitel);
            t.KnopfAuslegung = Text_("ZPG_BTN_AUSLEGUNG", t.KnopfAuslegung);
            t.KnopfMessdaten = Text_("ZPG_BTN_MESSDATEN", t.KnopfMessdaten);
            // Vergleich mit einer Messreihe und Kalibrierung (Stufe Z5, Gruppe 3)
            t.GruppeVergleich = Text_("ZPG_GRP_VERGLEICH", t.GruppeVergleich);
            t.LabelMessreihe = Text_("ZPG_LBL_MESSREIHE", t.LabelMessreihe);
            t.KnopfVergleich = Text_("ZPG_BTN_VERGLEICH", t.KnopfVergleich);
            t.KennzahlVergleich = Text_("ZPG_KZ_VERGLEICH", t.KennzahlVergleich);
            t.VergleichOhneReihe = Text_("ZPG_VERGL_OHNE_REIHE", t.VergleichOhneReihe);
            t.VergleichOhneWahl = Text_("ZPG_VERGL_OHNE_WAHL", t.VergleichOhneWahl);
            t.VergleichNurErweitert = Text_("ZPG_VERGL_NUR_ERWEITERT", t.VergleichNurErweitert);
            t.VergleichLeer = Text_("ZPG_VERGL_LEER", t.VergleichLeer);
            t.VergleichLaeuft = Text_("ZPG_VERGL_LAEUFT", t.VergleichLaeuft);
            t.VergleichAbgebrochen = Text_("ZPG_VERGL_ABGEBROCHEN", t.VergleichAbgebrochen);
            t.VergleichVeraltet = Text_("ZPG_VERGL_VERALTET", t.VergleichVeraltet);
            t.VergleichStatus = Text_("ZPG_VERGL_STATUS", t.VergleichStatus);
            t.VergleichStochastisch = Text_("ZPG_VERGL_STOCHASTISCH", t.VergleichStochastisch);
            t.KzEnergie = Text_("ZPG_KZ_ENERGIE_VERHAELTNIS", t.KzEnergie);
            t.KzEnergieAbweichung = Text_("ZPG_KZ_ENERGIE_ABWEICHUNG", t.KzEnergieAbweichung);
            t.KzSpitze = Text_("ZPG_KZ_SPITZE", t.KzSpitze);
            t.KzBand = Text_("ZPG_KZ_BAND", t.KzBand);
            t.KzSpitzenstreuung = Text_("ZPG_KZ_SPITZENSTREUUNG", t.KzSpitzenstreuung);
            t.KzWurzelN = Text_("ZPG_KZ_WURZELN", t.KzWurzelN);
            t.KzForm = Text_("ZPG_KZ_FORM", t.KzForm);
            t.KzFormTagtyp = Text_("ZPG_KZ_FORM_TAGTYP", t.KzFormTagtyp);
            t.KzMonate = Text_("ZPG_KZ_MONATE", t.KzMonate);
            t.LageImBand = Text_("ZPG_LAGE_IM_BAND", t.LageImBand);
            t.LageOberhalb = Text_("ZPG_LAGE_OBERHALB", t.LageOberhalb);
            t.LageUnterhalb = Text_("ZPG_LAGE_UNTERHALB", t.LageUnterhalb);
            t.LageUnbestimmt = Text_("ZPG_LAGE_UNBESTIMMT", t.LageUnbestimmt);
            t.LageNichtBewertbar = Text_("ZPG_LAGE_NICHT_BEWERTBAR", t.LageNichtBewertbar);
            t.VermerkBandNichtBewertbar = Text_("ZPG_VERGL_BAND_NICHT_BEWERTBAR", t.VermerkBandNichtBewertbar);
            t.FormImRahmen = Text_("ZPG_FORM_IM_RAHMEN", t.FormImRahmen);
            t.FormUeberSchwelle = Text_("ZPG_FORM_UEBER_SCHWELLE", t.FormUeberSchwelle);
            t.VermerkOhneEnsemble = Text_("ZPG_VERGL_OHNE_ENSEMBLE", t.VermerkOhneEnsemble);
            t.VermerkEnsembleZonen = Text_("ZPG_VERGL_ENSEMBLE_ZONEN", t.VermerkEnsembleZonen);
            t.VermerkOhneWert = Text_("ZPG_VERGL_OHNE_WERT", t.VermerkOhneWert);
            t.VermerkMonat = Text_("ZPG_VERGL_MONAT", t.VermerkMonat);
            t.VermerkTage = Text_("ZPG_VERGL_TAGE", t.VermerkTage);
            t.VermerkDauerlinie = Text_("ZPG_VERGL_DAUERLINIE", t.VermerkDauerlinie);
            t.VermerkEinheiten = Text_("ZPG_VERGL_EINHEITEN", t.VermerkEinheiten);
            t.VermerkStreubreite = Text_("ZPG_VERGL_STREUBREITE", t.VermerkStreubreite);
            t.KnopfKalibrieren = Text_("ZPG_BTN_KALIBRIEREN", t.KnopfKalibrieren);
            t.KnopfKalibriervorschlag = Text_("ZPG_BTN_VORSCHLAG_KALIBRIERT", t.KnopfKalibriervorschlag);
            t.HinweisKalibrieren = Text_("ZPG_HINW_KALIBRIEREN", t.HinweisKalibrieren);
            t.FrageKalibrieren = Text_("ZPG_FRAGE_KALIBRIEREN", t.FrageKalibrieren);
            t.StatusKalibriert = Text_("ZPG_STATUS_KALIBRIERT", t.StatusKalibriert);
            t.KalibrierungLaeuft = Text_("ZPG_KAL_LAEUFT", t.KalibrierungLaeuft);
            t.KalibrierungAbgebrochen = Text_("ZPG_KAL_ABGEBROCHEN", t.KalibrierungAbgebrochen);
            t.VorschlagTitel = Text_("ZPG_VORSCHLAG_TITEL", t.VorschlagTitel);
            t.VorschlagVorlage = Text_("ZPG_VORSCHLAG_VORLAGE", t.VorschlagVorlage);
            t.VorschlagKopie = Text_("ZPG_VORSCHLAG_KOPIE", t.VorschlagKopie);
            t.VorschlagTagesbedarf = Text_("ZPG_VORSCHLAG_TAGESBEDARF", t.VorschlagTagesbedarf);
            t.VorschlagJeEinheit = Text_("ZPG_VORSCHLAG_JE_EINHEIT", t.VorschlagJeEinheit);
            t.VorschlagTage = Text_("ZPG_VORSCHLAG_TAGE", t.VorschlagTage);
            t.VorschlagWoche = Text_("ZPG_VORSCHLAG_WOCHE", t.VorschlagWoche);
            t.VorschlagGaenge = Text_("ZPG_VORSCHLAG_GAENGE", t.VorschlagGaenge);
            t.VorschlagStunde = Text_("ZPG_VORSCHLAG_STUNDE", t.VorschlagStunde);
            t.VorschlagHinweis = Text_("ZPG_VORSCHLAG_HINWEIS", t.VorschlagHinweis);
            t.KnopfVorschlagUebernehmen = Text_("ZPG_BTN_VORSCHLAG_UEBERNEHMEN", t.KnopfVorschlagUebernehmen);
            t.FrageVorschlag = Text_("ZPG_FRAGE_VORSCHLAG", t.FrageVorschlag);
            t.VorschlagOhne = Text_("ZPG_VORSCHLAG_OHNE", t.VorschlagOhne);
            // Der Titel der Überlagerung steht als ZPG_-Schlüssel (jede Beschriftung dieses Bündels tut
            // das); sein Wort gleicht dem Titel des eingebetteten Dialogs (ZPGM_TITEL).
            t.MessdatenTitel = Text_("ZPG_MESSDATEN_TITEL", t.MessdatenTitel);
            t.StatusAuslegung = Text_("ZPG_STATUS_AUSLEGUNG", t.StatusAuslegung);
            t.AuslegungOhnePunkt = Text_("ZPG_AUSLEGUNG_OHNE_PUNKT", t.AuslegungOhnePunkt);
            t.PunktUeberholt = Text_("ZPG_PUNKT_UEBERHOLT", t.PunktUeberholt);
            t.StatusVorschau = Text_("ZPG_STATUS_VORSCHAU", t.StatusVorschau);
            t.StatusOhneVorschau = Text_("ZPG_STATUS_OHNE_VORSCHAU", t.StatusOhneVorschau);
            t.HinweisWeitereExperte = Text_("ZPG_HINW_WEITERE_EXPERTE", t.HinweisWeitereExperte);
            t.GrundDauerlinie = Text_("ZPG_GRUND_DAUERLINIE", t.GrundDauerlinie);
            t.VorgabeEintrag = Text_("ZPG_VORGABE_EINTRAG", t.VorgabeEintrag);
            t.OptionAuto = Text_("ZPG_AUS_AUTO", t.OptionAuto);
            t.OptionManuell = Text_("ZPG_AUS_MANUELL", t.OptionManuell);
            t.OptionJa = Text_("ZPG_OPT_JA", t.OptionJa);
            t.OptionNein = Text_("ZPG_OPT_NEIN", t.OptionNein);
            t.FeldZeile = Text_("ZPG_FELD_ZEILE", t.FeldZeile);
            t.SpalteTopologie = Text_("ZPG_SP_TOPOLOGIE", t.SpalteTopologie);
            t.SpalteAnteil = Text_("ZPG_SP_ANTEIL", t.SpalteAnteil);
            t.SpalteRechenweg = Text_("ZPG_SP_RECHENWEG", t.SpalteRechenweg);
            t.RechenwegKatalog = Text_("ZPG_RECHENWEG_KATALOG", t.RechenwegKatalog);
            t.RechenwegManuell = Text_("ZPG_RECHENWEG_MANUELL", t.RechenwegManuell);
            t.RechenwegMesswert = Text_("ZPG_RECHENWEG_MESSWERT", t.RechenwegMesswert);
            t.TopologieSpeicher = Text_("ZPG_AUS_TOPOLOGIE_SPEICHER", t.TopologieSpeicher);
            t.TopologieFrischwasserstation = Text_("ZPG_AUS_TOPOLOGIE_FRISCHWASSERSTATION", t.TopologieFrischwasserstation);
            t.TopologieDurchfluss = Text_("ZPG_AUS_TOPOLOGIE_DURCHFLUSS", t.TopologieDurchfluss);
            t.TopologieWohnungsstation = Text_("ZPG_AUS_TOPOLOGIE_WOHNUNGSSTATION", t.TopologieWohnungsstation);
            t.KatalogAufgeklappt = Text_("ZPG_KAT_AUFGEKLAPPT", t.KatalogAufgeklappt);
            t.SpalteKalender = Text_("ZPG_SP_KALENDER", t.SpalteKalender);
            t.HinweisBezugsmengeWohnungstabelle = Text_("ZPG_HINW_BEZUGSMENGE_WOHNUNGSTABELLE", t.HinweisBezugsmengeWohnungstabelle);
            t.GruppeBelegung = Text_("ZPG_GRP_BELEGUNG", t.GruppeBelegung);
            t.LabelWohnungstabelle = Text_("ZPG_LBL_WOHNUNGSTABELLE", t.LabelWohnungstabelle);
            t.LabelAnzahl = Text_("ZPG_LBL_ANZAHL", t.LabelAnzahl);
            t.LabelRaumzahl = Text_("ZPG_LBL_RAUMZAHL", t.LabelRaumzahl);
            t.LabelPersonen = Text_("ZPG_LBL_PERSONEN", t.LabelPersonen);
            t.LabelAusstattung = Text_("ZPG_LBL_AUSSTATTUNG", t.LabelAusstattung);
            t.SpalteAktion = Text_("ZPG_SP_AKTION", t.SpalteAktion);
            t.KnopfWohnungNeu = Text_("ZPG_BTN_WOHNUNG_NEU", t.KnopfWohnungNeu);
            t.KnopfWohnungEntfernen = Text_("ZPG_BTN_WOHNUNG_ENTFERNEN", t.KnopfWohnungEntfernen);
            t.AusstattungVorgabe = Text_("ZPG_AUSSTATTUNG_VORGABE", t.AusstattungVorgabe);
            t.HinweisWohnungstabelle = Text_("ZPG_HINW_WOHNUNGSTABELLE", t.HinweisWohnungstabelle);
            t.HinweisWohnungstabelleLeer = Text_("ZPG_HINW_WOHNUNGSTABELLE_LEER", t.HinweisWohnungstabelleLeer);
            t.LabelPersonenJeWe = Text_("ZPG_LBL_PERSONEN_JE_WE", t.LabelPersonenJeWe);
            t.EinheitPersonenJeWe = Text_("ZPG_EINHEIT_PERSONEN_JE_WE", t.EinheitPersonenJeWe);
            t.HinweisPersonenJeWe = Text_("ZPG_HINW_PERSONEN_JE_WE", t.HinweisPersonenJeWe);
            t.LabelWohnflaecheJeWe = Text_("ZPG_LBL_WOHNFLAECHE_JE_WE", t.LabelWohnflaecheJeWe);
            t.HinweisWohnflaecheJeWe = Text_("ZPG_HINW_WOHNFLAECHE_JE_WE", t.HinweisWohnflaecheJeWe);
            t.LabelTopologie = Text_("ZPG_LBL_TOPOLOGIE", t.LabelTopologie);
            t.HinweisTopologie = Text_("ZPG_HINW_TOPOLOGIE", t.HinweisTopologie);
            t.LabelZirkulationVorhanden = Text_("ZPG_LBL_ZIRKULATION_VORHANDEN", t.LabelZirkulationVorhanden);
            t.HinweisZirkulationVorhanden = Text_("ZPG_HINW_ZIRKULATION_VORHANDEN", t.HinweisZirkulationVorhanden);
            t.LabelKalender = Text_("ZPG_LBL_KALENDER", t.LabelKalender);
            t.KalenderNutzungsart = Text_("ZPG_KALENDER_NUTZUNGSART", t.KalenderNutzungsart);
            t.KalenderGebaeude = Text_("ZPG_KALENDER_GEBAEUDE", t.KalenderGebaeude);
            t.HinweisKalender = Text_("ZPG_HINW_KALENDER", t.HinweisKalender);
            t.LabelFerien = Text_("ZPG_LBL_FERIEN", t.LabelFerien);
            t.LabelBeginnTag = Text_("ZPG_LBL_BEGINN_TAG", t.LabelBeginnTag);
            t.LabelBeginnMonat = Text_("ZPG_LBL_BEGINN_MONAT", t.LabelBeginnMonat);
            t.LabelEndeTag = Text_("ZPG_LBL_ENDE_TAG", t.LabelEndeTag);
            t.LabelEndeMonat = Text_("ZPG_LBL_ENDE_MONAT", t.LabelEndeMonat);
            t.HinweisFerien = Text_("ZPG_HINW_FERIEN", t.HinweisFerien);
            t.LabelBundesland = Text_("ZPG_LBL_BUNDESLAND", t.LabelBundesland);
            t.BundeslandKlimaregion = Text_("ZPG_BUNDESLAND_KLIMAREGION", t.BundeslandKlimaregion);
            t.GrundBundesland = Text_("ZPG_GRUND_BUNDESLAND", t.GrundBundesland);
            t.LabelJahresmesswert = Text_("ZPG_LBL_JAHRESMESSWERT", t.LabelJahresmesswert);
            t.LabelMesswertEinheit = Text_("ZPG_LBL_MESSWERT_EINHEIT", t.LabelMesswertEinheit);
            t.MesswertKwh = Text_("ZPG_MESSWERT_KWH", t.MesswertKwh);
            t.MesswertM3 = Text_("ZPG_MESSWERT_M3", t.MesswertM3);
            t.LabelMesswertGrenze = Text_("ZPG_LBL_MESSWERT_GRENZE", t.LabelMesswertGrenze);
            t.GrenzeZapfstelle = Text_("ZPG_GRENZE_ZAPFSTELLE", t.GrenzeZapfstelle);
            t.GrenzeVerteilung = Text_("ZPG_GRENZE_VERTEILUNG", t.GrenzeVerteilung);
            t.GrenzeSpeicher = Text_("ZPG_GRENZE_SPEICHER", t.GrenzeSpeicher);
            t.LabelSpeicherverlust = Text_("ZPG_LBL_SPEICHERVERLUST", t.LabelSpeicherverlust);
            t.LabelMesswertQuelle = Text_("ZPG_LBL_MESSWERT_QUELLE", t.LabelMesswertQuelle);
            t.LabelMesswertZeitraum = Text_("ZPG_LBL_MESSWERT_ZEITRAUM", t.LabelMesswertZeitraum);
            t.HinweisJahresmesswert = Text_("ZPG_HINW_JAHRESMESSWERT", t.HinweisJahresmesswert);
            t.GruppeSchaetzhilfen = Text_("ZPG_GRP_SCHAETZHILFEN", t.GruppeSchaetzhilfen);
            t.HinweisSchaetzhilfen = Text_("ZPG_HINW_SCHAETZHILFEN", t.HinweisSchaetzhilfen);
            t.LabelTagesbedarf = Text_("ZPG_LBL_TAGESBEDARF", t.LabelTagesbedarf);
            t.LabelTagesbedarfManuell = Text_("ZPG_LBL_TAGESBEDARF_MANUELL", t.LabelTagesbedarfManuell);
            t.LabelVorschlag = Text_("ZPG_LBL_VORSCHLAG", t.LabelVorschlag);
            t.KnopfVorschlag = Text_("ZPG_BTN_VORSCHLAG", t.KnopfVorschlag);
            t.HinweisVorschlagOhne = Text_("ZPG_HINW_VORSCHLAG_OHNE", t.HinweisVorschlagOhne);
            t.HinweisOhneVorschlag = Text_("ZPG_HINW_OHNE_VORSCHLAG", t.HinweisOhneVorschlag);
            t.HinweisManuell = Text_("ZPG_HINW_MANUELL", t.HinweisManuell);
            t.LabelAngesetzt = Text_("ZPG_LBL_ANGESETZT", t.LabelAngesetzt);
            t.AngesetztKalibriert = Text_("ZPG_ANGESETZT_KALIBRIERT", t.AngesetztKalibriert);
            t.LabelRechenweg = Text_("ZPG_LBL_RECHENWEG", t.LabelRechenweg);
            t.LabelLadeleistung = Text_("ZPG_LBL_LADELEISTUNG", t.LabelLadeleistung);
            t.LabelLadeleistungManuell = Text_("ZPG_LBL_LADELEISTUNG_MANUELL", t.LabelLadeleistungManuell);
            t.LabelLadefenster = Text_("ZPG_LBL_LADEFENSTER", t.LabelLadefenster);
            t.LabelLadefensterBeginn = Text_("ZPG_LBL_LADEFENSTER_BEGINN", t.LabelLadefensterBeginn);
            t.HinweisLadefenster = Text_("ZPG_HINW_LADEFENSTER", t.HinweisLadefenster);
            t.HinweisLadeleistung = Text_("ZPG_HINW_LADELEISTUNG", t.HinweisLadeleistung);
            t.LadeAngesetztAuto = Text_("ZPG_LADE_ANGESETZT_AUTO", t.LadeAngesetztAuto);
            t.LadeAngesetztManuell = Text_("ZPG_LADE_ANGESETZT_MANUELL", t.LadeAngesetztManuell);
            t.LabelZirkulation = Text_("ZPG_LBL_ZIRKULATION", t.LabelZirkulation);
            t.LabelZirkManuell = Text_("ZPG_LBL_ZIRK_MANUELL", t.LabelZirkManuell);
            t.LabelZirkMethode = Text_("ZPG_LBL_ZIRK_METHODE", t.LabelZirkMethode);
            t.ZirkMethodeLaenge = Text_("ZPG_ZIRK_METHODE_LAENGE", t.ZirkMethodeLaenge);
            t.ZirkMethodeAnteil = Text_("ZPG_ZIRK_METHODE_ANTEIL", t.ZirkMethodeAnteil);
            t.ZirkMethodeFlaeche = Text_("ZPG_ZIRK_METHODE_FLAECHE", t.ZirkMethodeFlaeche);
            t.LabelZirkLaenge = Text_("ZPG_LBL_ZIRK_LAENGE", t.LabelZirkLaenge);
            t.LabelZirkVerlust = Text_("ZPG_LBL_ZIRK_VERLUST", t.LabelZirkVerlust);
            t.LabelZirkAnteil = Text_("ZPG_LBL_ZIRK_ANTEIL", t.LabelZirkAnteil);
            t.LabelZirkLage = Text_("ZPG_LBL_ZIRK_LAGE", t.LabelZirkLage);
            t.LageInnen = Text_("ZPG_LAGE_INNEN", t.LageInnen);
            t.LageAussen = Text_("ZPG_LAGE_AUSSEN", t.LageAussen);
            t.HinweisZirkVorgaben = Text_("ZPG_HINW_ZIRK_VORGABEN", t.HinweisZirkVorgaben);
            t.HinweisZirkulation = Text_("ZPG_HINW_ZIRKULATION", t.HinweisZirkulation);
            t.LabelLeitungsinhalt = Text_("ZPG_LBL_LEITUNGSINHALT", t.LabelLeitungsinhalt);
            t.HinweisLeitungsinhalt = Text_("ZPG_HINW_LEITUNGSINHALT", t.HinweisLeitungsinhalt);
            t.GruppeFachwerte = Text_("ZPG_GRP_FACHWERTE", t.GruppeFachwerte);
            t.LabelBedarfSpez = Text_("ZPG_LBL_BEDARF_SPEZ", t.LabelBedarfSpez);
            t.HinweisBedarfSpez = Text_("ZPG_HINW_BEDARF_SPEZ", t.HinweisBedarfSpez);
            t.LabelZapftemperatur = Text_("ZPG_LBL_ZAPFTEMPERATUR", t.LabelZapftemperatur);
            t.HinweisZapftemperatur = Text_("ZPG_HINW_ZAPFTEMPERATUR", t.HinweisZapftemperatur);
            t.LabelKaltwasserMittel = Text_("ZPG_LBL_KALTWASSER_MITTEL", t.LabelKaltwasserMittel);
            t.LabelKaltwasserAmplitude = Text_("ZPG_LBL_KALTWASSER_AMPLITUDE", t.LabelKaltwasserAmplitude);
            t.HinweisKaltwasser = Text_("ZPG_HINW_KALTWASSER", t.HinweisKaltwasser);
            t.LabelAuslastungsgang = Text_("ZPG_LBL_AUSLASTUNGSGANG", t.LabelAuslastungsgang);
            t.HinweisAuslastungsgang = Text_("ZPG_HINW_AUSLASTUNGSGANG", t.HinweisAuslastungsgang);
            t.LabelTagesgangsatz = Text_("ZPG_LBL_TAGESGANGSATZ", t.LabelTagesgangsatz);
            t.GruppeTyptage = Text_("ZPG_GRP_TYPTAGE", t.GruppeTyptage);
            t.LabelTyptageAktiv = Text_("ZPG_LBL_TYPTAGE_AKTIV", t.LabelTyptageAktiv);
            t.LabelTyptageZone = Text_("ZPG_LBL_TYPTAGE_ZONE", t.LabelTyptageZone);
            t.LabelTyptageGebaeudeart = Text_("ZPG_LBL_TYPTAGE_GEBAEUDEART", t.LabelTyptageGebaeudeart);
            t.HinweisTyptage = Text_("ZPG_HINW_TYPTAGE", t.HinweisTyptage);
            t.HinweisTyptageOhne = Text_("ZPG_HINW_TYPTAGE_OHNE", t.HinweisTyptageOhne);
            t.KnopfTyptage = Text_("ZPG_BTN_TYPTAGE", t.KnopfTyptage);
            t.TagesgangsatzVorgabe = Text_("ZPG_TAGESGANGSATZ_VORGABE", t.TagesgangsatzVorgabe);
            t.HinweisTagesgangsatz = Text_("ZPG_HINW_TAGESGANGSATZ", t.HinweisTagesgangsatz);
            t.KnopfTagesgang = Text_("ZPG_BTN_TAGESGANG", t.KnopfTagesgang);
            t.KnopfKategorien = Text_("ZPG_BTN_KATEGORIEN", t.KnopfKategorien);
            t.GruppeFachwerteGebaeude = Text_("ZPG_GRP_FACHWERTE_GEBAEUDE", t.GruppeFachwerteGebaeude);
            t.LabelKaltwasserAuslegung = Text_("ZPG_LBL_KALTWASSER_AUSLEGUNG", t.LabelKaltwasserAuslegung);
            t.HinweisKaltwasserAuslegung = Text_("ZPG_HINW_KALTWASSER_AUSLEGUNG", t.HinweisKaltwasserAuslegung);
            t.LabelSpeichertemperatur = Text_("ZPG_LBL_SPEICHERTEMPERATUR", t.LabelSpeichertemperatur);
            t.HinweisSpeichertemperatur = Text_("ZPG_HINW_SPEICHERTEMPERATUR", t.HinweisSpeichertemperatur);
            t.LabelZirkKennwert = Text_("ZPG_LBL_ZIRK_KENNWERT", t.LabelZirkKennwert);
            t.LabelZirkFlaeche = Text_("ZPG_LBL_ZIRK_FLAECHE", t.LabelZirkFlaeche);
            t.LabelZirkLaufzeit = Text_("ZPG_LBL_ZIRK_LAUFZEIT", t.LabelZirkLaufzeit);
            t.HinweisZirkExperte = Text_("ZPG_HINW_ZIRK_EXPERTE", t.HinweisZirkExperte);
            t.LabelAnzeigetemperatur = Text_("ZPG_LBL_ANZEIGETEMPERATUR", t.LabelAnzeigetemperatur);
            t.LabelStundenschwelle = Text_("ZPG_LBL_STUNDENSCHWELLE", t.LabelStundenschwelle);
            t.HinweisAnzeige = Text_("ZPG_HINW_ANZEIGE", t.HinweisAnzeige);
            t.UnterschriftDauerlinie = Text_("ZPG_UNTERSCHRIFT_DAUERLINIE", t.UnterschriftDauerlinie);
            t.DauerlinieMarke = Text_("ZPG_DAUERLINIE_MARKE", t.DauerlinieMarke);
            t.DauerlinieSchwelle = Text_("ZPG_DAUERLINIE_SCHWELLE", t.DauerlinieSchwelle);
            t.DauerlinieOhne = Text_("ZPG_DAUERLINIE_OHNE", t.DauerlinieOhne);
            t.GruppeWarnliste = Text_("ZPG_GRP_WARNLISTE", t.GruppeWarnliste);
            t.WarnlisteUnter = Text_("ZPG_WARNLISTE_UNTER", t.WarnlisteUnter);
            t.WarnlisteLeer = Text_("ZPG_WARNLISTE_LEER", t.WarnlisteLeer);
            t.GruppeHerkunft = Text_("ZPG_GRP_HERKUNFT", t.GruppeHerkunft);
            t.HerkunftUnter = Text_("ZPG_HERKUNFT_UNTER", t.HerkunftUnter);
            t.HerkunftLeer = Text_("ZPG_HERKUNFT_LEER", t.HerkunftLeer);
            t.HerkunftSpalteGroesse = Text_("ZPG_HERKUNFT_SP_GROESSE", t.HerkunftSpalteGroesse);
            t.HerkunftSpalteWert = Text_("ZPG_HERKUNFT_SP_WERT", t.HerkunftSpalteWert);
            t.HerkunftSpalteZone = Text_("ZPG_HERKUNFT_SP_ZONE", t.HerkunftSpalteZone);
            t.HerkunftSpalteStand = Text_("ZPG_HERKUNFT_SP_STAND", t.HerkunftSpalteStand);
            t.HerkunftSpalteQuelle = Text_("ZPG_HERKUNFT_SP_QUELLE", t.HerkunftSpalteQuelle);
            t.HerkunftSpalteVermerk = Text_("ZPG_HERKUNFT_SP_VERMERK", t.HerkunftSpalteVermerk);
            t.StufeWarnung = Text_("ZPG_AUS_STUFE_WARNUNG", t.StufeWarnung);
            t.StufeHinweis = Text_("ZPG_AUS_STUFE_HINWEIS", t.StufeHinweis);
            // Editoren Tagesgang und Zapfkategorien (Z4, Gruppe 2b)
            t.GrundOhneNutzungsart = Text_("ZPG_GRUND_OHNE_NUTZUNGSART", t.GrundOhneNutzungsart);
            t.TagtypRuhetag = Text_("ZPG_TAGTYP_RUHETAG", t.TagtypRuhetag);
            t.TgeTitel = Text_("ZPG_TGE_TITEL", t.TgeTitel);
            t.TgeKontext = Text_("ZPG_TGE_KONTEXT", t.TgeKontext);
            t.TgeGruppeTagesgang = Text_("ZPG_TGE_GRP_TAGESGANG", t.TgeGruppeTagesgang);
            t.TgeLabelTagtyp = Text_("ZPG_TGE_LBL_TAGTYP", t.TgeLabelTagtyp);
            t.TgeFeldStunde = Text_("ZPG_TGE_FELD_STUNDE", t.TgeFeldStunde);
            t.TgeHerkunft = Text_("ZPG_TGE_HERKUNFT", t.TgeHerkunft);
            t.SummeOk = Text_("ZPG_TGE_SUMME_OK", t.SummeOk);
            t.SummeAbweichend = Text_("ZPG_TGE_SUMME_ABWEICHEND", t.SummeAbweichend);
            t.SummeNull = Text_("ZPG_TGE_SUMME_NULL", t.SummeNull);
            t.TgeVorschauNormiert = Text_("ZPG_TGE_VORSCHAU_NORMIERT", t.TgeVorschauNormiert);
            t.KnopfNormieren = Text_("ZPG_TGE_BTN_NORMIEREN", t.KnopfNormieren);
            t.KnopfTagKopieren = Text_("ZPG_TGE_BTN_TAG_KOPIEREN", t.KnopfTagKopieren);
            t.KnopfTagEinfuegen = Text_("ZPG_TGE_BTN_TAG_EINFUEGEN", t.KnopfTagEinfuegen);
            t.TgeGrundEinfuegen = Text_("ZPG_TGE_GRUND_EINFUEGEN", t.TgeGrundEinfuegen);
            t.TgeGruppeWoche = Text_("ZPG_TGE_GRP_WOCHE", t.TgeGruppeWoche);
            t.TgeHinweisWoche = Text_("ZPG_TGE_HINW_WOCHE", t.TgeHinweisWoche);
            t.TgeGruppeVorlage = Text_("ZPG_TGE_GRP_VORLAGE", t.TgeGruppeVorlage);
            t.TgeLabelVorlage = Text_("ZPG_TGE_LBL_VORLAGE", t.TgeLabelVorlage);
            t.TgeKnopfVorlage = Text_("ZPG_TGE_BTN_VORLAGE", t.TgeKnopfVorlage);
            t.TgeHinweisVorlage = Text_("ZPG_TGE_HINW_VORLAGE", t.TgeHinweisVorlage);
            t.TgeGrundVorlage = Text_("ZPG_TGE_GRUND_VORLAGE", t.TgeGrundVorlage);
            t.TgeStatusGespeichert = Text_("ZPG_TGE_STATUS_GESPEICHERT", t.TgeStatusGespeichert);
            t.KnopfZuruecksetzen = Text_("ZPG_BTN_ZURUECKSETZEN", t.KnopfZuruecksetzen);
            t.KnopfKopie = Text_("ZPG_BTN_KOPIE", t.KnopfKopie);
            t.LabelKatalogversion = Text_("ZPG_LBL_KATALOGVERSION_KOPIE", t.LabelKatalogversion);
            t.HinweisKopie = Text_("ZPG_HINW_KOPIE", t.HinweisKopie);
            t.HinweisFrei = Text_("ZPG_HINW_FREI", t.HinweisFrei);
            t.HinweisNurLesen = Text_("ZPG_HINW_NUR_LESEN", t.HinweisNurLesen);
            t.KatTitel = Text_("ZPG_KATEG_TITEL", t.KatTitel);
            t.KatKontext = Text_("ZPG_KATEG_KONTEXT", t.KatKontext);
            t.KatHinweisRegeln = Text_("ZPG_KATEG_HINW_REGELN", t.KatHinweisRegeln);
            t.KatSpalteReihenfolge = Text_("ZPG_KATEG_SP_REIHENFOLGE", t.KatSpalteReihenfolge);
            t.KatSpalteName = Text_("ZPG_KATEG_SP_NAME", t.KatSpalteName);
            t.KatSpalteVolumenstrom = Text_("ZPG_KATEG_SP_VOLUMENSTROM", t.KatSpalteVolumenstrom);
            t.KatSpalteDauer = Text_("ZPG_KATEG_SP_DAUER", t.KatSpalteDauer);
            t.KatSpalteAnteil = Text_("ZPG_KATEG_SP_ANTEIL", t.KatSpalteAnteil);
            t.KatSpalteStreuung = Text_("ZPG_KATEG_SP_STREUUNG", t.KatSpalteStreuung);
            t.KatSpalteKappung = Text_("ZPG_KATEG_SP_KAPPUNG", t.KatSpalteKappung);
            t.KatSpalteHerkunft = Text_("ZPG_KATEG_SP_HERKUNFT", t.KatSpalteHerkunft);
            t.KatKappungKeine = Text_("ZPG_KATEG_KAPPUNG_KEINE", t.KatKappungKeine);
            t.KatKnopfNeu = Text_("ZPG_KATEG_BTN_NEU", t.KatKnopfNeu);
            t.KatKnopfEntfernen = Text_("ZPG_KATEG_BTN_ENTFERNEN", t.KatKnopfEntfernen);
            t.KatKnopfHoch = Text_("ZPG_KATEG_BTN_HOCH", t.KatKnopfHoch);
            t.KatKnopfRunter = Text_("ZPG_KATEG_BTN_RUNTER", t.KatKnopfRunter);
            t.KatGrundLetzte = Text_("ZPG_KATEG_GRUND_LETZTE", t.KatGrundLetzte);
            t.KatGrundRand = Text_("ZPG_KATEG_GRUND_RAND", t.KatGrundRand);
            t.KatKnopfVorgabe = Text_("ZPG_KATEG_BTN_VORGABE", t.KatKnopfVorgabe);
            t.KatHinweisVorgabe = Text_("ZPG_KATEG_HINW_VORGABE", t.KatHinweisVorgabe);
            t.KatGrundOhneVorgabe = Text_("ZPG_KATEG_GRUND_OHNE_VORGABE", t.KatGrundOhneVorgabe);
            t.KatNeuName = Text_("ZPG_KATEG_NEU_NAME", t.KatNeuName);
            t.KatLeer = Text_("ZPG_KATEG_LEER", t.KatLeer);
            t.KatStatusGespeichert = Text_("ZPG_KATEG_STATUS_GESPEICHERT", t.KatStatusGespeichert);
            return t;
        }

        /// <summary>Die Beschriftungen der Vorschaubilder in der Oberflächensprache.</summary>
        internal static ZapfprofilBildtexte Bildtexte()
        {
            var t = new ZapfprofilBildtexte();
            t.TitelTagesgang = Text_("ZPG_BILD_TAGESGANG", t.TitelTagesgang);
            t.TitelWochenprofil = Text_("ZPG_BILD_WOCHENPROFIL", t.TitelWochenprofil);
            t.TitelJahresgang = Text_("ZPG_BILD_JAHRESGANG", t.TitelJahresgang);
            t.Werktag = Text_("ZPG_TAGTYP_WERKTAG", t.Werktag);
            t.Samstag = Text_("ZPG_TAGTYP_SAMSTAG", t.Samstag);
            t.SonnFeiertag = Text_("ZPG_TAGTYP_SONNTAG", t.SonnFeiertag);
            t.Zapfung = Text_("ZPG_REIHE_ZAPFUNG", t.Zapfung);
            t.Zirkulation = Text_("ZPG_REIHE_ZIRKULATION", t.Zirkulation);
            t.AchseStunde = Text_("ZPG_ACHSE_STUNDE", t.AchseStunde);
            t.AchseWochenstunde = Text_("ZPG_ACHSE_WOCHENSTUNDE", t.AchseWochenstunde);
            t.AchseLeistung = Text_("ZPG_ACHSE_LEISTUNG", t.AchseLeistung);
            t.TitelDauerlinie = Text_("ZPG_BILD_DAUERLINIE", t.TitelDauerlinie);
            t.Gesamt = Text_("ZPG_REIHE_GESAMT", t.Gesamt);
            t.AchseRang = Text_("ZPG_ACHSE_RANG", t.AchseRang);
            t.Perzentil = Text_("ZPG_BILD_PERZENTIL", t.Perzentil);
            return t;
        }

        private static readonly string[] MONATE_DE =
        { "Januar", "Februar", "März", "April", "Mai", "Juni",
          "Juli", "August", "September", "Oktober", "November", "Dezember" };

        private static readonly string[] WOCHENTAGE_DE =
        { "Montag", "Dienstag", "Mittwoch", "Donnerstag", "Freitag", "Samstag", "Sonntag" };

        /// <summary>Der Monatsname 1 … 12 (<c>ALLG_MONAT_n</c>).</summary>
        internal static string Monatsname(int monat)
            => monat >= 1 && monat <= 12 ? Text_("ALLG_MONAT_" + monat.ToString(CultureInfo.InvariantCulture), MONATE_DE[monat - 1]) : "";

        /// <summary>Der Wochentagsname, Montag = 0 (<c>ALLG_WOCHENTAG_n</c>, n = 1 … 7).</summary>
        internal static string Wochentagsname(int wochentag)
            => wochentag >= 0 && wochentag < 7
                ? Text_("ALLG_WOCHENTAG_" + (wochentag + 1).ToString(CultureInfo.InvariantCulture), WOCHENTAGE_DE[wochentag])
                : "";

        /// <summary>Tag im Monat und Monat eines Jahrestags 1 … 365.</summary>
        internal static (int Tag, int Monat) TagUndMonat(int jahrestag)
        {
            int monat = Zapfkalender.Monat(jahrestag);
            int vorher = 0;
            for (int m = 0; m < monat - 1; m++) vorher += Zapfkalender.TageJeMonat[m];
            return (jahrestag - vorher, monat);
        }

        /// <summary>Ein Aufzählungsname in Großschreibung mit Unterstrich: <c>KalenderUngueltig</c> → <c>KALENDER_UNGUELTIG</c>.</summary>
        internal static string Gross(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            var sb = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (i > 0 && char.IsUpper(c)) sb.Append('_');
                sb.Append(char.ToUpperInvariant(c));
            }
            return sb.ToString();
        }

        private static string Format(string muster, params object[] werte)
        {
            try { return string.Format(CultureInfo.CurrentCulture, muster ?? "", werte); }
            catch (FormatException) { return muster ?? ""; }
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
