# Testing Plan

## Coverage Goals

- Class libraries: `90%` line coverage target
- Web app: `70%` line coverage target

## Test Layers

### Domain

Focus on pure business logic first because it is the cheapest and most stable place to build regression protection.

Priority areas:

- `StarWin.Domain/Services/WorldControlService.cs`
- `StarWin.Domain/Services/SpaceHabitatConstructionService.cs`
- `StarWin.Domain/Services/IndependentColonyEmpireFactory.cs`
- `StarWin.Domain/Services/StarWin2SectorFileSetResolver.cs`
- `StarWin.Domain/Services/SectorRoutePlanner.cs`
- `StarWin.Domain/Services/GurpsTemplateBuilder.cs`

Goals:

- Validate state transitions and derived values
- Cover edge cases and null validation
- Lock down legacy-import mapping helpers and route calculations

### Application

Application-layer tests should cover orchestration and query shaping without depending on UI rendering.

Priority areas:

- `StarWin.Application/Services/StarWinSearchService.cs`
- explorer query contracts and DTO shaping
- import preview/result models

Goals:

- Search ranking and result routing
- Import-id parsing behavior
- Empty-query and max-result handling

### Infrastructure

Infrastructure tests should use lightweight database-backed integration tests where pure unit tests are not enough.

Priority areas:

- `StarWin.Infrastructure/Services/StarWinExplorerQueryService.cs`
- `StarWin.Infrastructure/Services/StarWinExplorerContextLoader.cs`
- `StarWin.Infrastructure/Services/StarWinSectorConfigurationService.cs`
- `StarWin.Infrastructure/Services/StarWinImageService.cs`

Goals:

- Verify EF query shaping and paging
- Confirm section loaders only hydrate requested data
- Protect import and persistence workflows

### Web

Web tests should focus on render behavior, routing expectations, and component state transitions.

Priority areas:

- `Home.razor`
- `NotFound.razor`
- `ExplorerMetricsBand.razor`
- `ExplorerSectionTabs.razor`
- `ExplorerTimelineSection.razor`
- `SectorExplorer.razor`

Goals:

- Confirm important content and links render
- Verify active-tab behavior and route generation
- Cover loading and empty states
- Add higher-value component tests before broad snapshot-style coverage

## Initial Milestones

1. Establish test projects and coverage tooling.
2. Add fast domain and application tests for pure logic.
3. Add web component render tests for high-traffic pages and shared Explorer components.
4. Add infrastructure query tests around Explorer loading and paging.
5. Start enforcing coverage thresholds once the suite is stable.

## Recommended Tooling

- `xUnit`
- `coverlet.collector`
- `bUnit` for Blazor component tests
- SQLite-backed tests for EF Core query behavior

## Regression Priorities From Current Work

- Timeline event list/detail loading
- Explorer route split and direct deep-link behavior
- Section loading overlays and navigation states
- Section-specific Explorer data loading instead of full graph hydration
- Not-found routing behavior
