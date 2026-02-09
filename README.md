# 💰 Portfel aktywów

> Wieloplatformowa aplikacja mobilna do zarządzania portfelem inwestycyjnym.

Zbudowana w **.NET MAUI** (.NET 10) - działa na **Android**, **iOS**, **macOS** i **Windows** z jednego kodu źródłowego.

---

## 📋 Spis treści

- [Funkcje](#-funkcje)
- [Zrzuty ekranu](#-zrzuty-ekranu)
- [Architektura](#-architektura)
- [Technologie](#-technologie)
- [Wymagania](#-wymagania)
- [Instalacja i uruchomienie](#-instalacja-i-uruchomienie)
- [Struktura projektu](#-struktura-projektu)
- [Źródła danych rynkowych](#-źródła-danych-rynkowych)

---

## ✨ Funkcje

### Dashboard
- Podgląd wartości portfela w czasie rzeczywistym (w PLN)
- Wykres historycznej wartości portfela (7 / 30 / 90 / 365 dni)
- Wykres alokacji aktywów (kryptowaluty, akcje, waluty)
- Top 3 najlepszych i najsłabszych pozycji
- Dzienny zysk / strata (P&L)

### Rynki
- Notowania kryptowalut (BTC, ETH, BNB, XRP, SOL)
- Akcje z GPW (PKO BP, ORLEN, Allegro, KGHM, Dino Polska)
- Akcje USA (NVIDIA, Apple, Microsoft, Google, Amazon)
- Akcje europejskie (ASML, LVMH, SAP, Novo Nordisk, TotalEnergies)
- Akcje azjatyckie (TSMC, Tencent, Samsung, Alibaba, Toyota)
- Kursy walut z NBP (EUR, USD, GBP, CHF)

### Portfel
- Lista wszystkich otwartych pozycji z bieżącymi cenami
- Kalkulacja zysku / straty brutto i netto (z uwzględnieniem prowizji)
- Średnia cena zakupu (metoda FIFO)
- Zmiana procentowa 24h

### Transakcje
- Dodawanie transakcji kupna i sprzedaży
- Obsługa kryptowalut, akcji i walut
- Rejestrowanie prowizji
- Notatki do transakcji

### Ustawienia i dane
- Eksport transakcji do pliku CSV
- Import transakcji z pliku CSV (z walidacją)
- Usuwanie wszystkich danych
- Licznik zapisanych transakcji

---

## 🖼️ Zrzuty ekranu

<img width="1898" height="996" alt="Zrzut ekranu 1" src="https://github.com/user-attachments/assets/b6851e0b-0a75-4eea-8879-1b13b9fc210e" />

<img width="1897" height="992" alt="Zrzut ekranu 2" src="https://github.com/user-attachments/assets/23e47bcc-85da-4ceb-8c66-07a8d3d0f286" />

<img width="1897" height="995" alt="Zrzut ekranu 3" src="https://github.com/user-attachments/assets/9c77ad07-47c2-42bc-820d-06da46195cfc" />

<img width="1901" height="994" alt="Zrzut ekranu 4" src="https://github.com/user-attachments/assets/79fce65f-5d3f-418e-9f31-56d20ac1d93a" />

<img width="1899" height="996" alt="Zrzut ekranu 5" src="https://github.com/user-attachments/assets/bbeaeda7-1e71-45c3-b421-c7897cef9412" />

<img width="1900" height="994" alt="Zrzut ekranu 6" src="https://github.com/user-attachments/assets/266178dc-8d29-4a0e-9c13-d5cb1d0b214f" />

---

## 🏗️ Architektura

Projekt wykorzystuje wzorzec **MVVM** (Model–View–ViewModel) z wyraźnym podziałem odpowiedzialności:

```
┌─────────────────────────────────────────┐
│              Views (XAML)               │
│Dashboard · Rynki · Portfel · Ustawienia │
├─────────────────────────────────────────┤
│           ViewModels (C#)               │
│  CommunityToolkit.Mvvm · Commands       │
├─────────────────────────────────────────┤
│            Services (C#)                │
│API · Database · Portfolio · CSV · Cache │
├─────────────────────────────────────────┤
│            Models (C#)                  │
│  Transaction · PortfolioPosition · ...  │
├─────────────────────────────────────────┤
│          SQLite (lokalna baza)          │
└─────────────────────────────────────────┘
```

**Kluczowe decyzje architektoniczne:**
- **Dependency Injection** - pełne DI przez `Microsoft.Extensions.DependencyInjection`
- **Cache w pamięci** - `ICacheService` z konfigurowalnymi czasami wygaśnięcia
- **Fallback na cache** - przy braku internetu dane serwowane z pamięci podręcznej i lokalnej bazy
- **Throttling API** - ograniczenie równoległych zapytań (`SemaphoreSlim`)
- **Konwersja walut** - automatyczne przeliczanie na PLN (kursy NBP)

---

## 🛠️ Technologie

| Kategoria | Technologia |
|---|---|
| Framework | [.NET MAUI](https://learn.microsoft.com/dotnet/maui/) (.NET 10) |
| Język | C# 14 |
| Wzorzec | MVVM — [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) |
| UI Toolkit | [CommunityToolkit.Maui](https://learn.microsoft.com/dotnet/communitytoolkit/maui/) |
| Baza danych | [SQLite](https://www.sqlite.org/) via `sqlite-net-pcl` |
| Wykresy | [Microcharts](https://github.com/dotnet-ad/Microcharts) + SkiaSharp |
| JSON | Newtonsoft.Json |
| Nawigacja | .NET MAUI Shell (TabBar) |

---

## 📌 Wymagania

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio 2022 17.14+ z workloadem **.NET Multi-platform App UI**
- Dla Androida: Android SDK 21+ (Android 5.0)
- Dla iOS / macOS: Xcode 15+, macOS 15+
- Dla Windows: Windows 10 (17763+)

---

## 🚀 Instalacja i uruchomienie

1. Pobierz projekt w rozszerzeniu .zip.
2. Wypakuj całą zawartość folderu.
3. Otwórz `PortfelStudenta.csproj` bezpośrednio w **Visual Studio** i uruchom projekt przyciskiem ▶️ z wybraną platformą docelową.

---

## 📁 Struktura projektu

```
PortfelStudenta/
├── Models/
│   ├── AssetPrice.cs            # Cena aktywa z API
│   ├── CsvImportResult.cs       # Wynik importu CSV
│   ├── Enums.cs                 # AssetType, TransactionType
│   ├── PortfolioPosition.cs     # Pozycja portfelowa (z P&L)
│   ├── PortfolioSummary.cs      # Podsumowanie portfela
│   ├── PriceHistory.cs          # Historia cen (cache DB)
│   └── Transaction.cs           # Transakcja (tabela SQLite)
├── ViewModels/
│   ├── BaseViewModel.cs         # Bazowy VM (IsBusy, Title)
│   ├── DashboardViewModel.cs    # Dashboard — wykresy, P&L
│   ├── MarketsViewModel.cs      # Notowania rynkowe
│   ├── PortfolioViewModel.cs    # Lista pozycji
│   ├── SettingsViewModel.cs     # CSV import/export, reset
│   └── AddTransactionViewModel.cs # Formularz dodawania transakcji
├── Views/
│   ├── DashboardPage.xaml/.cs   # Ekran główny
│   ├── MarketsPage.xaml/.cs     # Przegląd rynków
│   ├── PortfolioPage.xaml/.cs   # Portfel użytkownika
│   ├── SettingsPage.xaml/.cs    # Ustawienia
│   └── AddTransactionPage.xaml/.cs # Dodawanie transakcji
├── Services/
│   ├── BaseApiService.cs        # Bazowa klasa API (cache + fallback)
│   ├── CoinCapApiService.cs     # Kryptowaluty (Binance API)
│   ├── NbpApiService.cs         # Kursy walut (NBP API)
│   ├── YahooFinanceApiService.cs # Akcje (Yahoo Finance API)
│   ├── DatabaseService.cs       # SQLite — transakcje + historia cen
│   ├── PortfolioService.cs      # Logika portfela i kalkulacje
│   ├── CsvService.cs            # Import / eksport CSV
│   └── CacheService.cs          # Cache w pamięci operacyjnej
├── Converters/
│   └── ValueConverters.cs       # Konwertery XAML
├── Behaviors/
│   └── SwitchToggledBehavior.cs # Zachowania UI
├── Resources/                   # Ikony, czcionki, obrazy, splash
├── AppShell.xaml                # Nawigacja (TabBar)
├── App.xaml                     # Globalne style i zasoby
├── MauiProgram.cs               # Konfiguracja DI i startowa
└── PortfelStudenta.csproj       # Plik projektu (.NET 10 MAUI)
```

---

## 🌐 Źródła danych rynkowych

| Dane | API | Odświeżanie |
|---|---|---|
| Kryptowaluty | [Binance Public API](https://binance-docs.github.io/apidocs/) | co 1 min |
| Kursy walut | [NBP Web API](https://api.nbp.pl/) | co 2 min |
| Akcje | [Yahoo Finance API](https://finance.yahoo.com/) | co 2 min |

> **Uwaga:** Wszystkie ceny są automatycznie przeliczane na **PLN** za pomocą kursów z Narodowego Banku Polskiego.

---

<p align="center">
  Zbudowane z .NET MAUI
</p>
