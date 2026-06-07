# Dokumentacja — Wypożyczalnia Samochodów

## Spis treści

1. [Informacje ogólne](#1-informacje-ogólne)
2. [Instalacja i uruchomienie](#2-instalacja-i-uruchomienie)
3. [Role użytkowników](#3-role-użytkowników)
4. [Model danych](#4-model-danych)
5. [Instrukcja obsługi](#5-instrukcja-obsługi)
   - 5.1 [Logowanie i rejestracja](#51-logowanie-i-rejestracja)
   - 5.2 [Panel administratora](#52-panel-administratora)
   - 5.3 [Wyszukiwanie i filtrowanie](#53-wyszukiwanie-i-filtrowanie)

---

## 1. Informacje ogólne

Celem projektu jest demonstracja działającego systemu wypożyczalni samochodów z podziałem na role użytkowników, zarządzaniem pojazdami oraz podstawową autoryzacją.

### Główne funkcje

- **Logowanie i rejestracja** — każdy nowy użytkownik może założyć konto i się zalogować.
- **Przeglądanie floty** — zalogowany użytkownik widzi listę wszystkich samochodów z ich statusem dostępności i cennikiem. Użytkownik może wyszukiwać i filtrować dostępne samochody.
- **Wypożyczenia i zwroty** — klient może wypożyczyć dostępny samochód, przeglądać swoje wypożyczenia i zwrócić aktywne wypożyczenie.
- **Zgłaszanie usterek** — klient może zgłosić usterkę tylko dla aktywnego wypożyczenia, przed zwróceniem samochodu. Zgłoszenie może zawierać zdjęcie.
- **Panel administratora** — administrator może dodawać, edytować i usuwać samochody oraz zarządzać kontami użytkowników.
- **Rezerwacje i proces wypożyczeń** — zalogowany klient może wybrać zakres dat w kalendarzu, sprawdzić dostępność floty w danym terminie, a następnie przejść na dedykowaną podstronę podsumowania zamówienia (Checkout Page), aby sfinalizować bezpieczny wynajem.
- **Zarządzanie rezerwacjami przez klienta** — użytkownik ma wgląd w historię swoich rezerwacji (aktywne, zakończone, anulowane) z możliwością zwrotu pojazdu lub anulowania rezerwacji przed jej rozpoczęciem.

### Użyte technologie

- **Framework UI** — .NET MAUI Blazor Hybrid
- **Język** — C# 13 / .NET 10
- **Baza danych** — SQLite
- **ORM** — Entity Framework Core 10
- **Komponenty UI** — Blazor Razor Components + Bootstrap
- **Hasła** — BCrypt.Net

---

## 2. Instalacja i uruchomienie

### Uruchomienie z kodu źródłowego

```powershell
# Budowanie projektu
dotnet build CarRental/CarRental.csproj -f net10.0-windows10.0.19041.0

# Uruchomienie
dotnet run --project CarRental/CarRental.csproj -f net10.0-windows10.0.19041.0
```

### Baza danych

Baza danych SQLite tworzona jest automatycznie przy pierwszym uruchomieniu. Plik `carrental.db` znajduje się w:

```
%APPDATA%\CarRental\carrental.db
```

Przy pierwszym starcie aplikacja automatycznie wstawia do bazy przykładowe dane startowe:

**Domyślne konta:**

| Login | Hasło | Rola |
|---|---|---|
| `admin` | `password123` | Administrator |
| `customer` | `password123` | Klient |

**Domyślne samochody:**

| Marka | Model | Rok | Cena / dzień | Przebieg | Usterki | Status |
|---|---|---|---|---|---|---|
| Toyota | Corolla | 2021 | 129,00 zł | 45 000 km | — | Dostępny |
| Skoda | Octavia | 2022 | 149,00 zł | 22 000 km | — | Dostępny |
| Volkswagen | Golf | 2020 | 119,00 zł | 78 000 km | Minor scratch on bumper | Wynajęty |
| Kia | Sportage | 2023 | 189,00 zł | 8 500 km | — | Dostępny |

---

## 3. Role użytkowników

Aplikacja wyróżnia dwie role:

### Klient

- Domyślna rola przydzielana podczas rejestracji.
- Może się zalogować i wylogować.
- Ma dostęp do strony głównej z listą samochodów.
- Nie ma dostępu do panelu administratora.

### Administrator

- Rola przydzielana ręcznie przez innego administratora (lub z poziomu danych startowych).
- Ma wszystkie uprawnienia klienta.
- Widzi w menu bocznym dodatkową pozycję **Admin Panel**.
- Może zarządzać flotą samochodów (dodawanie, edycja, usuwanie).
- Może zarządzać kontami użytkowników (dodawanie, usuwanie).

---

## 4. Model danych

Aplikacja przechowuje dane w lokalnej bazie SQLite. Schemat tworzony jest automatycznie przez Entity Framework Core.

### Tabela `Users` — użytkownicy

| Kolumna | Typ | Opis |
|---|---|---|
| `Id` | `INTEGER` (PK) | Unikalny identyfikator użytkownika |
| `Username` | `TEXT` (max 100, unikalny) | Nazwa użytkownika (login) |
| `PasswordHash` | `TEXT` (max 100) | Hash hasła wygenerowany przez BCrypt |
| `Role` | `INTEGER` | Rola: `0` = Klient, `1` = Administrator |

**Walidacja:**
- Login musi mieć co najmniej 3 znaki.
- Login musi być unikalny w bazie.
- Hasło musi mieć co najmniej 6 znaków.
- Hasło przechowywane wyłącznie jako hash BCrypt — nigdy w postaci jawnej.

### Tabela `Cars` — samochody

| Kolumna | Typ | Opis |
|---|---|---|
| `Id` | `INTEGER` (PK) | Unikalny identyfikator samochodu |
| `Brand` | `TEXT` (max 100) | Marka, np. Toyota |
| `Model` | `TEXT` (max 100) | Model, np. Corolla |
| `Year` | `INTEGER` | Rok produkcji |
| `DailyRate` | `NUMERIC` (10,2) | Cena wynajmu za dobę (w złotych) |
| `IsAvailable` | `INTEGER` (bool) | `true` = dostępny, `false` = wynajęty |
| `Mileage` | `INTEGER` | Przebieg w kilometrach |
| `Faults` | `TEXT` (max 200) | Opis usterek/uszkodzeń (puste = brak usterek) |
| `ImageData` | `BLOB` | Dane obrazu (opcjonalnie) |
| `ImageContentType` | `TEXT` (max 100) | Typ MIME obrazu, np. `image/jpeg` |
| `ImageFileName` | `TEXT` (max 200) | Oryginalna nazwa pliku obrazu |

**Uwaga:** pole `IsAvailable` nie jest eksponowane w formularzu edycji — nowe samochody dodawane są zawsze jako dostępne.

**Walidacja:**
- Marka i model są wymagane.
- Rok musi być z zakresu 1900 – (bieżący rok + 1).
- Cena za dobę musi być większa od zera.
- Przebieg nie może być ujemny.

### Tabela `Rentals` — wypożyczenia

| Kolumna | Typ | Opis |
|---|---|---|
| `Id` | `INTEGER` (PK) | Unikalny identyfikator wypożyczenia |
| `CarId` | `INTEGER` (FK) | Wypożyczony samochód |
| `UserId` | `INTEGER` (FK) | Użytkownik, który wypożyczył samochód |
| `StartDate` | `TEXT` / data | Początek wypożyczenia |
| `EndDate` | `TEXT` / data | Planowany koniec wypożyczenia |
| `ReturnDate` | `TEXT` / data | Faktyczny termin zwrotu, jeśli samochód został zwrócony |
| `TotalCost` | `NUMERIC` (10,2) | Łączny koszt wypożyczenia |
| `Status` | `INTEGER` | `0` = aktywne, `1` = zakończone, `2` = anulowane |

### Tabela `RentalDefects` — zgłoszenia usterek

| Kolumna | Typ | Opis |
|---|---|---|
| `Id` | `INTEGER` (PK) | Unikalny identyfikator zgłoszenia |
| `RentalId` | `INTEGER` (FK) | Wypożyczenie, którego dotyczy zgłoszenie |
| `Type` | `INTEGER` | Typ usterki: `0` = wina wypożyczającego, `1` = wypadek drogowy, `2` = usterka odkryta przez klienta i nieujawniona przy wypożyczeniu |
| `Description` | `TEXT` (max 1000) | Opis usterki |
| `OtherPartyInsuranceNumber` | `TEXT` (max 100) | Numer ubezpieczenia drugiej osoby uczestniczącej w wypadku drogowym |
| `ReportedAt` | `TEXT` / data | Data i godzina zgłoszenia |
| `PhotoData` | `BLOB` | Zdjęcie usterki (opcjonalnie) |
| `PhotoContentType` | `TEXT` (max 100) | Typ MIME zdjęcia, np. `image/jpeg` |
| `PhotoFileName` | `TEXT` (max 200) | Oryginalna nazwa pliku zdjęcia |

**Walidacja zgłoszeń usterek:**
- Zgłoszenie może dodać tylko zalogowany właściciel aktywnego wypożyczenia.
- Zgłoszenie jest możliwe tylko przed zwróceniem samochodu.
- Opis usterki jest wymagany.
- Dla typu `Road accident` można podać numer ubezpieczenia drugiej osoby uczestniczącej w wypadku.
- Zdjęcie jest opcjonalne i ma limit 2 MB.

### Tabela `Rentals` — wypożyczenia

| Kolumna | Typ | Opis |
|---|---|---|
| `Id` | `INTEGER` (PK) | Unikalny identyfikator wypożyczenia |
| `CarId` | `INTEGER` (FK) | Powiązanie z tabelą `Cars` (ON DELETE RESTRICT) |
| `UserId` | `INTEGER` (FK) | Powiązanie z tabelą `Users` (ON DELETE RESTRICT) |
| `StartDate` | `TEXT` / `DATETIME` | Data rozpoczęcia wynajmu |
| `EndDate` | `TEXT` / `DATETIME` | Data zakończenia wynajmu |
| `ReturnDate` | `TEXT` / `DATETIME` (nullable) | Faktyczna data zwrotu samochodu |
| `TotalCost` | `NUMERIC` (10,2) | Całkowity koszt wynajmu (Dni × Stawka dzienna) |
| `Status` | `INTEGER` | Status: `0` = Active, `1` = Completed, `2` = Cancelled |

**Walidacja biznesowa (RentalService):**
- Data rozpoczęcia nie może być z przeszłości.
- Data zakończenia musi być późniejsza niż data rozpoczęcia.
- Maksymalny jednorazowy okres rezerwacji to 30 dni.
- System uniemożliwia rezerwację pojazdu, jeśli jego terminy nakładają się na inną aktywną rezerwację tego samego auta w bazie danych.

---

## 5. Instrukcja obsługi

### 5.1 Logowanie i rejestracja

Po uruchomieniu aplikacji wyświetla się ekran logowania z dwoma zakładkami: **Login** i **Register**.

**Logowanie:** wpisz login i hasło, kliknij **Login**. W razie błędu pojawi się komunikat pod formularzem.

**Rejestracja:** przejdź na zakładkę **Register**, podaj login (min. 3 znaki), hasło (min. 6 znaków) i jego potwierdzenie, kliknij **Register**. Nowe konto automatycznie otrzymuje rolę Klienta; po rejestracji następuje natychmiastowe zalogowanie.

---

### 5.2 Panel administratora

Panel dostępny pod adresem `/admin`, widoczny w menu tylko dla konta z rolą **Admin**. Próba wejścia bez uprawnień przekierowuje na stronę główną.

Panel zawiera dwie zakładki: **Cars** i **Users**.

**Zakładka Cars:**
- **Dodawanie** — wypełnij formularz (Brand, Model, Year, Daily Rate, Mileage, opcjonalnie Faults), kliknij **Add Car**.
- **Edycja** — kliknij **Edit** przy wybranym pojeździe, zmień dane, kliknij **Update Car**. Aby anulować, kliknij **Cancel**.
- **Usuwanie** — kliknij **Delete**; samochód zostaje usunięty natychmiast bez potwierdzenia.

**Zakładka Users:**
- **Dodawanie** — podaj Username, Password i wybierz rolę (`Customer` lub `Admin`), kliknij **Add User**.
- **Usuwanie** — kliknij **Delete** przy wybranym koncie. Nie można usunąć własnego konta (przycisk jest nieaktywny).

---

### 5.3 Wyszukiwanie i filtrowanie

Na stronie głównej dostępne są:

- **Wyszukiwarka** — pole tekstowe „Search by any field...”. Wpisana fraza filtruje listę po wszystkich widocznych informacjach: Brand, Model, Year, Daily rate i Status.
- **Filtry** — zestaw check-list dla Brand, Model, Year i Status (domyślnie zwinięte). Kliknij **Show** przy wybranym filtrze, aby rozwinąć listę i zaznaczyć wartości.
- **Max daily rate** — pole liczbowo-tekstowe ograniczające maksymalną cenę za dobę.
- **Clear** — czyści wyszukiwanie, usuwa wszystkie zaznaczenia i zwija listy filtrów.

Wyniki aktualizują się na bieżąco podczas wpisywania i zaznaczania opcji.

### 5.4 Proces wypożyczania (Checkout)

1. Na stronie głównej użytkownik wybiera w panelu filtrów zakres dat ("Date from" oraz "Date to") i klika **Szukaj pojazdów**. System automatycznie weryfikuje rezerwacje w bazie i oznacza auta jako `Available` lub `Rented` w tym konkretnym okresie.
2. Po kliknięciu przycisku **Rent**, aplikacja nie otwiera okna modalnego, lecz bezpiecznie przekierowuje użytkownika na osobną podstronę pod adresem `/rent/{CarId}`, przekazując wybrane daty w parametrach URL (`Query String`).
3. Na podstronie checkoutu użytkownik widzi pełne podsumowanie specyfikacji pojazdu, zdjęcie, wyliczoną automatycznie liczbę dni oraz całkowity koszt algorytmu. Po kliknięciu **Confirm rental**, rezerwacja zostaje zapisana.

### 5.5 Moje wypożyczenia (My Rentals)

Z poziomu menu bocznego zalogowany użytkownik ma dostęp do podstrony `/myrentals`:
- Widzi tam pełną listę swoich rezerwacji wraz z podsumowaniem kosztów i statusem.
- Jeśli rezerwacja ma status `Active`, użytkownik może kliknąć **Return** (co zwalnia pojazd i uzupełnia `ReturnDate`) lub **Cancel** (opcja anulowania jest aktywna tylko wtedy, gdy data rozpoczęcia rezerwacji jeszcze nie nadeszła).

### 5.6 Usterki

W formularzu **Report defect** wybierz jeden z typów usterki:

- **Renter fault** — usterka powstała z winy wypożyczającego.
- **Road accident** — usterka powstała w wyniku wypadku drogowego.
- **Undisclosed before rental** — usterka została odkryta przez wypożyczającego i nie była ujawniona podczas wypożyczania.

Następnie wpisz opis usterki i opcjonalnie dodaj zdjęcie. Po kliknięciu **Submit defect** zgłoszenie pojawia się pod danym wypożyczeniem. Po kliknięciu **Return** formularz zgłaszania usterek nie jest już dostępny dla tego wypożyczenia.

Jeżeli wybierzesz typ **Road accident**, pojawi się dodatkowe pole **Other party insurance number**, w którym można wpisać numer ubezpieczenia drugiej osoby uczestniczącej w wypadku. Zgłoszone usterki są wyświetlane w tabeli, gdzie każdy wiersz oznacza osobny incydent.
