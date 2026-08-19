using System.Collections.Generic;
using KiKern;

namespace KiKern.Tests
{
    /// <summary>
    /// Ein Register, das jeden Parametertyp genau einmal fuehrt.
    /// </summary>
    /// <remarks>
    /// Die Tests arbeiten bewusst NICHT gegen das echte Register des Anwendungsprojekts:
    /// der Kern soll ohne die Anwendung pruefbar sein (Fachkonzept 3.7), und die Faelle
    /// sollen sich nicht aendern, wenn dem Register eine Aktion zuwaechst. Die Formen der
    /// Deklaration sind aber dieselben wie dort (IDs, Aufzaehlung aus DbWerte, ID-Liste).
    /// </remarks>
    internal static class Beispielregister
    {
        public const string Gewerk1 = "Wärmepumpe";
        public const string Gewerk2 = "BHKW";

        public static KiAktion OhneParameter() => new KiAktion(
            name: "projekte_auflisten",
            zweck: "Listet alle Projekte der Datenbank.",
            stufe: Schutzstufe.Lesen,
            andockpunkt: "ProjektCtrl.ReadAll",
            ausfuehren: _ => KiErgebnis.Ok("2 Projekte", null, 2));

        public static KiAktion MitId() => new KiAktion(
            name: "projekt_lesen",
            zweck: "Liest die Kopfdaten eines Projekts.",
            stufe: Schutzstufe.Lesen,
            andockpunkt: "ProjektCtrl.ReadSingle(int)",
            parameter: new[]
            {
                new KiParameter("projekt_id", KiParameterTyp.Ganzzahl,
                                "Schlüssel des Projekts aus projekte_auflisten.",
                                anzeigename: "Projekt (ID)", min: 1)
            },
            ausfuehren: a => KiErgebnis.Ok("Projekt " + a.Id("projekt_id")));

        public static KiAktion MitAllenTypen() => new KiAktion(
            name: "vielerlei",
            zweck: "Prüffall mit jedem Parametertyp.",
            stufe: Schutzstufe.Rechnen,
            andockpunkt: "Testfall",
            parameter: new[]
            {
                new KiParameter("projekt_id", KiParameterTyp.Ganzzahl,
                                "Schlüssel des Projekts.", anzeigename: "Projekt (ID)", min: 1),
                new KiParameter("schwelle_kw", KiParameterTyp.Zahl,
                                "Zielschwelle.", pflicht: false, anzeigename: "Zielschwelle",
                                min: 0, max: 100000, einheit: "kW"),
                new KiParameter("bezeichner", KiParameterTyp.Text,
                                "Name der Variante.", pflicht: false, anzeigename: "Bezeichner",
                                maxLaenge: 10),
                new KiParameter("speichern", KiParameterTyp.Wahrheitswert,
                                "Ergebnis speichern?", pflicht: false, anzeigename: "Speichern"),
                new KiParameter("gewerk", KiParameterTyp.Aufzaehlung,
                                "Gewerk der Übernahme.", pflicht: false, anzeigename: "Gewerk",
                                werte: new[] { Gewerk1, Gewerk2 }),
                new KiParameter("projekt_ids", KiParameterTyp.GanzzahlListe,
                                "Projekte des Vergleichs.", pflicht: false, anzeigename: "Projekte",
                                min: 1)
            },
            wirkung: "Rechnet und speichert das Ergebnis.",
            vorschau: _ => "Ich würde rechnen.");

        public static KiRegister Erzeuge()
        {
            return new KiRegister()
                .Aufnehmen(OhneParameter())
                .Aufnehmen(MitId())
                .Aufnehmen(MitAllenTypen());
        }

        /// <summary>Kurzschreibweise fuer die Rohwerte eines Aufrufs.</summary>
        public static Dictionary<string, object?> Werte(params object?[] paare)
        {
            var d = new Dictionary<string, object?>();
            for (int i = 0; i + 1 < paare.Length; i += 2)
                d[(string)paare[i]!] = paare[i + 1];
            return d;
        }
    }
}
