# PS4 Remarry

A Windows GUI tool for working with PS4 PKG files — specifically for **remarrying**, **merging**, **repacking**, and **verifying** fake PKG (FPKG) packages. Built for jailbroken PS4 consoles running GoldHEN or similar.

This is a modern replacement for the batch-script workflow that's been passed around the PS4 scene for years. It keeps the proven external tools (`orbis-pub-cmd.exe`, `gengp4_patch.exe`, `gengp4_app.exe`) but wraps them in a proper application with live progress, verification, parallel batch processing, and safety guards around every destructive operation.

---

## What It Does

### 1. Remarry — pair a game with an update

Takes a base game PKG and an update PKG and produces a **new update PKG** whose metadata is rewritten to match your specific game dump. Use this when:

- You have an update from a different region than your game.
- The update and game were dumped separately and their fingerprints don't match.
- The console refuses to install an update because it "belongs to a different game".

The original files are never touched. The output is a new PKG in your chosen output folder.

### 2. Merge — game + update → single game PKG

Takes a base game and its update and produces a **new full game PKG** with the update's files baked in. Useful when you want a single installable file instead of a game + separate patch.

The original files are never touched.

### 3. Repack — extract, modify, rebuild

Extracts a PS4 PKG into a folder, lets you add, remove, or replace files freely, then rebuilds a new PKG. Use this for:

- Stripping foreign language audio/video to shrink a game.
- Adding custom content to a PKG.
- Rebuilding a PKG you've manually edited.

Games and updates are fully supported. **DLC is not supported** — use a dedicated DLC tool for that.

### 4. Verify — is this update married to this game?

Reads the marry digest from a game PKG and the origin digest from an update PKG and tells you whether they match. Run this before installing to know whether a remarry succeeded.

### 5. Batch — run many remarry jobs in parallel

Queue multiple game/update pairs and process them concurrently, up to a configurable limit. Each job runs in its own work subdirectory, and each is automatically verified after it builds.

### Bonus features

- **PKG info panel** — shows TITLE_ID, CONTENT_ID, TITLE, CATEGORY, APP_VER, VERSION, required System Software version, SDK version, and cover-art availability for any loaded PKG.
- **Rename PKG** — renames a PKG to `<Title>-<TitleID>-<Game|Update|DLC>-(A<AppVer>-V<Version>).pkg`, stripping trademark symbols and other invalid filename characters.
- **Cover art viewer** — extracts and displays `icon0.png` and `pic1.png` from a PKG, with Save As and Copy-to-clipboard.
- **Region mismatch detection** — warns when the loaded game and update are for different regions or different Title IDs, before you commit to a long build.

---

## Requirements

- **Windows 10 or 11** (x64 or x86)
- **.NET 10 SDK** to build, or the **.NET 10 Desktop Runtime** to run a published build
- **Visual Studio 2026** (or any IDE with .NET 10 support) if you want to build from source

---

## Setup

### 1. Get the external tools

The tool shells out to Sony's official Orbis Publishing Tools. These are not distributed with this repo — you need to obtain them separately.

You need **`PS4-Fake-PKG-Tools-3.87-main`** (or newer), which contains:

- `orbis-pub-cmd.exe` — the extract/build tool
- `gengp4_patch.exe` — GP4 generator for update PKGs
- `gengp4_app.exe` — GP4 generator for base game PKGs
- `ext\sc.exe`, `ext\di.exe`, and others — helper executables `orbis-pub-cmd.exe` depends on
- Any accompanying DLLs

### 2. Place them in the `tools\` folder

Copy the **entire contents** of `PS4-Fake-PKG-Tools-3.87-main` into `tools\` at the solution root:

```
PS4Remarry\
├─ tools\
│  ├─ orbis-pub-cmd.exe
│  ├─ gengp4_patch.exe
│  ├─ gengp4_app.exe
│  ├─ ext\
│  │  ├─ sc.exe
│  │  ├─ di.exe
│  │  └─ ...
│  └─ (any DLLs the tools need)
├─ src\
│  ├─ PS4Remarry.Core\
│  └─ PS4Remarry.WinForms\
└─ PS4Remarry.sln
```

The app checks for the essential files on startup and refuses to run if anything is missing. **Do not omit the `ext\` folder** — without it, `orbis-pub-cmd.exe` fails with a cryptic "invalid or missing sc.exe" error.

### 3. Build

Open `PS4Remarry.sln` in Visual Studio, set **`PS4Remarry.WinForms`** as the startup project (right-click → Set as Startup Project), then **Build → Rebuild Solution**.

The `tools\` folder is copied to the output directory automatically by the `.csproj`, so the built `.exe` and its tools end up in:

```
src\PS4Remarry.WinForms\bin\Debug\net10.0-windows\tools\
```

Run `PS4Remarry.exe` from there.

For a redistributable build:

```powershell
dotnet publish src/PS4Remarry.WinForms/PS4Remarry.WinForms.csproj `
    -c Release -r win-x86 --self-contained false `
    -p:PublishSingleFile=false -o publish/
```

---

## Safety Model

This tool writes to disk and — in some operations — deletes files. Every destructive operation is guarded by multiple safety checks. **Read this section before you use the tool on real data.**

### Where the tool writes

The tool has **three** places it touches on disk:

1. **`tools\Work\`** — a scratch directory inside the tools folder, wiped before every job. This is where PKG extraction, GP4 generation, and PKG builds happen.
2. **The output folder you choose** — where the finished PKG lands.
3. **An extract folder you choose** — only used by the Repack tab.

**Nowhere else.** It never touches your source PKGs, and it never touches any folder you haven't explicitly told it to.

### The safety guards

Every call to `WipeDirectory()` goes through five independent guards:

| Guard | What it blocks |
|---|---|
| **Deny-list** | Desktop, Documents, Downloads, Pictures, Music, Videos, user profile, drive roots |
| **Ancestor check** | Any folder that *contains* one of the above (e.g. `C:\Users\Alan` when the Desktop is under it) |
| **Work-root check** | Any `WorkDir` that isn't inside `tools\Work` |
| **ToolsDir check** | Refuses to start if `tools\` itself is inside a protected folder |
| **UI confirmation** | If `tools\Work\` has content, asks before wiping |

If any of the first four fire, the operation aborts with a clear error and nothing is deleted. The fifth shows a dialog with file/folder counts and total size.

### Known limitation

The tool **does not prompt** before writing the output PKG. If the output filename matches an existing file, the existing file is replaced. If you're rebuilding the same game repeatedly, this is usually what you want; if you're not sure, use a dedicated output folder per session.

### Recommendation

Run the tool from a folder that is **not** on your Desktop, in Documents, or in your user profile. Something like `C:\PS4Remarry\` is ideal. This puts the whole tool — including its `tools\Work\` scratch directory — outside any protected area, and makes the safety guards' behaviour simple to reason about.

---

## Building an FPKG

The tool produces **fake PKGs** (FPKG) suitable for installation on a jailbroken PS4. It does **not** produce retail-signed PKGs and will not work on a stock console.

If you don't know what "fake PKG" means, this tool is not for you.

---

## Project Layout

```
PS4Remarry/
├─ PS4Remarry.sln
├─ README.md
├─ tools/                              # external Sony tools (not in repo)
└─ src/
   ├─ PS4Remarry.Core/                 # UI-free library — pipelines, parsers, safety
   │  ├─ PkgTitleIdReader.cs           # reads TITLE_ID from PKG header @ 0x47
   │  ├─ SfoParser.cs                  # read-only param.sfo parser + cover art extraction
   │  ├─ MarryDigestReader.cs          # reads marry digest from PKG entry 0x1001
   │  ├─ PkgRenamer.cs                 # renames PKGs based on SFO metadata
   │  ├─ ToolLocator.cs                # locates and validates the external tools
   │  ├─ ProcessRunner.cs              # async process runner with hidden console
   │  ├─ FileSystemUtil.cs             # directory copy/wipe with safety guards
   │  ├─ SafePathGuard.cs              # central destructive-operation safety checks
   │  ├─ Formatting.cs                 # byte-size formatting helper
   │  ├─ ProgressReport.cs             # progress message record
   │  ├─ PkgBuildProgressMonitor.cs    # size-based build progress
   │  ├─ RemarryPipeline.cs            # remarry (game + update → married update)
   │  ├─ MergePipeline.cs              # merge (game + update → single game)
   │  ├─ RepackPipeline.cs             # repack (extract, modify, rebuild)
   │  ├─ RepackExtractLog.cs           # sidecar log written into extract folders
   │  ├─ PkgIntegrityChecker.cs        # pre-repack sanity checks
   │  ├─ BatchJob.cs                   # batch job model
   │  └─ BatchRunner.cs                # parallel batch executor
   └─ PS4Remarry.WinForms/             # WinForms UI
      ├─ Program.cs
      ├─ MainForm.cs / .Designer.cs    # Single tab
      ├─ BatchTab.cs                   # Batch tab
      ├─ VerifyTab.cs                  # Verify tab
      ├─ RepackTab.cs                  # Repack tab
      ├─ ImageViewerForm.cs            # cover art popup
      ├─ DataGridViewProgressColumn.cs # custom progress-bar cell
      └─ DataGridViewColoredTextColumn.cs # custom coloured-text cell
```

The Core library has no UI dependencies. It can be reused from a console application, a script, or a service — a CLI front-end is a straightforward addition.

---

## Usage Tips

### First run

1. Run the app. It should show "Ready. Tools verified." in the log.
2. If it shows "TOOLS MISSING", read the message carefully — it tells you exactly which file is missing and where it expected to find it.
3. Test with a small PKG first. A 20 GB remarry is not the right place to discover a config problem.

### Before a long build

- Check the log for any `[warn]` lines after loading both PKGs. Region mismatches and Title ID mismatches are warnings, not errors, but they tell you whether the remarry is going to work.
- The pre-flight dialog for Title ID mismatches exists to save you from a long build that won't produce a working result.
- If you have any doubt, use the Verify tab first — it takes seconds and tells you whether the game and update are compatible.

### After a build

- Click **Verify Output**. This compares the game's marry digest against the built PKG's digest. A match means the remarry succeeded.
- If it says MISMATCH, don't install the PKG — something went wrong, and the log has the details.

### Batch mode

- Start with parallelism = 1 to confirm the workflow works for one pair.
- Then increase gradually. The `NumericUpDown` default is `ProcessorCount / 2`, which is a reasonable starting point.
- Each job's output goes into its own subfolder (`Job1-<gamename>`) so two jobs with the same TITLE_ID don't collide.
- The batch grid shows per-job progress and per-job digest verification after the build.

---

## Troubleshooting

**"The version of sc.exe is invalid or missing"**
The `ext\` subfolder is missing from `tools\`, or contains an incompatible `sc.exe`. Copy the full contents of the tools package, including `ext\`, into the `tools\` folder.

**"in_path or out_path is invalid"** from `img_extract`
The tool's `img_extract` command fails when paths contain spaces. Move the source PKGs to a path without spaces (e.g. `C:\test\`) and try again.

**"Format of the project file is not valid"**
The GP4 project couldn't be parsed. Usually means a tool version mismatch, or a GP4 file that got truncated. Rebuild the GP4 by re-running the extract step.

**Build takes a long time**
`img_create` recompresses and re-hashes the entire PKG contents. A 20 GB game takes roughly as long as the original build did. There's no shortcut — the tool has to walk every file and write the output.

**Console window flashes during a build**
Some builds of `orbis-pub-cmd.exe` allocate their own console regardless of `CreateNoWindow`. The tool hides it with a Win32 `ShowWindow(SW_HIDE)` call shortly after the process starts. If a window stays visible for the whole build, that's a bug — open an issue with the log output.

---

## Contributions

Issues and pull requests are welcome. If you're reporting a bug, please include:

- The exact log output from the app's log box
- The version of `PS4-Fake-PKG-Tools` you're using
- Whether the bug happens on every PKG or only specific ones
- Your Windows version

If the bug is data-loss related, please **stop using the tool** and report it immediately with as much detail as you can. The safety guards are there specifically to make data loss impossible, and any bypass of them is a critical bug.

---

## License

This project is provided as-is for personal use. The external tools in `tools\` are Sony's, and their use is subject to whatever terms come with them.

This tool is not affiliated with Sony Interactive Entertainment. Use at your own risk.