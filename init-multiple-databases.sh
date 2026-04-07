#!/bin/bash
set -e

if [ -n "$POSTGRES_MULTIPLE_DATABASES" ]; then
    echo "Creating additional databases: $POSTGRES_MULTIPLE_DATABASES"
    for db in $(echo $POSTGRES_MULTIPLE_DATABASES | tr ',' ' '); do
        echo "  → Creating database: $db"
        psql --username "$POSTGRES_USER" --dbname "postgres" \
            -c "SELECT 1 FROM pg_database WHERE datname='$db'" \
            | grep -q 1 || \
        psql --username "$POSTGRES_USER" --dbname "postgres" \
            -c "CREATE DATABASE $db OWNER $POSTGRES_USER;"
    done
    echo "Done."
fi
