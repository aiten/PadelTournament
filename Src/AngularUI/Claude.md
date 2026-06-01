# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Angular 21 frontend for the Examination WebAPI. The backend is an ASP.NET Core Minimal API at `https://localhost:5001`; the Angular dev server proxies `/api` to it via [proxy.conf.json](proxy.conf.json).

- Angular 21 standalone components, no NgModules
- State: Angular built-in `signal()` and `computed()` from `@angular/core`
- HTTP: `HttpClient` with Observables (no httpResource / NgRx)
- UI: custom CSS in [src/styles.css](src/styles.css) (no component library)
- Testing: Vitest via `ng test`

## Commands

```bash
npm start          # dev server on http://localhost:4200 (proxies /api → https://localhost:5001)
npm run build      # production build → ../Solution/WebAPI/wwwroot
npm test           # Vitest unit tests
npm run lint       # ESLint
```

Run a single test file: `npx vitest run src/app/app.spec.ts`

## Architecture

Features live as flat folders under `src/app/`: `teachers/`, `classes/`, `exams/`, `students/`. Each folder holds list and form components directly; no subfolders or barrel index.ts files.

Models in `src/app/models/` mirror backend DTOs. Services in `src/app/services/` wrap HttpClient calls; all use constructor injection and `providedIn: 'root'`.

`ExamOverview` (from `GET /api/exam/overview`) is a flattened read model with teacher/class names and student lists. `ExamDto` (from `GET /api/exam/{id}`) is the full editable model with foreign-key IDs. Form components load both the entity and the required dropdown data (`teachers`, `classes`) in `ngOnInit`.

## Conventions

- All components: `standalone: true`, `ChangeDetectionStrategy.OnPush` encouraged
- Templates: use `@if`/`@for` control flow (not `*ngIf`/`*ngFor`)
- State in components: `signal()` / `computed()` — no `subscribe()` for derived state
- Constructor injection (`private service: SomeService`) — not `inject()`
- `subscribe()` is acceptable in `ngOnInit` for initial data loads

## Avoid

- No NgModules, no CommonModule imports
- No `*ngIf`/`*ngFor` directives (use `@if`/`@for`)
