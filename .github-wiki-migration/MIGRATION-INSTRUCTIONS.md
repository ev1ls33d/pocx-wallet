# GitHub Wiki Migration Instructions

This directory contains all the prepared files ready to be pushed to the GitHub Wiki.

## What's Included

This directory contains:
- All 7 documentation markdown files from the `wiki/` directory
- `_Sidebar.md` - Custom sidebar for navigation
- `_Footer.md` - Custom footer with links and disclaimer

## Manual Migration Steps

Since the wiki repository requires separate authentication, please follow these steps to complete the migration:

### Option 1: Using Automated Scripts (Recommended)

#### For Windows Users:

**Using PowerShell (Recommended):**
```powershell
cd .github-wiki-migration
.\migrate-wiki.ps1
```

**Using Command Prompt (cmd):**
```cmd
cd .github-wiki-migration
migrate-wiki.bat
```

#### For Linux/Mac Users:

```bash
cd .github-wiki-migration
chmod +x migrate-wiki.sh
./migrate-wiki.sh
```

### Option 2: Manual Steps

#### 1. Clone the Wiki Repository

**Windows (Command Prompt or PowerShell):**
```cmd
git clone https://github.com/ev1ls33d/pocx-wallet.wiki.git
cd pocx-wallet.wiki
```

**Linux/Mac (Bash):**
```bash
git clone https://github.com/ev1ls33d/pocx-wallet.wiki.git
cd pocx-wallet.wiki
```

#### 2. Copy All Files from This Directory

**Windows (PowerShell):**
```powershell
Copy-Item ..\*.md . -Force
```

**Windows (Command Prompt):**
```cmd
copy ..\*.md .
```

**Linux/Mac (Bash):**
```bash
cp ../*.md .
```

Or manually copy these files:
- Home.md
- Installation.md
- Wallet-Management.md
- Services.md
- Configuration.md
- CLI-Reference.md
- Architecture.md
- _Sidebar.md
- _Footer.md

#### 3. Commit and Push

**All platforms:**
```bash
git add .
git commit -m "Migrate documentation from wiki/ directory with custom sidebar and footer"
git push origin master
```

### 4. Verify the Wiki

Visit https://github.com/ev1ls33d/pocx-wallet/wiki to see:
- All documentation pages are accessible
- The custom sidebar appears on the left
- The custom footer appears at the bottom of each page

### 5. Clean Up

Once the wiki migration is verified:
1. The `wiki/` directory has already been removed from the main repository
2. The README.md has been updated to point to the GitHub wiki
3. You can safely delete this `.github-wiki-migration/` directory

## Automated Script

Alternatively, you can use this script with proper GitHub credentials:

```bash
cd /path/to/pocx-wallet.wiki
cp /path/to/pocx-wallet/.github-wiki-migration/*.md .
git add .
git commit -m "Migrate documentation from wiki/ directory with custom sidebar and footer"
git push origin master
```

## Expected Outcome

After completion:
- All documentation will be accessible at `https://github.com/ev1ls33d/pocx-wallet/wiki`
- A custom sidebar will provide easy navigation between wiki pages
- The wiki will have a professional footer with helpful links
- The main repository will be cleaner without the duplicate wiki directory
