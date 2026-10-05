using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using SpeicherEngine;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc4.ActorResource;
using Xbim.Ifc4.DateTimeResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MaterialResource;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.QuantityResource;
using Xbim.Ifc4.UtilityResource;
using Xbim.IO.Memory;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Round-Trip-Anreicherung einer fremden IFC4-Datei</b> (Stufe G7d, Datenaustauschkonzept 6.6,
    /// Probe 13; D11/E27): Die vom Anwender <b>erneut gewählte</b> Originaldatei wird gegen die gespeicherte
    /// Importquelle gehalten und — nur wenn alle drei Sperren frei sind — um U-Werte, Baustoffe und
    /// Ergebnisse ergänzt; Geometrie, Struktur und vorhandene <c>GlobalId</c> bleiben unverändert.
    ///
    /// <para><b>Die drei Sperren</b> (je benannte Verweigerung mit dem Angebot einer eigenen Datei nach G7c):
    /// SHA-256 der gewählten Datei ≠ <see cref="ImportquelleModel.Hash"/>; <see cref="ImportquelleModel.Schemastand"/>
    /// ≠ <c>IFC4</c>; <see cref="ImportquelleModel.FehlendeEntitaeten"/> &gt; 0 — und dazu ein erneutes Laden
    /// über <c>MemoryModel</c> mit der Protokollsenke des Lesers (<see cref="IfcProtokoll"/>), das beide
    /// Verlustkanäle zählt. Angereichert wird nur eine STEP-Datei (<c>.ifc</c>).</para>
    ///
    /// <para><b>Ergänzen statt doppeln:</b> Die Standardsätze (<c>Pset_WallCommon</c>, <c>Pset_SlabCommon</c>,
    /// <c>Pset_WindowCommon</c>, <c>Pset_DoorCommon</c>, <c>Pset_SpaceThermalRequirements</c> …) werden
    /// wiederverwendet; die Fachwerte von EPOS (U-Wert, g-Wert, Sollwerte) ersetzen eine vorhandene
    /// Eigenschaft, alle übrigen kommen nur hinzu, wenn sie fehlen. Ein Satz, den mehrere Objekte teilen, wird
    /// für das Objekt abgespalten (die übrigen Eigenschaften werden mitgenommen, nicht kopiert).
    /// <c>Qto_*</c> nur, wenn sie fehlen, und nur bei einer 1:1-Paarung. Die <c>EPOS_*</c>-Sätze stehen immer
    /// als eigene Sätze und ersetzen die eines früheren Durchlaufs. Baustoffe nur ohne Materialzuordnung
    /// (am Objekt oder an seinem Typ) — sonst trägt <c>EPOS_Bauteil</c> allein Schichtrichtung und Herkunft.</para>
    ///
    /// <para><b>Kennungen:</b> Neue <c>IfcRoot</c>-Instanzen bekommen ihre <c>GlobalId</c> über
    /// <see cref="IfcExportKennung"/> mit dem Schlüssel <c>GlobalId der Quellentität</c> und dem Rollenglied
    /// <c>Anreicherung:…</c> — derselbe Lauf auf derselben Datei ergibt dieselben Kennungen. Sie tragen eine
    /// eigene <c>IfcOwnerHistory</c> mit eigener <c>IfcApplication</c> (EPOS-Plan, <c>ADDED</c>).</para>
    ///
    /// <para><b>Kennung der Datei</b> (6.6 Nr. 5): <c>FILE_DESCRIPTION</c> bekommt den Vermerk
    /// <c>GEXP_IFC_ANR_VERMERK</c>; <c>FILE_NAME</c> (samt <c>OriginatingSystem</c> und
    /// <c>PreprocessorVersion</c>) und <c>FILE_SCHEMA</c> bleiben. <b>Prüfung:</b> der Validator des Schreibers
    /// vor und nach dem Eingriff; abgebrochen wird bei jedem Verstoß, den die Eingangsdatei nicht schon trug —
    /// dann wird nichts geschrieben.</para>
    /// </summary>
    internal sealed class IfcAnreicherung
    {
        /// <summary>Der einzige Schemastand, der die Rückgabe zulässt.</summary>
        internal const string SCHEMA_IFC4 = "IFC4";

        /// <summary>Präfix der Anreicherungsmeldungen.</summary>
        internal const string P = GebaeudeExportAblauf.P + "ANR_";

        /// <summary>Keine IFC-Importquelle am Gebäude — Verweigerung.</summary>
        internal const string KEINE_QUELLE = P + "KEINE_QUELLE";
        /// <summary>SHA-256 der gewählten Datei ≠ Importquelle — Verweigerung (6.6 Nr. 1).</summary>
        internal const string HASH = P + "HASH";
        /// <summary>Schemastand ≠ IFC4 — Verweigerung (6.6, Einleitung); Wert: der Schemastand.</summary>
        internal const string SCHEMA = P + "SCHEMA";
        /// <summary>Beim Import gingen Entitäten verloren — Verweigerung (6.6 Nr. 2); Wert: die Zahl.</summary>
        internal const string VERLUST = P + "VERLUST";
        /// <summary>Beim erneuten Laden gingen Entitäten verloren — Verweigerung; Werte: Summe, nicht angelegt, Verweise, Beispiel.</summary>
        internal const string VERLUST_LADEN = P + "VERLUST_LADEN";
        /// <summary>Die Datei ist keine STEP-Datei (ifcXML, Behälter) — Verweigerung.</summary>
        internal const string NUR_STEP = P + "NUR_STEP";
        /// <summary>Die Datei lässt sich nicht laden — Verweigerung; Wert: der Befund.</summary>
        internal const string LESEFEHLER = P + "LESEFEHLER";
        /// <summary>Beipackzettel (D11): „Sie geben eine fremde Datei verändert weiter" — der Dialog lässt ihn bestätigen.</summary>
        internal const string BEIPACK_FREMDDATEI = P + "BEIPACK_FREMDDATEI";
        /// <summary>Die Quellentität einer Zuordnung fehlt in der Datei; Werte: Quellkennung, Quelltyp.</summary>
        internal const string ENTITAET_FEHLT = P + "ENTITAET_FEHLT";
        /// <summary>Die EPOS-Zeile einer Zuordnung steht nicht im Abbild; Werte: Exportkennung, Quellkennung.</summary>
        internal const string ZEILE_FEHLT = P + "ZEILE_FEHLT";
        /// <summary>Die Quellentität hat nicht die erwartete Art; Werte: Quellkennung, IFC-Klasse, erwartete Art.</summary>
        internal const string TYP_FALSCH = P + "TYP_FALSCH";
        /// <summary>Eine Quellentität ist mehreren EPOS-Zeilen zugeordnet; die erste gilt. Werte: Quellkennung, Exportkennung.</summary>
        internal const string MEHRFACH = P + "MEHRFACH";
        /// <summary>Eine Zone verteilt sich auf mehrere Räume; ihr Ergebnis steht nur am Gebäude. Wert: Exportkennung, Zahl der Räume.</summary>
        internal const string ERGEBNIS_GETEILT = P + "ERGEBNIS_GETEILT";
        /// <summary>Die Eingangsdatei trägt schon Verstöße gegen das Schema; sie bleiben. Wert: Zahl.</summary>
        internal const string VERSTOESSE_VORHER = P + "VERSTOESSE_VORHER";
        /// <summary>Die Bilanz: angereicherte Objekte, ergänzt, ersetzt, übersprungen.</summary>
        internal const string BILANZ = P + "BILANZ";
        /// <summary>Eine neue Kennung trifft eine vorhandene <c>GlobalId</c> — Abbruch. Wert: die Kennung.</summary>
        internal const string KENNUNG_BELEGT = P + "KENNUNG_BELEGT";

        private const string ROLLE = "Anreicherung:";

        private readonly IfcErgebnisse _ergebnisse;

        /// <summary>Legt die Anreicherung an.</summary>
        /// <param name="ergebnisse">Die Rechenergebnisse für <c>EPOS_Ergebnis</c>; <c>null</c> = keine.</param>
        internal IfcAnreicherung(IfcErgebnisse ergebnisse = null)
        {
            _ergebnisse = ergebnisse;
        }

        /// <summary>Ein Eingriff in das angereicherte Modell vor der Prüfung — nur für die Proben.</summary>
        internal Action<IModel> VorDerPruefung { get; set; }

        /// <summary>Der SHA-256 einer Datei, hexadezimal klein — wie beim Import (<see cref="ImportquelleModel.Hash"/>).</summary>
        internal static string Hash(byte[] datei) => Convert.ToHexStringLower(SHA256.HashData(datei ?? Array.Empty<byte>()));

        // ==================================================================
        //  Sperren
        // ==================================================================

        /// <summary>
        /// <b>Die Sperren der Rückgabe</b> — für die Vorschau, sobald die Datei gewählt ist: alle zutreffenden
        /// Verweigerungen aus der Importquelle (Hash, Schemastand, Verluste beim Import) und, wenn diese frei
        /// sind, das erneute Laden mit Zählung beider Verlustkanäle. Leer = die Rückgabe ist zulässig.
        /// Jede Meldung ist ein Fehler und trägt das Angebot einer eigenen Datei nach G7c im Text.
        /// </summary>
        internal static IReadOnlyList<PruefMeldung> Sperren(byte[] datei, ImportquelleModel quelle)
        {
            List<PruefMeldung> sperren = QuellSperren(datei, quelle);
            if (sperren.Count > 0) return sperren;
            MemoryModel modell = Laden(datei, sperren);
            modell?.Dispose();
            return sperren;
        }

        /// <summary>Die Sperren aus der gespeicherten Importquelle und dem Hash der gewählten Datei.</summary>
        private static List<PruefMeldung> QuellSperren(byte[] datei, ImportquelleModel quelle)
        {
            var sperren = new List<PruefMeldung>();
            if (quelle == null || !string.Equals(quelle.Format, GebaeudeQuelle.FORMAT_IFC, StringComparison.Ordinal))
            {
                sperren.Add(Fehler(KEINE_QUELLE));
                return sperren;
            }
            if (!string.Equals(Hash(datei), (quelle.Hash ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
                sperren.Add(Fehler(HASH, quelle.Dateiname ?? ""));
            string stand = (quelle.Schemastand ?? "").Trim();
            if (!string.Equals(stand, SCHEMA_IFC4, StringComparison.OrdinalIgnoreCase))
                sperren.Add(Fehler(SCHEMA, stand.Length == 0 ? "?" : stand));
            if (quelle.FehlendeEntitaeten > 0)
                sperren.Add(Fehler(VERLUST, Ganz(quelle.FehlendeEntitaeten)));
            return sperren;
        }

        /// <summary>
        /// Lädt die Datei als IFC4-Modell mit der Protokollsenke des Lesers und ohne übergangene Typen
        /// (vierter Wert <c>null</c>). <c>null</c> = verweigert, der Grund steht in <paramref name="sperren"/>.
        /// </summary>
        private static MemoryModel Laden(byte[] datei, List<PruefMeldung> sperren)
        {
            if (datei == null || datei.Length == 0 || IfcLeser.Inhaltsart(datei) != IfcLeser.INHALT_STEP)
            {
                sperren.Add(Fehler(NUR_STEP));
                return null;
            }
            XbimSchemaVersion fassung;
            try
            {
                fassung = MemoryModel.GetStepFileXbimSchemaVersion(new MemoryStream(datei, false));
            }
            catch (Exception ex)
            {
                sperren.Add(Fehler(LESEFEHLER, ex.Message));
                return null;
            }
            if (fassung != XbimSchemaVersion.Ifc4)
            {
                sperren.Add(Fehler(SCHEMA, IfcSchemaStaende.Kurzform(IfcSchemaStaende.AusXbim(fassung)) ?? fassung.ToString()));
                return null;
            }
            var protokoll = new IfcProtokoll();
            MemoryModel modell = null;
            try
            {
                modell = new MemoryModel(new Xbim.Ifc4.EntityFactoryIfc4(), (Microsoft.Extensions.Logging.ILoggerFactory)protokoll, 0);
                using (var strom = new MemoryStream(datei, false))
                    modell.LoadStep21(strom, datei.LongLength, null, null);   // vierter Wert null: KEINE Typen übergehen
            }
            catch (Exception ex)
            {
                modell?.Dispose();
                sperren.Add(Fehler(LESEFEHLER, ex.Message));
                return null;
            }
            if (protokoll.Summe > 0)
            {
                modell.Dispose();
                sperren.Add(Fehler(VERLUST_LADEN, Ganz(protokoll.Summe), Ganz(protokoll.NichtAngelegt), Ganz(protokoll.VerweiseInsLeere),
                                   protokoll.Beispiele.Count > 0 ? protokoll.Beispiele[0] : ""));
                return null;
            }
            return modell;
        }

        // ==================================================================
        //  Anreichern
        // ==================================================================

        /// <summary>
        /// <b>Reichert die gewählte Originaldatei an</b> und schreibt sie in <paramref name="ziel"/> (immer eine
        /// neue Datei; den Namen schlägt <see cref="GebaeudeExportAblauf.Dateivorschlag"/> vor). Verweigert
        /// benannt (<see cref="GebaeudeAnreicherungBilanz.Verweigert"/>), wenn eine Sperre greift; bricht ab,
        /// wenn der Validator neue Verstöße meldet — beides ohne ein Byte in <paramref name="ziel"/>.
        /// </summary>
        /// <param name="datei">Der Inhalt der erneut gewählten Originaldatei.</param>
        /// <param name="quelle">Die gespeicherte Importquelle des Gebäudes.</param>
        /// <param name="zuordnungen">Die Paarungen EPOS-Zeile ↔ Quellkennung dieser Quelle.</param>
        /// <param name="abbild">Das Abbild des Gebäudes wie beim IFC-Export (Zonenweg, mit Ergebnissen).</param>
        /// <param name="ziel">Der Zielstrom.</param>
        /// <param name="profil">Das Exportprofil (Sprache, Uhr, Programmfassung).</param>
        /// <param name="abbruch">Abbruch.</param>
        internal GebaeudeAnreicherungBilanz Anreichern(byte[] datei, ImportquelleModel quelle, IReadOnlyList<ImportzuordnungModel> zuordnungen,
                                                      GebaeudeAbbild abbild, Stream ziel, GebaeudeExportProfil profil, CancellationToken abbruch)
        {
            if (abbild == null) throw new ArgumentNullException(nameof(abbild));
            if (ziel == null) throw new ArgumentNullException(nameof(ziel));
            if (profil == null) throw new ArgumentNullException(nameof(profil));
            abbruch.ThrowIfCancellationRequested();

            var meldungen = new List<PruefMeldung>();
            List<PruefMeldung> sperren = QuellSperren(datei, quelle);
            MemoryModel modell = sperren.Count == 0 ? Laden(datei, sperren) : null;
            if (modell == null)
                return GebaeudeAnreicherungBilanz.Verweigerung(sperren);

            using (modell)
            {
                meldungen.Add(new PruefMeldung(PruefStufe.Warnung, BEIPACK_FREMDDATEI));
                abbruch.ThrowIfCancellationRequested();
                List<string> vorher = IfcSchreiber.Pruefen(modell).Select(Schluessel).ToList();
                if (vorher.Count > 0) meldungen.Add(new PruefMeldung(PruefStufe.Info, VERSTOESSE_VORHER, Ganz(vorher.Count)));

                var lauf = new Lauf(modell, abbild, zuordnungen ?? Array.Empty<ImportzuordnungModel>(), profil, _ergebnisse, abbruch);
                try
                {
                    using (ITransaction t = modell.BeginTransaction("EPOS-Plan IFC-Anreicherung"))
                    {
                        lauf.Bauen();
                        t.Commit();
                    }
                }
                catch (KennungBelegt kb)
                {
                    meldungen.AddRange(lauf.Meldungen);
                    meldungen.Add(Fehler(KENNUNG_BELEGT, kb.Kennung));
                    return new GebaeudeAnreicherungBilanz(false, false, lauf.Objekte, lauf.Ergaenzt, lauf.Ersetzt, lauf.Uebersprungen, meldungen, 0);
                }
                meldungen.AddRange(lauf.Meldungen);
                meldungen.Add(new PruefMeldung(PruefStufe.Info, BILANZ, Ganz(lauf.Objekte), Ganz(lauf.Ergaenzt), Ganz(lauf.Ersetzt), Ganz(lauf.Uebersprungen)));
                VorDerPruefung?.Invoke(modell);
                abbruch.ThrowIfCancellationRequested();

                List<PruefMeldung> neue = NeueVerstoesse(vorher, IfcSchreiber.Pruefen(modell));
                if (neue.Count > 0)
                {
                    meldungen.AddRange(neue.Take(IfcSchreiber.MELDUNGEN_SCHEMA_MAX));
                    meldungen.Add(Fehler(IfcSchreiber.ABGEBROCHEN, Ganz(neue.Count)));
                    return new GebaeudeAnreicherungBilanz(false, false, lauf.Objekte, lauf.Ergaenzt, lauf.Ersetzt, lauf.Uebersprungen, meldungen, 0);
                }

                byte[] bytes = Speichern(modell, lauf.Zeitstempel, profil);
                abbruch.ThrowIfCancellationRequested();
                ziel.Write(bytes, 0, bytes.Length);
                ziel.Flush();
                return new GebaeudeAnreicherungBilanz(true, false, lauf.Objekte, lauf.Ergaenzt, lauf.Ersetzt, lauf.Uebersprungen, meldungen, bytes.Length);
            }
        }

        /// <summary>Die Verstöße nach dem Eingriff, die vorher nicht da waren (als Mehrfachmenge verglichen).</summary>
        private static List<PruefMeldung> NeueVerstoesse(List<string> vorher, List<PruefMeldung> nachher)
        {
            var rest = vorher.GroupBy(s => s, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
            var neue = new List<PruefMeldung>();
            foreach (PruefMeldung m in nachher)
            {
                string k = Schluessel(m);
                if (rest.TryGetValue(k, out int n) && n > 0) rest[k] = n - 1;
                else neue.Add(m);
            }
            return neue;
        }

        private static string Schluessel(PruefMeldung m) => string.Join("\u001f", m.Werte ?? Array.Empty<string>());

        /// <summary>
        /// Die Byteform: der Kopf der Eingangsdatei mit dem Vermerk in <c>FILE_DESCRIPTION</c>; <c>FILE_NAME</c>
        /// und <c>FILE_SCHEMA</c> bleiben. Zeilenende CRLF wie beim Schreiber.
        /// </summary>
        internal static byte[] Speichern(MemoryModel modell, DateTime zeit, GebaeudeExportProfil profil)
        {
            IStepFileHeader kopf = modell.Header;
            kopf.FileDescription.Description.Add(Vermerk(zeit, profil));
            using (var text = new StringWriter(CultureInfo.InvariantCulture))
            {
                modell.SaveAsStep21(text, null);
                string inhalt = text.ToString().Replace("\r\n", "\n").Replace("\n", "\r\n");
                return new UTF8Encoding(false).GetBytes(inhalt);
            }
        }

        /// <summary>Der Vermerk in <c>FILE_DESCRIPTION</c> (6.6 Nr. 5) in der Sprache des Profils.</summary>
        internal static string Vermerk(DateTime zeit, GebaeudeExportProfil profil)
            => string.Format(profil.Sprache, IfcSchreiber.T(profil, "GEXP_IFC_ANR_VERMERK"),
                             profil.Programmname + " " + profil.Programmversion, IfcSchreiber.Zeitstempel(zeit));

        private static PruefMeldung Fehler(string schluessel, params string[] werte) => new PruefMeldung(PruefStufe.Fehler, schluessel, werte);

        private static string Ganz(int w) => w.ToString(CultureInfo.InvariantCulture);

        /// <summary>Eine neue Kennung trifft eine <c>GlobalId</c> der Datei.</summary>
        private sealed class KennungBelegt : Exception
        {
            internal KennungBelegt(string kennung) : base("IFC-Anreicherung: Die Kennung " + kennung + " ist in der Datei schon vergeben.")
            {
                Kennung = kennung;
            }

            internal string Kennung { get; }
        }

        // ==================================================================
        //  Ein Anreicherungsdurchgang
        // ==================================================================

        /// <summary>Eine Eigenschaft, die geschrieben werden soll: ersetzt sie eine vorhandene oder kommt sie nur hinzu?</summary>
        private sealed record Angabe(string Name, IfcValue Wert, IfcUnit Einheit, string Beschreibung, bool Ersetzen,
                                     string BeschreibungWoertlich = null);

        /// <summary>Das Ziel einer Zuordnung im Abbild.</summary>
        private enum Zielart
        {
            Gebaeude,
            Raum,
            Bauteil,
        }

        private sealed class Lauf
        {
            private readonly MemoryModel _m;
            private readonly GebaeudeAbbild _abbild;
            private readonly IReadOnlyList<ImportzuordnungModel> _zuordnungen;
            private readonly GebaeudeExportProfil _profil;
            private readonly IfcErgebnisse _ergebnisse;
            private readonly CancellationToken _abbruch;
            private readonly IfcExportKennung _kennung = new IfcExportKennung();
            private readonly Dictionary<string, IfcRoot> _index = new Dictionary<string, IfcRoot>(StringComparer.Ordinal);
            private readonly HashSet<int> _bearbeitet = new HashSet<int>();
            private readonly Dictionary<string, AbbildRaum> _raeume = new Dictionary<string, AbbildRaum>(StringComparer.Ordinal);
            private readonly Dictionary<string, (AbbildBauteil Bauteil, AbbildBauteil Wirt)> _bauteile
                = new Dictionary<string, (AbbildBauteil, AbbildBauteil)>(StringComparer.Ordinal);
            private readonly Dictionary<string, IfcMaterialLayerSet> _saetze = new Dictionary<string, IfcMaterialLayerSet>(StringComparer.Ordinal);
            private readonly Dictionary<string, List<IfcObjectDefinition>> _satzObjekte = new Dictionary<string, List<IfcObjectDefinition>>(StringComparer.Ordinal);
            private readonly List<string> _satzReihenfolge = new List<string>();
            private readonly Dictionary<string, IfcMaterial> _stoffe = new Dictionary<string, IfcMaterial>(StringComparer.Ordinal);
            private AbbildGebaeude _g;
            private IfcOwnerHistory _geschichte;
            private IfcConversionBasedUnit _kwh;
            private IfcConversionBasedUnit _grad;
            private IfcSIUnit _watt;
            private IfcSIUnit _kelvin;
            private double _laenge = 1.0, _flaeche = 1.0, _volumen = 1.0;
            private bool _mehrzonig;

            internal Lauf(MemoryModel m, GebaeudeAbbild abbild, IReadOnlyList<ImportzuordnungModel> zuordnungen, GebaeudeExportProfil profil,
                          IfcErgebnisse ergebnisse, CancellationToken abbruch)
            {
                _m = m;
                _abbild = abbild;
                _zuordnungen = zuordnungen;
                _profil = profil;
                _ergebnisse = ergebnisse;
                _abbruch = abbruch;
            }

            /// <summary>Angereicherte Quellentitäten.</summary>
            internal int Objekte { get; private set; }
            /// <summary>Neu angelegte Sätze und hinzugekommene Eigenschaften.</summary>
            internal int Ergaenzt { get; private set; }
            /// <summary>Ersetzte Eigenschaften und ersetzte EPOS-Sätze eines früheren Durchlaufs.</summary>
            internal int Ersetzt { get; private set; }
            /// <summary>Übersprungene Zuordnungen (Entität fehlt, Zeile fehlt, falsche Art, mehrfach).</summary>
            internal int Uebersprungen { get; private set; }
            internal List<PruefMeldung> Meldungen { get; } = new List<PruefMeldung>();
            internal DateTime Zeitstempel { get; private set; }

            private T Neu<T>(Action<T> belegen = null) where T : IInstantiableEntity
                => _m.Instances.New(belegen ?? (_ => { }));

            /// <summary>Die eine Erzeugungsfunktion für jede neue <c>IfcRoot</c>-Instanz: <c>GlobalId</c> und eigene <c>OwnerHistory</c>.</summary>
            private T Wurzel<T>(string schluessel, string rolle, Action<T> belegen = null) where T : IfcRoot, IInstantiableEntity
            {
                IfcGloballyUniqueId id = _kennung.Vergeben(schluessel, ROLLE + rolle);
                if (_index.ContainsKey(id.ToString())) throw new KennungBelegt(id.ToString());
                T e = Neu<T>(x =>
                {
                    x.GlobalId = id;
                    x.OwnerHistory = _geschichte;
                    belegen?.Invoke(x);
                });
                _index[id.ToString()] = e;
                return e;
            }

            internal void Bauen()
            {
                if (_abbild.Gebaeude.Count != 1)
                    throw new InvalidOperationException("IFC-Anreicherung: Das Abbild trägt " + _abbild.Gebaeude.Count.ToString(CultureInfo.InvariantCulture)
                                                        + " Gebäude; angereichert wird genau eines.");
                _g = _abbild.Gebaeude[0];
                _mehrzonig = _g.Raeume.Where(r => r.ZonenKennung != null).Select(r => r.ZonenKennung).Distinct(StringComparer.Ordinal).Count() > 1;
                foreach (AbbildRaum r in _g.Raeume) _raeume[r.Kennung] = r;
                foreach (AbbildBauteil b in _g.Bauteile)
                {
                    _bauteile[b.Kennung] = (b, null);
                    foreach (AbbildBauteil o in b.Oeffnungen) _bauteile[o.Kennung] = (o, b);
                }
                foreach (IfcRoot r in _m.Instances.OfType<IfcRoot>())
                {
                    string id = r.GlobalId.ToString();
                    if (!string.IsNullOrEmpty(id) && !_index.ContainsKey(id)) _index[id] = r;
                }
                Einheiten();
                Zeitstempel = _profil.Uhr();
                Geschichte();

                // Je EPOS-Zeile die Zahl ihrer Quellentitäten (1:1 erlaubt Mengen und Raumergebnisse).
                var paare = new List<(ImportzuordnungModel Z, Zielart Art, string Kennung)>();
                foreach (ImportzuordnungModel z in _zuordnungen.OrderBy(Rang).ThenBy(z => z.ID))
                {
                    if (z.ID_Gebaeude.HasValue) paare.Add((z, Zielart.Gebaeude, GebaeudeExportKennung.Gebaeude(z.ID_Gebaeude.Value)));
                    else if (z.ID_Zone.HasValue) paare.Add((z, Zielart.Raum, GebaeudeExportKennung.Raum(z.ID_Zone.Value)));
                    else if (z.ID_Bauteil.HasValue)
                    {
                        string k = GebaeudeExportKennung.Bauteil(z.ID_Bauteil.Value);
                        if (!_bauteile.ContainsKey(k)) k = GebaeudeExportKennung.Oeffnung(z.ID_Bauteil.Value);
                        paare.Add((z, Zielart.Bauteil, k));
                    }
                    // Aufbau und Baustoff: angereichert wird über das Bauteil, das sie trägt.
                }
                Dictionary<string, int> anzahl = paare.GroupBy(p => p.Kennung, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.Count(), StringComparer.Ordinal);

                foreach (var (z, art, kennung) in paare)
                {
                    _abbruch.ThrowIfCancellationRequested();
                    if (!_index.TryGetValue((z.Quellkennung ?? "").Trim(), out IfcRoot quelle))
                    {
                        Ueberspringen(PruefStufe.Warnung, ENTITAET_FEHLT, z.Quellkennung ?? "", z.Quelltyp ?? "");
                        continue;
                    }
                    bool eins = anzahl[kennung] == 1;
                    switch (art)
                    {
                        case Zielart.Gebaeude:
                            if (kennung != _g.Kennung) { Ueberspringen(PruefStufe.Warnung, ZEILE_FEHLT, kennung, z.Quellkennung); continue; }
                            if (!(quelle is IfcBuilding geb)) { Ueberspringen(PruefStufe.Warnung, TYP_FALSCH, z.Quellkennung, ((IPersistEntity)quelle).ExpressType.ExpressName, "IfcBuilding"); continue; }
                            if (!Erstmals(quelle, kennung)) continue;
                            Gebaeude(geb);
                            break;
                        case Zielart.Raum:
                            if (!_raeume.TryGetValue(kennung, out AbbildRaum raum)) { Ueberspringen(PruefStufe.Warnung, ZEILE_FEHLT, kennung, z.Quellkennung); continue; }
                            if (!(quelle is IfcSpace || quelle is IfcZone)) { Ueberspringen(PruefStufe.Warnung, TYP_FALSCH, z.Quellkennung, ((IPersistEntity)quelle).ExpressType.ExpressName, "IfcSpace"); continue; }
                            if (!Erstmals(quelle, kennung)) continue;
                            Raum((IfcObject)quelle, raum, eins, anzahl[kennung]);
                            break;
                        default:
                            if (!_bauteile.TryGetValue(kennung, out var bt)) { Ueberspringen(PruefStufe.Warnung, ZEILE_FEHLT, kennung, z.Quellkennung); continue; }
                            if (!(quelle is IfcElement el)) { Ueberspringen(PruefStufe.Warnung, TYP_FALSCH, z.Quellkennung, ((IPersistEntity)quelle).ExpressType.ExpressName, "IfcElement"); continue; }
                            if (!Erstmals(quelle, kennung)) continue;
                            Bauteil(el, bt.Bauteil, bt.Wirt, eins);
                            break;
                    }
                    Objekte++;
                }

                foreach (string k in _satzReihenfolge)
                {
                    IfcMaterialLayerSet satz = _saetze[k];
                    Wurzel<IfcRelAssociatesMaterial>(k, "RelMaterial", r =>
                    {
                        r.RelatingMaterial = satz;
                        r.RelatedObjects.AddRange(_satzObjekte[k]);
                    });
                    Ergaenzt++;
                }
            }

            private static int Rang(ImportzuordnungModel z) => z.ID_Gebaeude.HasValue ? 0 : z.ID_Zone.HasValue ? 1 : z.ID_Bauteil.HasValue ? 2 : 3;

            private bool Erstmals(IfcRoot quelle, string kennung)
            {
                if (_bearbeitet.Add(quelle.EntityLabel)) return true;
                Ueberspringen(PruefStufe.Info, MEHRFACH, quelle.GlobalId.ToString(), kennung);
                return false;
            }

            private void Ueberspringen(PruefStufe stufe, string schluessel, params string[] werte)
            {
                Uebersprungen++;
                Meldungen.Add(new PruefMeldung(stufe, schluessel, werte));
            }

            // --------------------------------------------------------------
            //  Kopf: Einheiten und eigene Geschichte
            // --------------------------------------------------------------

            /// <summary>Die Mengeneinheiten der Datei: Mengen werden in den Projekteinheiten geschrieben.</summary>
            private void Einheiten()
            {
                IIfcProject projekt = _m.Instances.FirstOrDefault<IIfcProject>();
                IfcEinheiten e = IfcEinheiten.Lesen(projekt, new List<PruefMeldung>());
                if (e.Laenge > 0.0) _laenge = e.Laenge;
                if (e.Flaeche > 0.0) _flaeche = e.Flaeche;
                if (e.Volumen > 0.0) _volumen = e.Volumen;
            }

            /// <summary>Eine eigene <c>IfcApplication</c> mit eigener <c>IfcOwnerHistory</c> (6.6 Nr. 5) — <c>ADDED</c>.</summary>
            private void Geschichte()
            {
                IfcOrganization org = Neu<IfcOrganization>(o => o.Name = _profil.Programmname);
                IfcPerson person = Neu<IfcPerson>(p => p.FamilyName = _profil.Programmname);
                IfcPersonAndOrganization nutzer = Neu<IfcPersonAndOrganization>(n =>
                {
                    n.ThePerson = person;
                    n.TheOrganization = org;
                });
                IfcApplication programm = Neu<IfcApplication>(a =>
                {
                    a.ApplicationDeveloper = org;
                    a.Version = _profil.Programmversion;
                    a.ApplicationFullName = _profil.Programmname;
                    a.ApplicationIdentifier = _profil.Programmname;
                });
                long sekunden = Zeitstempel.Kind == DateTimeKind.Local
                    ? new DateTimeOffset(Zeitstempel).ToUnixTimeSeconds()
                    : (long)Math.Floor((DateTime.SpecifyKind(Zeitstempel, DateTimeKind.Utc) - DateTime.UnixEpoch).TotalSeconds);
                _geschichte = Neu<IfcOwnerHistory>(h =>
                {
                    h.OwningUser = nutzer;
                    h.OwningApplication = programm;
                    h.CreationDate = new IfcTimeStamp(sekunden);
                    // Mit LastModifiedDate erlaubt die Regel CorrectChangeAction auch ADDED.
                    h.LastModifiedDate = new IfcTimeStamp(sekunden);
                    h.LastModifyingUser = nutzer;
                    h.LastModifyingApplication = programm;
                    h.ChangeAction = IfcChangeActionEnum.ADDED;
                });
            }

            // --------------------------------------------------------------
            //  Gebäude, Räume, Bauteile
            // --------------------------------------------------------------

            private void Gebaeude(IfcBuilding b)
            {
                string baujahr = !string.IsNullOrWhiteSpace(_g.BaujahrText) ? _g.BaujahrText.Trim() : _g.Baujahr?.ToString(CultureInfo.InvariantCulture);
                Ergaenzen(b, "Pset_BuildingCommon", baujahr == null ? null : Neben("YearOfConstruction", new IfcLabel(baujahr)));
                Eigen(b, IfcSchreiber.EPOS_GEBAEUDE,
                      Wert("Kennung", new IfcIdentifier(_g.Kennung)),
                      string.IsNullOrWhiteSpace(_g.Art) ? null : Wert("Gebaeudeart", new IfcLabel(_g.Art.Trim())),
                      string.IsNullOrWhiteSpace(_g.Baualtersklasse) ? null : Wert("Baualtersklasse", new IfcLabel(_g.Baualtersklasse.Trim())));
                Ergebnis(b, _g.Kennung, null);
                Rechenlauf(b);
            }

            private void Raum(IfcObject o, AbbildRaum r, bool eins, int raeume)
            {
                if (o is IfcSpace s)
                {
                    if (eins)
                        Mengen(s, "Qto_SpaceBaseQuantities",
                               Flaeche("NetFloorArea", r.FlaecheM2), Laenge("Height", r.HoeheM), Volumen("NetVolume", r.VolumenM3));
                    if (r.Beheizt)
                        Ergaenzen(s, "Pset_SpaceThermalRequirements",
                                  Temperatur("SpaceTemperature", r.SollHeizenC),
                                  Temperatur("SpaceTemperatureSummerMax", r.SollKuehlenC),
                                  r.Nachtabsenkung.HasValue ? Wert("DiscontinuedHeating", new IfcBoolean(r.Nachtabsenkung.Value)) : null,
                                  r.LuftwechselNutzerJeH.HasValue ? Wert("NaturalVentilationRate", new IfcNumericMeasure(Endlich(r.LuftwechselNutzerJeH.Value))) : null);
                }
                var zone = new List<Angabe>
                {
                    Wert("IstBeheizt", new IfcBoolean(r.Beheizt)),
                    r.ZonenKennung == null ? null : Wert("Kennung", new IfcIdentifier(r.ZonenKennung)),
                    Zahl("LuftwechselInfiltration", r.LuftwechselJeH, "GEXP_IFC_LUFTWECHSEL"),
                    Zahl("Personen", r.Personen, null),
                    Zahl("GeraeteWm2", r.GeraeteWm2, null),
                    Zahl("LichtWm2", r.LichtWm2, null),
                };
                zone.AddRange(IfcKonditionierungssatz.Zonenwerte(r.Konditionierung).Select(Satzwert));
                Eigen(o, IfcSchreiber.EPOS_ZONE, zone.ToArray());
                // Die Kalendersätze (6.3, 16.3): je Größe der Satz eines früheren Durchlaufs ersetzt, ein überholter entfernt —
                // nie doppelt.
                foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                {
                    AbbildKalender k = r.Konditionierung?.KalenderVon(g);
                    Eigen(o, IfcKonditionierungssatz.Satzname(g),
                          k == null ? Array.Empty<Angabe>() : IfcKonditionierungssatz.Kalenderwerte(k).Select(Satzwert).ToArray());
                }
                if (!r.Beheizt) return;
                if (eins) Ergebnis(o, r.Kennung, r.FlaecheM2);
                else if (_ergebnisse != null && _ergebnisse.JeKennung.ContainsKey(r.Kennung)
                         && !Meldungen.Any(m => m.Schluessel == ERGEBNIS_GETEILT && m.Werte.Length > 0 && m.Werte[0] == r.Kennung))
                    Meldungen.Add(new PruefMeldung(PruefStufe.Info, ERGEBNIS_GETEILT, r.Kennung, Ganz(raeume)));
            }

            private void Bauteil(IfcElement e, AbbildBauteil b, AbbildBauteil wirt, bool eins)
            {
                Randbedingung rand = Rand(wirt ?? b);
                bool aussen = rand == Randbedingung.Aussenluft || rand == Randbedingung.Erdreich;
                double? u = b.UWertWm2K.HasValue ? Endlich(b.UWertWm2K.Value) : (double?)null;
                string klasse = Klasse(e);
                if (klasse != null)
                    Ergaenzen(e, "Pset_" + klasse + "Common",
                              u.HasValue ? Wert("ThermalTransmittance", new IfcThermalTransmittanceMeasure(u.Value)) : null,
                              Neben("IsExternal", new IfcBoolean(aussen)));
                if ((klasse == "Window" || klasse == "Door") && b.GWert.HasValue)
                    Ergaenzen(e, "Pset_DoorWindowGlazingType",
                              Wert("SolarHeatGainTransmittance", new IfcNormalisedRatioMeasure(Endlich(b.GWert.Value))));
                if (eins)
                {
                    double? flaeche = b.BruttoflaecheM2.HasValue ? Endlich(b.BruttoflaecheM2.Value) : (double?)null;
                    switch (klasse)
                    {
                        case "Wall":
                            Mengen(e, "Qto_WallBaseQuantities", Flaeche("GrossSideArea", flaeche), Laenge("Width", b.DickeM));
                            break;
                        case "Slab":
                            Mengen(e, "Qto_SlabBaseQuantities", Flaeche("GrossArea", flaeche), Laenge("Width", b.DickeM));
                            break;
                        case "Roof":
                            Mengen(e, "Qto_RoofBaseQuantities", Flaeche("GrossArea", flaeche));
                            break;
                        case "Window":
                        case "Door":
                            Mengen(e, "Qto_" + klasse + "BaseQuantities", Flaeche("Area", flaeche), Laenge("Width", b.BreiteM), Laenge("Height", b.HoeheM));
                            break;
                    }
                }

                AbbildAufbau a = b.Aufbau;
                bool schichten = a != null && a.Schichten.Count > 0;
                bool vorhanden = HatMaterial(e);
                string herkunft = vorhanden ? "Datei" : schichten ? _profil.Programmname : null;
                if (!vorhanden && schichten) Aufbau(a, e);
                bool zusammen = b.Kennung.StartsWith(IfcSchreiber.PRAEFIX_ZUSAMMENFASSUNG, StringComparison.Ordinal);
                Eigen(e, IfcSchreiber.EPOS_BAUTEIL,
                      Wert("Kennung", new IfcIdentifier(b.Kennung)),
                      Wert("Bauteilart", new IfcLabel(b.Art.ToString())),
                      Wert("Randbedingung", new IfcLabel(rand.ToString())),
                      Winkel("Azimut", b.AzimutGrad, "GEXP_IFC_AZIMUT_BEZUG"),
                      Winkel("Neigung", b.NeigungGrad, "GEXP_IFC_NEIGUNG_BEZUG"),
                      Wert("IstZusammenfassung", new IfcBoolean(zusammen)),
                      Zahl("WaermebrueckeUA", b.WaermebrueckeWK, "GEXP_IFC_WAERMEBRUECKE"),
                      schichten ? Wert("Schichtrichtung", new IfcLabel(IfcSchreiber.SCHICHTRICHTUNG_AUSSEN_INNEN), null, "GEXP_IFC_SCHICHTRICHTUNG") : null,
                      a != null ? Wert("Ersatzschichtung", new IfcBoolean(a.IstErsatz)) : null,
                      herkunft != null ? Wert("Materialherkunft", new IfcLabel(herkunft), null, "GEXP_IFC_ANR_MATERIALHERKUNFT") : null);
            }

            /// <summary>Die Klasse des Standardsatzes (<c>Pset_&lt;Klasse&gt;Common</c>); <c>null</c> = keiner.</summary>
            private static string Klasse(IfcElement e)
            {
                switch (e)
                {
                    case Xbim.Ifc4.SharedBldgElements.IfcWall _: return "Wall";
                    case Xbim.Ifc4.SharedBldgElements.IfcSlab _: return "Slab";
                    case Xbim.Ifc4.SharedBldgElements.IfcRoof _: return "Roof";
                    case Xbim.Ifc4.SharedBldgElements.IfcWindow _: return "Window";
                    case Xbim.Ifc4.SharedBldgElements.IfcDoor _: return "Door";
                    case Xbim.Ifc4.SharedBldgElements.IfcCurtainWall _: return "CurtainWall";
                    case Xbim.Ifc4.SharedBldgElements.IfcCovering _: return "Covering";
                    case Xbim.Ifc4.SharedBldgElements.IfcPlate _: return "Plate";
                    default: return null;
                }
            }

            /// <summary>Die Randbedingung wie beim Schreiber: aus der gbXML-Flächenart, sonst aus dem Abbild; zwei Nachbarn sind innen.</summary>
            private static Randbedingung Rand(AbbildBauteil b)
            {
                Randbedingung rand = b.Quellart != null && GbxmlVokabular.Flaechenarten.TryGetValue(b.Quellart, out var art) ? art.Rand : b.Randbedingung;
                if (b.Nachbarn.Count > 1 && (rand == Randbedingung.Aussenluft || rand == Randbedingung.Erdreich)) rand = Randbedingung.Innen;
                return rand;
            }

            /// <summary>Trägt das Objekt oder sein Typ schon eine Materialzuordnung?</summary>
            private static bool HatMaterial(IfcObject o)
            {
                if (o.HasAssociations.OfType<IfcRelAssociatesMaterial>().Any()) return true;
                foreach (IfcRelDefinesByType t in o.IsTypedBy)
                    if (t.RelatingType != null && t.RelatingType.HasAssociations.OfType<IfcRelAssociatesMaterial>().Any()) return true;
                return false;
            }

            /// <summary><c>EPOS_Ergebnis</c> eines Objekts, soweit die Ergebnisse es tragen; Energie in kWh mit Unit.</summary>
            private void Ergebnis(IfcObject ziel, string kennung, double? flaeche)
            {
                if (_ergebnisse == null || !_ergebnisse.JeKennung.TryGetValue(kennung, out IfcErgebnis e) || e == null)
                {
                    Entfernen(ziel, IfcSchreiber.EPOS_ERGEBNIS);
                    return;
                }
                double? bezogen = e.HeizwaermebedarfKwh.HasValue && flaeche > 0.0 ? e.HeizwaermebedarfKwh / flaeche : null;
                Eigen(ziel, IfcSchreiber.EPOS_ERGEBNIS,
                      e.HeizwaermebedarfKwh.HasValue ? Wert("Heizwaermebedarf", new IfcEnergyMeasure(Endlich(e.HeizwaermebedarfKwh.Value)), Kwh()) : null,
                      Zahl("HeizwaermebedarfFlaechenbezogen", bezogen, "GEXP_IFC_FLAECHENBEZOGEN"),
                      e.HeizlastW.HasValue ? Wert("Heizlast", new IfcPowerMeasure(Endlich(e.HeizlastW.Value)), Watt()) : null,
                      Temperatur("RaumtemperaturMittel", e.RaumtemperaturMittelC),
                      Temperatur("RaumtemperaturMax", e.RaumtemperaturMaxC),
                      e.KaeltebedarfKwh.HasValue ? Wert("Kaeltebedarf", new IfcEnergyMeasure(Endlich(e.KaeltebedarfKwh.Value)), Kwh(), "GEXP_DATEI_KAELTE_SENSIBEL") : null,
                      e.KaeltelastW.HasValue ? Wert("Kaeltelast", new IfcPowerMeasure(Endlich(e.KaeltelastW.Value)), Watt(), "GEXP_DATEI_KAELTE_SENSIBEL") : null);
            }

            /// <summary><c>EPOS_Rechenlauf</c> am Gebäude: Rechenmodell, Produktausweis (E10), Fassung, Zeitpunkt, Wetter.</summary>
            private void Rechenlauf(IfcObject ziel)
            {
                string modell = IfcSchreiber.T(_profil, _mehrzonig ? "GEXP_IFC_RECHENMODELL_MEHRZONE" : "GEXP_IFC_RECHENMODELL_EINZONE");
                Eigen(ziel, IfcSchreiber.EPOS_RECHENLAUF,
                      Wert("Rechenmodell", new IfcLabel(modell)),
                      Wert("Validierung", new IfcLabel(IfcSchreiber.T(_profil, nameof(MyResource.Resource.GEB_PRODUKTAUSWEIS_VDI6007)))),
                      Wert("Programmfassung", new IfcLabel(_profil.Programmname + " " + _profil.Programmversion)),
                      _ergebnisse?.Rechenzeitpunkt != null
                          ? Wert("Rechenzeitpunkt", new IfcDateTime(_ergebnisse.Rechenzeitpunkt.Value.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture)))
                          : null,
                      !string.IsNullOrWhiteSpace(_ergebnisse?.Wetterdatensatz) ? Wert("Wetterdatensatz", new IfcLabel(_ergebnisse.Wetterdatensatz.Trim())) : null);
            }

            // --------------------------------------------------------------
            //  Baustoffe — nur ohne Materialzuordnung
            // --------------------------------------------------------------

            /// <summary>Der Aufbau als <c>IfcMaterialLayerSet</c> wie beim Schreiber, erste Schicht außen; Dicken in Projekteinheiten.</summary>
            private void Aufbau(AbbildAufbau a, IfcElement e)
            {
                string kennung = string.IsNullOrWhiteSpace(a.Kennung) ? "epos-aufbau-" + e.GlobalId.ToString() : a.Kennung;
                if (!_saetze.TryGetValue(kennung, out IfcMaterialLayerSet satz))
                {
                    List<AbbildSchicht> schichten = a.Schichten.ToList();
                    if (a.Richtung == Schichtrichtung.InnenNachAussen) schichten.Reverse();
                    var lagen = schichten.Select(Schicht).ToList();
                    satz = Neu<IfcMaterialLayerSet>(s =>
                    {
                        s.LayerSetName = Label(a.Name ?? kennung);
                        s.Description = Text(a.Beschreibung);
                        s.MaterialLayers.AddRange(lagen);
                    });
                    _saetze[kennung] = satz;
                    _satzObjekte[kennung] = new List<IfcObjectDefinition>();
                    _satzReihenfolge.Add(kennung);
                }
                _satzObjekte[kennung].Add(e);
            }

            private IfcMaterialLayer Schicht(AbbildSchicht s)
            {
                IfcMaterial stoff = Stoff(s);
                bool luft = s.NurRWert;
                return Neu<IfcMaterialLayer>(l =>
                {
                    l.Material = stoff;
                    l.LayerThickness = s.DickeM > 0.0 ? Endlich(s.DickeM.Value) / _laenge : 0.0;
                    l.IsVentilated = luft ? new IfcLogical((bool?)null) : new IfcLogical(false);
                    l.Name = Label(s.Name);
                });
            }

            private IfcMaterial Stoff(AbbildSchicht s)
            {
                string kennung = string.IsNullOrWhiteSpace(s.BaustoffKennung) ? s.Kennung : s.BaustoffKennung;
                if (string.IsNullOrWhiteSpace(kennung)) throw new InvalidOperationException("IFC-Anreicherung: Eine Schicht ohne Baustoffkennung.");
                if (_stoffe.TryGetValue(kennung, out IfcMaterial m)) return m;
                m = Neu<IfcMaterial>(x =>
                {
                    x.Name = s.Name ?? kennung;
                    x.Description = kennung;
                });
                _stoffe[kennung] = m;
                Stoffsatz(m, "Pset_MaterialThermal",
                          s.LambdaWmK > 0.0 ? Neu<IfcPropertySingleValue>(p => { p.Name = "ThermalConductivity"; p.NominalValue = new IfcThermalConductivityMeasure(Endlich(s.LambdaWmK.Value)); }) : null,
                          s.CpJkgK > 0.0 ? Neu<IfcPropertySingleValue>(p => { p.Name = "SpecificHeatCapacity"; p.NominalValue = new IfcSpecificHeatCapacityMeasure(Endlich(s.CpJkgK.Value)); }) : null);
                Stoffsatz(m, "Pset_MaterialCommon",
                          s.RhoKgM3 > 0.0 ? Neu<IfcPropertySingleValue>(p => { p.Name = "MassDensity"; p.NominalValue = new IfcMassDensityMeasure(Endlich(s.RhoKgM3.Value)); }) : null);
                Stoffsatz(m, IfcSchreiber.EPOS_BAUSTOFF,
                          s.RWertM2KW > 0.0 ? Neu<IfcPropertySingleValue>(p => { p.Name = "Waermedurchlasswiderstand"; p.NominalValue = new IfcThermalResistanceMeasure(Endlich(s.RWertM2KW.Value)); }) : null);
                return m;
            }

            private void Stoffsatz(IfcMaterial m, string name, params IfcPropertySingleValue[] werte)
            {
                List<IfcPropertySingleValue> liste = werte.Where(w => w != null).ToList();
                if (liste.Count == 0) return;
                Neu<IfcMaterialProperties>(p =>
                {
                    p.Name = name;
                    p.Material = m;
                    p.Properties.AddRange(liste);
                });
            }

            // --------------------------------------------------------------
            //  Sätze: ergänzen statt doppeln, eigene Sätze ersetzen
            // --------------------------------------------------------------

            /// <summary>Die Eigenschaftssätze eines Objekts mit diesem Namen (am Vorkommnis, nicht am Typ).</summary>
            private static List<IfcPropertySet> Saetze(IfcObject o, string name)
                => o.IsDefinedBy.Select(r => r.RelatingPropertyDefinition).OfType<IfcPropertySet>()
                    .Where(s => s.Name.HasValue && string.Equals(s.Name.Value.ToString(), name, StringComparison.Ordinal))
                    .Distinct().ToList();

            /// <summary>Wie viele Objekte teilen den Satz?</summary>
            private static int Teilende(IfcPropertySetDefinition satz) => satz.DefinesOccurrence.Sum(r => r.RelatedObjects.Count);

            /// <summary>
            /// <b>Ergänzt einen Standardsatz:</b> fehlt er, wird er angelegt; sonst ersetzt eine Angabe mit
            /// <see cref="Angabe.Ersetzen"/> die gleichnamige Eigenschaft, die übrigen kommen nur hinzu, wenn sie fehlen.
            /// Teilt das Objekt den Satz mit anderen und ändert sich etwas, wird er abgespalten.
            /// </summary>
            private void Ergaenzen(IfcObject o, string name, params Angabe[] angaben)
            {
                List<Angabe> liste = angaben.Where(a => a != null).ToList();
                if (liste.Count == 0) return;
                string schluessel = o.GlobalId.ToString();
                IfcPropertySet satz = Saetze(o, name).FirstOrDefault();
                if (satz == null)
                {
                    Anlegen(o, schluessel, "Pset:" + name, name, liste.Select(Eigenschaft));
                    Ergaenzt++;
                    return;
                }

                Dictionary<string, IfcProperty> da = satz.HasProperties.Where(p => p.Name != null)
                    .GroupBy(p => p.Name.ToString(), StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
                var tausch = new List<(IfcProperty Alt, Angabe Neu)>();
                var dazu = new List<Angabe>();
                foreach (Angabe a in liste)
                {
                    if (!da.TryGetValue(a.Name, out IfcProperty alt)) dazu.Add(a);
                    else if (a.Ersetzen && !Gleich(alt, a)) tausch.Add((alt, a));
                }
                if (tausch.Count == 0 && dazu.Count == 0) return;

                if (Teilende(satz) > 1)
                {
                    // Abspalten: ein eigener Satz mit den übrigen Eigenschaften (verwiesen, nicht kopiert) und den neuen Werten.
                    var behalten = satz.HasProperties.Where(p => !tausch.Any(t => ReferenceEquals(t.Alt, p))).ToList();
                    foreach (IfcRelDefinesByProperties r in satz.DefinesOccurrence.Where(r => r.RelatedObjects.Contains(o)).ToList())
                        r.RelatedObjects.Remove(o);
                    Anlegen(o, schluessel, "Pset:" + name + ":Eigen", name,
                            behalten.Concat(tausch.Select(t => Eigenschaft(t.Neu))).Concat(dazu.Select(Eigenschaft)));
                    Ersetzt += tausch.Count;
                    Ergaenzt += dazu.Count;
                    return;
                }

                foreach (var (alt, neu) in tausch)
                {
                    if (alt is IfcPropertySingleValue w && w.PartOfPset.Count() <= 1)
                    {
                        w.NominalValue = neu.Wert;
                        w.Unit = neu.Einheit;
                        if (neu.Beschreibung != null) w.Description = IfcSchreiber.T(_profil, neu.Beschreibung);
                    }
                    else
                    {
                        satz.HasProperties.Remove(alt);
                        satz.HasProperties.Add(Eigenschaft(neu));
                    }
                    Ersetzt++;
                }
                foreach (Angabe a in dazu) satz.HasProperties.Add(Eigenschaft(a));
                Ergaenzt += dazu.Count;
            }

            private static bool Gleich(IfcProperty alt, Angabe neu)
                => alt is IfcPropertySingleValue w && w.NominalValue != null && neu.Wert != null
                   && w.NominalValue.GetType() == neu.Wert.GetType() && Equals(w.NominalValue.Value, neu.Wert.Value)
                   && neu.Einheit == null && w.Unit == null;

            /// <summary>
            /// <b>Ein eigener EPOS-Satz:</b> der eines früheren Durchlaufs wird entfernt und neu angelegt (gezählt als
            /// ersetzt), sonst angelegt (gezählt als ergänzt). Ohne Angabe entfällt der Satz.
            /// </summary>
            private void Eigen(IfcObject o, string name, params Angabe[] angaben)
            {
                bool frueher = Entfernen(o, name);
                List<Angabe> liste = angaben.Where(a => a != null).ToList();
                if (liste.Count == 0) return;
                Anlegen(o, o.GlobalId.ToString(), "Pset:" + name, name, liste.Select(Eigenschaft));
                if (frueher) Ersetzt++;
                else Ergaenzt++;
            }

            /// <summary>Löst die Sätze dieses Namens vom Objekt; ein Satz, den sonst niemand trägt, wird samt Beziehung gelöscht.</summary>
            private bool Entfernen(IfcObject o, string name)
            {
                List<IfcPropertySet> saetze = Saetze(o, name);
                foreach (IfcPropertySet satz in saetze)
                {
                    foreach (IfcRelDefinesByProperties r in satz.DefinesOccurrence.Where(r => r.RelatedObjects.Contains(o)).ToList())
                    {
                        if (r.RelatedObjects.Count > 1) r.RelatedObjects.Remove(o);
                        else Loeschen(r);
                    }
                    if (satz.DefinesOccurrence.Any()) continue;
                    var eigenschaften = satz.HasProperties.ToList();
                    Loeschen(satz);
                    foreach (IfcProperty p in eigenschaften)
                        if (!p.PartOfPset.Any()) _m.Delete(p);
                }
                return saetze.Count > 0;
            }

            private void Loeschen(IfcRoot r)
            {
                _index.Remove(r.GlobalId.ToString());
                _m.Delete(r);
            }

            /// <summary>Ein Mengensatz — nur, wenn das Objekt keinen gleichnamigen trägt (vorhandene Mengen der Datei bleiben).</summary>
            private void Mengen(IfcObject o, string name, params Menge[] mengen)
            {
                List<Menge> liste = mengen.Where(w => w.Wert.HasValue).ToList();
                if (liste.Count == 0) return;
                bool da = o.IsDefinedBy.Select(r => r.RelatingPropertyDefinition).OfType<IfcElementQuantity>()
                           .Any(q => q.Name.HasValue && string.Equals(q.Name.Value.ToString(), name, StringComparison.Ordinal));
                if (da) return;
                List<IfcPhysicalSimpleQuantity> werte = liste.Select(Mengenwert).ToList();
                string schluessel = o.GlobalId.ToString();
                IfcElementQuantity satz = Wurzel<IfcElementQuantity>(schluessel, "Qto:" + name, q =>
                {
                    q.Name = name;
                    q.Quantities.AddRange(werte);
                });
                Wurzel<IfcRelDefinesByProperties>(schluessel, "RelProps:Qto:" + name, r =>
                {
                    r.RelatingPropertyDefinition = satz;
                    r.RelatedObjects.Add(o);
                });
                Ergaenzt++;
            }

            /// <summary>Eine Menge in SI: Art 2 = Fläche, 1 = Länge, 3 = Volumen.</summary>
            private readonly record struct Menge(int Art, string Name, double? Wert);

            /// <summary>Die Menge in den Projekteinheiten der Datei.</summary>
            private IfcPhysicalSimpleQuantity Mengenwert(Menge m)
            {
                double w = Endlich(m.Wert.Value);
                switch (m.Art)
                {
                    case 2: return Neu<IfcQuantityArea>(q => { q.Name = m.Name; q.AreaValue = w / _flaeche; });
                    case 3: return Neu<IfcQuantityVolume>(q => { q.Name = m.Name; q.VolumeValue = w / _volumen; });
                    default: return Neu<IfcQuantityLength>(q => { q.Name = m.Name; q.LengthValue = w / _laenge; });
                }
            }

            private void Anlegen(IfcObject o, string schluessel, string rolle, string name, IEnumerable<IfcProperty> eigenschaften)
            {
                List<IfcProperty> liste = eigenschaften.ToList();
                IfcPropertySet satz = Wurzel<IfcPropertySet>(schluessel, rolle, p =>
                {
                    p.Name = name;
                    p.HasProperties.AddRange(liste);
                });
                Wurzel<IfcRelDefinesByProperties>(schluessel, "RelProps:" + rolle, r =>
                {
                    r.RelatingPropertyDefinition = satz;
                    r.RelatedObjects.Add(o);
                });
            }

            // --------------------------------------------------------------
            //  Werte und Einheiten
            // --------------------------------------------------------------

            private IfcPropertySingleValue Eigenschaft(Angabe a)
                => Neu<IfcPropertySingleValue>(p =>
                {
                    p.Name = a.Name;
                    p.NominalValue = a.Wert;
                    if (a.Einheit != null) p.Unit = a.Einheit;
                    if (a.Beschreibung != null) p.Description = IfcSchreiber.T(_profil, a.Beschreibung);
                    if (a.BeschreibungWoertlich != null) p.Description = new IfcText(a.BeschreibungWoertlich);
                });

            /// <summary>Ein Wert der Konditionierungssätze als Fachwert von EPOS (<see cref="IfcSatzwertart"/>).</summary>
            private Angabe Satzwert(IfcSatzwert w)
            {
                Angabe a;
                switch (w.Art)
                {
                    case IfcSatzwertart.Temperatur: a = Temperatur(w.Name, w.Zahl); break;
                    case IfcSatzwertart.Zahl: a = Zahl(w.Name, w.Zahl, w.Beschreibung); break;
                    case IfcSatzwertart.Leistung: a = w.Zahl.HasValue ? Wert(w.Name, new IfcPowerMeasure(Endlich(w.Zahl.Value)), Watt()) : null; break;
                    case IfcSatzwertart.Kennwort: a = Wert(w.Name, new IfcLabel(w.Text ?? ""), null, w.Beschreibung); break;
                    case IfcSatzwertart.Wahrheit: a = Wert(w.Name, new IfcBoolean(w.Wahr == true), null, w.Beschreibung); break;
                    default: a = Wert(w.Name, new IfcText(w.Text ?? ""), null, w.Beschreibung); break;
                }
                return a == null || w.BeschreibungWoertlich == null ? a : a with { BeschreibungWoertlich = w.BeschreibungWoertlich };
            }

            /// <summary>Ein Fachwert von EPOS: ersetzt eine vorhandene Eigenschaft.</summary>
            private static Angabe Wert(string name, IfcValue wert, IfcUnit einheit = null, string beschreibung = null)
                => new Angabe(name, wert, einheit, beschreibung, true);

            /// <summary>Ein Nebenwert: kommt nur hinzu, wenn er fehlt.</summary>
            private static Angabe Neben(string name, IfcValue wert) => new Angabe(name, wert, null, null, false);

            private static Angabe Zahl(string name, double? wert, string beschreibung)
                => wert.HasValue ? Wert(name, new IfcReal(Endlich(wert.Value)), null, beschreibung) : null;

            /// <summary>Eine Temperatur in Kelvin mit ausdrücklicher Einheit — unabhängig von der Temperatureinheit der Datei.</summary>
            private Angabe Temperatur(string name, double? celsius)
                => celsius.HasValue ? Wert(name, new IfcThermodynamicTemperatureMeasure(Endlich(celsius.Value) + IfcSchreiber.NULLPUNKT_K), Kelvin()) : null;

            private Angabe Winkel(string name, double? grad, string beschreibung)
                => grad.HasValue ? Wert(name, new IfcPlaneAngleMeasure(Endlich(grad.Value)), Grad(), beschreibung) : null;

            private static Menge Flaeche(string name, double? wert) => new Menge(2, name, wert);

            private static Menge Laenge(string name, double? wert) => new Menge(1, name, wert);

            private static Menge Volumen(string name, double? wert) => new Menge(3, name, wert);

            private IfcSIUnit Si(IfcUnitEnum art, IfcSIUnitName name) => Neu<IfcSIUnit>(u =>
            {
                u.UnitType = art;
                u.Name = name;
            });

            private IfcSIUnit Watt() => _watt ??= Si(IfcUnitEnum.POWERUNIT, IfcSIUnitName.WATT);

            private IfcSIUnit Kelvin() => _kelvin ??= Si(IfcUnitEnum.THERMODYNAMICTEMPERATUREUNIT, IfcSIUnitName.KELVIN);

            private IfcConversionBasedUnit Kwh() => _kwh ??= Umrechnung(IfcUnitEnum.ENERGYUNIT, IfcSchreiber.KILOWATTSTUNDE, 2, 1, -2,
                new IfcEnergyMeasure(3.6e6), Si(IfcUnitEnum.ENERGYUNIT, IfcSIUnitName.JOULE));

            private IfcConversionBasedUnit Grad() => _grad ??= Umrechnung(IfcUnitEnum.PLANEANGLEUNIT, IfcSchreiber.GRAD, 0, 0, 0,
                new IfcPlaneAngleMeasure(Math.PI / 180.0), Si(IfcUnitEnum.PLANEANGLEUNIT, IfcSIUnitName.RADIAN));

            private IfcConversionBasedUnit Umrechnung(IfcUnitEnum art, string name, int laenge, int masse, int zeit, IfcValue faktor, IfcSIUnit basis)
            {
                IfcDimensionalExponents dim = Neu<IfcDimensionalExponents>(d =>
                {
                    d.LengthExponent = laenge;
                    d.MassExponent = masse;
                    d.TimeExponent = zeit;
                });
                return Neu<IfcConversionBasedUnit>(u =>
                {
                    u.UnitType = art;
                    u.Name = name;
                    u.Dimensions = dim;
                    u.ConversionFactor = Neu<IfcMeasureWithUnit>(w =>
                    {
                        w.ValueComponent = faktor;
                        w.UnitComponent = basis;
                    });
                });
            }

            private static IfcLabel? Label(string text) => string.IsNullOrWhiteSpace(text) ? (IfcLabel?)null : new IfcLabel(text.Trim());

            private static IfcText? Text(string text) => string.IsNullOrWhiteSpace(text) ? (IfcText?)null : new IfcText(text.Trim());

            private static double Endlich(double wert)
            {
                if (double.IsNaN(wert) || double.IsInfinity(wert)) throw new InvalidOperationException("IFC-Anreicherung: Eine Zahl ist nicht endlich.");
                return wert;
            }
        }
    }
}
