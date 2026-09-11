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
        /// <c>KiAusfuehrung.LetzteAktionen(int)</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// VORLAEUFIG: Die Quelle ist der schlanke In-Memory-Speicher des Ausfuehrers.
        /// Paket B6 bringt das gemeinsame Sitzungsgedaechtnis des Chats; diese Aktion
        /// wechselt dann die Quelle, nicht ihre Form.
        /// </para>
        /// <para>
        /// <b>Seit Auftrag #201 nimmt sie den Ausfuehrer entgegen</b>, statt einen
        /// statischen anzusprechen: <see cref="KiAusfuehrung"/> traegt seinen Zustand als
        /// Instanz, damit ein Pruefling einen frischen anlegen kann. Das Register wird
        /// vom Ausfuehrer selbst gebaut - er reicht sich hier durch.
        /// </para>
        /// </remarks>
        /// <param name="ausfuehrung">Der Ausfuehrer, dessen Gedaechtnis gelesen wird.</param>
        internal static KiAktion LetzteAktionen(KiAusfuehrung ausfuehrung)
        {
            return new KiAktion(
                name: "letzte_aktionen",
                zweck: KiAktionsTexte.ZweckLetzteAktionen,
                titel: KiAktionsTexte.TitelLetzteAktionen,
                beispiel: KiAktionsTexte.BeispielLetzteAktionen,
                stufe: Schutzstufe.Lesen,
                andockpunkt: "KiAusfuehrung.LetzteAktionen",
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
                    foreach (KiSitzungseintrag e in ausfuehrung.LetzteAktionen(anzahl))
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
