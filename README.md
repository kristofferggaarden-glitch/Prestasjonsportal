# ABBs Prestasjonsportal - Firebase Edition

## 🚀 Rask Start

### Steg 1: Installer NuGet-pakker
1. Åpne prosjektet i Visual Studio
2. Høyreklikk på prosjektet → "Manage NuGet Packages"
3. Installer disse pakkene hvis de ikke allerede er installert:
   - FirebaseDatabase.net (versjon 4.2.0)
   - Newtonsoft.Json (versjon 13.0.3)

### Steg 2: Sett opp Firebase
1. Gå til https://firebase.google.com
2. Opprett et nytt prosjekt
3. Gå til "Realtime Database" og opprett en database
4. Velg "Start in test mode" for enkel testing
5. Kopier database-URL-en din

### Steg 3: Konfigurer applikasjonen
1. Åpne `Services/FirebaseService.cs`
2. Bytt ut URL-en på linje 13 med din Firebase URL:
   ```csharp
   private const string FirebaseDatabaseUrl = "https://ditt-prosjekt.firebasedatabase.app/";
   ```

### Steg 4: Kjør programmet
Trykk F5 i Visual Studio for å bygge og kjøre programmet.

## ✨ Nye Funksjoner

- **Sanntidssynkronisering**: Alle endringer synkroniseres automatisk mellom alle brukere
- **Sky-lagring**: All data lagres sikkert i Firebase
- **Live oppdateringer**: Se endringer fra andre brukere umiddelbart
- **Synk-status indikator**: Viser tilkoblingsstatus i sidemenyen

## 🔒 Sikkerhet

For produksjonsbruk, endre Firebase Security Rules fra "test mode" til sikre regler:

```json
{
  "rules": {
    ".read": "auth != null",
    ".write": "auth != null"
  }
}
```

## 📝 Notater

- Første gang du kjører programmet vil det legge til eksempeldata automatisk
- Sørg for at du har internettforbindelse for at synkroniseringen skal fungere
- Alle brukere som har samme Firebase URL vil se samme data

## 🆘 Feilsøking

**Problem**: Får ikke kontakt med Firebase
- Sjekk at Firebase URL er riktig
- Sjekk internettforbindelse
- Sjekk at Firebase-prosjektet er aktivt

**Problem**: Data vises ikke
- Sjekk Firebase Rules (bruk test mode for testing)
- Sjekk at NuGet-pakkene er installert

## 📧 Support

For spørsmål, kontakt Kristoffer Gaarden
