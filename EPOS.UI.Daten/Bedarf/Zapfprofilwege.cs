using System;
using System.Collections.Generic;
using EPOS.UI.Dialoge.Bedarf;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die NAHT des Zapfprofilgenerators (Umsetzungskonzept Zapfprofilgenerator 5.2, 5.5; Stufe
    /// Z1, Gruppe 3): was die SCHALE beisteuert, damit der Dialog „Brauchwasser-Zapfprofil" aus
    /// ihrem Bedarfsprofil-Dialog heraus aufgeht — den Behälter des Arbeitsstands bis zum OK
    /// ihres Bedarfsprofil-Dialogs. Bauform wie <see cref="SimulationPlattformwege"/>.
    ///
    /// <para><b>Warum es sie gibt.</b> Der Bedarfsprofil-Dialog hat seine Datenhälfte unter
    /// Windows in der Schale (<c>BedarfsProfileHuelle</c>); eine plattformfreie Fassung kommt
    /// erst mit iU11 (A11). Bis dahin hängt die Windows-Hülle ihren
    /// <see cref="ZapfprofilBehaelter"/> über <see cref="ZapfprofilBehaelter.Wege"/> hier ein;
    /// iOS reicht <see cref="Ohne"/> mit einem Grund — oder nichts.</para>
    ///
    /// <para><b>Kein Delegat ist kein Knopf — aber benannt.</b> Ohne <see cref="Uebernehmen"/>
    /// liefert <see cref="ZapfprofilHuelle.Einstieg"/> keinen Parametersatz, sondern den Grund
    /// (<see cref="Sperrgrund"/>, sonst <c>ZPG_MSG_PLATTFORM</c>); die Oberfläche zeigt ihn statt
    /// den Weg still zu übergehen.</para>
    /// </summary>
    internal sealed class Zapfprofilwege
    {
        /// <summary>
        /// Der Arbeitsstand, den die Schale bis zum OK ihres Bedarfsprofil-Dialogs hält;
        /// <c>null</c> als Delegat oder als Ergebnis = der gespeicherte Stand des Projekts gilt.
        /// </summary>
        internal Func<ZapfprofilStand> Arbeitsstand;

        /// <summary>
        /// Nimmt den übernommenen Arbeitsstand in den Behälter der Schale (geschrieben wird
        /// erst mit dem OK des Bedarfsprofil-Dialogs). <c>null</c> = diese Schale bietet den
        /// Dialog nicht an; dann gilt <see cref="Sperrgrund"/>.
        /// </summary>
        internal Action<ZapfprofilStand> Uebernehmen;

        /// <summary>Der benannte Grund, wenn die Schale den Dialog nicht anbietet; leer = der Vorgabesatz.</summary>
        internal string Sperrgrund = "";

        /// <summary>Die Naht einer Schale, die den Dialog nicht anbietet.</summary>
        internal static Zapfprofilwege Ohne(string sperrgrund)
            => new Zapfprofilwege { Sperrgrund = sperrgrund ?? "" };
    }

    /// <summary>
    /// Der EINSTIEG in den Dialog, wie ihn der Bedarfsprofil-Dialog braucht (5.2): entweder die
    /// zwei Delegaten für seine Parameter <c>ZapfprofilGaben</c> und
    /// <c>ZapfprofilUebernommen</c> — oder der benannte Grund, warum es den Knopf nicht gibt.
    /// </summary>
    /// <param name="Angeboten">Gibt es den Weg? Nur dann sind die Delegaten gesetzt.</param>
    /// <param name="Grund">Warum nicht; leer, wenn angeboten.</param>
    /// <param name="Gaben">Der Parametersatz des Dialogs, frisch je Öffnen.</param>
    /// <param name="Uebernommen">Nimmt das Ergebnis des Dialogs (OK) in den Behälter.</param>
    internal sealed record ZapfprofilEinstieg(bool Angeboten, string Grund,
                                              Func<IReadOnlyDictionary<string, object>> Gaben,
                                              Action<ZapfprofilErgebnisDaten> Uebernommen)
    {
        /// <summary>Kein Einstieg, mit Grund.</summary>
        internal static ZapfprofilEinstieg Ohne(string grund) => new ZapfprofilEinstieg(false, grund ?? "", null, null);
    }

    /// <summary>Das Ergebnis des Schreibwegs: geschrieben (samt Stand mit den Ids der Datenbank) oder benannt abgelehnt.</summary>
    /// <param name="Erfolg">Ist geschrieben worden (oder gab es nichts zu schreiben)?</param>
    /// <param name="Stand">Der geschriebene Stand mit den Ids der Datenbank; <c>null</c>, wenn nichts zu schreiben war.</param>
    /// <param name="Meldung">Die benannte Ablehnung; <c>null</c> bei Erfolg.</param>
    internal sealed record ZapfprofilSpeicherergebnis(bool Erfolg, ZapfprofilStand Stand, ZapfprofilMeldung Meldung);

    /// <summary>
    /// <b>Der Zapfprofil-Behälter</b> (5.2): Er hält den Arbeitsstand, den der Dialog über
    /// „OK" oder die Optionsgruppe „Rechenweg Brauchwasser" setzt, bis der Bedarfsprofil-Dialog
    /// selbst OK sagt — dann schreibt die Schale im selben <see cref="DbVorgang"/> wie ihre
    /// Profilzuordnungen (<see cref="Schreiben"/>). Plattformfrei; die Windows-Hülle des
    /// Bedarfsprofil-Dialogs hält je Öffnen einen.
    ///
    /// <para><b>Unverändert heißt: nichts schreiben.</b> Solange weder OK noch die Optionsgruppe
    /// ihn berührt haben, ist <see cref="Arbeitsstand"/> <c>null</c>, und <see cref="Schreiben"/>
    /// fasst die Tww-Tabellen nicht an.</para>
    /// </summary>
    internal sealed class ZapfprofilBehaelter
    {
        internal ZapfprofilBehaelter(int idProjekt) { IdProjekt = idProjekt; }

        /// <summary>Das Projekt des Behälters.</summary>
        internal int IdProjekt { get; }

        /// <summary>Der geänderte Arbeitsstand; <c>null</c> = unverändert (der gespeicherte Stand gilt).</summary>
        internal ZapfprofilStand Arbeitsstand { get; private set; }

        /// <summary>Hat der Anwender etwas geändert, das beim OK zu schreiben ist?</summary>
        internal bool Geaendert => Arbeitsstand != null;

        /// <summary>Der Weg, der mit dem OK gilt: der des Arbeitsstands, sonst der gespeicherte.</summary>
        internal ZapfprofilWeg Weg
            => ZapfprofilHuelle.AlsWeg(Arbeitsstand?.Weg ?? ZapfprofilCtrl.Weg(IdProjekt));

        /// <summary>Nimmt den Stand aus dem OK des Zapfprofils.</summary>
        internal void Uebernehmen(ZapfprofilStand stand)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            Arbeitsstand = stand;
        }

        /// <summary>
        /// Die Optionsgruppe „Rechenweg Brauchwasser" (ZU4): stellt nur den Weg um und behält
        /// die Zonen — auch beim Zurückschalten auf die Bestandsprofile.
        /// </summary>
        internal void WegSetzen(ZapfprofilWeg weg)
        {
            ZapfprofilStand basis = Arbeitsstand ?? ZapfprofilCtrl.Lies(IdProjekt);
            Arbeitsstand = basis with { Weg = ZapfprofilHuelle.AlsWeg(weg) };
        }

        /// <summary>
        /// Schreibt den Arbeitsstand im übergebenen Vorgang (kein Commit). Unverändert: nichts.
        /// Eine benannte Ablehnung kommt als Meldung zurück; der Aufrufer rollt seinen Vorgang
        /// zurück und lässt seinen Dialog offen.
        /// </summary>
        internal ZapfprofilSpeicherergebnis Schreiben(DbVorgang v)
        {
            if (!Geaendert) return new ZapfprofilSpeicherergebnis(true, null, null);
            // Zonen, Projektgrößen samt Auslegung und ein konstruierter Bedarfstag (Z2) in DEMSELBEN Vorgang.
            ZapfprofilSpeicherergebnis e = ZapfprofilHuelle.AuslegungSpeichern(IdProjekt, Arbeitsstand, v);
            return e;
        }

        /// <summary>Nach einem Commit des Aufrufers: Der Behälter gilt wieder als unverändert.</summary>
        internal void Geschrieben() => Arbeitsstand = null;

        /// <summary>Die Naht zu diesem Behälter.</summary>
        internal Zapfprofilwege Wege() => new Zapfprofilwege
        {
            Arbeitsstand = () => Arbeitsstand,
            Uebernehmen = Uebernehmen
        };
    }
}
