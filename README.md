# Studio Starter

Een uitbreidbare Nederlandse WPF-desktopapp voor Windows, zonder ingebouwde AI. Gebruik deze app als oefenproject of als basis waarop je jouw eigen AI code laat toevoegen.

## Starten

Open `publish/StudioStarter.Desktop.exe`. Houd de bestanden in de map `publish` bij elkaar. Deze versie vereist de **.NET 10 Desktop Runtime**; die is op de computer waarop de app is gebouwd al aanwezig.

Voor ontwikkeling: open `StudioStarter.slnx` in een IDE met ondersteuning voor .NET 10/WPF, of gebruik de .NET 10 SDK:

```powershell
dotnet restore StudioStarter.slnx --configfile NuGet.Config
dotnet build StudioStarter.slnx -c Release
dotnet run --project src/StudioStarter.Desktop
```

## Wat werkt

- Dashboard met aantallen en voortgang die direct meeveranderen.
- Taken toevoegen, afvinken, zoeken en verwijderen.
- Vrije notities en een persoonlijke begroeting.
- Opslaan via de knop of Ctrl+S; JSON-export via een bestandsdialoog.
- Waarschuwing bij afsluiten met niet-opgeslagen wijzigingen.
- Lokale opslag met vervanging via een tijdelijk bestand; beschadigde gegevens worden niet stilzwijgend overschreven.
- Dagelijkse logbestanden, zonder de inhoud van taken en notities.

Gegevens staan in `%LocalAppData%\StudioStarter\workspace.json`; logs in de submap `logs`. Wijzigingen worden **handmatig** opgeslagen. De eerste start toont drie voorbeeldtaken. De app maakt zelf geen netwerkverzoeken.

## Projectstructuur

| Project | Verantwoordelijkheid |
|---|---|
| `StudioStarter.Core` | Domeinmodellen en het opslagcontract `IWorkspaceStore` |
| `StudioStarter.Infrastructure` | JSON-opslag; plek voor database- en API-implementaties |
| `StudioStarter.Desktop` | WPF-schermen, MVVM, opdrachten, thema en registratie van services |
| `StudioStarter.SmokeTests` | Uitvoerbare controles voor gedrag, opslag en echte WPF-layout |

## Dependencies

| Package | Gebruik |
|---|---|
| CommunityToolkit.Mvvm 8.4.0 | Observable properties en commands zonder repetitieve code |
| Microsoft.Extensions.Hosting 10.0.0 | Centrale configuratie, dependency injection en services |
| Microsoft.Extensions.Http 10.0.0 | Geregistreerde HttpClientFactory voor toekomstige API-koppelingen |
| Serilog.Extensions.Hosting 10.0.0 | Logging via de host en `ILogger` |
| Serilog.Sinks.File 7.0.0 | Dagelijkse logbestanden met bewaartermijn |
| System.Text.Json, onderdeel van .NET | Leesbare lokale gegevens en exports |

MVVM is gebaseerd op [Microsofts MVVM Toolkit](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/). Er zijn bewust concrete uitbreidingspunten: vervang een opslagimplementatie zonder je schermen te herschrijven, of registreer een nieuwe service in `App.xaml.cs`. HttpClientFactory is voorbereid; er is nog geen externe API gekoppeld.

## Verder bouwen

1. **Een extra pagina:** voeg een `UserControl` en ViewModel toe in Desktop, registreer benodigde services in `App.xaml.cs` en voeg een tab toe in `MainWindow.xaml`.
2. **Database:** implementeer `IWorkspaceStore` in Infrastructure met bijvoorbeeld SQLite en vervang de registratie in `App.xaml.cs`.
3. **Eigen API:** voeg een typed HttpClient en interface toe; injecteer die service in het ViewModel.
4. **Grotere app:** splits het huidige MainViewModel per pagina en voeg navigatie toe. De bestaande basis gebruikt vier tabs.

Mogelijke opdrachten om een AI mee te testen:

- Voeg prioriteit en deadline aan taken toe en behoud bestaande opgeslagen gegevens.
- Maak een projectenpagina waarmee taken aan een project gekoppeld kunnen worden.
- Voeg JSON-import toe met validatie en een voorbeeld vóór het overschrijven.
- Voeg een licht thema toe met een instelbare themakeuze.

## Controleren en publiceren

```powershell
dotnet run --project tests/StudioStarter.SmokeTests -c Release
dotnet publish src/StudioStarter.Desktop -c Release -o publish
```

De 11 controles omvatten toevoegen, zoeken, afvinken, verwijderen, Unicode-opslag, foutafhandeling, bescherming van een beschadigd bestand en WPF-layout. Optioneel kun je als argument een map meegeven om de vier schermen naar PNG te renderen.

Voor een versie met meegeleverde runtime:

```powershell
dotnet publish src/StudioStarter.Desktop -c Release -r win-x64 --self-contained true -o publish-standalone
```

Dit is een starterproject: geen installer, updater, gebruikersaccounts, importfunctie of automatisch opslaan. Gebruik één appvenster tegelijk; gelijktijdige schrijfacties vanuit meerdere processen worden nog niet gesynchroniseerd. Notities zijn gewone tekst. JSON-opslag is niet versleuteld.
