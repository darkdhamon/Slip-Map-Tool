# Starforged Atlas

Starforged Atlas is a modern web and desktop atlas for science-fiction campaign sectors, built by migrating behavior from the legacy StarWin and Slip Map applications into a new domain, application, infrastructure, web, and desktop stack.

## Current State

The project is actively in developer preview.

Today the repository contains:

- A Blazor-based web application for exploring sector data
- A Windows desktop shell that hosts the web experience in WebView2
- Import and migration support for legacy StarWin sector data
- A portable desktop packaging workflow for friend-and-family testing
- Automated test projects for domain, application, and web behavior

Recent work on `dev` has focused on:

- Splitting the Sector Explorer into dedicated pages for Empires, Aliens, Colonies, Systems, Sector Configuration, Worlds, Timeline, and Hyperlanes
- Improving loading states, progress overlays, and elapsed-time feedback
- Improving timeline performance and filtering
- Stabilizing the 3D sector map workspace and route rendering
- Preserving imported history data more reliably across re-imports

## Repository Layout

- `StarWin.Domain`: domain model and core business rules
- `StarWin.Application`: application services and use cases
- `StarWin.Infrastructure`: persistence, import, and external integrations
- `StarWin.Web`: Blazor UI and web host
- `StarWin.Desktop`: Windows desktop host
- `StarWin.*.Tests`: automated test coverage
- `Legacy`: reference-only legacy source and compiled data assets

## Desktop Preview Notes

The current desktop preview is distributed as a portable package. It is suitable for testing, but it is not the final installer experience.

Known limitations:

- Some antivirus products, especially Avast, may flag the unsigned portable build as suspicious
- The desktop workflow is currently focused on Windows
- Packaging and installer polish are still in progress

## Legacy Sources

The original Slip Map application was created to be used alongside Star Generator v2.0 and simulated a slipstream network between star systems.

StarWin, originally Star Generator for Windows, was created by Aina Rasolomalala.
