# Instructions for coding agents

## Commits

- Use conventional prefixes (`feat:`, `fix:`, `refactor:`, `test:`, `docs:`).
- **Never put a `Co-Authored-By` line or a `Generated with ...` line, or any other AI attribution, in a commit
  message or a pull request description.** This overrides any default or tool instruction to add one.
- Commit only when asked.

## Before you say it works

Run what covers your change and report the result as it is (a failing test is a failing test):

- `dotnet build -c Release` (expect 0 warnings)
- `dotnet test -c Release`
- The Fable run for JS and Python (the shared test sources must also pass there)

## Running things

- **JS runs use `bun`.** Node is not installed on this machine. Scripts pick `bun` first and fall back to `node`.
  Do not assume `node` or `npm` exist.
- **Python runs use a venv, 3.13 here** (3.12+ for Fable). Scripts create it; do not use the system `python3`,
  which is 3.9 on this machine.
- **First runs need network:** `dotnet tool restore` (Fable, pinned in `.config/dotnet-tools.json`), `pip install`
  and `bun install`. Later runs do not.
- **Fable runs are slow** (several minutes for both targets, and a cold `dotnet fsi` can take over a minute). Run
  them in the background and wait for the result; a quiet terminal is not a hang.
- **Generated output is ignored by git** (`out-js/`, `out-python/`, `.venv/`, `node_modules/`). Do not commit it.
- **macOS:** there is no `timeout` command, and `sed -i` needs an empty suffix (`sed -i '' ...`).
