<div align="center">

# Async, Threads & Thread Safety
### Find the Problems, Fix Them, and Build an Async Order Processing System

<img src="https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=dotnet&logoColor=white" alt=".NET 10">
<img src="https://img.shields.io/badge/Language-C%23-239120?style=flat-square&logo=csharp&logoColor=white" alt="C#">
<img src="https://img.shields.io/badge/Parts-7-0969da?style=flat-square" alt="7 parts">
<img src="https://img.shields.io/badge/NuGet%20packages-0-2da44e?style=flat-square" alt="No NuGet packages">

</div>

| | |
|---|---|
| **Student** | Tariq Zeyad Khashan |
| **Group / ID** | Group 1 |
| **Programme** | Simulation Academy, .NET Diploma, Cycle 1 |
| **Track** | Backend .NET |
| **Assignment** | Async, Threads & Thread Safety (graded) |

## Contents

1. [Submission overview](#1-submission-overview)
2. [Repository structure](#2-repository-structure)
3. [How to run each part](#3-how-to-run-each-part)
4. [Notes and assumptions](#4-notes-and-assumptions)

---

## 1. Submission overview

The assignment consists of seven parts. Parts 01 to 05 start from the Simulation Academy starter repositories: each project was run first, its problems were recorded, and the fixed version is delivered together with an `ANSWERS.md`. Part 06 is the mini project, built from scratch. Part 11 contains the LinkedIn posts.

| Part | Folder | Subject | Deliverable |
|:--:|---|---|---|
| 01 | [`01-InvoiceExport`](01-InvoiceExport) | Nightly invoice export: load order, read customer, write invoice, for many orders | Fixed project + [`ANSWERS.md`](01-InvoiceExport/ANSWERS.md) |
| 02 | [`02-TableBooking`](02-TableBooking) | Many customers booking a limited number of tables at the same time | Fixed project + [`ANSWERS.md`](02-TableBooking/ANSWERS.md) |
| 03 | [`03-LockingResearch`](03-LockingResearch) | Research: `System.Threading.Monitor` and the `lock` statement | [`ANSWERS.md`](03-LockingResearch/ANSWERS.md) (Q1 to Q5 and links) |
| 04 | [`04-MenuViews`](04-MenuViews) | Many request threads incrementing one menu-item view counter | Fixed project + [`ANSWERS.md`](04-MenuViews/ANSWERS.md) |
| 05 | [`05-ViewsAndEmails`](05-ViewsAndEmails) | Shared `Dictionary` of page views and a `Queue` of email jobs drained by two workers | Fixed project + [`ANSWERS.md`](05-ViewsAndEmails/ANSWERS.md) |
| 06 | [`06-OrderProcessing`](06-OrderProcessing) | Mini project: asynchronous Order Processing System with JSON file storage, four phases | Source, `sample-output/`, `screenshots/` and [`README.md`](06-OrderProcessing/README.md) |
| 11 | [`LinkedIn`](LinkedIn) | Public LinkedIn posts written from this assignment | [`linked.md`](LinkedIn/linked.md) with the topic number of each post |

### The mini project in brief

The Order Processing System validates, prices, charges, saves and confirms 20 seeded orders concurrently. Every collection is a JSON file, and every database call, payment and email is simulated with `Task.Delay()` steps.

| Phase | Subject |
|:--:|---|
| 1 | Asynchronous data access layer and order validation, synchronous vs asynchronous run |
| 2 | Concurrent processing, race-condition demonstration, invoices and report |
| 3 | Full checkout: stock, pricing, payment, persistence, email with retry, compensation; sync vs async comparison; fire-and-forget investigation |
| 4 | Live progress, customer dashboard, fastest shipping quote, fraud scoring, bounded email re-send, cancellation |

The full documentation (design, storage, measurements, console output and answers to every question) is in the [mini project README](06-OrderProcessing/README.md).

## 2. Repository structure

```text
(repository root)
├── README.md                          this file
├── 01-InvoiceExport/
│   ├── InvoiceExport/                 fixed project
│   └── ANSWERS.md
├── 02-TableBooking/
│   ├── TableBooking/                  fixed project
│   └── ANSWERS.md
├── 03-LockingResearch/
│   └── ANSWERS.md
├── 04-MenuViews/
│   ├── MenuViews/                     fixed project
│   └── ANSWERS.md
├── 05-ViewsAndEmails/
│   ├── ViewsAndEmails/                fixed project
│   └── ANSWERS.md
├── 06-OrderProcessing/
│   ├── OrderProcessingSystem/         source: Shared/, Phase1/ ... Phase4/, Program.cs
│   ├── sample-output/                 final JSON files of phase1 ... phase4
│   ├── screenshots/                   console screenshots of each phase
│   └── README.md
└── LinkedIn/
    └── linked.md
```

## 3. How to run each part

All commands are run from the folder of the part. The .NET 10 SDK is required (`dotnet --list-sdks`).

| Part | Command |
|:--:|---|
| 01 | `dotnet run --project InvoiceExport` |
| 02 | `dotnet run --project TableBooking` |
| 03 | Research only, no application to run |
| 04 | `dotnet run --project MenuViews` |
| 05 | `dotnet run --project ViewsAndEmails` |
| 06 | `dotnet run` from `06-OrderProcessing/OrderProcessingSystem` (opens a numbered menu) |

Part 06 also accepts a phase name and an optional cancellation delay:

```powershell
cd .\06-OrderProcessing\OrderProcessingSystem

dotnet run -- phase1
dotnet run -- phase2
dotnet run -- phase3
dotnet run -- phase4
dotnet run -- all
dotnet run -- phase3 --cancel-after 4000
```

## 4. Notes and assumptions

- **Framework.** The .NET 10 SDK is required; the mini project targets `net10.0`.
- **Dependencies.** No third-party NuGet packages, real databases, HTTP calls or ASP.NET Core are used.
- **Generated data.** The mini project's `Data/` folder is created at runtime by `DataSeeder.ResetAsync()` under `bin/Debug/net10.0/Data` and is not committed. Build output (`bin/`, `obj/`) is excluded through `.gitignore`.
- **Measurements.** Every number in the `ANSWERS.md` files and in the mini project README comes from runs on my own machine (8 logical processors). Simulated latency is random, so timings vary slightly between runs.
- **Rules followed.** In the mini project, no `.Result`, `.Wait()` or `.GetAwaiter().GetResult()` is used to wait on an unfinished task, and cancellation tokens are passed through the asynchronous call chains.
- **Known limitations.** The limitations of the mini project (for example, the comparison of the synchronous and asynchronous processors and the end state of a cancelled run) are listed openly in section 10 of its README.

<div align="center"><sub>Tariq Zeyad Khashan | Group 1 | Simulation Academy, .NET Diploma, Cycle 1</sub></div>