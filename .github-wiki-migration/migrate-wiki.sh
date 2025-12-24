#!/bin/bash
# Script to push wiki documentation to GitHub Wiki
# This script should be run with appropriate GitHub credentials

echo "Migrating documentation to GitHub Wiki..."

# Get the directory where the script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

echo ""
echo "Step 1: Checking if wiki repository exists..."
if [ -d "pocx-wallet.wiki" ]; then
    echo "Wiki repository already exists. Pulling latest changes..."
    cd pocx-wallet.wiki
    git pull origin master
    cd ..
else
    echo "Cloning wiki repository..."
    git clone https://github.com/ev1ls33d/pocx-wallet.wiki.git
fi

echo ""
echo "Step 2: Copying documentation files..."
cp *.md pocx-wallet.wiki/
echo "Files copied successfully."

echo ""
echo "Step 3: Committing and pushing changes..."
cd pocx-wallet.wiki
git add .
git commit -m "Migrate documentation from wiki/ directory with custom sidebar and footer"
git push origin master

echo ""
echo "Migration complete! Visit https://github.com/ev1ls33d/pocx-wallet/wiki to view your documentation."
echo ""
