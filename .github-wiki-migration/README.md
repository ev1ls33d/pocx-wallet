# Wiki Migration Files

This directory contains all the files prepared for the GitHub Wiki migration.

## Quick Start

To complete the wiki migration, run these commands:

```bash
# 1. Clone the wiki repository
git clone https://github.com/ev1ls33d/pocx-wallet.wiki.git
cd pocx-wallet.wiki

# 2. Copy all markdown files from this directory
cp /path/to/pocx-wallet/.github-wiki-migration/*.md .

# 3. Commit and push
git add .
git commit -m "Migrate documentation from wiki/ directory with custom sidebar and footer"
git push origin master
```

## What's Ready

✅ All 7 documentation files copied and ready
✅ Custom _Sidebar.md created with navigation
✅ Custom _Footer.md created with links
✅ Main repository README updated to point to wiki
✅ wiki/ directory removed from main repository

## What's Next

1. Follow the steps above to push these files to the GitHub Wiki
2. Verify at https://github.com/ev1ls33d/pocx-wallet/wiki
3. Delete this `.github-wiki-migration` directory once migration is complete

See [MIGRATION-INSTRUCTIONS.md](MIGRATION-INSTRUCTIONS.md) for detailed instructions.
