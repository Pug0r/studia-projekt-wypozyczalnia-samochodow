# Dokumentacja — Wypożyczalnia Samochodów

## Spis treści

1. [Informacje ogólne](#1-informacje-ogólne)
2. [Instalacja i uruchomienie](#2-instalacja-i-uruchomienie)
3. [Role użytkowników](#3-role-użytkowników)
4. [Model danych](#4-model-danych)
5. [Instrukcja obsługi](#5-instrukcja-obsługi)
   - 5.1 [Logowanie i rejestracja](#51-logowanie-i-rejestracja)
   - 5.2 [Panel administratora](#52-panel-administratora)

---

## 1. Informacje ogólne

Celem projektu jest demonstracja działającego systemu wypożyczalni samochodów z podziałem na role użytkowników, zarządzaniem pojazdami oraz podstawową autoryzacją.

### Główne funkcje

- **Logowanie i rejestracja** — każdy nowy użytkownik może założyć konto i się zalogować.
- **Przeglądanie floty** — zalogowany użytkownik widzi listę wszystkich samochodów z ich statusem dostępności i cennikiem.
- **Panel administratora** — administrator może dodawać, edytować i usuwać samochody oraz zarządzać kontami użytkowników.

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

**Uwaga:** pole `IsAvailable` nie jest eksponowane w formularzu edycji — nowe samochody dodawane są zawsze jako dostępne.

**Walidacja:**
- Marka i model są wymagane.
- Rok musi być z zakresu 1900 – (bieżący rok + 1).
- Cena za dobę musi być większa od zera.
- Przebieg nie może być ujemny.

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
