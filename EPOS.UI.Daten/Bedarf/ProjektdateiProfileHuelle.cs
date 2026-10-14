using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Bedarf;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Hülle der Komponente <c>ProjektdateiProfile</c></b> (Konzept Nutzungsprofile Q46, Stufe NP4b): baut aus
    /// <see cref="SqprojRaumnutzung"/> und <see cref="RaumnutzungCtrl"/> den Weg — Lesen, Vorschau, Übernehmen. Zwei Zugänge:
    /// <see cref="BlattWeg"/> für das Blatt „Nutzungsprofile" (Dateiwahl über <c>Dienste.Datei</c>, Größengrenze der
    /// Plattform) und <see cref="ImportWeg"/> für den Gebäudeimport (die schon geladene Projektdatei, ohne Dateiwahl).
    /// Geschrieben wird erst mit „Übernehmen" und nur die zuletzt gelesene Datei; die Zuordnung der DIN-Nummern bleibt.
    /// </summary>
    internal static class ProjektdateiProfileHuelle
    {
        /// <summary>Die Texte aus den Ressourcen <c>RNP_PD_*</c>.</summary>
        internal static ProjektdateiProfileTexte Texte() => new ProjektdateiProfileTexte
        {
            KnopfBlatt = MyResource.Resource.RNP_PD_BTN_BLATT,
            KnopfImport = MyResource.Resource.RNP_PD_BTN_IMPORT,
            Tooltip = MyResource.Resource.RNP_PD_TOOLTIP,
            Ueberschrift = MyResource.Resource.RNP_PD_UEBERSCHRIFT,
            SpalteNummer = MyResource.Resource.RNP_PD_SP_NUMMER,
            SpalteName = MyResource.Resource.RNP_PD_SP_NAME,
            SpalteWerte = MyResource.Resource.RNP_PD_SP_WERTE,
            SpalteZonen = MyResource.Resource.RNP_PD_SP_ZONEN,
            SpalteStand = MyResource.Resource.RNP_PD_SP_STAND,
            StandNeu = MyResource.Resource.RNP_PD_STAND_NEU,
            StandVorhanden = MyResource.Resource.RNP_PD_STAND_VORHANDEN,
            KategorieVorhanden = MyResource.Resource.RNP_PD_KATEGORIE_VORHANDEN,
            KnopfUebernehmen = MyResource.Resource.RNP_PD_BTN_UEBERNEHMEN,
            KnopfErgaenzen = MyResource.Resource.RNP_PD_BTN_ERGAENZEN,
            KnopfErsetzen = MyResource.Resource.RNP_PD_BTN_ERSETZEN,
            KnopfSchliessen = MyResource.Resource.RNP_PD_BTN_SCHLIESSEN,
            GrundKeine = MyResource.Resource.RNP_PD_GRUND_KEINE,
            GrundAlleDa = MyResource.Resource.RNP_PD_GRUND_ALLE_DA,
            GrundBeschaeftigt = MyResource.Resource.RNP_PD_GRUND_BESCHAEFTIGT,
            HinweisZuordnung = MyResource.Resource.RNP_PD_HINWEIS_ZUORDNUNG,
            HinweisHerkunft = MyResource.Resource.RNP_PD_HINWEIS_HERKUNFT,
            Meldungen = MyResource.Resource.RNP_PD_MELDUNGEN,
        };

        /// <summary>
        /// <b>Der Weg des Blatts</b>: Dateiwahl der Plattform (Filter <c>.sqproj</c>), Lesen im Arbeitsfaden mit der Grenze der
        /// Plattform (<see cref="SqprojProfil.GrenzeFuerPlattform"/>); <c>null</c> ohne Katalog.
        /// </summary>
        internal static ProjektdateiProfileWeg BlattWeg(bool? ios = null)
        {
            if (!RaumnutzungCtrl.Lesbar()) return null;
            long grenze = SqprojProfil.GrenzeFuerPlattform(ios ?? OperatingSystem.IsIOS());
            Projektdateiprofile satz = null;
            return new ProjektdateiProfileWeg
            {
                Texte = Texte(),
                Laden = async () =>
                {
                    string pfad = await Dienste.Datei.DateiOeffnenAsync(MyResource.Resource.GIMP_DLG_SQ_TITEL,
                                                                        MyResource.Resource.GIMP_DLG_SQ_FILTER, null);
                    if (string.IsNullOrEmpty(pfad)) return null;
                    satz = await LesenAsync(pfad, grenze);
                    return Vorschau(satz);
                },
                Uebernehmen = ersetzen => Uebernehmen(satz, ersetzen),
            };
        }

        /// <summary>
        /// <b>Der Weg des Gebäudeimports</b>: bildet die schon geladene Projektdatei ab (<paramref name="stand"/>, ohne
        /// Dateiwahl); <c>null</c> ohne Katalog.
        /// </summary>
        internal static ProjektdateiProfileWeg ImportWeg(Func<SqprojStand> stand)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            if (!RaumnutzungCtrl.Lesbar()) return null;
            Projektdateiprofile satz = null;
            return new ProjektdateiProfileWeg
            {
                Texte = Texte(),
                Laden = () =>
                {
                    SqprojStand s = stand();
                    satz = s?.Abbild == null
                        ? new Projektdateiprofile
                        {
                            Dateiname = s?.Dateiname ?? "", Kategorie = SqprojRaumnutzung.Kategoriename(s?.Dateiname ?? ""),
                            Ablehnung = GebaeudeZuordnungsModell.MeldungText(s?.Ablehnung
                                ?? new PruefMeldung(PruefStufe.Fehler, SqprojProtokoll.NICHT_GELESEN)),
                        }
                        : SqprojRaumnutzung.Bilden(s.Abbild, s.Dateiname);
                    return Task.FromResult(Vorschau(satz));
                },
                Uebernehmen = ersetzen => Uebernehmen(satz, ersetzen),
            };
        }

        /// <summary>Liest die Datei im Arbeitsfaden; ein Öffnungsfehler ist eine benannte Ablehnung.</summary>
        internal static async Task<Projektdateiprofile> LesenAsync(string pfad, long grenze, CancellationToken abbruch = default)
        {
            string name = GebaeudeQuelle.NurName(pfad ?? "");
            Projektdateiprofile satz = null;
            await Kulturweitergabe.Starten(() =>
            {
                try
                {
                    using (FileStream strom = File.OpenRead(pfad))
                        satz = SqprojRaumnutzung.Lesen(strom, name, grenze, abbruch);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException
                                           || ex is ArgumentException || ex is NotSupportedException)
                {
                    satz = new Projektdateiprofile
                    {
                        Dateiname = name, Kategorie = SqprojRaumnutzung.Kategoriename(name),
                        Ablehnung = GebaeudeZuordnungsModell.MeldungText(new PruefMeldung(PruefStufe.Fehler, SqprojProtokoll.LESEFEHLER, ex.Message)),
                    };
                }
                return 0;
            }, abbruch);
            return satz;
        }

        /// <summary>
        /// <b>Die Vorschau</b>: je Profil Nummer, Name, Kurzform der Werte (<see cref="RaumnutzungHuelle.Kurzform"/>), Zonen
        /// und ob die Nummer schon in der Kategorie gleichen Namens steht.
        /// </summary>
        internal static ProjektdateiProfileVorschau Vorschau(Projektdateiprofile satz)
        {
            if (satz == null) return null;
            if (satz.Abgelehnt)
                return new ProjektdateiProfileVorschau(satz.Dateiname, satz.Kategorie, false, Array.Empty<ProjektdateiProfilZeile>(),
                                                       Array.Empty<string>(), satz.Ablehnung);
            var ctrl = new RaumnutzungCtrl();
            RaumnutzungCtrl.Kategorie k = ctrl.KategorieMitNamen(satz.Kategorie);
            List<Raumnutzungsprofil> vorhanden = k == null ? new List<Raumnutzungsprofil>() : ctrl.Profile(k.Id);
            RaumnutzungTexte t = RaumnutzungHuelle.Texte();
            List<ProjektdateiProfilZeile> zeilen = satz.Profile
                .Select(p => new ProjektdateiProfilZeile(p.Profil.Nummer ?? "", p.Profil.Bezeichner ?? "", RaumnutzungHuelle.Kurzform(p.Profil, t),
                                                         p.Zonen, vorhanden.Any(x => RaumnutzungCtrl.GleichesProjektdateiprofil(x, p.Profil))))
                .ToList();
            return new ProjektdateiProfileVorschau(satz.Dateiname, satz.Kategorie, k != null, zeilen, satz.Meldungen.ToList());
        }

        /// <summary>Schreibt den gelesenen Satz (<see cref="RaumnutzungCtrl.ProjektdateiUebernehmen"/>) und fasst das Ergebnis.</summary>
        internal static ProjektdateiProfileErgebnis Uebernehmen(Projektdateiprofile satz, bool ersetzen)
        {
            if (satz == null) return ProjektdateiProfileErgebnis.Fehler(MyResource.Resource.RNP_PD_KEINE);
            RaumnutzungCtrl.Projektdateiuebernahme e = new RaumnutzungCtrl().ProjektdateiUebernehmen(satz, ersetzen);
            if (!e.Ok) return ProjektdateiProfileErgebnis.Fehler(e.Meldung);
            CultureInfo c = CultureInfo.CurrentCulture;
            string zeile = string.Format(c, MyResource.Resource.RNP_PD_ERGEBNIS, satz.Kategorie, e.Neu.ToString(c), e.Ersetzt.ToString(c),
                                         e.Unveraendert.ToString(c), e.Entfernt.ToString(c));
            return new ProjektdateiProfileErgebnis(true, zeile, e.Meldungen);
        }
    }
}
