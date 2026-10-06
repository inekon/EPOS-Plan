using System;
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
    /// <b>„Datei erneut lesen“ im Gebäudedialog</b> (HottCAD-Verbund 4.3 und 6.5, HC-4; Entscheid E87 F3) — die
    /// Datenseite: die gespeicherte Importquelle einer Projektzeile (<see cref="GebaeudeImportCtrl.LesenQuellenDerZuordnung"/>),
    /// die Dateiwahl über <c>Dienste.Datei.DateiOeffnenAsync</c> (erwartet, nie synchron), das Lesen im Arbeitsfaden über
    /// <see cref="Kulturweitergabe"/> mit dem Kernweg <see cref="GebaeudeNeulesen"/> und bei passendem SHA-256 dieselbe
    /// <see cref="GebaeudeAnsichtDaten"/> wie im Import (<see cref="GebaeudeImportAnsicht.AnsichtDaten"/>, beide Farbmodi).
    ///
    /// <para><b>Geschrieben wird nichts.</b> Jede andere Lage ist ein benannter Zustand mit Hinweiszeile und ohne Ansicht;
    /// ein Abbruch der Dateiwahl ist <c>null</c>, kein Fehler.</para>
    ///
    /// <para><b>Vorbelegung des Dateinamens.</b> <c>Tab_Importquelle</c> speichert nur den Namen, nie den Pfad, und
    /// <see cref="IDateiDienst.DateiOeffnenAsync"/> kennt keinen vorbelegten Namen: Der Name steht im Titel des Wählers
    /// und unter Windows als erster Filter (nur Dateien dieses Namens), danach der gemeinsame Filter beider Formate.
    /// Auf iOS bleibt es beim gemeinsamen Filter — der Wähler bildet Filter auf Typkennungen ab, ein Dateiname trägt
    /// dort keine. Die Größengrenze ist die der Plattform (<see cref="GebaeudeImportProfil.GrenzeFuerPlattform"/>).</para>
    /// </summary>
    internal sealed class GebaeudeNeulesenHuelle
    {
        /// <summary>Zeichen eines gekürzten SHA-256 in der Hinweiszeile.</summary>
        internal const int HASH_KURZ = 12;

        private readonly bool _ios;

        /// <param name="ios">Plattform der Größengrenze und des Filters; <c>null</c> = die laufende.</param>
        internal GebaeudeNeulesenHuelle(bool? ios = null)
        {
            _ios = ios ?? OperatingSystem.IsIOS();
        }

        /// <summary>
        /// Die jüngste Importquelle einer Projektzeile; <c>null</c>, wenn die Zeile keine Projektkopie hat (eben
        /// aufgenommen) oder das Gebäude nicht importiert ist — dann zeigt der Dialog keinen Knopf.
        /// </summary>
        internal static ImportquelleModel QuelleDerZeile(GebaeudeProjektZeile zeile)
        {
            if (zeile == null || !zeile.HatProjektkopie || zeile.IdZ <= 0 || zeile.IdZ >= GebaeudeHuelle.STARTINDEX) return null;
            return new GebaeudeImportCtrl().LesenQuellenDerZuordnung(zeile.IdZ).FirstOrDefault();
        }

        /// <summary>Die Quelle als Anzeige (Name, Format, Zeitpunkt in der Anzeigekultur); <c>null</c> ohne Quelle.</summary>
        internal static GebaeudeImportquelleAngabe Angabe(GebaeudeProjektZeile zeile)
            => Angabe(QuelleDerZeile(zeile));

        internal static GebaeudeImportquelleAngabe Angabe(ImportquelleModel q)
        {
            if (q == null) return null;
            string format = Formattext(q.Format);
            string zeitpunkt = GebaeudeImportHuelle.Zeitpunkttext(q.Zeitpunkt);
            return new GebaeudeImportquelleAngabe(q.Dateiname, format, zeitpunkt,
                Formatieren(MyResource.Resource.GEB_NL_QUELLE, q.Dateiname, format, zeitpunkt));
        }

        /// <summary>
        /// Wählen, prüfen, lesen: <c>null</c> = die Dateiwahl wurde abgebrochen; sonst der Stand mit benanntem Zustand.
        /// </summary>
        internal async Task<GebaeudeNeulesestand> LesenAsync(GebaeudeProjektZeile zeile, CancellationToken abbruch = default)
        {
            ImportquelleModel q = QuelleDerZeile(zeile);
            if (q == null)
                return new GebaeudeNeulesestand
                {
                    Zustand = GebaeudeNeulesezustand.KeineQuelle, Hinweis = MyResource.Resource.GEB_NL_KEINE_QUELLE
                };

            string pfad = await Dienste.Datei.DateiOeffnenAsync(
                Formatieren(MyResource.Resource.GEB_NL_DATEI_TITEL, q.Dateiname), Filter(q.Dateiname), null);
            if (string.IsNullOrEmpty(pfad)) return null;

            string kennung = new GebaeudeImportCtrl().Gebaeudekennung(q);
            return await LesenAsync(pfad, q, kennung, abbruch);
        }

        /// <summary>Der Teil nach der Dateiwahl — eigener Einstieg für die Proben.</summary>
        internal async Task<GebaeudeNeulesestand> LesenAsync(string pfad, ImportquelleModel q, string kennung,
                                                             CancellationToken abbruch = default)
        {
            bool ios = _ios;
            string oeffnungsfehler = null;
            NeulesenErgebnis e = await Kulturweitergabe.Starten(() =>
            {
                FileStream strom;
                try
                {
                    strom = File.OpenRead(pfad);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException
                                           || ex is ArgumentException || ex is NotSupportedException)
                {
                    oeffnungsfehler = ex.Message;
                    return GebaeudeNeulesen.Lesen(null, pfad, q, kennung, ios, abbruch);
                }
                using (strom)
                    return GebaeudeNeulesen.Lesen(strom, pfad, q, kennung, ios, abbruch);
            }, abbruch);
            return Stand(e, q, pfad, oeffnungsfehler);
        }

        /// <summary>Das Ergebnis des Kerns als Stand des Dialogs.</summary>
        internal static GebaeudeNeulesestand Stand(NeulesenErgebnis e, ImportquelleModel q, string pfad, string oeffnungsfehler = null)
        {
            string name = e.Dateiname;
            switch (e.Zustand)
            {
                case NeulesenZustand.Passend:
                    string regel = GebaeudeZuordnungsModell.ZonenregelText(e.Zonenregel);
                    return new GebaeudeNeulesestand
                    {
                        Zustand = GebaeudeNeulesezustand.Passend,
                        Hinweis = Formatieren(MyResource.Resource.GEB_NL_PASSEND, name),
                        Zonenhinweis = Formatieren(e.Hottcad ? MyResource.Resource.GEB_NL_NUR_GEOMETRIE : MyResource.Resource.GEB_NL_ZONENREGEL, regel),
                        Ansicht = GebaeudeImportAnsicht.AnsichtDaten(e.Geometrie, false, null, e.Gebaeude),
                    };
                case NeulesenZustand.HashAbweichend:
                    return Fehlstand(GebaeudeNeulesezustand.HashAbweichend,
                        Formatieren(MyResource.Resource.GEB_NL_HASH_ABWEICHEND, name, Dateidatum(pfad), Kurz(e.HashDatei), Kurz(e.HashQuelle)));
                case NeulesenZustand.FormatUnbekannt:
                    return Fehlstand(GebaeudeNeulesezustand.FormatUnbekannt,
                        Formatieren(MyResource.Resource.GEB_NL_FORMAT_UNBEKANNT, name, Formattext(q?.Format)));
                case NeulesenZustand.ZuGross:
                    return Fehlstand(GebaeudeNeulesezustand.ZuGross,
                        Formatieren(MyResource.Resource.GEB_NL_ZU_GROSS, name, GebaeudeZuordnungsModell.GroesseText(e.Groesse),
                                    GebaeudeZuordnungsModell.GroesseText(e.Grenze)));
                case NeulesenZustand.KeineQuelle:
                    return new GebaeudeNeulesestand
                    {
                        Zustand = GebaeudeNeulesezustand.KeineQuelle, Hinweis = MyResource.Resource.GEB_NL_KEINE_QUELLE
                    };
                default:
                    string grund = oeffnungsfehler ?? string.Join(" ", e.Meldungen.Select(GebaeudeZuordnungsModell.MeldungText).Where(t => !string.IsNullOrWhiteSpace(t)));
                    return Fehlstand(GebaeudeNeulesezustand.NichtLesbar,
                        Formatieren(MyResource.Resource.GEB_NL_NICHT_LESBAR, name, grund).Trim());
            }
        }

        private static GebaeudeNeulesestand Fehlstand(GebaeudeNeulesezustand zustand, string hinweis)
            => new GebaeudeNeulesestand { Zustand = zustand, Hinweis = hinweis };

        /// <summary>Der Dateifilter: unter Windows zuerst nur der gespeicherte Name, dann beide Formate; auf iOS beide Formate.</summary>
        internal string Filter(string dateiname)
        {
            string name = GebaeudeQuelle.NurName(dateiname);
            if (_ios || string.IsNullOrWhiteSpace(name) || name.IndexOfAny(new[] { '|', ';', '*', '?' }) >= 0)
                return GebaeudeImportProfil.DATEIFILTER_ALLE;
            return name + "|" + name + "|" + GebaeudeImportProfil.DATEIFILTER_ALLE;
        }

        /// <summary>Ein SHA-256 gekürzt auf <see cref="HASH_KURZ"/> Zeichen.</summary>
        internal static string Kurz(string hash)
            => string.IsNullOrEmpty(hash) ? "—" : hash.Length <= HASH_KURZ ? hash : hash.Substring(0, HASH_KURZ);

        private static string Dateidatum(string pfad)
        {
            try
            {
                return File.GetLastWriteTime(pfad).ToString("g", CultureInfo.CurrentCulture);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException
                                       || ex is ArgumentException || ex is NotSupportedException)
            {
                return "—";
            }
        }

        private static string Formattext(string format)
        {
            if (string.Equals(format, GebaeudeQuelle.FORMAT_IFC, StringComparison.Ordinal))
                return GebaeudeZuordnungsModell.FormatText(new IfcImportProfil());
            if (string.Equals(format, GebaeudeQuelle.FORMAT_GBXML, StringComparison.Ordinal))
                return GebaeudeZuordnungsModell.FormatText(new GbxmlImportProfil());
            return format ?? "";
        }

        private static string Formatieren(string vorlage, params object[] werte)
        {
            try { return string.Format(CultureInfo.CurrentCulture, vorlage ?? "", werte); }
            catch (FormatException) { return vorlage ?? ""; }
        }
    }
}
