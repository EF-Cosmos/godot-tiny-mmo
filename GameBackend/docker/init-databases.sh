#!/bin/bash
set -e

# Function to create a database
function create_database() {
    local database=$1
    echo "Creating database: $database"

    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
        CREATE DATABASE $database;
        GRANT ALL PRIVILEGES ON DATABASE $database TO $POSTGRES_USER;
EOSQL
}

# Create additional databases
create_database game_auth
create_database game_chat
create_database game_analytics
create_database game_leaderboard

echo "All databases created successfully!"