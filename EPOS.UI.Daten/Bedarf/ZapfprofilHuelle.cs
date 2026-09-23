using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using EPOS.UI.Dialoge.Bedarf;

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
    /// Was die Stufe Einfach nicht zeigt, bleibt am Arbeitsstand des Kerns stehen — eine
    /// Rückübersetzung setzt nur Name, Nutzungsart, Bezugsmenge, Niveau und Reihenfolge.</para>
    ///
    /// <para><b>Die Vorschau ruft den Lauf.</b> <see cref="Vorschau"/> geht über
    /// <see cref="BedarfsVorschauCtrl.ProjektVorschau"/> mit dem Arbeitsstand, also über
    /// denselben Generatoraufruf, den der Lauf nimmt (2.4); die Reihen für Tagesgang und Woche
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
    /// <c>Daten</c>, <c>Texte</c>, <c>Vorschau</c>, <c>Pruefen</c>, <c>AuslegungGaben</c>,
    /// <c>HilfeSchluessel</c> und <c>HilfeRechenweg</c> — die <c>[Parameter]</c> der Komponente
    /// <c>ZapfprofilDialog.razor</c>. <c>AuslegungGaben</c> baut je Öffnen den Parametersatz der
    /// Überlagerung „Auslegung" zum Arbeitsstand des Dialogs (<see cref="AuslegungGaben"/>).</para>
    /// </summary>
    internal static partial class ZapfprofilHuelle
    {
        /// <summary>Der Hilfeschlüssel des Dialogs (5.8).</summary>
        internal const string HILFE_DIALOG = "Form_Zapfprofil.btn_Help";

        /// <summary>Der Hilfeschlüssel der Berechnungsseite (5.8).</summary>
        internal const string HILFE_RECHENWEG = "Form_Zapfprofil_Berechnung";

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
            return new Dictionary<string, object>
            {
                ["Daten"] = Laden(idProjekt, basis),
                ["Texte"] = Texte(),
                ["Vorschau"] = new Func<ZapfprofilEingabeDaten, ZapfprofilVorschauDaten>(e => Vorschau(idProjekt, e, basis)),
                ["Pruefen"] = new Func<ZapfprofilEingabeDaten, IReadOnlyList<ZapfprofilMeldung>>(Pruefen),
                ["AuslegungGaben"] = new Func<ZapfprofilEingabeDaten, IReadOnlyDictionary<string, object>>(
                    e => AuslegungGaben(idProjekt, e, basis, ZapfprofilStufe.Einfach)),
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
                Stufe = ZapfprofilStufe.Einfach
            };

            ZapfVerfuegbarkeit verfuegbar = ZapfprofilCtrl.Verfuegbar();
            daten.Verfuegbar = verfuegbar.Ja;
            daten.Sperrgrund = verfuegbar.Ja ? "" : Verfuegbarkeitsgrund(verfuegbar.Grund);

            var projekt = new ProjektCtrl();
            if (idProjekt > 0) projekt.ReadSingle(idProjekt);
            daten.Kontext = Kontext(projekt);
            if (!verfuegbar.Ja) return daten;

            daten.Katalog = Katalog();
            daten.Vorschau = Vorschau(idProjekt, daten.Eingabe, stand);
            return daten;
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

        /// <summary>Der Arbeitsstand des Kerns als DTO der Stufe Einfach.</summary>
        internal static ZapfprofilEingabeDaten AlsEingabe(ZapfprofilStand stand)
        {
            var e = new ZapfprofilEingabeDaten { Weg = AlsWeg(stand?.Weg ?? BrauchwasserWeg.Bestand) };
            if (stand?.Zonen != null)
                foreach (ZonenStand z in stand.Zonen) e.Zonen.Add(AlsZone(z));
            return e;
        }

        /// <summary>Eine Zone des Kerns als DTO; die Bezugsmenge 0 gilt als „nicht eingegeben".</summary>
        internal static ZapfprofilZoneDaten AlsZone(ZonenStand z) => new ZapfprofilZoneDaten
        {
            Id = z.Id,
            Name = z.Name ?? "",
            IdNutzungsart = z.IdNutzungsart,
            Bezugsmenge = z.Bezugsmenge > 0 ? z.Bezugsmenge : null,
            Niveau = Enum.IsDefined(typeof(ZapfprofilNiveau), (int)z.Niveau) ? (ZapfprofilNiveau)(int)z.Niveau
                                                                              : ZapfprofilNiveau.Mittel,
            Ueberschrieben = Ueberschrieben(z)
        };

        /// <summary>
        /// Der Arbeitsstand des Dialogs für den Kern. Je Zone gilt als Grundlage: die Zone
        /// derselben Id im <paramref name="basis"/>-Stand, sonst die Vorlage eines Duplikats
        /// (mit neuen Ids), sonst eine neue Zone mit den Vorgaben. Gesetzt werden nur die Felder
        /// der Stufe Einfach und die Reihenfolge; Projektzeile und alles Übrige bleiben.
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

                zonen.Add(grund with
                {
                    Id = grund.Id,
                    Name = (d.Name ?? "").Trim(),
                    IdNutzungsart = d.IdNutzungsart,
                    Bezugsmenge = d.Bezugsmenge ?? 0.0,
                    Niveau = (ZapfNiveau)(int)d.Niveau,
                    Reihenfolge = i + 1
                });
            }

            // Die Auslegung (Z2): Mit OK der Überlagerung trägt der Arbeitsstand ihre Eingaben samt
            // Punkt — sie gehen in die Projektgrößen, ein konstruierter Tag als Entwurf mit; ohne
            // sie bleiben Projektgrößen und Entwurf der Basis, wie sie sind.
            ProjektStand projekt = basis?.Projekt;
            BedarfstagKatalogzeile entwurf = basis?.BedarfstagEntwurf;
            if (eingabe.Auslegung != null)
            {
                projekt = MitAuslegung(projekt ?? ZapfprofilCtrl.ProjektVorgabe(), eingabe.Auslegung);
                entwurf = EntwurfAus(eingabe.Auslegung);
            }
            return new ZapfprofilStand(AlsWeg(eingabe.Weg), zonen.AsReadOnly(), projekt) { BedarfstagEntwurf = entwurf };
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
        /// Wie viele Größen der höheren Stufen eine Zone überschreibt — jede nullbare Größe mit
        /// Wert, jeder Schalter abseits seiner Vorgabe, jedes Ferienfenster und jeder
        /// Auslastungsmonat mit Wert, eine gepflegte Wohnungstabelle. Die Bindung an ein Gebäude
        /// ist keine Überschreibung.
        /// </summary>
        internal static int Ueberschrieben(ZonenStand z)
        {
            if (z == null) return 0;
            int n = 0;
            if (z.IdTagesgangsatz.HasValue) n++;
            if (z.PersonenJeWe.HasValue) n++;
            if (z.WohnflaecheJeWeM2.HasValue) n++;
            if (z.Topologie != ZapfTopologie.Speicher) n++;
            if (!z.Zirkulation) n++;
            for (int i = 0; i < Math.Max(z.Ferienbeginn?.Length ?? 0, z.Ferienende?.Length ?? 0); i++)
            {
                int? b = z.Ferienbeginn != null && i < z.Ferienbeginn.Length ? z.Ferienbeginn[i] : null;
                int? e = z.Ferienende != null && i < z.Ferienende.Length ? z.Ferienende[i] : null;
                if (b.HasValue || e.HasValue) n++;
            }
            if (z.Jahresmesswert.HasValue) n++;
            if (z.SpeicherverlustKwhJeJahr.HasValue) n++;
            if (!z.TagesbedarfAuto) n++;
            if (z.TagesbedarfManuellKwh.HasValue) n++;
            if (z.BedarfSpezKwhJeEinheitTag.HasValue) n++;
            if (z.ZapftemperaturC.HasValue) n++;
            if (z.KaltwasserMittelC.HasValue) n++;
            if (z.KaltwasserAmplitudeK.HasValue) n++;
            if (z.Auslastung != null) n += z.Auslastung.Count(a => a.HasValue);
            if (z.Wohnungen != null && z.Wohnungen.Count > 0) n++;
            return n;
        }

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
                Sperrgrund = vollstaendig ? "" : Text_("ZPG_KAT_SPERRE_TAGESGANG", "Der Tagesgangsatz dieser Nutzungsart ist unvollständig.")
            };
        }

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
        /// sie zeigt, was das Zapfprofil rechnen würde. Ohne Zone keine Rechnung; kann der
        /// Generator für das Projekt nicht rechnen, der benannte Grund.
        /// </summary>
        internal static ZapfprofilVorschauDaten Vorschau(int idProjekt, ZapfprofilEingabeDaten eingabe,
                                                         ZapfprofilStand basis)
        {
            if (eingabe == null || eingabe.Zonen.Count == 0)
                return OhneVorschau(ZapfprofilVorschauZustand.NichtGerechnet, "ZPG_MSG_KEINE_ZONE",
                                    Text_("ZPG_MSG_KEINE_ZONE", "Es ist keine Zone angelegt."), "");

            ZapfVerfuegbarkeit verfuegbar = ZapfprofilCtrl.Verfuegbar();
            if (!verfuegbar.Ja)
                return OhneVorschau(ZapfprofilVorschauZustand.Abgebrochen, VerfuegbarkeitsKennung(verfuegbar.Grund),
                                    Verfuegbarkeitsgrund(verfuegbar.Grund), verfuegbar.Klartext);

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
                return AlsVorschau(v.Waerme.Zapfprofil, v.Waerme.WochentagJan1, v.Waerme.WochenendkennzeichenKopie(),
                                   eingabe);
            }
            catch (Exception ex) { return Unerwartet(ex.Message); }
        }

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
                Status = Text_("ZPG_STATUS_VORSCHAU", "Vorschau aktuell · deterministisch · Stochastik noch nicht gerechnet")
            };

            string summe = Text_("ZPG_ANSICHT_SUMME", "Summe aller Zonen");
            ZapfprofilAnsichtDaten gesamt = Ansicht(0, summe, false, e.Zapfung, e.Zirkulation, kalender, wochentagJan1, bildtexte);
            gesamt.Kennzahlen = Kennzahlen(e.Kennzahlen);
            vorschau.Ansichten.Add(gesamt);

            for (int i = 0; i < e.JeZone.Count; i++)
            {
                ZonenErgebnis z = e.JeZone[i];
                ZapfprofilZoneDaten d = eingabe != null && i < eingabe.Zonen.Count ? eingabe.Zonen[i] : null;
                string name = string.IsNullOrEmpty(z.Zone) ? d?.Name ?? "" : z.Zone;

                ZapfprofilAnsichtDaten a = Ansicht(z.IdZone, name, z.Abgelehnt, z.Zapfung, z.Zirkulation,
                                                   z.Kalender ?? grund, wochentagJan1, bildtexte);
                a.Kennzahlen = Kennzahlen(z, d != null && einheiten.TryGetValue(d.IdNutzungsart, out string eh) ? eh : "");
                vorschau.Ansichten.Add(a);

                vorschau.Zonen.Add(new ZapfprofilZonenwertDaten
                {
                    IdZone = z.IdZone,
                    Position = i,
                    Zone = name,
                    JahresbedarfZapfungKwh = z.JahresbedarfZapfungKwh,
                    Abgelehnt = z.Abgelehnt
                });
            }

            // Die Meldungen tragen die Position ihrer Zone, wo der Name sie eindeutig bestimmt —
            // der Dialog ordnet sie darüber zu, nicht über den Namen.
            IReadOnlyDictionary<string, int> position = EindeutigePositionen(e.JeZone.Select(z => z.Zone));
            foreach (ZapfAblehnung a in e.Ablehnungen) vorschau.Meldungen.Add(MitPosition(Meldung(a), position));
            foreach (ZapfHinweis h in e.Hinweise) vorschau.Meldungen.Add(MitPosition(Meldung(h), position));
            return vorschau;
        }

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
        /// </summary>
        internal static IReadOnlyList<ZapfprofilMeldung> Pruefen(ZapfprofilEingabeDaten eingabe)
        {
            var m = new List<ZapfprofilMeldung>();
            if (eingabe == null || eingabe.Zonen.Count == 0)
            {
                m.Add(Fehler("ZPG_MSG_KEINE_ZONE", "", Text_("ZPG_MSG_KEINE_ZONE", "Es ist keine Zone angelegt.")));
                return m;
            }
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
            }
            foreach (string doppelt in eingabe.Zonen.Select(z => (z.Name ?? "").Trim())
                                                    .Where(n => n.Length > 0)
                                                    .GroupBy(n => n, StringComparer.OrdinalIgnoreCase)
                                                    .Where(g => g.Count() > 1)
                                                    .Select(g => g.First()))
                m.Add(Fehler("ZPG_MSG_ZONE_NAME_DOPPELT", doppelt,
                    Format(Text_("ZPG_MSG_ZONE_NAME_DOPPELT", "Zone „{0}“: Der Name ist mehrfach vergeben — bitte jeder Zone einen eigenen Namen geben."), doppelt)));
            return m;
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
        /// oder je abgelehnter Zone ihr Satz („Zone „…“ trägt 0: …") in der Oberflächensprache;
        /// auf dem Bestandsweg leer.
        /// </summary>
        internal static string Leistenmeldung(BedarfsVorschau v)
        {
            if (v == null || !v.Zapfprofilweg) return "";
            if (v.Erfolgreich && v.Waerme?.Zapfprofil != null)
                return string.Join(Environment.NewLine, v.Waerme.Zapfprofil.Ablehnungen.Select(a => Meldung(a).Text));

            ZapfVerfuegbarkeit verfuegbar = ZapfprofilCtrl.Verfuegbar();
            if (!verfuegbar.Ja) return Verfuegbarkeitsgrund(verfuegbar.Grund);
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
        /// </summary>
        internal static ZapfprofilSpeicherergebnis BrauchwasserSchreiben(int idProjekt,
                                                                         List<Z_ProjektBrauchwasserModel> liste,
                                                                         ZapfprofilBehaelter behaelter)
        {
            var wizctrl = new WizardCtrl();
            ZapfprofilSpeicherergebnis e;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                if (!wizctrl.Del_Projekt_Brauchwasser(idProjekt, 0, v)
                    || !wizctrl.Add_Projekt_Brauchwasser(idProjekt, liste ?? new List<Z_ProjektBrauchwasserModel>(), v))
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

        /// <summary>Der Ressourcenschlüssel einer Ablehnung des Rechenwegs: <c>ZPG_EINGABE_…</c>.</summary>
        internal static string Schluessel(ZapfEingabefehler f) => "ZPG_EINGABE_" + Gross(f.ToString());

        /// <summary>Der Ressourcenschlüssel einer Ablehnung des Schreibwegs: <c>ZPG_SPEICHER_…</c>.</summary>
        internal static string Schluessel(ZapfSpeicherfehler f) => "ZPG_SPEICHER_" + Gross(f.ToString());

        /// <summary>Der Ressourcenschlüssel eines Fehlers des Parametersatzes: <c>ZPG_PARAMETER_…</c>.</summary>
        internal static string Schluessel(ParametersatzFehler f) => "ZPG_PARAMETER_" + Gross(f.ToString());

        /// <summary>Der Ressourcenschlüssel eines Hinweises des Rechenwegs: <c>ZPG_HINW_</c> + Kennung des Kerns.</summary>
        internal static string HinweisSchluessel(string code) => "ZPG_HINW_" + (code ?? "");

        /// <summary>Die Ablehnung des Schreibwegs als Meldung — „nicht gespeichert — [Zone „…“:] Grund".</summary>
        internal static ZapfprofilMeldung Meldung(ZapfprofilSpeicherException ex)
        {
            string schluessel = Schluessel(ex.Fehler);
            string grund = Text_(schluessel, ex.Message);
            if (!string.IsNullOrEmpty(ex.Zone))
                grund = Format(Text_("ZPG_MSG_ZONE", "Zone „{0}“: {1}"), ex.Zone, grund);
            string text = Format(Text_("ZPG_MSG_NICHT_GESPEICHERT", "Das Zapfprofil wurde nicht gespeichert — {0}"), grund);
            return new ZapfprofilMeldung(schluessel, ex.Zone ?? "", text, ZapfprofilMeldungsart.Fehler, ex.Message ?? "");
        }

        /// <summary>Eine Ablehnung des Rechenwegs: die Zone (bzw. die Zirkulation) trägt 0, mit Grund.</summary>
        internal static ZapfprofilMeldung Meldung(ZapfAblehnung a)
        {
            string schluessel = Schluessel(a.Grund);
            string grund = Text_(schluessel, a.Klartext);
            string text = string.IsNullOrEmpty(a.Zone)
                ? Format(Text_("ZPG_MSG_ANTEIL_TRAEGT_NULL", "Die Zirkulation trägt 0: {0}"), grund)
                : Format(Text_("ZPG_MSG_ZONE_TRAEGT_NULL", "Zone „{0}“ trägt 0: {1}"), a.Zone, grund);
            return new ZapfprofilMeldung(schluessel, a.Zone ?? "", text, ZapfprofilMeldungsart.Ablehnung, a.Klartext ?? "");
        }

        /// <summary>
        /// Ein Hinweis des Rechenwegs; eine Kennung ohne Ressource behält den Wortlaut des Kerns —
        /// benannt statt still.
        /// </summary>
        internal static ZapfprofilMeldung Meldung(ZapfHinweis h)
        {
            string schluessel = HinweisSchluessel(h.Code);
            string muster = Text_(schluessel, null);
            string text = muster == null ? h.Text : Format(muster, h.Zone ?? "");
            return new ZapfprofilMeldung(schluessel, h.Zone ?? "", text, ZapfprofilMeldungsart.Hinweis, h.Text ?? "");
        }

        private static string VerfuegbarkeitsKennung(ZapfVerfuegbarkeitsgrund g)
            => g == ZapfVerfuegbarkeitsgrund.TabellenFehlen ? "ZPG_MSG_TABELLEN_FEHLEN" : "ZPG_MSG_KEINE_KATALOGVERSION";

        /// <summary>Der Grund der Nichtverfügbarkeit in der Oberflächensprache.</summary>
        internal static string Verfuegbarkeitsgrund(ZapfVerfuegbarkeitsgrund g)
        {
            switch (g)
            {
                case ZapfVerfuegbarkeitsgrund.Verfuegbar: return "";
                case ZapfVerfuegbarkeitsgrund.TabellenFehlen:
                    return Text_("ZPG_MSG_TABELLEN_FEHLEN", "Der Zapfprofilgenerator ist in dieser Datenbank nicht verfügbar — es fehlen seine Tabellen.");
                default:
                    return Text_("ZPG_MSG_KEINE_KATALOGVERSION", "Der Zapfprofilgenerator ist in dieser Datenbank nicht verfügbar — die Brauchwasserparameter tragen keine Katalogversion.");
            }
        }

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

            t.KnopfStochastik = Text_("ZPG_BTN_STOCHASTIK", t.KnopfStochastik);
            t.KnopfAuslegung = Text_("ZPG_BTN_AUSLEGUNG", t.KnopfAuslegung);
            t.StatusAuslegung = Text_("ZPG_STATUS_AUSLEGUNG", t.StatusAuslegung);
            t.AuslegungOhnePunkt = Text_("ZPG_AUSLEGUNG_OHNE_PUNKT", t.AuslegungOhnePunkt);
            t.StatusVorschau = Text_("ZPG_STATUS_VORSCHAU", t.StatusVorschau);
            t.StatusOhneVorschau = Text_("ZPG_STATUS_OHNE_VORSCHAU", t.StatusOhneVorschau);
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
        private static (int Tag, int Monat) TagUndMonat(int jahrestag)
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
