#!/usr/bin/env bash
# Exporte le jeu en livrables Linux (x86_64) et Windows (x86_64), puis les zippe.
# Prérequis : Godot 4.6.3 mono + les modèles d'exportation de la MÊME version
#             (Éditeur > Aide > Gérer les modèles d'exportation, ou voir rapport/).
set -euo pipefail

GODOT="${GODOT:-/usr/local/bin/godot}"
RACINE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$RACINE"

rm -rf builds
mkdir -p builds/linux builds/linux-arm64 builds/windows

echo "== Compilation C# =="
# Sortie NON masquée : c'est le seul endroit où passent les warnings du compilateur.
"$GODOT" --headless --build-solutions --quit

echo "== Export Linux x86_64 =="
"$GODOT" --headless --export-release "Linux"

echo "== Export Linux arm64 =="
"$GODOT" --headless --export-release "Linux ARM64"

echo "== Export Windows =="
"$GODOT" --headless --export-release "Windows Desktop"

echo "== Archives =="
(cd builds/linux       && zip -qr ../GloobyAventures-linux-x86_64.zip   .)
(cd builds/linux-arm64 && zip -qr ../GloobyAventures-linux-arm64.zip    .)
(cd builds/windows     && zip -qr ../GloobyAventures-windows-x86_64.zip .)

ls -lh builds/*.zip
