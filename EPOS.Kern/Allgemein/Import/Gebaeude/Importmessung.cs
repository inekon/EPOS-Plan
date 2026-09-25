using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using SpeicherEngine;
using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.Common.Step21;
using Xbim.IO.Memory;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Ein Stand des Prozessspeichers</b> in Bytes, wie ihn die Plattform ausweist. Unter iOS liefert
    /// ihn die Schale aus <c>task_info(TASK_VM_INFO)</c> (<c>phys_footprint</c> — das Maß, an dem iOS
    /// eine App beendet — und dessen Lebensspitze); ohne Schale gilt <see cref="Standard"/>.
    /// </summary>
    internal readonly struct Prozessspeicherstand
    {
        public Prozessspeicherstand(long? aktuell, long? spitze, string quelle)
        {
            Aktuell = aktuell;
            Spitze = spitze;
            Quelle = quelle ?? "";
        }

        /// <summary>Der gegenwärtige Stand; <c>null</c>, wenn die Plattform ihn nicht liefert.</summary>
        public long? Aktuell { get; }

        /// <summary>Die Spitze seit Prozessbeginn, soweit die Plattform sie führt; sonst <c>null</c>.</summary>
        public long? Spitze { get; }

        /// <summary>Woher der Wert stammt (<c>phys_footprint</c>, <c>WorkingSet</c>, <c>nv-…</c>).</summary>
        public string Quelle { get; }

        /// <summary>Der plattformfreie Rückfall: <see cref="Environment.WorkingSet"/>, ohne Spitze.</summary>
        public static Prozessspeicherstand Standard()
        {
            try
            {
                long w = Environment.WorkingSet;
                return new Prozessspeicherstand(w > 0 ? w : (long?)null, null, "WorkingSet");
            }
            catch (Exception ex)
            {
                return new Prozessspeicherstand(null, null, "nv-" + ex.GetType().Name);
            }
        }
    }

    /// <summary>
    /// <b>Das Ergebnis einer Importmessung</b> — was der Importweg aus einer Datei gemacht hat und was er
    /// dafür an Zeit und Speicher gebraucht hat. Zwei invariante Protokollzeilen: <see cref="ProbeZeile"/>
    /// (Funktion) und <see cref="MessZeile"/> (Zeit und Speicher).
    /// </summary>
    internal sealed class Importmessergebnis
    {
        public const string OK = "OK";
        public const string LESEFEHLER = "LESEFEHLER";
        public const string RAEUME_FEHLEN = "RAEUME_FEHLEN";
        public const string AUSNAHME = "AUSNAHME";
        public const string FORMAT_UNBEKANNT = "FORMAT_UNBEKANNT";
        public const string ZEITGRENZE = "ZEITGRENZE";

        public string Fall { get; set; } = "";
        public string Format { get; set; } = "";
        public string Ergebnis { get; set; } = AUSNAHME;
        public string Schema { get; set; }
        public long Bytes { get; set; }
        public long? EntpackteBytes { get; set; }
        public long GrenzeIos { get; set; }
        public int Gebaeude { get; set; }
        public int Raeume { get; set; }
        public int? ErwarteteRaeume { get; set; }
        public double? Nutzflaeche { get; set; }
        public int Fehler { get; set; }
        public int Warnungen { get; set; }
        public int Hinweise { get; set; }
        public int PruefFehler { get; set; }
        public int PruefWarnungen { get; set; }
        public int FehlendeEntitaeten { get; set; }
        public string ErsteMeldung { get; set; }
        public string Ausnahme { get; set; }

        public long DauerLesenMs { get; set; }
        public long DauerMs { get; set; }
        public long VerwaltetVorher { get; set; }
        public long VerwaltetSpitze { get; set; }
        public long VerwaltetGehalten { get; set; }
        public long? Alloziert { get; set; }
        public long? ProzessVorher { get; set; }
        public long? ProzessSpitze { get; set; }
        public long? ProzessLebensspitze { get; set; }
        public string ProzessQuelle { get; set; } = "";
        public int Proben { get; set; }
        public int Gc0 { get; set; }
        public int Gc1 { get; set; }
        public int Gc2 { get; set; }

        /// <summary>Die Bezugsgröße der Faktoren: bei <c>.ifczip</c> der entpackte Inhalt, sonst die Datei.</summary>
        public long Basisbytes => EntpackteBytes.HasValue && EntpackteBytes.Value > 0 ? EntpackteBytes.Value : Bytes;

        /// <summary>Spitzenzuwachs des verwalteten Speichers je Byte der Bezugsgröße („MB je MB").</summary>
        public double? FaktorVerwaltet
            => Basisbytes > 0 ? Math.Max(0L, VerwaltetSpitze - VerwaltetVorher) / (double)Basisbytes : (double?)null;

        /// <summary>Spitzenzuwachs des Prozessspeichers je Byte der Bezugsgröße; <c>null</c> ohne Prozesswerte.</summary>
        public double? FaktorProzess
            => ProzessSpitze.HasValue && ProzessVorher.HasValue && Basisbytes > 0
                ? Math.Max(0L, ProzessSpitze.Value - ProzessVorher.Value) / (double)Basisbytes
                : (double?)null;

        /// <summary><c>IMPORTPROBE fall=… format=… ergebnis=… …</c> — was gelesen wurde.</summary>
        public string ProbeZeile()
        {
            var sb = new StringBuilder(Importmessung.PROBE);
            Feld(sb, "fall", Fall);
            Feld(sb, "format", Format);
            Feld(sb, "schema", Schema);
            Feld(sb, "ergebnis", Ergebnis);
            Feld(sb, "gebaeude", Importmessung.Ganz(Gebaeude));
            Feld(sb, "raeume", Importmessung.Ganz(Raeume));
            if (ErwarteteRaeume.HasValue) Feld(sb, "erwartet", Importmessung.Ganz(ErwarteteRaeume.Value));
            Feld(sb, "nutzflaeche", Nutzflaeche.HasValue ? Importmessung.Zahl(Nutzflaeche.Value, "0.##") : null);
            Feld(sb, "meldungen", "F" + Importmessung.Ganz(Fehler) + "/W" + Importmessung.Ganz(Warnungen) + "/I" + Importmessung.Ganz(Hinweise));
            Feld(sb, "pruefung", "F" + Importmessung.Ganz(PruefFehler) + "/W" + Importmessung.Ganz(PruefWarnungen));
            Feld(sb, "fehlend", Importmessung.Ganz(FehlendeEntitaeten));
            Feld(sb, "bytes", Importmessung.Ganz(Bytes));
            if (EntpackteBytes.HasValue) Feld(sb, "entpackt", Importmessung.Ganz(EntpackteBytes.Value));
            Feld(sb, "grenze_ios", Importmessung.Ganz(GrenzeIos));
            if (!string.IsNullOrEmpty(ErsteMeldung)) Text(sb, "erste", ErsteMeldung);
            if (!string.IsNullOrEmpty(Ausnahme)) Text(sb, "ausnahme", Ausnahme);
            return sb.ToString();
        }

        /// <summary><c>IMPORTMESSUNG fall=… bytes=… faktor=… …</c> — Zeit und Speicher (MB = 1024² Bytes).</summary>
        public string MessZeile()
        {
            var sb = new StringBuilder(Importmessung.MESSUNG);
            Feld(sb, "fall", Fall);
            Feld(sb, "bytes", Importmessung.Ganz(Bytes));
            Feld(sb, "basis_mb", Importmessung.Mb(Basisbytes));
            Feld(sb, "dauer_lesen_ms", Importmessung.Ganz(DauerLesenMs));
            Feld(sb, "dauer_ms", Importmessung.Ganz(DauerMs));
            Feld(sb, "verwaltet_vor_mb", Importmessung.Mb(VerwaltetVorher));
            Feld(sb, "verwaltet_spitze_mb", Importmessung.Mb(VerwaltetSpitze));
            Feld(sb, "zuwachs_mb", Importmessung.Mb(Math.Max(0L, VerwaltetSpitze - VerwaltetVorher)));
            Feld(sb, "gehalten_mb", Importmessung.Mb(VerwaltetGehalten));
            Feld(sb, "alloziert_mb", Alloziert.HasValue ? Importmessung.Mb(Alloziert.Value) : null);
            Feld(sb, "faktor", FaktorVerwaltet.HasValue ? Importmessung.Zahl(FaktorVerwaltet.Value, "0.0") : null);
            Feld(sb, "prozess", ProzessQuelle);
            Feld(sb, "prozess_vor_mb", ProzessVorher.HasValue ? Importmessung.Mb(ProzessVorher.Value) : null);
            Feld(sb, "prozess_spitze_mb", ProzessSpitze.HasValue ? Importmessung.Mb(ProzessSpitze.Value) : null);
            Feld(sb, "faktor_prozess", FaktorProzess.HasValue ? Importmessung.Zahl(FaktorProzess.Value, "0.0") : null);
            Feld(sb, "prozess_lebensspitze_mb", ProzessLebensspitze.HasValue ? Importmessung.Mb(ProzessLebensspitze.Value) : null);
            Feld(sb, "gc", Importmessung.Ganz(Gc0) + "/" + Importmessung.Ganz(Gc1) + "/" + Importmessung.Ganz(Gc2));
            Feld(sb, "proben", Importmessung.Ganz(Proben));
            return sb.ToString();
        }

        private static void Feld(StringBuilder sb, string name, string wert)
            => sb.Append(' ').Append(name).Append('=').Append(Importmessung.Wort(wert));

        private static void Text(StringBuilder sb, string name, string wert)
            => sb.Append(' ').Append(name).Append("=\"").Append(Importmessung.Einzeilig(wert).Replace('"', '\'')).Append('"');
    }

    /// <summary>Ein synthetischer Messfall: Dateiname (die Endung wählt das Profil) und Erzeuger.</summary>
    internal sealed class Importmessfall
    {
        public Importmessfall(string name, Func<(byte[] Daten, int Raeume)> erzeugen)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Erzeugen = erzeugen ?? throw new ArgumentNullException(nameof(erzeugen));
        }

        public string Name { get; }

        public Func<(byte[] Daten, int Raeume)> Erzeugen { get; }
    }

    /// <summary>
    /// <b>Die Importmessung</b> (Umsetzungskonzept Gebäudesimulation 3.6 und 3.8, G4-8): fährt für eine
    /// Datei den ganzen Importweg — <see cref="GebaeudeImportAblauf.Lesen"/> und
    /// <see cref="GebaeudeImportAblauf.Zuordnen"/> für das erste Gebäude, Profil nach Endung — und misst
    /// dabei Dauer und Speicher. Plattformfrei; der Prüfmodus der iOS-Schale ruft
    /// <see cref="Probelauf"/> mit den Proben aus dem App-Paket, die Tests unter Windows mit den Proben
    /// aus <c>Referenzlaeufe/Importproben/</c>.
    ///
    /// <para><b>Wozu.</b> Die iOS-Größengrenzen (IFC 20 MB, gbXML 10 MB) sind geschätzt
    /// (Softwarearchitektur 1.5, Regel 2). Aus <c>faktor</c> (Spitzenzuwachs des verwalteten Speichers je
    /// Byte der Datei) und <c>faktor_prozess</c> (dasselbe für den Prozessspeicher, wo die Plattform ihn
    /// liefert) und dem Speicherrahmen einer App auf dem kleinsten unterstützten iPad folgt die Grenze:
    /// Rahmen ÷ Faktor. Drei Größen je Format zeigen, wie Faktor und Dauer mit der Dateigröße gehen.</para>
    ///
    /// <para><b>Wie gemessen wird.</b> Vorher wird aufgeräumt (<see cref="GC.Collect()"/>); die Spitze
    /// des verwalteten Speichers wird an jedem Fortschrittspunkt des Laufs abgetastet
    /// (<c>GC.GetTotalMemory(false)</c>, also samt noch nicht eingesammeltem Abfall — näher an dem, was
    /// der Prozess tatsächlich belegt, als die lebenden Objekte). Das Gesamtvolumen der Zuweisungen gibt
    /// <see cref="GC.GetTotalAllocatedBytes(bool)"/>, wo die Laufzeit es führt. Kein eigener Faden —
    /// die Abtastung hängt am Melder des Ablaufs (Wächter <c>ParallelitaetWacheTests</c>). Gemessen wird
    /// OHNE Größengrenze; die Grenze der Plattform steht zum Vergleich in der Zeile.</para>
    ///
    /// <para><b>Fehler bleiben Zeilen.</b> Eine Ausnahme wird mit Typ, Nachricht und den ersten
    /// Stapelzeilen geschrieben, nie geworfen — genug, um aus einem einzigen iOS-Lauf abzuleiten, ob ein
    /// <c>TrimmerRootDescriptor</c> nötig ist (Umsetzungskonzept 3.6). Dazu fährt <see cref="Diagnose"/>
    /// den IFC-Weg Schritt für Schritt ohne die fangenden Blöcke von Ablauf und Leser.</para>
    /// </summary>
    internal static class Importmessung
    {
        /// <summary>Präfix der Funktionszeilen.</summary>
        public const string PROBE = "IMPORTPROBE";

        /// <summary>Präfix der Messzeilen.</summary>
        public const string MESSUNG = "IMPORTMESSUNG";

        /// <summary>Präfix der Diagnosezeilen.</summary>
        public const string DIAGNOSE = "IMPORTDIAGNOSE";

        /// <summary>Zahl der Stapelzeilen, die eine Ausnahme mitbringt.</summary>
        public const int STAPELZEILEN = 8;

        /// <summary>Zeitrahmen des Probelaufs in Minuten: danach beginnt kein synthetischer Fall mehr.</summary>
        public const int BUDGET_MINUTEN = 10;

        /// <summary>
        /// Zeitgrenze eines synthetischen Falls in Minuten — über das Abbruchzeichen des Ablaufs, also nur
        /// an dessen Prüfpunkten wirksam (Fortschritt des Parsers, Abschnitte des Abbilds).
        /// </summary>
        public const int FALLZEIT_MINUTEN = 5;

        /// <summary>1 MB = 1024² Bytes — wie die Grenzen der Profile.</summary>
        public const long MB = 1024L * 1024;

        /// <summary>
        /// Die Zahl der EXPRESS-Typen je Schema, wie <c>ExpressMetaData</c> sie ungetrimmt (Windows, Linux)
        /// aus den Schema-Assemblies liest. Weicht die Zahl in einer getrimmten App ab, hat der Trimmer
        /// Typen entfernt, die <c>module.GetTypes()</c> finden müsste.
        /// </summary>
        public static readonly IReadOnlyDictionary<XbimSchemaVersion, int> ErwarteteTypen = new Dictionary<XbimSchemaVersion, int>
        {
            [XbimSchemaVersion.Ifc2X3] = 771,
            [XbimSchemaVersion.Ifc4] = 933,
            [XbimSchemaVersion.Ifc4x3] = 1008,
        };

        /// <summary>Die EXPRESS-Namen, ohne die der IFC-Leser nicht auskommt.</summary>
        public static readonly IReadOnlyList<string> Schluesseltypen = new[]
        {
            "IFCPROJECT", "IFCSITE", "IFCBUILDING", "IFCBUILDINGSTOREY", "IFCSPACE", "IFCWALL", "IFCWINDOW",
            "IFCDOOR", "IFCSLAB", "IFCROOF", "IFCRELSPACEBOUNDARY", "IFCRELDEFINESBYPROPERTIES",
            "IFCRELDEFINESBYTYPE", "IFCPROPERTYSET", "IFCPROPERTYSINGLEVALUE", "IFCELEMENTQUANTITY",
            "IFCQUANTITYAREA", "IFCMATERIALLAYERSETUSAGE", "IFCLOCALPLACEMENT", "IFCSIUNIT",
        };

        // ==================================================================
        //  Messen
        // ==================================================================

        /// <summary>
        /// Misst den Importweg für eine Datei. Wirft nicht: Jede Ausnahme steht im Ergebnis.
        /// </summary>
        /// <param name="daten">Der Inhalt der Datei.</param>
        /// <param name="dateiname">Name samt Endung — die Endung wählt das Profil.</param>
        /// <param name="prozess">Naht zum Prozessspeicher; <c>null</c> = <see cref="Prozessspeicherstand.Standard"/>.</param>
        /// <param name="erwarteteRaeume">Für synthetische Fälle: so viele Räume muss der Leser finden.</param>
        /// <param name="abbruch">Zeitgrenze des Falls; ein Abbruch ergibt <see cref="Importmessergebnis.ZEITGRENZE"/>.</param>
        public static Importmessergebnis Messen(byte[] daten, string dateiname, Func<Prozessspeicherstand> prozess = null,
                                                int? erwarteteRaeume = null, CancellationToken abbruch = default)
        {
            var e = new Importmessergebnis
            {
                Fall = GebaeudeQuelle.NurName(dateiname ?? ""),
                Bytes = daten?.LongLength ?? 0,
                ErwarteteRaeume = erwarteteRaeume,
            };
            prozess ??= Prozessspeicherstand.Standard;

            GebaeudeImportProfil profil = GebaeudeImportProfil.FuerDatei(dateiname ?? "");
            if (profil == null || daten == null)
            {
                e.Ergebnis = Importmessergebnis.FORMAT_UNBEKANNT;
                return e;
            }
            e.Format = profil.Format;
            e.GrenzeIos = profil.GrenzeFuerPlattform(true);
            profil.MaxBytes = 0;   // gemessen wird ohne Grenze

            GebaeudeImportAblauf ablauf = null;
            try
            {
                Aufraeumen();
                int gc0 = GC.CollectionCount(0), gc1 = GC.CollectionCount(1), gc2 = GC.CollectionCount(2);
                e.VerwaltetVorher = GC.GetTotalMemory(true);
                long? alloziertVorher = Zuweisungen();
                Prozessspeicherstand pVorher = Sicher(prozess);
                e.ProzessVorher = pVorher.Aktuell;

                var abtaster = new Abtaster(prozess);
                abtaster.Probe();
                Stopwatch uhr = Stopwatch.StartNew();

                ablauf = new GebaeudeImportAblauf();
                int zahl;
                using (var strom = new MemoryStream(daten, false))
                    zahl = ablauf.Lesen(strom, dateiname, profil, abtaster, abbruch);
                e.DauerLesenMs = uhr.ElapsedMilliseconds;
                abtaster.Probe();

                GebaeudeImportSatz satz = null;
                if (zahl > 0)
                {
                    satz = ablauf.Zuordnen(0, null);
                    abtaster.Probe();
                }
                e.DauerMs = uhr.ElapsedMilliseconds;

                long? alloziertNachher = Zuweisungen();
                e.Alloziert = alloziertVorher.HasValue && alloziertNachher.HasValue ? alloziertNachher - alloziertVorher : null;
                e.VerwaltetSpitze = Math.Max(abtaster.SpitzeVerwaltet, e.VerwaltetVorher);
                e.ProzessSpitze = abtaster.SpitzeProzess;
                e.Proben = abtaster.Proben;
                e.VerwaltetGehalten = Math.Max(0L, GC.GetTotalMemory(true) - e.VerwaltetVorher);
                Prozessspeicherstand pNachher = Sicher(prozess);
                e.ProzessLebensspitze = pNachher.Spitze;
                e.ProzessQuelle = pNachher.Quelle;
                e.Gc0 = GC.CollectionCount(0) - gc0;
                e.Gc1 = GC.CollectionCount(1) - gc1;
                e.Gc2 = GC.CollectionCount(2) - gc2;

                Auswerten(e, ablauf, zahl, satz);
                GC.KeepAlive(satz);
            }
            catch (OperationCanceledException)
            {
                e.Ergebnis = Importmessergebnis.ZEITGRENZE;
            }
            catch (Exception ex)
            {
                e.Ergebnis = Importmessergebnis.AUSNAHME;
                e.Ausnahme = Ausnahmetext(ex);
            }
            finally
            {
                GC.KeepAlive(ablauf);
            }
            return e;
        }

        private static void Auswerten(Importmessergebnis e, GebaeudeImportAblauf ablauf, int zahl, GebaeudeImportSatz satz)
        {
            foreach (PruefMeldung m in ablauf.Meldungen)
            {
                if (m.Stufe == PruefStufe.Fehler) e.Fehler++;
                else if (m.Stufe == PruefStufe.Warnung) e.Warnungen++;
                else e.Hinweise++;
            }
            PruefMeldung erste = ablauf.Meldungen.FirstOrDefault(m => m.Stufe == PruefStufe.Fehler)
                                 ?? ablauf.Meldungen.FirstOrDefault(m => m.Schluessel.EndsWith("ENTITAETEN_VERLOREN", StringComparison.Ordinal));
            e.ErsteMeldung = erste?.ToString();

            e.Gebaeude = zahl;
            if (zahl <= 0 || ablauf.Abbild == null)
            {
                e.Ergebnis = Importmessergebnis.LESEFEHLER;
                return;
            }

            GebaeudeAbbild abbild = ablauf.Abbild;
            e.Schema = abbild.Schemastand;
            e.FehlendeEntitaeten = abbild.FehlendeEntitaeten;
            e.Raeume = abbild.Gebaeude[0].Raeume.Count;
            if (abbild is IfcGebaeudeAbbild ifc && ifc.EntpackteGroesse > 0) e.EntpackteBytes = ifc.EntpackteGroesse;

            if (satz != null)
            {
                e.Nutzflaeche = satz.Zeile(GebaeudeZielfelder.NUTZFLAECHE)?.Wert;
                foreach (PruefMeldung m in GebaeudeImportAblauf.Pruefen(satz))
                {
                    if (m.Stufe == PruefStufe.Fehler) e.PruefFehler++;
                    else if (m.Stufe == PruefStufe.Warnung) e.PruefWarnungen++;
                }
            }

            e.Ergebnis = e.ErwarteteRaeume.HasValue && e.ErwarteteRaeume.Value != e.Raeume
                ? Importmessergebnis.RAEUME_FEHLEN
                : Importmessergebnis.OK;
        }

        /// <summary>Tastet den Speicher an jedem Fortschrittspunkt ab — im Faden des Laufs.</summary>
        private sealed class Abtaster : IProgress<ImportFortschritt>
        {
            private readonly Func<Prozessspeicherstand> _prozess;

            public Abtaster(Func<Prozessspeicherstand> prozess) => _prozess = prozess;

            public long SpitzeVerwaltet { get; private set; }

            public long? SpitzeProzess { get; private set; }

            public int Proben { get; private set; }

            public void Report(ImportFortschritt value) => Probe();

            public void Probe()
            {
                Proben++;
                long v = GC.GetTotalMemory(false);
                if (v > SpitzeVerwaltet) SpitzeVerwaltet = v;
                long? p = Sicher(_prozess).Aktuell;
                if (p.HasValue && (!SpitzeProzess.HasValue || p.Value > SpitzeProzess.Value)) SpitzeProzess = p;
            }
        }

        private static Prozessspeicherstand Sicher(Func<Prozessspeicherstand> prozess)
        {
            try { return prozess == null ? new Prozessspeicherstand(null, null, "nv") : prozess(); }
            catch (Exception ex) { return new Prozessspeicherstand(null, null, "nv-" + ex.GetType().Name); }
        }

        private static long? Zuweisungen()
        {
            try
            {
                long w = GC.GetTotalAllocatedBytes(true);
                return w > 0 ? w : (long?)null;
            }
            catch (Exception) { return null; }
        }

        private static void Aufraeumen()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        // ==================================================================
        //  Synthetische Fälle
        // ==================================================================

        /// <summary>
        /// Die synthetischen Großfälle des Prüfmodus, klein vor groß — ein Abbruch beim größten Fall lässt
        /// die kleineren im Protokoll stehen; erzeugt wird erst beim Messen. gbXML: 2 MB, 8 MB und knapp
        /// die iOS-Grenze des Profils (10 MB). IFC: 2, 4 und 8 MB — NICHT die Grenze von 20 MB: Das Abbild
        /// des IFC-Lesers wächst überproportional mit der Zahl der Bauteile (unter Windows 2 MB ≈ 3 s,
        /// 4 MB ≈ 6 s, 8 MB ≈ 1 min; 20 MB lief nach 12 min noch), ein Fall an der Grenze sprengte den
        /// Zeitrahmen des Laufs. Die drei Punkte zeigen den Verlauf von Speicher UND Zeit.
        /// </summary>
        public static IReadOnlyList<Importmessfall> SynthetischeFaelle()
        {
            long gbxmlGrenze = (long)(GbxmlImportProfil.MAX_BYTES_IOS * 0.98);
            return new[]
            {
                Fall("synth_gbxml_2mb.xml", 2 * MB, ImportmessungProben.Gbxml),
                Fall("synth_gbxml_8mb.xml", 8 * MB, ImportmessungProben.Gbxml),
                Fall("synth_gbxml_grenze_ios.xml", gbxmlGrenze, ImportmessungProben.Gbxml),
                Fall("synth_ifc_2mb.ifc", 2 * MB, ImportmessungProben.Ifc),
                Fall("synth_ifc_4mb.ifc", 4 * MB, ImportmessungProben.Ifc),
                Fall("synth_ifc_8mb.ifc", 8 * MB, ImportmessungProben.Ifc),
            };
        }

        /// <summary>Ein Fall von ungefähr <paramref name="zielBytes"/> Bytes.</summary>
        public static Importmessfall Fall(string name, long zielBytes, Func<int, byte[]> erzeugen)
            => new Importmessfall(name, () => ImportmessungProben.ZuGroesse(zielBytes, erzeugen));

        // ==================================================================
        //  Probelauf
        // ==================================================================

        /// <summary>
        /// <b>Der ganze Probelauf des Prüfmodus</b>: Kopfzeile, Metadaten der drei Schemata
        /// (<see cref="XbimDiagnose"/>), je Datei eine Probe- und eine Messzeile (dazu die Diagnose des
        /// IFC-Wegs), dann die synthetischen Fälle, zuletzt die Schlusszeile. Wirft nicht.
        /// </summary>
        /// <param name="dateien">Die Proben: Name samt Endung und ein Öffner, der <c>null</c> liefern darf.</param>
        /// <param name="zeile">Wohin die Zeilen gehen.</param>
        /// <param name="prozess">Naht zum Prozessspeicher; <c>null</c> = Standard.</param>
        /// <param name="faelle">Die synthetischen Fälle; <c>null</c> = <see cref="SynthetischeFaelle"/>.</param>
        /// <param name="budget">
        /// Zeitrahmen des ganzen Laufs; ist er verbraucht, wird kein weiterer synthetischer Fall begonnen
        /// (eine Zeile <c>ergebnis=UEBERSPRUNGEN</c> je Fall). <c>null</c> = <see cref="BUDGET_MINUTEN"/>.
        /// </param>
        /// <returns>Die Zahl der Fälle mit Ergebnis <c>OK</c>.</returns>
        public static int Probelauf(IEnumerable<(string Name, Func<Stream> Oeffnen)> dateien, Action<string> zeile,
                                    Func<Prozessspeicherstand> prozess = null, IEnumerable<Importmessfall> faelle = null,
                                    TimeSpan? budget = null)
        {
            if (zeile == null) throw new ArgumentNullException(nameof(zeile));
            Stopwatch uhr = Stopwatch.StartNew();
            int gesamt = 0, gut = 0;

            Schreiben(zeile, PROBE + " start laufzeit=" + Wort(Laufzeit()) + " prozess=" + Wort(Sicher(prozess ?? Prozessspeicherstand.Standard).Quelle));
            foreach (string z in Geschuetzt(() => XbimDiagnose(), "xbim")) Schreiben(zeile, z);

            foreach ((string name, Func<Stream> oeffnen) in dateien ?? Enumerable.Empty<(string, Func<Stream>)>())
            {
                gesamt++;
                byte[] daten;
                try
                {
                    daten = Lesen(oeffnen);
                }
                catch (Exception ex)
                {
                    Schreiben(zeile, PROBE + " fall=" + Wort(name) + " ergebnis=DATEI_UNLESBAR ausnahme=\"" + Einzeilig(Ausnahmetext(ex)).Replace('"', '\'') + "\"");
                    continue;
                }
                if (daten == null)
                {
                    Schreiben(zeile, PROBE + " fall=" + Wort(name) + " ergebnis=DATEI_FEHLT");
                    continue;
                }
                Importmessergebnis e = Messen(daten, name, prozess);
                Schreiben(zeile, e.ProbeZeile());
                Schreiben(zeile, e.MessZeile());
                if (e.Ergebnis == Importmessergebnis.OK) gut++;
                if (e.Format == GebaeudeQuelle.FORMAT_IFC || e.Ergebnis != Importmessergebnis.OK)
                    foreach (string z in Geschuetzt(() => Diagnose(daten, name), "diagnose")) Schreiben(zeile, z);
            }

            TimeSpan rahmen = budget ?? TimeSpan.FromMinutes(BUDGET_MINUTEN);
            foreach (Importmessfall fall in faelle ?? SynthetischeFaelle())
            {
                gesamt++;
                if (uhr.Elapsed > rahmen)
                {
                    Schreiben(zeile, PROBE + " fall=" + Wort(fall.Name) + " ergebnis=UEBERSPRUNGEN grund=zeitrahmen_"
                                     + Ganz((long)rahmen.TotalMinutes) + "min");
                    continue;
                }
                byte[] daten;
                int raeume;
                Stopwatch erzeugung = Stopwatch.StartNew();
                try
                {
                    (daten, raeume) = fall.Erzeugen();
                }
                catch (Exception ex)
                {
                    Schreiben(zeile, PROBE + " fall=" + Wort(fall.Name) + " ergebnis=ERZEUGUNG_FEHLER ausnahme=\"" + Einzeilig(Ausnahmetext(ex)).Replace('"', '\'') + "\"");
                    continue;
                }
                Schreiben(zeile, PROBE + " fall=" + Wort(fall.Name) + " erzeugt bytes=" + Ganz(daten.LongLength)
                                 + " raeume=" + Ganz(raeume) + " dauer_ms=" + Ganz(erzeugung.ElapsedMilliseconds));
                Importmessergebnis e;
                using (var fallzeit = new CancellationTokenSource(TimeSpan.FromMinutes(FALLZEIT_MINUTEN)))
                    e = Messen(daten, fall.Name, prozess, raeume, fallzeit.Token);
                Schreiben(zeile, e.ProbeZeile());
                Schreiben(zeile, e.MessZeile());
                if (e.Ergebnis == Importmessergebnis.OK) gut++;
                // Keine Diagnose nach der Zeitgrenze: Sie liefe denselben Weg noch einmal, ohne Grenze.
                else if (e.Ergebnis != Importmessergebnis.ZEITGRENZE)
                    foreach (string z in Geschuetzt(() => Diagnose(daten, fall.Name), "diagnose")) Schreiben(zeile, z);
                daten = null;
                Aufraeumen();
            }

            Schreiben(zeile, PROBE + " ende faelle=" + Ganz(gesamt) + " ok=" + Ganz(gut) + " dauer_s="
                             + Ganz((long)Math.Round(uhr.Elapsed.TotalSeconds)));
            return gut;
        }

        private static byte[] Lesen(Func<Stream> oeffnen)
        {
            if (oeffnen == null) return null;
            using (Stream s = oeffnen())
            {
                if (s == null) return null;
                var ziel = new MemoryStream();
                s.CopyTo(ziel);
                return ziel.ToArray();
            }
        }

        private static void Schreiben(Action<string> zeile, string text)
        {
            try { zeile(text); }
            catch (Exception) { }
        }

        private static IEnumerable<string> Geschuetzt(Func<IReadOnlyList<string>> teil, string name)
        {
            try { return teil(); }
            catch (Exception ex) { return new[] { DIAGNOSE + " " + name + " AUSNAHME " + Einzeilig(Ausnahmetext(ex)) }; }
        }

        private static string Laufzeit()
        {
            try { return System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription + "/" + System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier; }
            catch (Exception) { return "nv"; }
        }

        // ==================================================================
        //  Diagnose
        // ==================================================================

        /// <summary>
        /// <b>Die Metadaten der drei Schemata</b>: je Schema die Zahl der EXPRESS-Typen, die
        /// <c>ExpressMetaData</c> über <c>module.GetTypes()</c> findet, gegen <see cref="ErwarteteTypen"/>,
        /// und welche der <see cref="Schluesseltypen"/> fehlen. Eine Zeile je Schema.
        /// </summary>
        public static IReadOnlyList<string> XbimDiagnose()
        {
            var zeilen = new List<string>();
            foreach (XbimSchemaVersion schema in new[] { XbimSchemaVersion.Ifc2X3, XbimSchemaVersion.Ifc4, XbimSchemaVersion.Ifc4x3 })
            {
                string kopf = DIAGNOSE + " xbim schema=" + schema;
                try
                {
                    IEntityFactory fabrik = MemoryModel.GetFactory(schema);
                    ExpressMetaData metadaten = ExpressMetaData.GetMetadata(fabrik);
                    List<ExpressType> typen = metadaten.Types().ToList();
                    int eigenschaften = typen.Sum(t => t.Properties?.Count ?? 0);
                    var fehlen = Schluesseltypen.Where(n => !metadaten.TryGetExpressType(n, out ExpressType _)).ToList();
                    int erwartet = ErwarteteTypen.TryGetValue(schema, out int w) ? w : 0;
                    bool ok = fehlen.Count == 0 && (erwartet <= 0 || typen.Count == erwartet);
                    zeilen.Add(kopf + " typen=" + Ganz(typen.Count) + " erwartet=" + Ganz(erwartet)
                               + " eigenschaften=" + Ganz(eigenschaften)
                               + " schluessel_fehlen=" + (fehlen.Count == 0 ? "-" : string.Join(",", fehlen))
                               + " ergebnis=" + (ok ? "OK" : "ABWEICHUNG"));
                }
                catch (Exception ex)
                {
                    zeilen.Add(kopf + " ergebnis=AUSNAHME ausnahme=\"" + Einzeilig(Ausnahmetext(ex)).Replace('"', '\'') + "\"");
                }
            }
            return zeilen;
        }

        /// <summary>
        /// <b>Der IFC-Weg Schritt für Schritt</b> — Behälter, Schema, Fabrik, Metadaten, Laden, Abbild —
        /// ohne die fangenden Blöcke von Ablauf und Leser, damit eine Ausnahme mit Typ und Stapel sichtbar
        /// wird. Für gbXML nur das Laden des Dokuments. Eine Zeile je Schritt; nach einer Ausnahme endet
        /// die Folge.
        /// </summary>
        public static IReadOnlyList<string> Diagnose(byte[] daten, string dateiname)
        {
            var zeilen = new List<string>();
            string kopf = DIAGNOSE + " fall=" + Wort(GebaeudeQuelle.NurName(dateiname ?? ""));
            string schritt = "profil";
            try
            {
                GebaeudeImportProfil profil = GebaeudeImportProfil.FuerDatei(dateiname ?? "");
                if (profil == null || daten == null)
                {
                    zeilen.Add(kopf + " schritt=profil ergebnis=FORMAT_UNBEKANNT");
                    return zeilen;
                }

                if (profil.Format != GebaeudeQuelle.FORMAT_IFC)
                {
                    schritt = "xml";
                    var dokument = GbxmlLeser.Laden(new MemoryStream(daten, false));
                    int raeume = dokument.Descendants().Count(x => x.Name.LocalName == "Space");
                    zeilen.Add(kopf + " schritt=xml ergebnis=OK elemente=" + Ganz(dokument.Descendants().Count()) + " raeume=" + Ganz(raeume));
                    return zeilen;
                }

                schritt = "behaelter";
                byte[] inhalt = daten;
                if (IfcLeser.IstZip(daten))
                {
                    var behaelter = new IfcGebaeudeAbbild();
                    inhalt = IfcLeser.Auspacken(daten, new IfcImportProfil(0), behaelter, CancellationToken.None);
                    zeilen.Add(kopf + " schritt=behaelter ergebnis=" + (inhalt == null ? "FEHLER " + Einzeilig(string.Join(" | ", behaelter.Meldungen)) : "OK entpackt=" + Ganz(inhalt.LongLength)));
                    if (inhalt == null) return zeilen;
                }
                if (IfcLeser.Inhaltsart(inhalt) != IfcLeser.INHALT_STEP)
                {
                    zeilen.Add(kopf + " schritt=inhalt ergebnis=NUR_STEP_GEPRUEFT art=" + Wort(IfcLeser.Inhaltsart(inhalt)));
                    return zeilen;
                }

                schritt = "schema";
                XbimSchemaVersion fassung = MemoryModel.GetStepFileXbimSchemaVersion(new MemoryStream(inhalt, false));
                schritt = "fabrik";
                IEntityFactory fabrik = MemoryModel.GetFactory(fassung);
                schritt = "metadaten";
                ExpressMetaData metadaten = ExpressMetaData.GetMetadata(fabrik);
                zeilen.Add(kopf + " schritt=metadaten ergebnis=OK schema=" + fassung + " fabrik=" + Wort(fabrik.GetType().FullName)
                           + " typen=" + Ganz(metadaten.Types().Count()));

                schritt = "laden";
                var protokoll = new IfcProtokoll();
                using (var modell = new MemoryModel(fabrik, (Microsoft.Extensions.Logging.ILoggerFactory)protokoll, 0))
                {
                    modell.LoadStep21(new MemoryStream(inhalt, false), inhalt.LongLength, null, null);
                    int raeume = modell.Instances.OfType<Xbim.Ifc4.Interfaces.IIfcSpace>().Count();
                    int waende = modell.Instances.OfType<Xbim.Ifc4.Interfaces.IIfcWall>().Count();
                    zeilen.Add(kopf + " schritt=laden ergebnis=OK instanzen=" + Ganz(modell.Instances.Count) + " raeume=" + Ganz(raeume)
                               + " waende=" + Ganz(waende) + " nicht_angelegt=" + Ganz(protokoll.NichtAngelegt)
                               + " verweise_leer=" + Ganz(protokoll.VerweiseInsLeere)
                               + (protokoll.Beispiele.Count > 0 ? " beispiel=\"" + Einzeilig(protokoll.Beispiele[0]).Replace('"', '\'') + "\"" : ""));

                    schritt = "abbild";
                    var abbild = new IfcGebaeudeAbbild();
                    new IfcAbbildBauer(modell, abbild, null, CancellationToken.None, 0.0).Bauen();
                    zeilen.Add(kopf + " schritt=abbild ergebnis=OK gebaeude=" + Ganz(abbild.Gebaeude.Count)
                               + " raeume=" + Ganz(abbild.Gebaeude.Sum(g => g.Raeume.Count))
                               + " bauteile=" + Ganz(abbild.Gebaeude.Sum(g => g.Bauteile.Count))
                               + " meldungen=" + Ganz(abbild.Meldungen.Count));
                }
            }
            catch (Exception ex)
            {
                zeilen.Add(kopf + " schritt=" + schritt + " ergebnis=AUSNAHME ausnahme=\"" + Einzeilig(Ausnahmetext(ex)).Replace('"', '\'') + "\"");
            }
            return zeilen;
        }

        // ==================================================================
        //  Text
        // ==================================================================

        /// <summary>
        /// Typ und Nachricht einer Ausnahme samt innerer Ausnahmen und den ersten
        /// <paramref name="stapelzeilen"/> Stapelzeilen — mehrzeilig; für eine Protokollzeile
        /// <see cref="Einzeilig"/>.
        /// </summary>
        public static string Ausnahmetext(Exception ex, int stapelzeilen = STAPELZEILEN)
        {
            if (ex == null) return "";
            var sb = new StringBuilder();
            int tiefe = 0;
            for (Exception e = ex; e != null && tiefe < 4; e = e.InnerException, tiefe++)
            {
                if (tiefe > 0) sb.Append('\n').Append("innen: ");
                sb.Append(e.GetType().FullName).Append(": ").Append(e.Message);
                string[] stapel = (e.StackTrace ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string s in stapel.Take(Math.Max(0, stapelzeilen))) sb.Append('\n').Append(s.Trim());
            }
            return sb.ToString();
        }

        /// <summary>Zeilenumbrüche werden zu <c> | </c>.</summary>
        public static string Einzeilig(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string[] teile = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" | ", teile.Select(t => t.Trim()));
        }

        /// <summary>Ein Wert als ein Wort: Leerraum wird zu <c>_</c>, leer zu <c>nv</c>.</summary>
        public static string Wort(string wert)
        {
            if (string.IsNullOrWhiteSpace(wert)) return "nv";
            var sb = new StringBuilder(wert.Length);
            foreach (char c in wert.Trim()) sb.Append(char.IsWhiteSpace(c) ? '_' : c);
            return sb.ToString();
        }

        internal static string Ganz(long w) => w.ToString(CultureInfo.InvariantCulture);

        internal static string Zahl(double w, string format) => w.ToString(format, CultureInfo.InvariantCulture);

        internal static string Mb(long bytes) => (bytes / (double)MB).ToString("0.0", CultureInfo.InvariantCulture);
    }
}
