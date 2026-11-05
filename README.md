# Edge Profile Starter, gemaakt met AI

Een kleine Windows desktop-app om Microsoft Edge met een bestaand profiel te openen. Geen extra packages nodig; de app gebruikt .NET Framework 4.x van Windows en de geïnstalleerde Edge-browser.

![Edge Profile Starter in donkere modus](docs/screenshot.png)

Het venster heeft een eigen titelbalk met minimaliseren en sluiten. Versleep het via de bovenste strook. Maximaliseren is uitgeschakeld. Het eigen app-icoon is ingebouwd in de exe; de bronbestanden staan in `assets`. Met `generate-icon.ps1` kun je het icoon opnieuw genereren.

De launcher opent een nieuw Edge-venster en maximaliseert dit via Windows. De launcher blijft open, zodat je gedurende de dag opnieuw een profiel kunt kiezen. Er kan per Windows-gebruiker en sessie één launcher actief zijn, ook als je de exe vanuit verschillende mappen opent. Opnieuw starten brengt het bestaande venster naar voren en herstelt het als het geminimaliseerd is.

Via **Donkere modus** / **Lichte modus** linksboven wissel je het thema. De keuze wordt direct bewaard in `config.json` naast de exe (`"darkMode": true` of `false`) en bij de volgende start hersteld. Deze map moet beschrijfbaar zijn. Bouwen overschrijft bestaande instellingen en profielen niet.

## Bouwen en starten

Voer vanuit deze map uit:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

Dubbelklik daarna op `dist\EdgeProfileStarter.exe`. Je kunt de hele map `dist` naar een eigen locatie kopiëren. Houd `config.json` naast de exe. Maak eventueel via de Verkenner een snelkoppeling naar de exe op je bureaublad.

## Namen instellen

Open `dist\config.json` in een teksteditor, pas de namen aan en sla het bestand op. Start de app opnieuw om je wijzigingen te laden:

```json
{
  "windowTitle": "Mijn Edge-profielen",
  "darkMode": false,
  "profiles": [
    { "directory": "Default", "name": "Persoonlijk" },
    { "directory": "Profile 1", "name": "Werk" },
    { "directory": "Profile 3", "name": "Projecten" }
  ]
}
```

`name` is de naam van de knop. `directory` is de bestaande mapnaam onder `%LOCALAPPDATA%\Microsoft\Edge\User Data`. Voeg regels toe of verwijder ze om de knoppen te bepalen. De app verandert de profielnamen in Edge zelf niet. Ontbrekende profielen worden aangegeven en kunnen niet worden gestart; maak ze eerst aan in Edge.

Bij opnieuw bouwen blijft de configuratie in `dist` behouden. Het bestand in de hoofdmap is alleen het voorbeeld voor een eerste build. De launcher opent altijd een nieuw, gemaximaliseerd Edge-venster voor het gekozen profiel via `--new-window --start-maximized` en een expliciete Windows-maximaliseeropdracht. Bestaande Edge-vensters worden niet aangepast. Als binnen 15 seconden geen nieuw gemaximaliseerd venster gevonden wordt, blijft de launcher open met een foutmelding.

## Code-indeling

- `src/Models`: de configuratie en profielgegevens.
- `src/Services/ConfigurationStore.cs`: laden, valideren en atomisch opslaan van `config.json`.
- `src/Services/EdgeLauncher.cs`: Edge starten en wachten op een gemaximaliseerd venster.
- `src/Services/SingleInstanceApplication.cs`: één actief appvenster en activeren bij opnieuw starten.
- `src/Windows`: Windows-vensters detecteren en maximaliseren.
- `src/UI`: het launchervenster, thema en de getekende controls.

Alle instellingen en profielen staan uitsluitend in `config.json`; er is geen conversiecode voor oudere configuratiebestanden.

## Controleren

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\test.ps1
```

De controles testen configuratievalidatie, behoud van profielen en extra velden bij themawijzigingen, de levenscyclus van het venster, selectie van het nieuwe browservenster en het annuleren bij sluiten. De browseraansturing wordt met nagebootste start- en vensterfuncties getest; er wordt geen echte browser geopend.

De screenshot opnieuw maken, zonder de themakeuze in je configuratie te wijzigen:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\capture-screenshot.ps1
```
