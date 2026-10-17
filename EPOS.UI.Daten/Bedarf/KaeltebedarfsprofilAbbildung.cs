using System;
using System.Collections.Generic;
using EPOS.UI.Dialoge.Bedarf;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Abbildung Zeile ↔ Modell der Kältebedarfsprofile (Konzept Kältebedarf 3.3, 3.4; K5b) — plattformfrei, damit
    /// der Rückweg der Windows-Hülle (<c>BedarfsProfileHuelle</c>) und der Speicherweg der Startseite ohne Windows
    /// prüfbar sind. Der Dialog führt Name, Kopie, Summe, Temperaturpaar, Kalender, Kälte-Art und die acht
    /// Deckungsfelder (<see cref="DeckungsartDaten"/>); alles Übrige der Zuordnung schreibt der Rückweg aus dem beim
    /// Öffnen geladenen Modell fort.
    /// </summary>
    internal static class KaeltebedarfsprofilAbbildung
    {
        /// <summary>Die Dialogzeilen aus der Modellliste des Aufrufers.</summary>
        internal static List<BedarfsProfilZeile> Zeilen(IEnumerable<Z_ProjektKaeltebedarfModel> modelle)
        {
            var zeilen = new List<BedarfsProfilZeile>();
            if (modelle == null) return zeilen;
            foreach (Z_ProjektKaeltebedarfModel m in modelle)
                zeilen.Add(new BedarfsProfilZeile
                {
                    IdZ = m.ID_Z, IdStamm = m.ID_Kaeltebedarf, Name = m.Bezeichner ?? "", Summe = m.Summe,
                    Vorlauf = m.Vorlauf, Ruecklauf = m.Ruecklauf, TemperaturGeaendert = m.TemperaturGeaendert,
                    KaelteArt = KaelteArtSchema.Normal(m.KaelteArt),
                    KalenderId = m.ID_Betriebskalender,
                    Deckung = DeckungsartDaten.Aus(m),
                    // KBP-3a: Eingabeart der Zuordnung, Monatsanteile und Wochenprofil der Projektkopie.
                    Eingabeart = KaeltebedarfsprofilVorlagenSchema.Eingabeart(m.Eingabeart),
                    Monatsanteile = m.Monatsanteile ?? KaeltebedarfStammCtrl.ProjektMonate(m.ID_Kaeltebedarf),
                    Wochenprofil = m.Wochenprofil ?? KaeltebedarfStammCtrl.ProjektWoche(m.ID_Kaeltebedarf),
                    ProfilGeaendert = m.Monatsanteile != null || m.Wochenprofil != null,
                    // KBP-4b: Typtage und Matrix der Projektkopie (bearbeitet oder gespeichert).
                    Typtage = m.Typtage ?? KaeltetyptagCtrl.Lies(m.ID_Kaeltebedarf),
                    TyptageGeaendert = m.Typtage != null,
                    // KBP-5b: die Normtage der Vorlage für die Herleitungszeile am Kalender.
                    Normtage = m.NutzungstageNorm,
                    // KBP-5c: der tatsächliche Name des Normkalenders für die Herleitungszeile (ohne zu schreiben).
                    Normkalender = m.NutzungstageNorm.HasValue ? NormkalenderCtrl.Bezeichner(m.ID_Projekt, m.ID_Kaeltebedarf) : null
                });
            return zeilen;
        }

        /// <summary>
        /// KBP-3a: eine neu übernommene Kältezeile — Eingabeart 'jahr' (Vorgabe), der Art-Vorschlag der Vorlage
        /// (<see cref="KaeltebedarfStammCtrl.ArtVorschlag"/>; ohne Vorschlag bleibt die Vorgabe Raum, der Anwender hat hier
        /// noch nichts gewählt) und Monatsanteile und Wochenprofil der Kopie, auf die die Zeile zeigt.
        /// </summary>
        /// <remarks>KBP-5b (Entscheid 16.10.2026): Trägt die Vorlage Nutzungstage nach Norm, wählt die Zeile im Projekt
        /// (<paramref name="idProjekt"/> &gt; 0, Projektkopie angelegt) den Normkalender vor
        /// (<see cref="NormkalenderCtrl.Vorwahl"/>: vorhanden oder neu); im Assistenten bleibt die Wahl leer.</remarks>
        internal static void Aufgenommen(BedarfsProfilZeile zeile, string vorlage, int idProjekt = 0)
        {
            if (zeile == null) return;
            zeile.Normtage = idProjekt > 0 ? KaeltebedarfStammCtrl.ProjektNutzungstage(zeile.IdStamm)
                                           : KaeltebedarfStammCtrl.Nutzungstage(vorlage);
            if (idProjekt > 0 && zeile.Normtage.HasValue)
            {
                int? kalender = NormkalenderCtrl.Vorwahl(idProjekt, zeile.IdStamm, out string normname);
                if (kalender.HasValue) { zeile.KalenderId = kalender; zeile.Normkalender = normname; }
            }
            zeile.Eingabeart = KaeltebedarfsprofilVorlagenSchema.EINGABE_JAHR;
            string vorschlag = KaeltebedarfStammCtrl.ArtVorschlag(vorlage);
            if (vorschlag != null) zeile.KaelteArt = KaelteArtSchema.Normal(vorschlag);
            zeile.Monatsanteile = KaeltebedarfStammCtrl.ProjektMonate(zeile.IdStamm);
            zeile.Wochenprofil = KaeltebedarfStammCtrl.ProjektWoche(zeile.IdStamm);
            // KBP-4b: die Typtage der Kopie (eine neue Kopie führt keine).
            zeile.Typtage = KaeltetyptagCtrl.Lies(zeile.IdStamm);
            zeile.TyptageGeaendert = false;
        }

        /// <summary>
        /// Der Änderungsrückruf des Dialogs: schreibt die Dialogzeilen in die Modellliste des Aufrufers zurück — die
        /// Felder des Dialogs aus der Zeile, alles Übrige aus dem beim Öffnen geladenen Modell derselben Zuordnung
        /// (über <c>ID_Z</c>). Eine Zeile ohne Deckung (neu übernommen) steht auf „zentral“.
        /// </summary>
        internal static Action Rueckweg(List<BedarfsProfilZeile> zeilen, List<Z_ProjektKaeltebedarfModel> modelle,
                                        int projektId)
        {
            var vorher = new Dictionary<int, Z_ProjektKaeltebedarfModel>();
            foreach (Z_ProjektKaeltebedarfModel m in modelle) vorher[m.ID_Z] = m;
            return () =>
            {
                modelle.Clear();
                foreach (BedarfsProfilZeile z in zeilen)
                {
                    Z_ProjektKaeltebedarfModel m = z.IdZ != 0 && vorher.TryGetValue(z.IdZ, out var alt) ? alt
                        : new Z_ProjektKaeltebedarfModel { ID_Z = z.IdZ, ID_Projekt = projektId };
                    m.ID_Kaeltebedarf = z.IdStamm; m.Bezeichner = z.Name; m.Summe = z.Summe;
                    m.Vorlauf = z.Vorlauf; m.Ruecklauf = z.Ruecklauf; m.TemperaturGeaendert = z.TemperaturGeaendert;
                    m.KaelteArt = KaelteArtSchema.Normal(z.KaelteArt);
                    m.ID_Betriebskalender = z.KalenderId;
                    // KBP-3a: Eingabeart immer, Monate und Woche nur, wenn der Anwender sie übernommen hat.
                    m.Eingabeart = KaeltebedarfsprofilVorlagenSchema.Eingabeart(z.Eingabeart);
                    m.Monatsanteile = z.ProfilGeaendert ? z.Monatsanteile : null;
                    m.Wochenprofil = z.ProfilGeaendert ? z.Wochenprofil : null;
                    // KBP-4b: Typtage nur, wenn der Anwender sie übernommen hat.
                    m.Typtage = z.TyptageGeaendert ? z.Typtage : null;
                    (z.Deckung ??= new DeckungsartDaten()).InModell(m);
                    modelle.Add(m);
                }
            };
        }

        /// <summary>
        /// Der Speicherweg der Startseite: prüft zuerst jede Zeile mit der Regel des Kerns
        /// (<see cref="Kaeltedeckungsregel.Pruefe"/>) und lehnt „Split ohne EER“ BENANNT ab, ohne etwas zu schreiben;
        /// sonst Löschen + Neuanlegen der Zuordnungen in EINEM Vorgang.
        /// </summary>
        /// <returns><c>null</c> = gespeichert; sonst der Grund (Ablehnung oder Fehlschlag, nichts geändert).</returns>
        internal static string Speichern(int projektId, List<Z_ProjektKaeltebedarfModel> modelle)
        {
            modelle ??= new List<Z_ProjektKaeltebedarfModel>();
            foreach (Z_ProjektKaeltebedarfModel m in modelle)
                if (Kaeltedeckungsregel.Pruefe(m) != null)
                    return MyResource.Resource.DECK_MSG_OHNE_EER + " (" + (m.Bezeichner ?? "") + ")";

            var wiz = new WizardCtrl();
            using (DbVorgang vorgang = DataRepository.Vorgang())
            {
                if (wiz.Del_Projekt_Kaelte(projektId, 0, vorgang) && wiz.Add_Projekt_Kaelte(projektId, modelle, vorgang))
                {
                    vorgang.Commit();
                    return null;
                }
                vorgang.Rollback();
            }
            return MyResource.Resource.DECK_MSG_SPEICHERN_FEHLER;
        }
    }
}
