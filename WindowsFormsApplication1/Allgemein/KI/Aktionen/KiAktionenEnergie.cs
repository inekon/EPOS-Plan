using System.Collections.Generic;
using System.Globalization;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Leseaktionen der Energieträger-Einheiten (Konzept
    /// <c>Konzept_Kosten_Energietraeger_EPOS-Plan.md</c> § 4.4 und § 9 Punkt 4,
    /// Nachtrag zu Etappe K3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum eine eigene Datei.</b> Die Aktionen sind nach Sachgebiet abgelegt
    /// (Projekt, Wirtschaftlichkeit, Uebernahme, Lastgang, Sitzung, Schreiben). Die
    /// Einheiten-Konsistenz ist keines davon: Sie gehoert dem Katalog der
    /// Energietraeger, nicht der Rechnung. Dieselbe Gliederung wie
    /// <see cref="KiAktionenLastgang"/>, das aus demselben Grund eigenstaendig ist.
    /// </para>
    /// <para>
    /// <b>Schutzstufe Lesen, ohne Ausnahme.</b> Die Aktion ruft ausschliesslich
    /// <c>EnergieEinheitenPruefung</c>, und die schreibt nirgends - sie liest und
    /// meldet. Es gibt keinen Parameter, ueber den sich daran etwas aendern liesse.
    /// </para>
    /// </remarks>
    internal static class KiAktionenEnergie
    {
        // =====================================================================
        // energietraeger_pruefen
        // =====================================================================

        /// <summary>
        /// Einheiten-Konsistenz der Energietraeger. Andockpunkt
        /// <c>EnergieEinheitenPruefung.PruefeKatalog()</c> bzw.
        /// <c>PruefeProjekt(int)</c>.
        ///
        /// <para><b>Das Projekt ist OPTIONAL</b> - und der Unterschied ist fachlich,
        /// nicht bequem: Ohne Projekt prueft die Aktion den KATALOG (alle aktiven
        /// Traeger mit ihren Katalogwerten), mit Projekt nur die dort verwendeten
        /// Traeger und mit deren Projektueberschreibungen. Dieselbe Bauform wie bei den
        /// Ganglinien (<c>KiHilfe.ProjektIdOptional</c>), wo ohne Projekt der
        /// Stammkatalog gilt.</para>
        /// </summary>
        internal static KiAktion EnergietraegerPruefen()
        {
            return new KiAktion(
                name: "energietraeger_pruefen",
                zweck: KiAktionsTexte.ZweckEnergietraegerPruefen,
                stufe: Schutzstufe.Lesen,
                andockpunkt: "EnergieEinheitenPruefung.PruefeKatalog / PruefeProjekt",
                parameter: new[]
                {
                    KiHilfe.ProjektParameter(KiAktionsTexte.ErlProjektFuerEinheiten,
                                             pflicht: false)
                },
                ausfuehren: a =>
                {
                    int id = KiHilfe.ProjektIdOptional(a);

                    List<EinheitenBefund> befunde = id > 0
                        ? EnergieEinheitenPruefung.PruefeProjekt(id)
                        : EnergieEinheitenPruefung.PruefeKatalog();

                    var zeilen = KiHilfe.Liste();
                    foreach (EinheitenBefund b in befunde)
                        zeilen.Add(KiHilfe.Zeile(
                            "carrier_id", b.CarrierId,
                            "traeger", KiHilfe.Text(b.TraegerName),
                            "problem_code", KiHilfe.Text(b.Code),
                            "klartext", KiHilfe.Text(b.Klartext)));

                    string wo = id > 0 ? KiHilfe.ProjektName(id) : KiAktionsTexte.EinheitenKatalog;

                    if (zeilen.Count == 0)
                        return KiErgebnis.Ok(string.Format(CultureInfo.CurrentCulture,
                                                           KiAktionsTexte.EinheitenOhneBefund, wo));

                    return KiErgebnis.Ok(string.Format(CultureInfo.CurrentCulture,
                                                       KiAktionsTexte.EinheitenBefunde,
                                                       zeilen.Count, wo),
                                         zeilen);
                });
        }
    }
}
