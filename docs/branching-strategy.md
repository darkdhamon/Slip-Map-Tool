# Branching Strategy

This repository now uses a simple three-lane workflow so feature work can happen in parallel without stacking unrelated changes on `master`.

## Long-lived branches

- `master`: protected release branch. Only reviewed, tested changes land here.
- `dev`: shared integration branch for completed feature work that is ready to be combined with other in-flight changes.

## Short-lived branches

- Create all task branches from `dev`.
- Use a descriptive branch name:
  - `feature/<issue-number>-short-description`
  - `fix/<issue-number>-short-description`
  - `chore/<short-description>`
  - `codex/<short-description>` for Codex work that is not tied to an issue number

## Rules

1. Do not commit feature work directly to `master`.
2. Do not commit feature work directly to `dev` unless the change is explicitly repository maintenance for the shared branch itself.
3. Open pull requests into `dev` for normal feature, fix, and chore work.
4. Merge `dev` into `master` only when the integrated set of changes is ready to release.
5. Create urgent production fixes from `master` as `hotfix/<issue-number>-short-description`, merge them into `master`, and then merge or cherry-pick them back into `dev`.
6. Delete short-lived branches after they are merged.

## Recommended flow

1. Update local `dev`.
2. Create a feature branch from `dev`.
3. Make focused commits for one issue or concern.
4. Push the branch and open a pull request into `dev`.
5. After approval and validation, merge into `dev`.
6. Periodically promote `dev` into `master` as a release batch.

## Why this helps

- Multiple tasks can move forward without blocking each other on one shared branch.
- `dev` becomes the place where integration issues show up before release.
- `master` stays stable and easier to trust for releases, rollbacks, and debugging.
