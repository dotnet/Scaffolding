#!/usr/bin/env bash
set -euo pipefail

snapshot="$PIPELINE_WORKSPACE/scaffolding-sdk-temp-before.txt"

case "${1:-}" in
    snapshot)
        find "$AGENT_TEMPDIRECTORY" -mindepth 1 -maxdepth 1 -printf '%f\n' | sort > "$snapshot"
        ;;
    clean)
        if [[ ! -f "$snapshot" ]]; then
            echo "Missing SDK staging snapshot: $snapshot" >&2
            exit 1
        fi

        after="$(mktemp "$PIPELINE_WORKSPACE/scaffolding-sdk-temp-after.XXXXXX")"
        new_entries="$(mktemp "$PIPELINE_WORKSPACE/scaffolding-sdk-temp-new.XXXXXX")"
        trap 'rm -f -- "$after" "$new_entries"' EXIT
        find "$AGENT_TEMPDIRECTORY" -mindepth 1 -maxdepth 1 -printf '%f\n' | sort > "$after"
        comm -13 "$snapshot" "$after" > "$new_entries"

        while IFS= read -r name; do
            [[ "$name" =~ ^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$ ]] || continue
            path="$AGENT_TEMPDIRECTORY/$name"
            if [[ -d "$path" ]]; then
                [[ -d "$path/sdk" && -d "$path/shared/Microsoft.NETCore.App" ]] || continue
                sdk_dir="$(find "$path/sdk" -mindepth 1 -maxdepth 1 -type d \( -name '8.*' -o -name '9.*' -o -name '10.*' \) -print -quit)"
                [[ -n "$sdk_dir" ]] || continue
            elif [[ -f "$path" ]]; then
                [[ "$(file -b --mime-type "$path")" == application/gzip ]] || continue
                tar --force-local -tzf "$path" | awk '/(^|\/)sdk\/(8|9|10)\./ { found = 1 } END { exit !found }' || continue
            else
                continue
            fi

            du -sh -- "$path"
            rm -rf -- "$path"
        done < "$new_entries"
        rm -f -- "$snapshot"
        ;;
    *)
        echo "Usage: $0 snapshot|clean" >&2
        exit 1
        ;;
esac
