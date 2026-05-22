# Finance Tracker

A mobile-first personal finance tracker built with .NET MAUI.

The app uses a flat terminal-inspired design system instead of standard mobile cards and dashboards. It lets you log income and expenses, review history, assign monthly category budgets, and inspect spending trends with ASCII-style charts.

## Features

- `Overview`: full-month calendar, daily ledger filtering, spending summaries, and terminal-style charts
- `Add`: quick entry screen for income and expenses
- `Budget`: month-specific category budgets with unallocated pool tracking
- `History`: grouped monthly ledger with edit and delete actions
- Local persistence inside the app for transactions and budgets

## Design Notes

- Strict CLI/terminal visual language
- Monospace typography throughout
- Flat layout with no modern card UI
- Deep navy background with amber, cyan, gray, and coral status colors

## Tech Stack

- `.NET MAUI`
- `CommunityToolkit.Mvvm`
- Local JSON persistence in app storage

## Project Structure

- `Pages/`: UI screens
- `ViewModels/`: page state and presentation logic
- `Services/`: persistence and app data services
- `Models/`: transaction and budget models
- `Helpers/`: formatting and finance helper utilities
- `Resources/`: icons, styles, fonts, and platform assets

## Local Data

The app stores data in the platform app-data folder, not in the repo.

- Transactions: `transactions.json`
- Budgets: `budgets.json`

Budgets are scoped by month, so each month has its own separate allocation set.

## Run

Build the Windows target:

```powershell
dotnet build -c Release -f net10.0-windows10.0.19041.0
```

Run the Windows app:

```powershell
dotnet run -f net10.0-windows10.0.19041.0
```

If `dotnet run` hangs because it is attached to a GUI app, launch the built executable directly:

```powershell
.\bin\Debug\net10.0-windows10.0.19041.0\win-x64\FinanceTracker.exe
```

Build the Android target:

```powershell
dotnet build -c Release -f net10.0-android
```

To deploy to Android, use an emulator or a connected device with USB debugging enabled.

## Current Status

- Windows release build verified
- Android release build verified
- Remaining compiler warnings on this machine are external `LIB` environment-path warnings, not app-code errors

## Next Ideas

- Recurring budgets and entries
- Budget carry-forward modes
- Data export and backup
- Search and filters in history
- Store-ready signing and packaging
