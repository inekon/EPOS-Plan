using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>Was das erneute Lesen einer Importdatei ergeben hat (HottCAD-Verbund 6.5, HC-4).</summary>
    public enum NeulesenZustand
    {
        /// <summary>Die Datei stimmt mit der gespeicherten Quelle überein; Abbild und Grundriss stehen.</summary>
        Passend,

        /// <summary>Der SHA-256 der gewählten Datei weicht vom gespeicherten ab — keine Ansicht.</summary>
        HashAbweichend,

        /// <summary>Die Datei ließ sich nicht öffnen, oder der Leser fand kein Gebäude.</summary>
        NichtLesbar,

        /// <summary>Die Endung trägt kein Format, oder ein anderes als das der Quelle.</summary>
        FormatUnbekannt,

        /// <summary>Die Datei liegt über der Größengrenze des Formats auf dieser Plattform.</summary>
        ZuGross,

        /// <summary>Das Gebäude hat keine Importquelle — es gibt nichts erneut zu lesen.</summary>
        KeineQuelle
    }

    /// <summary>
    /// Das Ergebnis von <see cref="GebaeudeNeulesen.Lesen"/>: der benannte Zustand, beide Hashes, und nur bei
    /// <see cref="NeulesenZustand.Passend"/> das Abbild samt Gebäude, Zonierung und Grundriss. Geschrieben wurde nichts.
    /// </summary>
    internal sealed class NeulesenErgebnis
    {
        internal NeulesenErgebnis(NeulesenZustand zustand, string dateiname)
        {
            Zustand = zustand;
            Dateiname = dateiname ?? "";
        }

        /// <summary>Der benannte Zustand.</summary>
        public NeulesenZustand Zustand { get; }

        /// <summary>Der Name der gewählten Datei (ohne Pfad).</summary>
        public string Dateiname { get; }

        /// <summary>SHA-256 der gewählten Datei (klein, hexadezimal); leer, wenn nicht gelesen.</summary>
        public string HashDatei { get; internal set; } = "";

        /// <summary>SHA-256 der gespeicherten Quelle.</summary>
        public string HashQuelle { get; internal set; } = "";

        /// <summary>Größe der gewählten Datei [Byte]; 0 = unbekannt.</summary>
        public long Groesse { get; internal set; }

        /// <summary>Größengrenze des Formats auf dieser Plattform [Byte]; 0 = keine.</summary>
        public long Grenze { get; internal set; }

        /// <summary>Was Ablauf und Leser gemeldet haben (bei <see cref="NeulesenZustand.NichtLesbar"/> der Grund).</summary>
        public IReadOnlyList<PruefMeldung> Meldungen { get; internal set; } = Array.Empty<PruefMeldung>();

        /// <summary>Das Abbild der Datei; nur bei <see cref="NeulesenZustand.Passend"/>.</summary>
        public GebaeudeAbbild Abbild { get; internal set; }

        /// <summary>Die Stelle des Gebäudes im Abbild; -1 ohne.</summary>
        public int Gebaeudeindex { get; internal set; } = -1;

        /// <summary>Das Gebäude des Abbilds samt Flächenklassifikation; <c>null</c> ohne.</summary>
        public AbbildGebaeude Gebaeude => Abbild != null && Gebaeudeindex >= 0 && Gebaeudeindex < Abbild.Gebaeude.Count
            ? Abbild.Gebaeude[Gebaeudeindex] : null;

        /// <summary>Die wirksame Zonenregel: die gespeicherte, wenn das Gebäude sie trägt, sonst die Vorgabe der Datei.</summary>
        public string Zonenregel { get; internal set; }

        /// <summary>Die Zonierung nach <see cref="Zonenregel"/> — ohne Zuordnungen von Hand und ohne Projektdatei.</summary>
        public GebaeudeZonierung Zonierung { get; internal set; }

        /// <summary>Das Zonengeometrie-Modell für die Ansicht.</summary>
        public Zonengeometrie Geometrie { get; internal set; }

        /// <summary>Eine HottCAD-IFC — die Zonen eines Imports mit Projektdatei sind hier allein die der Geometriedatei.</summary>
        public bool Hottcad { get; internal set; }

        /// <summary>
        /// HC-5 (F7): die frisch abgeleiteten Grundrisse je Raum — dieselben, die der Import speichert; nur bei
        /// <see cref="NeulesenZustand.Passend"/>, sonst leer. Geschrieben werden sie allein über den Knopf „Grundriss übernehmen“.
        /// </summary>
        internal IReadOnlyList<Raumgrundriss> Raumgrundrisse { get; set; } = Array.Empty<Raumgrundriss>();
    }

    /// <summary>
    /// <b>„Datei erneut lesen“</b> (HottCAD-Verbund 4.3 letzter Punkt und 6.5, Entscheid E87 F3): Die Geometrie eines
    /// importierten Gebäudes wird nicht gespeichert; für die Ansicht liest der Kern die Datei der gespeicherten Quelle
    /// (<c>Tab_Importquelle</c>: Name, Format, SHA-256) erneut. Der Weg prüft Format und Größe gegen die Grenze der
    /// Plattform, dann den SHA-256 gegen die Quelle — erst bei Übereinstimmung geht der Inhalt durch denselben Lese- und
    /// Klassifikationsweg wie beim Import (<see cref="GebaeudeImportAblauf.Lesen"/>: Leser, Raumkörper,
    /// Körpernachbarschaft, Flächenklassifikation). Danach Zonierung nach der gespeicherten Regel und Grundriss.
    ///
    /// <para><b>Geschrieben wird nichts</b> — keine Zuordnung, keine Herkunft, kein Zonenplan; der Weg fragt die
    /// Datenbank nicht. Die Projektdatei einer HottCAD-IFC wird nicht gelesen; die Zonen stammen dann allein aus der
    /// Geometriedatei (<see cref="NeulesenErgebnis.Hottcad"/>).</para>
    /// </summary>
    internal static class GebaeudeNeulesen
    {
        /// <summary>Liest <paramref name="inhalt"/> gegen <paramref name="quelle"/>.</summary>
        /// <param name="inhalt">Der Dateiinhalt; wird gelesen, nicht geschlossen. <c>null</c> = nicht lesbar.</param>
        /// <param name="dateiname">Name (oder Pfad) der gewählten Datei — die Endung bestimmt das Profil.</param>
        /// <param name="quelle">Die gespeicherte Importquelle; <c>null</c> = <see cref="NeulesenZustand.KeineQuelle"/>.</param>
        /// <param name="gebaeudekennung">Die Quellkennung des Gebäudes (Paarung Gebäude ↔ <c>Building/@id</c> bzw.
        /// <c>IfcBuilding.GlobalId</c>); leer = das erste Gebäude der Datei.</param>
        /// <param name="ios">Plattform für die Größengrenze.</param>
        /// <param name="abbruch">Abbruchzeichen.</param>
        internal static NeulesenErgebnis Lesen(Stream inhalt, string dateiname, ImportquelleModel quelle, string gebaeudekennung,
                                               bool ios, CancellationToken abbruch = default)
        {
            string name = GebaeudeQuelle.NurName(dateiname);
            if (quelle == null) return new NeulesenErgebnis(NeulesenZustand.KeineQuelle, name);
            string hashQuelle = (quelle.Hash ?? "").Trim();

            GebaeudeImportProfil profil = GebaeudeImportProfil.FuerDatei(dateiname);
            if (profil == null || !string.Equals(profil.Format, quelle.Format, StringComparison.Ordinal))
                return new NeulesenErgebnis(NeulesenZustand.FormatUnbekannt, name) { HashQuelle = hashQuelle };
            profil.MaxBytes = profil.GrenzeFuerPlattform(ios);
            long grenze = profil.MaxBytes > 0 ? profil.MaxBytes : 0;

            if (inhalt == null)
                return new NeulesenErgebnis(NeulesenZustand.NichtLesbar, name) { HashQuelle = hashQuelle, Grenze = grenze };

            if (profil is SqprojImportProfil)
                return ProjektdateiLesen(inhalt, name, profil, quelle, gebaeudekennung, hashQuelle, grenze, abbruch);

            byte[] puffer;
            try
            {
                if (inhalt.CanSeek && !GebaeudeImportAblauf.GroesseZulaessig(inhalt.Length - inhalt.Position, profil))
                    return new NeulesenErgebnis(NeulesenZustand.ZuGross, name)
                    {
                        HashQuelle = hashQuelle, Groesse = inhalt.Length - inhalt.Position, Grenze = grenze
                    };
                puffer = Einlesen(inhalt, grenze, abbruch);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is NotSupportedException)
            {
                return new NeulesenErgebnis(NeulesenZustand.NichtLesbar, name)
                {
                    HashQuelle = hashQuelle, Grenze = grenze,
                    Meldungen = new[] { new PruefMeldung(PruefStufe.Fehler, profil.Meldung("LESEFEHLER"), ex.Message) }
                };
            }
            if (puffer == null)
                return new NeulesenErgebnis(NeulesenZustand.ZuGross, name) { HashQuelle = hashQuelle, Groesse = grenze + 1, Grenze = grenze };

            string hash = Convert.ToHexStringLower(SHA256.HashData(puffer));
            var ergebnis = new NeulesenErgebnis(NeulesenZustand.Passend, name)
            {
                HashDatei = hash, HashQuelle = hashQuelle, Groesse = puffer.LongLength, Grenze = grenze
            };
            if (!string.Equals(hash, hashQuelle, StringComparison.OrdinalIgnoreCase))
                return Mit(ergebnis, NeulesenZustand.HashAbweichend);

            // Derselbe Lese- und Klassifikationsweg wie beim Import; die Größe prüft der Ablauf ein zweites Mal.
            var ablauf = new GebaeudeImportAblauf();
            int zahl;
            using (var strom = new MemoryStream(puffer, false))
                zahl = ablauf.Lesen(strom, name, profil, null, abbruch);
            return Auswerten(ablauf, zahl, ergebnis, quelle, gebaeudekennung, abbruch);
        }

        /// <summary>
        /// <b>Die Projektdatei neu lesen</b> — ohne Puffer: Der Ablauf reicht den Strom an den Leser durch und hasht unterwegs
        /// (eine Projektdatei kann groß sein); weicht der Hash von dem der Quelle ab, gilt <see cref="NeulesenZustand.HashAbweichend"/>.
        /// </summary>
        private static NeulesenErgebnis ProjektdateiLesen(Stream inhalt, string name, GebaeudeImportProfil profil, ImportquelleModel quelle,
                                                          string gebaeudekennung, string hashQuelle, long grenze, CancellationToken abbruch)
        {
            if (inhalt.CanSeek && !GebaeudeImportAblauf.GroesseZulaessig(inhalt.Length - inhalt.Position, profil))
                return new NeulesenErgebnis(NeulesenZustand.ZuGross, name)
                {
                    HashQuelle = hashQuelle, Groesse = inhalt.Length - inhalt.Position, Grenze = grenze
                };
            var ablauf = new GebaeudeImportAblauf();
            int zahl;
            try
            {
                zahl = ablauf.Lesen(inhalt, name, profil, null, abbruch);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is NotSupportedException)
            {
                return new NeulesenErgebnis(NeulesenZustand.NichtLesbar, name)
                {
                    HashQuelle = hashQuelle, Grenze = grenze,
                    Meldungen = new[] { new PruefMeldung(PruefStufe.Fehler, profil.Meldung("LESEFEHLER"), ex.Message) }
                };
            }
            (string hash, long bytes) = ablauf.GeleseneDatei;
            if (ablauf.Meldungen.Any(m => m.Schluessel == profil.Meldung("ZU_GROSS")))
                return new NeulesenErgebnis(NeulesenZustand.ZuGross, name) { HashQuelle = hashQuelle, Groesse = Math.Max(bytes, grenze + 1), Grenze = grenze };
            var ergebnis = new NeulesenErgebnis(NeulesenZustand.Passend, name)
            {
                HashDatei = hash, HashQuelle = hashQuelle, Groesse = bytes, Grenze = grenze
            };
            if (!string.Equals(hash, hashQuelle, StringComparison.OrdinalIgnoreCase))
                return Mit(ergebnis, NeulesenZustand.HashAbweichend);
            return Auswerten(ablauf, zahl, ergebnis, quelle, gebaeudekennung, abbruch);
        }

        /// <summary>Der gemeinsame Rest nach dem Lesen: Nordwinkel, Gebäude, Zonierung, Geometrie.</summary>
        private static NeulesenErgebnis Auswerten(GebaeudeImportAblauf ablauf, int zahl, NeulesenErgebnis ergebnis, ImportquelleModel quelle,
                                                  string gebaeudekennung, CancellationToken abbruch)
        {
            // G5-N (N5/N6): Der gespeicherte Nordwinkel ersetzt den Dateiwert nur, wenn ihn der Anwender eingegeben hat (beim
            // Import oder über „Ausrichtung ändern“) — weicht der frisch gelesene ab, wird mit ihm noch einmal gelesen. Stammt er
            // aus der Datei, gilt der frisch gelesene Dateiwert; war er eine Annahme, gilt der Dateiwert, falls die Datei jetzt
            // einen nennt, sonst wieder die Annahme.
            if (zahl > 0 && ablauf.Abbild != null && GespeicherterWinkelGilt(quelle) is double gespeichert
                && !(Nordrichtung.Normiert(ablauf.Abbild.NordwinkelWirksamGrad) is double frisch && Math.Abs(frisch - gespeichert) <= 1e-9))
                zahl = ablauf.NordwinkelVorgeben(gespeichert, null, abbruch);
            if (zahl <= 0 || ablauf.Abbild == null)
            {
                NeulesenErgebnis f = Mit(ergebnis, NeulesenZustand.NichtLesbar);
                f.Meldungen = ablauf.Meldungen.Where(m => m.Stufe == PruefStufe.Fehler).ToList();
                return f;
            }

            GebaeudeAbbild abbild = ablauf.Abbild;
            int index = Gebaeudeindex(abbild, gebaeudekennung);
            (IReadOnlyList<string> regeln, string vorgabe, bool _) = GebaeudeZonierung.Waehlbar(abbild, index);
            string regel = quelle.Zonenregel != null && regeln.Contains(quelle.Zonenregel) ? quelle.Zonenregel : vorgabe;
            GebaeudeZonierung zonierung = GebaeudeZonierung.Bilden(abbild, index, regel);

            ergebnis.Meldungen = ablauf.Meldungen.ToList();
            ergebnis.Abbild = abbild;
            ergebnis.Gebaeudeindex = index;
            ergebnis.Zonenregel = regel;
            ergebnis.Zonierung = zonierung;
            ergebnis.Geometrie = GebaeudeGrundriss.BildenMitGrundriss(abbild, index, zonierung, out IReadOnlyList<Raumgrundriss> grundrisse);
            ergebnis.Raumgrundrisse = grundrisse;
            ergebnis.Hottcad = GebaeudeImportAblauf.IstHottcad(abbild, index);
            return ergebnis;
        }

        /// <summary>
        /// G5-N (N6): <b>Der gespeicherte Nordwinkel, der beim Neulesen den Dateiwert ersetzt</b> — nur einer mit der Herkunft
        /// <see cref="Nordwinkelherkunft.Eingabe"/>; <c>null</c> = der frisch gelesene Dateiwert bzw. die Annahme gilt.
        /// </summary>
        internal static double? GespeicherterWinkelGilt(ImportquelleModel quelle)
            => quelle != null && quelle.NordwinkelHerkunft == Nordwinkelherkunft.Eingabe ? Nordrichtung.Normiert(quelle.NordwinkelGrad) : null;

        /// <summary>
        /// Das Gebäude der Paarung: gleiche (gekürzte) Kennung, sonst das erste der Datei — eine Datei mit
        /// demselben SHA-256 trägt dieselben Kennungen; der Rückfall greift nur ohne gespeicherte Paarung.
        /// </summary>
        internal static int Gebaeudeindex(GebaeudeAbbild abbild, string kennung)
        {
            if (abbild == null || abbild.Gebaeude.Count == 0) return -1;
            if (!string.IsNullOrWhiteSpace(kennung))
                for (int i = 0; i < abbild.Gebaeude.Count; i++)
                    if (string.Equals(Quellkennung.Kuerzen(abbild.Gebaeude[i].Kennung ?? ""), kennung, StringComparison.Ordinal))
                        return i;
            return 0;
        }

        private static NeulesenErgebnis Mit(NeulesenErgebnis e, NeulesenZustand zustand)
            => new NeulesenErgebnis(zustand, e.Dateiname)
            {
                HashDatei = e.HashDatei, HashQuelle = e.HashQuelle, Groesse = e.Groesse, Grenze = e.Grenze, Meldungen = e.Meldungen
            };

        /// <summary>Der Strom in einen Puffer, höchstens <paramref name="grenze"/> + 1 Byte; <c>null</c> = zu groß.</summary>
        private static byte[] Einlesen(Stream quelle, long grenze, CancellationToken abbruch)
        {
            long max = grenze > 0 ? grenze : long.MaxValue;
            var ziel = new MemoryStream();
            var block = new byte[81920];
            int n;
            while ((n = quelle.Read(block, 0, block.Length)) > 0)
            {
                abbruch.ThrowIfCancellationRequested();
                ziel.Write(block, 0, n);
                if (ziel.Length > max) return null;
            }
            return ziel.ToArray();
        }
    }
}
