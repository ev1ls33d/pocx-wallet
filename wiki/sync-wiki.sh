#!/bin/bash
# Sync wiki content from main repository to wiki repository

set -e

echo "Syncing wiki content..."

# Clone wiki repository
echo "Cloning wiki repository..."
git clone https://github.com/ev1ls33d/pocx-wallet.wiki.git wiki-repo

# Copy wiki files
echo "Copying wiki files..."
cp -r wiki/* wiki-repo/

# Commit and push changes
cd wiki-repo
git add .

if git commit -m "Update wiki from main repository"; then
    echo "Pushing changes..."
    git push
    echo "Wiki synced successfully!"
else
    echo "No changes to commit"
fi

# Cleanup
cd ..
rm -rf wiki-repo

echo "Done!"
