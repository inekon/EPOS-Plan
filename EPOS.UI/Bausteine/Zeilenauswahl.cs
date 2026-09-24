using System;
using System.Collections.Generic;
using System.Linq;

namespace EPOS.UI.Bausteine;

/// <summary>
/// <b>Der Zustand der Auswahl einer Verwaltung</b> (Konzept Administrationsdialoge,
/// Stufe 3: V3, V6, V8, V12) — die über das Kästchen gewählten Zeilen, ob das Stammblatt
/// gerade vergleicht und ob es im schmalen Fenster als Blatt über der Liste steht.
///
/// <para><b>Warum eine Klasse und nicht drei Felder je Wirt.</b> Dieselben Regeln gelten
/// in jeder Verwaltung: Unter zwei gewählten Zeilen endet der Vergleich von selbst; eine
/// Handlung wirkt auf die gewählten Zeilen, sonst auf die Fokuszeile; nach einem Löschen
/// fallen die Kästchen der verschwundenen Zeilen. Drei Fassungen davon liefen
/// auseinander (Hausregel „ein Dialog baut kein Hausmuster selbst nach").</para>
///
/// <para><b>Die Liste der gewählten Zeilen ist die Gabe der <c>Katalogliste</c></b>
/// (<c>Gewaehlte</c>): Was sie meldet, speichert <see cref="Setzen"/> als DIESELBE
/// Instanz, und die Liste erkennt sie beim nächsten Zeichnen wieder. Jede Änderung von
/// außen (<see cref="Aufheben"/>, <see cref="Abgleichen"/>) legt eine NEUE Instanz an —
/// daran erkennt die Liste, dass sie nachziehen muss.</para>
/// </summary>
public sealed class Zeilenauswahl
{
    /// <summary>Die Schlüssel der über das Kästchen gewählten Zeilen, in der Reihenfolge des Wählens.</summary>
    public IReadOnlyList<string> Gewaehlte { get; private set; } = Array.Empty<string>();

    /// <summary>Zeigt das Stammblatt den Vergleich der gewählten Zeilen (V12)?</summary>
    public bool Vergleich { get; private set; }

    /// <summary>
    /// Steht das Stammblatt im schmalen Fenster als Blatt über der Liste (V3)? Im breiten
    /// Fenster steht es ohnehin daneben; dort wirkt der Schalter nicht.
    /// </summary>
    public bool BlattOffen { get; set; }

    /// <summary>Wie viele Zeilen über das Kästchen gewählt sind.</summary>
    public int Anzahl => Gewaehlte.Count;

    /// <summary>
    /// <b>Die Fassungsnummer der Übernahmen</b> — jede <see cref="Uebernommen"/> zählt eins
    /// weiter. Der Wirt reicht sie der <c>Katalogliste</c> als <c>Zeigeanlass</c>: Wechselt
    /// sie, rollt die Liste die neue Fokuszeile ins Bild, denselben Weg wie nach einem
    /// Tastenschritt. Nach einem Import steht der neue Satz sonst irgendwo in einer Liste
    /// von Tausenden, gewählt, aber außer Sicht.
    /// </summary>
    public int Uebernahmen { get; private set; }

    /// <summary>Die Liste meldet ihre Kästchen — unter zwei endet der Vergleich.</summary>
    public void Setzen(IReadOnlyList<string>? gewaehlte)
    {
        Gewaehlte = gewaehlte ?? Array.Empty<string>();
        if (Gewaehlte.Count < 2) Vergleich = false;
    }

    /// <summary>„Auswahl aufheben" — alle Kästchen weg, der Vergleich endet.</summary>
    public void Aufheben()
    {
        Gewaehlte = new List<string>();
        Vergleich = false;
    }

    /// <summary>„Vergleichen" schaltet um; ohne zwei gewählte Zeilen bleibt es aus.</summary>
    public bool VergleichUmschalten()
    {
        Vergleich = !Vergleich && Gewaehlte.Count >= 2;
        return Vergleich;
    }

    /// <summary>„‹ Stammblatt von …" — zurück zur Fokuszeile, die Kästchen bleiben.</summary>
    public void VergleichBeenden() => Vergleich = false;

    /// <summary>
    /// <b>Worauf eine Handlung wirkt</b> (Konzept 3.3): Sind Kästchen gesetzt, auf die
    /// gewählten Zeilen, sonst auf die Fokuszeile — ohne Fokuszeile auf nichts.
    /// </summary>
    public IReadOnlyList<string> Ziele(string? fokus)
        => Gewaehlte.Count > 0 ? Gewaehlte
         : string.IsNullOrEmpty(fokus) ? Array.Empty<string>()
         : new[] { fokus };

    /// <summary>
    /// Nach einem Neuaufbau der Liste: Kästchen, deren Zeile es nicht mehr gibt (gelöscht),
    /// fallen. Stehen alle noch, bleibt die Instanz dieselbe.
    /// </summary>
    public void Abgleichen(IEnumerable<string> vorhandene)
    {
        var menge = new HashSet<string>(vorhandene ?? Array.Empty<string>(), StringComparer.Ordinal);
        if (Gewaehlte.All(menge.Contains)) return;
        Setzen(Gewaehlte.Where(menge.Contains).ToList());
    }

    /// <summary>
    /// <b>Nach einer Übernahme</b> („Import…", Konzept Administrationsdialoge V14 und 7.1 d):
    /// Die neu übernommenen Zeilen SIND die Auswahl. Ab zweien stehen ihre Kästchen — die
    /// Auswahlleiste sagt „n gewählt", und Vergleichen oder Löschen wirken auf genau sie;
    /// eine einzelne ist als Fokuszeile allein die Wahl („Zeile ist Wahl", wie nach dem
    /// Einlesen einer Klimaregion oder Zeitreihe). Kästchen von vorher fallen, ein Vergleich
    /// endet. Legt immer eine NEUE Instanz an, damit die Liste nachzieht, und zählt
    /// <see cref="Uebernahmen"/> weiter, damit sie die Fokuszeile ins Bild rollt.
    /// </summary>
    /// <param name="neue">Die Schlüssel der neuen Zeilen, die Fokuszeile zuerst.</param>
    public void Uebernommen(IEnumerable<string> neue)
    {
        var liste = new List<string>();
        var menge = new HashSet<string>(StringComparer.Ordinal);
        foreach (string s in neue ?? Array.Empty<string>())
            if (!string.IsNullOrEmpty(s) && menge.Add(s)) liste.Add(s);

        Gewaehlte = liste.Count >= 2 ? liste : new List<string>();
        Vergleich = false;
        Uebernahmen++;
    }
}
