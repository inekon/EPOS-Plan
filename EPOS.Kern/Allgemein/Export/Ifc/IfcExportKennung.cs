#if !OHNE_XBIM
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Xbim.Ifc4.UtilityResource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Kennungen des IFC-Exports</b> (Stufe G7c; Entscheid D5, Datenaustauschkonzept 6.4) —
    /// <c>GlobalId</c> je <c>IfcRoot</c> als namensbasierte UUID nach RFC 4122 Version 5 (SHA-1) aus einem
    /// festen EPOS-Namensraum und einem Schlüsselpfad mit Rollenglied, umgewandelt in die 22 Zeichen der
    /// IFC-Base64-Form.
    ///
    /// <para><b>Der Schlüsselpfad</b> ist die gbXML-Kennung des Objekts aus dem Abbild
    /// (<see cref="GebaeudeExportKennung"/>: aus den IDs der Projektkopie, nie aus Namen) und das Rollenglied
    /// hinter <c>#</c>: <c>epos-bauteil-101#Objekt</c>, <c>epos-bauteil-101#Pset:EPOS_Bauteil</c>,
    /// <c>epos-raum-11#Qto:Qto_SpaceBaseQuantities</c>, <c>epos-bauteil-101#SpaceBoundary:2a:epos-raum-11:0</c>.
    /// Ein zweiter Export desselben Stands trägt dieselben Kennungen; ein dupliziertes Projekt trägt andere,
    /// weil seine Zeilen andere IDs haben. <b>Nachträglich nicht zu ändern</b>, ohne alte Exporte zu
    /// entwerten — der Namensraum und die Form der Pfade sind mit Prüfwerten eingefroren
    /// (<c>IfcSchreiberTests</c>).</para>
    /// </summary>
    internal sealed class IfcExportKennung
    {
        /// <summary>Der feste EPOS-Namensraum der Kennungen (UUID, nie ändern).</summary>
        internal static readonly Guid NAMENSRAUM = new Guid("3c9d2e71-5a4b-4f08-9b6e-e705c1a8d2f4");

        private readonly HashSet<string> _vergeben = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// Die <c>GlobalId</c> zu einem Schlüssel und einer Rolle. Ein Pfad wird je Lauf nur einmal
        /// vergeben; ein zweites Mal ist ein Programmfehler und wirft.
        /// </summary>
        internal IfcGloballyUniqueId Vergeben(string schluessel, string rolle)
        {
            if (string.IsNullOrWhiteSpace(schluessel)) throw new InvalidOperationException("IFC-Export: Eine Kennung braucht einen Schlüssel.");
            if (string.IsNullOrWhiteSpace(rolle)) throw new InvalidOperationException("IFC-Export: Eine Kennung braucht ein Rollenglied.");
            string pfad = schluessel + "#" + rolle;
            if (!_vergeben.Add(pfad)) throw new InvalidOperationException("IFC-Export: Die Kennung „" + pfad + "\" ist doppelt vergeben.");
            return new IfcGloballyUniqueId(IfcGloballyUniqueId.ConvertToBase64(NamensUuid(NAMENSRAUM, pfad)));
        }

        /// <summary>Die vergebenen Schlüsselpfade (für die Proben).</summary>
        internal IReadOnlyCollection<string> Pfade => _vergeben;

        /// <summary>
        /// Die namensbasierte UUID nach RFC 4122 Version 5: SHA-1 über die 16 Bytes des Namensraums in
        /// Netzreihenfolge und den Namen in UTF-8, die ersten 16 Bytes, Version 5 und Variante RFC 4122.
        /// Die BCL bringt dafür keine Fabrik mit.
        /// </summary>
        internal static Guid NamensUuid(Guid namensraum, string name)
        {
            byte[] ns = NetzReihenfolge(namensraum.ToByteArray());
            byte[] text = Encoding.UTF8.GetBytes(name ?? "");
            var eingabe = new byte[ns.Length + text.Length];
            Buffer.BlockCopy(ns, 0, eingabe, 0, ns.Length);
            Buffer.BlockCopy(text, 0, eingabe, ns.Length, text.Length);
            byte[] hash = SHA1.HashData(eingabe);
            var uuid = new byte[16];
            Array.Copy(hash, uuid, 16);
            uuid[6] = (byte)((uuid[6] & 0x0F) | 0x50);
            uuid[8] = (byte)((uuid[8] & 0x3F) | 0x80);
            return new Guid(NetzReihenfolge(uuid));
        }

        /// <summary>
        /// Wechselt zwischen der Bytefolge von <see cref="Guid.ToByteArray"/> (die ersten drei Felder
        /// little-endian) und der Netzreihenfolge der RFC 4122 — in beide Richtungen dieselbe Vertauschung.
        /// </summary>
        private static byte[] NetzReihenfolge(byte[] b)
        {
            var r = (byte[])b.Clone();
            Array.Reverse(r, 0, 4);
            Array.Reverse(r, 4, 2);
            Array.Reverse(r, 6, 2);
            return r;
        }
    }
}
#endif
