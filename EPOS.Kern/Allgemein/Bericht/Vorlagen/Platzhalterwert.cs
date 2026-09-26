using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der aufgelöste Wert eines Platzhalters</b> — was
    /// <see cref="Vorlagenfeldkatalog.Loese(Vorlagenfeld, Berichtswerte, IReadOnlyList{Formatangabe})"/>
    /// liefert und die Engine in Word oder Excel schreibt.
    ///
    /// <para><see cref="Text"/> ist immer gesetzt und fertig formatiert — nach der Kultur des
    /// Berichts, dem Katalogformat und den Formatangaben; ohne Wert steht dort der Leerwert
    /// des Eintrags (Konzept 4.10: „—“ statt 0). Für Excel liegen Zahl und Datum zusätzlich
    /// als Rohwert vor (BV-P2, BV-P3).</para>
    /// </summary>
    public sealed class Platzhalterwert
    {
        private Platzhalterwert(Vorlagenfeldart art, string text, bool istLeer, string grund,
                                IReadOnlyList<string> zeilen, IReadOnlyList<string> kapitel,
                                double? zahl, DateTime? datum, string ausnahme,
                                bool? schalter = null, Bildinhalt bild = null, Berichtstabelle tabelle = null,
                                Diagrammbild diagramm = null)
        {
            Art = art;
            Text = text ?? "";
            IstLeer = istLeer;
            Grund = grund;
            Zeilen = zeilen ?? Array.Empty<string>();
            Kapitel = kapitel ?? Array.Empty<string>();
            Zahl = zahl;
            Datum = datum;
            Ausnahme = ausnahme;
            Schalter = schalter;
            Bild = bild;
            Tabelle = tabelle;
            Diagramm = diagramm;
        }

        /// <summary>Die Platzhalterklasse des Eintrags.</summary>
        public Vorlagenfeldart Art { get; }

        /// <summary>Der fertige Text; bei einer Liste die Einträge zeilenweise (<c>\n</c>), bei einem
        /// Kapitel leer. Nie <c>null</c>.</summary>
        public string Text { get; }

        /// <summary>Lag kein Wert vor (<see cref="Text"/> ist dann der Leerwert)?</summary>
        public bool IstLeer { get; }

        /// <summary>Warum kein Wert vorliegt, in der Sprache des Berichts; <c>null</c> = kein Grund
        /// bekannt oder ein Wert liegt vor. Die Angabe <c>|mit grund</c> hängt ihn an den Leerwert.</summary>
        public string Grund { get; }

        /// <summary>Bei einer Liste die Einträge; sonst leer.</summary>
        public IReadOnlyList<string> Zeilen { get; }

        /// <summary>Bei einem Kapitel die Namen der Kapitel (<see cref="Berichtskapitel.Name"/>), die an
        /// der Stelle einzusetzen sind, in Berichtsreihenfolge; sonst leer.</summary>
        public IReadOnlyList<string> Kapitel { get; }

        /// <summary>Bei einem Schalter sein Wert (Häkchen gesetzt?); sonst <c>null</c>.</summary>
        public bool? Schalter { get; }

        /// <summary>Bei einem Bild der Inhalt (Bytes, Maße, Inhaltstyp); sonst und ohne Bild <c>null</c>.</summary>
        public Bildinhalt Bild { get; }

        /// <summary>Bei einer Tabelle mit Zeilen die Strukturtabelle (BV-E5); sonst und ohne Zeilen <c>null</c>.</summary>
        public Berichtstabelle Tabelle { get; }
        /// <summary>Bei einem Diagramm (<c>bild.*</c>, <c>stand.bild.*</c>, BV-E5) wie es im Zielmaß entsteht;
        /// sonst <c>null</c>.</summary>
        public Diagrammbild Diagramm { get; }

        /// <summary>Bei einer Zahl der Rohwert (für Excel); sonst <c>null</c>.</summary>
        public double? Zahl { get; }

        /// <summary>Bei einem Datum der Rohwert (für Excel); sonst <c>null</c>.</summary>
        public DateTime? Datum { get; }

        /// <summary>Die Meldung einer Ausnahme beim Auflösen — für die Laufmeldung, nicht für den
        /// Bericht; <c>null</c> = keine. Der Bericht bricht an einem Einzelwert nie ab.</summary>
        public string Ausnahme { get; }

        /// <inheritdoc/>
        public override string ToString() { return Text; }

        internal static Platzhalterwert MitText(Vorlagenfeldart art, string text)
        {
            return new Platzhalterwert(art, text, false, null, null, null, null, null, null);
        }

        internal static Platzhalterwert MitZahl(string text, double zahl)
        {
            return new Platzhalterwert(Vorlagenfeldart.Zahl, text, false, null, null, null, zahl, null, null);
        }

        internal static Platzhalterwert MitDatum(string text, DateTime datum)
        {
            return new Platzhalterwert(Vorlagenfeldart.Datum, text, false, null, null, null, null, datum, null);
        }

        internal static Platzhalterwert MitZeilen(IReadOnlyList<string> zeilen)
        {
            return new Platzhalterwert(Vorlagenfeldart.Liste, string.Join("\n", zeilen), false, null, zeilen, null, null, null, null);
        }

        internal static Platzhalterwert MitKapiteln(IReadOnlyList<string> kapitel)
        {
            return new Platzhalterwert(Vorlagenfeldart.Kapitel, "", false, null, null, kapitel, null, null, null);
        }

        internal static Platzhalterwert MitSchalter(bool wert)
        {
            return new Platzhalterwert(Vorlagenfeldart.Schalter, "", false, null, null, null, null, null, null, wert);
        }

        internal static Platzhalterwert MitBild(Bildinhalt bild)
        {
            return new Platzhalterwert(Vorlagenfeldart.Bild, bild?.Dateiname ?? "", false, null, null, null, null, null, null,
                                       null, bild);
        }

        internal static Platzhalterwert MitTabelle(Berichtstabelle tabelle)
        {
            return new Platzhalterwert(Vorlagenfeldart.Tabelle, "", false, null, null, null, null, null, null,
                                       null, null, tabelle);
        }

        internal static Platzhalterwert MitDiagramm(Diagrammbild diagramm)
        {
            return new Platzhalterwert(Vorlagenfeldart.Bild, "", false, null, null, null, null, null, null,
                                       null, null, null, diagramm);
        }

        internal static Platzhalterwert Leer(Vorlagenfeldart art, string text, string grund, string ausnahme)
        {
            return new Platzhalterwert(art, text, true, grund, null, null, null, null, ausnahme);
        }
    }

    /// <summary>
    /// Ein fehlender Wert samt Grund — was eine <see cref="Vorlagenfeld.Quelle"/> liefert, wenn
    /// sie weiß, warum nichts da ist (Stammprojekt ohne Ergebnis, Lauf fehlgeschlagen …). Der
    /// Grund steht in der Sprache des Berichts.
    /// </summary>
    public sealed class Leergrund
    {
        /// <summary>Legt den Grund an.</summary>
        public Leergrund(string grund) { Grund = grund; }

        /// <summary>Warum kein Wert vorliegt; <c>null</c> = unbekannt.</summary>
        public string Grund { get; }

        /// <inheritdoc/>
        public override string ToString() { return Grund ?? ""; }
    }
}
