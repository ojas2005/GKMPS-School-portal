#!/usr/bin/env bash
# Restores a nightly backup (see .github/workflows/backup.yml).
#
#   1. Download the backup from GitHub: Actions -> "Nightly database backup" -> the run you
#      want -> Artifacts. Unzip it to get gkmps-backup-YYYY-MM-DD.tar.gz.gpg.
#   2. Run:  scripts/restore-backup.sh gkmps-backup-YYYY-MM-DD.tar.gz.gpg
#      It asks for the backup passphrase and decrypts into ./restored-backup/.
#   3. Load it into a database. Try it on a local copy first:
#        mysql -h 127.0.0.1 -P 4000 -u root < restored-backup/backup/school.sql
#      For production, create a fresh TiDB instance and load it there, then point the app at
#      it -- never load over the live database without a copy of it first.
#
# Needs gpg (brew install gnupg / apt install gnupg).
set -euo pipefail

file="${1:?usage: scripts/restore-backup.sh gkmps-backup-YYYY-MM-DD.tar.gz.gpg}"
out="restored-backup"
umask 077
mkdir -p "$out"
gpg --decrypt --output "$out/backup.tar.gz" "$file"
tar -xzf "$out/backup.tar.gz" -C "$out"
rm "$out/backup.tar.gz"
echo "Decrypted into $out/backup/ -- school.sql holds every school database."
echo "This folder contains children's personal data: delete it when you're done."
