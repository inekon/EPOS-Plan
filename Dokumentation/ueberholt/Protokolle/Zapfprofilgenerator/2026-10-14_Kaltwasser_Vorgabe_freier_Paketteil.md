# Zapfprofilgenerator: Kaltwasser-Vorgabe im freien Paketteil (14.10.2026)

## Anlass

Anwendermeldung vom 14.10.2026 aus dem Zapfprofil-Dialog: „Zone trägt 0: Parameter fehlt: Kaltwasser.Bilanz.Mittel steht nicht in der Katalogversion FREI-1“. Die mitgelieferten Nutzungsarten rechneten in der Stufe „Einfach“ nur mit einem externen Katalogpaket.

## Entscheid

Anwenderentscheid 14.10.2026: übliche Vorgabewerte in den freien Paketteil, damit „Einfach“ rechnet.

## Umsetzung

Commits `c2bd90d8d`, `c35dc8d3f`, `a8d5e536e`, `1b45e1087`. Neu in `Referenzlaeufe/Katalogpaket_frei/` (Katalogversion FREI-1, Herkunftsart EIGENKONSTRUKTION):

| Schlüssel | Wert | Quelle |
|---|---|---|
| `Kaltwasser.Bilanz.Mittel` | 10 °C | INEKON-Vorlage TWW-Auslegung V4, Eingaben Z. 23 |
| `Kaltwasser.Bilanz.Amplitude` | 3 K | INEKON-Setzung |
| `Kaltwasser.Bilanz.MonatMaximum` | 8 | INEKON-Setzung |
| `Zirkulation.Laufzeit` | 24 h | V4 |
| `Zirkulation.Anteil` | 0,2 | V4 |
| `Zirkulation.VerlustJeMeter` | 10 W/m | V4 |

`TwwPaketteilCtrl.ParameterNachtragen` trägt die sechs Schlüssel beim ersten `ZapfprofilCtrl.Verfuegbar()` in bestehende Datenbanken nach; eingefügt wird nur, was fehlt, kein Schemaschritt. Das Laufprotokoll meldet `PAKETTEIL_PARAMETER_NACHGETRAGEN`.

## Nachweise

Neue Wache `EPOS.Kern.Tests/TwwPaketteilParameterNachtragTests` (5 Fälle). Testdatenbank byte-gleich. Referenzlauf 1045 und 1066–1071 PASS gegen `2026-10-14_R55_KaeltebedarfDezentral`. Gate: siehe Statuszeile #1096. Das eine rote Ergebnis `FormularrasterTests.Im_Raster_steht_die_Beschriftung_neben_dem_Feld` kam von origin (`0671de376`: die Regel `.epos-bericht-karte .epos-formularraster .epos-feld` steht vor der Grundregel und enthält deren Selektor); behoben mit `67a1ec97e`, der Test sucht den Regelblock nur am Zeilenanfang.

## Offene Punkte

- (a) `A100.Kaltwasser.Auslegung` fehlt im freien Teil; die Speicherauslegung im Dialog bricht ohne ihn ab. Vorschlag: V4 10 °C nach Freigabe.
- (b) `DIN4708.w_b` (Normzahl) fehlt. Vorschlag: prüfen, ob ein Ecodesign-Profil des freien Teils als Vorgabe-Bedarfstag der Stufe „Einfach“ dienen kann.
- Flächenbezogene Zonen und die Zirkulation über den Flächenkennwert bleiben benannt vom externen Paket abhängig (Normzahlen).
