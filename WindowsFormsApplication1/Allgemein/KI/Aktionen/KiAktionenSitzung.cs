using System;
using System.Collections.Generic;
using System.Globalization;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Zugriff auf das Sitzungsgedaechtnis (Fachkonzept 5.1, letzte Zeile; 7.3).
    /// </summary>
    internal static class KiAktionenSitzung
    {
        /// <summary>Vorbelegung, wenn der Aufruf keine Anzahl nennt.</summary>
        private const int VORGABE_ANZAHL = 5;

        /// <summary>
        /// Die zuletzt ausgefuehrten Aktionen dieser Sitzung. Andockpunkt
        /// <c>KiAusfuehrer.LetzteAktionen(int)</c>.
        /// </summary>
        /// <remarks>
        /// VORLAEUFIG: Die Quelle ist der schlanke In-Memory-Speicher des Ausfuehrers.
        /// Paket B6 bringt das gemeinsame Sitzungsgedaechtnis des Chats; diese Aktion
        /// wechselt dann die Quelle, nicht ihre Form.
        /// </remarks>
        internal static KiAktion LetzteAktionen()
        {
            return new KiAktion(
                name: "letzte_aktionen",
                zweck: KiAktionsTexte.ZweckLetzteAktionen,
                stufe: Schutzstufe.Lesen,
                andockpunkt: "KiAusfuehrer.LetzteAktionen",
                parameter: new[]
                {
                    new KiParameter("anzahl", KiParameterTyp.Ganzzahl, KiAktionsTexte.ErlAnzahl,
                                    pflicht: false, anzeigename: KiAktionsTexte.AnzahlName,
                                    min: 1, max: 50)
                },
                ausfuehren: a =>
                {
                    int anzahl = a.Id("anzahl", VORGABE_ANZAHL);

                    var zeilen = KiHilfe.Liste();
                    foreach (KiSitzungseintrag e in KiAusfuehrer.LetzteAktionen(anzahl))
                    {
                        zeilen.Add(KiHilfe.Zeile(
                            "zeitpunkt", e.Zeitpunkt.ToString(KiProtokoll.Zeitformat,
                                                              CultureInfo.InvariantCulture),
                            "aktion", KiHilfe.Text(e.Aktion),
                            "stufe", SchutzstufeText.Schluessel(e.Stufe),
                            "parameter", KiHilfe.Text(e.Parameter),
                            "projekt_id", e.ProjektId,
                            "status", SchutzstufeText.Schluessel(e.Status),
                            "ergebnis", KiHilfe.Text(e.Ergebnis),
                            "dauer_ms", e.DauerMs));
                    }

                    if (zeilen.Count == 0) return KiErgebnis.Ok(KiAktionsTexte.LetzteAktionenKeine);

                    return KiErgebnis.Ok(string.Format(CultureInfo.CurrentCulture,
                                                       KiAktionsTexte.LetzteAktionenGefunden, zeilen.Count),
                                         zeilen);
                });
        }
    }
}
