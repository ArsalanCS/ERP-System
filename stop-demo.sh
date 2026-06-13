#!/usr/bin/env bash
# Stops anything left running from run-demo.sh (use only if Ctrl+C didn't).
pkill -f "Erp.Api"  2>/dev/null && echo "Backend stopped."  || echo "Backend was not running."
pkill -f "vite"     2>/dev/null && echo "Frontend stopped." || echo "Frontend was not running."
pkill -f "dotnet run" 2>/dev/null || true
