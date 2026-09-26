# gbXML-Schemakopie — lokal beizustellen, nie versionieren

Dieser Ordner nimmt die **Schemakopie des gbXML-Formats** auf, gegen die der Test den
gbXML-Export von EPOS-Plan prüft (Probe 3 im
[Datenaustauschkonzept](../../Dokumentation/aktuell/Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md),
Kapitel 9). Das Schema trägt keine Lizenz. Es gehört deshalb weder ins Repositorium noch in die
Auslieferung und wird nie aus dem Netz geladen (Entscheide D3 und D17, beide mit E27 im
[Register](../../Dokumentation/aktuell/Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md)).
`.gitignore` schließt alles unter `Referenzlaeufe/Schemakopien/` aus — bis auf diese Datei.

**Hier liegt kein Schema im Repositorium, und hier kommt auch keines hinein.**

## Herkunft

| Datei | Herkunft | Abgerufen | Größe | Lizenzstand |
|---|---|---|---|---|
| `GreenBuildingXML_Ver8.01.xsd` | Repositorium `GreenBuildingXML/gbXML_Schemas` auf GitHub | 26.09.2026 | 387 450 Byte | keine |

Das Attribut `version` der Wurzel kennt in dieser Fassung als höchsten Wert `6.01`; der Export
schreibt deshalb `version="6.01"` (Datenaustauschkonzept 5.2).

## Einrichten

Die Datei unverändert und unter ihrem Namen hierher legen:
`Referenzlaeufe/Schemakopien/GreenBuildingXML_Ver8.01.xsd`. Die Tests suchen den Ordner vom
Laufordner aus aufwärts. Fehlt die Datei, meldet die Schemaprüfung den Verzicht in der
Testausgabe und kehrt ohne Befund zurück — so läuft die CI, die die Datei nicht hat. Der
Nachweis gegen das Schema wird lokal geführt und im Protokoll der Stufe festgehalten.
