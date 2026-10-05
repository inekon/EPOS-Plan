using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Raumabgleich Projektdatei ↔ IFC-Abbild</b> (Datenaustauschkonzept 16.3): zuerst über den <b>Raumnamen je
    /// Geschoss</b> (Geschossname aus <c>BmFloor</c> gegen <c>IfcBuildingStorey.Name</c>; Groß- und Kleinschreibung und
    /// Leerraum am Rand zählen nicht, ein Name, der im Geschoss doppelt vorkommt, trifft nicht), zweitens über
    /// <c>BmRoom.BIMUUID</c> ↔ <c>IfcSpace.GlobalId</c> (die GUID umkodiert in die 22-stellige IFC-Form,
    /// <see cref="IfcKennung"/>). Trifft kein Weg, bleibt der Raum unzugeordnet und wird benannt
    /// (<c>IMP_SQ_PROT_RAUM_OHNE_TREFFER</c>, bis fünf Namen). Jeder IFC-Raum trifft höchstens einmal.
    /// </summary>
    internal sealed class SqprojRaumabgleich
    {
        private const string ZEICHEN = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_$";

        private readonly Dictionary<string, string> _ifcJeRaum = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Die Paarungen Raumkennung der Projektdatei → Raumkennung des IFC-Abbilds, in Raumreihenfolge der Projektdatei.</summary>
        internal IReadOnlyDictionary<string, string> Paarungen => _ifcJeRaum;

        /// <summary>Über den Namen je Geschoss abgeglichen.</summary>
        internal int UeberName { get; private set; }

        /// <summary>Über <c>BIMUUID</c> ↔ <c>GlobalId</c> abgeglichen.</summary>
        internal int UeberKennung { get; private set; }

        /// <summary>Abgeglichene Räume zusammen.</summary>
        internal int Abgeglichen => UeberName + UeberKennung;

        /// <summary>Räume der Projektdatei ohne Treffer, in Raumreihenfolge.</summary>
        internal List<SqprojRaum> OhneTreffer { get; } = new List<SqprojRaum>();

        /// <summary>IFC-Räume ohne Gegenstück, in Dateireihenfolge.</summary>
        internal List<AbbildRaum> IfcOhneGegenstueck { get; } = new List<AbbildRaum>();

        /// <summary>Die Meldungen des Abgleichs.</summary>
        internal List<PruefMeldung> Meldungen { get; } = new List<PruefMeldung>();

        /// <summary>Die IFC-Raumkennung eines Raums der Projektdatei; <c>null</c> = ohne Treffer.</summary>
        internal string IfcRaum(string raum) => raum != null && _ifcJeRaum.TryGetValue(raum, out string k) ? k : null;

        /// <summary><b>Gleicht die Räume ab</b> — deterministisch: die Räume der Projektdatei in ihrer festen Reihenfolge.</summary>
        internal static SqprojRaumabgleich Bilden(SqprojAbbild projekt, AbbildGebaeude ifc)
        {
            var a = new SqprojRaumabgleich();
            if (projekt == null || ifc == null) return a;
            var geschossName = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (AbbildGeschoss g in ifc.Geschosse) geschossName.TryAdd(g.Kennung ?? "", g.Name);
            string Geschoss(AbbildRaum r) => r.GeschossName
                ?? (r.GeschossKennung != null && geschossName.TryGetValue(r.GeschossKennung, out string n) ? n : null);

            // Name je Geschoss: nur eindeutige Schlüssel auf beiden Seiten treffen.
            var ifcJeSchluessel = ifc.Raeume.GroupBy(r => Schluessel(Geschoss(r), r.Name), StringComparer.Ordinal)
                                            .Where(g => g.Count() == 1).ToDictionary(g => g.Key, g => g.Single(), StringComparer.Ordinal);
            var doppelt = new HashSet<string>(projekt.Raeume.GroupBy(r => Schluessel(r.GeschossName, r.Name), StringComparer.Ordinal)
                                                     .Where(g => g.Count() > 1).Select(g => g.Key), StringComparer.Ordinal);
            var vergeben = new HashSet<string>(StringComparer.Ordinal);
            foreach (SqprojRaum r in projekt.Raeume)
            {
                string s = Schluessel(r.GeschossName, r.Name);
                if (s == null || doppelt.Contains(s) || !ifcJeSchluessel.TryGetValue(s, out AbbildRaum treffer) || !vergeben.Add(treffer.Kennung)) continue;
                a._ifcJeRaum[r.Uuid] = treffer.Kennung;
                a.UeberName++;
            }
            // BIMUUID ↔ GlobalId für die übrigen.
            var ifcJeKennung = new Dictionary<string, AbbildRaum>(StringComparer.Ordinal);
            foreach (AbbildRaum r in ifc.Raeume) ifcJeKennung.TryAdd(r.Kennung ?? "", r);
            foreach (SqprojRaum r in projekt.Raeume)
            {
                if (a._ifcJeRaum.ContainsKey(r.Uuid)) continue;
                string k = IfcKennung(r.Bimuuid);
                if (k != null && ifcJeKennung.TryGetValue(k, out AbbildRaum treffer) && vergeben.Add(treffer.Kennung))
                {
                    a._ifcJeRaum[r.Uuid] = treffer.Kennung;
                    a.UeberKennung++;
                }
                else a.OhneTreffer.Add(r);
            }
            a.IfcOhneGegenstueck.AddRange(ifc.Raeume.Where(r => !vergeben.Contains(r.Kennung)));
            if (a.OhneTreffer.Count > 0)
                a.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, SqprojProtokoll.RAUM_OHNE_TREFFER, SqprojProtokoll.Z(a.OhneTreffer.Count),
                    SqprojProtokoll.Namen(a.OhneTreffer.Select(r => r.Name))));
            if (a.IfcOhneGegenstueck.Count > 0)
                a.Meldungen.Add(new PruefMeldung(PruefStufe.Info, SqprojProtokoll.IFC_RAUM_OHNE_GEGENSTUECK, SqprojProtokoll.Z(a.IfcOhneGegenstueck.Count),
                    SqprojProtokoll.Namen(a.IfcOhneGegenstueck.Select(Zonenplan.Raumname))));
            return a;
        }

        /// <summary>Der Abgleichschlüssel Geschoss/Raum; <c>null</c> ohne Raumnamen.</summary>
        internal static string Schluessel(string geschoss, string raum)
        {
            string r = (raum ?? "").Trim();
            if (r.Length == 0) return null;
            return (geschoss ?? "").Trim().ToUpperInvariant() + "\u001F" + r.ToUpperInvariant();
        }

        /// <summary>
        /// <b>Die 22-stellige IFC-Form einer GUID</b> (<c>IfcGloballyUniqueId</c>): die 32 Hexziffern in Schreibreihenfolge
        /// (mit oder ohne Klammern und Bindestriche) als 128-Bit-Zahl, das erste Byte in zwei Zeichen, dann je drei Bytes in
        /// vier Zeichen des IFC-Alphabets <c>0-9A-Za-z_$</c>. <c>null</c>, wenn der Text keine GUID ist.
        /// </summary>
        internal static string IfcKennung(string guid)
        {
            if (string.IsNullOrWhiteSpace(guid)) return null;
            string hex = new string(guid.Where(Uri.IsHexDigit).ToArray());
            string rest = new string(guid.Where(ch => !Uri.IsHexDigit(ch) && ch != '{' && ch != '}' && ch != '-' && !char.IsWhiteSpace(ch)).ToArray());
            if (hex.Length != 32 || rest.Length != 0) return null;
            var b = new byte[16];
            for (int i = 0; i < 16; i++) b[i] = Convert.ToByte(hex.Substring(2 * i, 2), 16);
            var s = new StringBuilder(22);
            Anhaengen(s, b[0], 2);
            for (int i = 1; i < 16; i += 3) Anhaengen(s, (b[i] << 16) | (b[i + 1] << 8) | b[i + 2], 4);
            return s.ToString();
        }

        private static void Anhaengen(StringBuilder s, int wert, int stellen)
        {
            var c = new char[stellen];
            for (int i = stellen - 1; i >= 0; i--)
            {
                c[i] = ZEICHEN[wert % 64];
                wert /= 64;
            }
            s.Append(c);
        }
    }
}
