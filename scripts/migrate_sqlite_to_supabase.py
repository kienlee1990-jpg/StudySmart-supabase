#!/usr/bin/env python3
"""Copy an offline SQLite database to an empty, migrated Supabase schema.

Requires psycopg[binary]. SUPABASE_DATABASE_URL is a PostgreSQL URI/libpq DSN.
Default: validate the entire import, then roll back. Use --apply to commit.
"""
import argparse
from datetime import datetime, timezone
from decimal import Decimal
import os
import re
from pathlib import Path
import sqlite3

TABLES = [
    'AspNetRoles', 'AspNetUsers', 'Permissions', 'AuditLogs',
    'AspNetRoleClaims', 'AspNetUserClaims', 'AspNetUserLogins',
    'AspNetUserRoles', 'AspNetUserTokens', 'RefreshTokens',
    'RolePermissions', 'UserPermissions', 'QuizExams', 'QuizQuestions',
    'QuizLessons', 'QuizOptions', 'ExamQuestions', 'QuizAttempts',
    'TowerProgress', 'QuizAnswers',
]


def convert(value, data_type):
    if value is None:
        return None
    if data_type == 'boolean':
        if value not in (0, 1):
            raise ValueError(f'Invalid SQLite boolean: {value!r}')
        return bool(value)
    if data_type == 'timestamp with time zone':
        # SQLite/.NET may emit 1–7 fractional digits; Python 3.9 accepts
        # only 3 or 6. PostgreSQL timestamps have microsecond precision.
        text = re.sub(r'(\d{2}:\d{2}:\d{2})\.(\d+)',
                      lambda match: match[1] + '.' + (match[2] + '000000')[:6],
                      str(value).replace('Z', '+00:00'))
        result = datetime.fromisoformat(text)
        return (result.replace(tzinfo=timezone.utc) if result.tzinfo is None
                else result.astimezone(timezone.utc))
    if data_type == 'numeric':
        return Decimal(str(value))
    return value


def main():
    import psycopg
    from psycopg import sql

    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('sqlite_file', type=Path)
    parser.add_argument('--apply', action='store_true', help='Commit after validation')
    args = parser.parse_args()
    if not args.sqlite_file.is_file():
        parser.error('SQLite source file does not exist')
    dsn = os.environ.get('SUPABASE_DATABASE_URL')
    if not dsn:
        parser.error('Set SUPABASE_DATABASE_URL to the Supabase PostgreSQL URI (with sslmode=require)')

    # Read-only source. Stop the API before running to obtain a consistent offline copy.
    with sqlite3.connect(args.sqlite_file.resolve().as_uri() + '?mode=ro', uri=True) as source:
        source.execute('BEGIN')
        source_tables = {row[0] for row in source.execute(
            "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'")}
        unknown = source_tables - set(TABLES) - {'__EFMigrationsHistory'}
        if unknown:
            raise ValueError(f'Unmapped SQLite tables: {sorted(unknown)}')
        with psycopg.connect(dsn) as target:
            with target.cursor() as cursor:
                cursor.execute("SELECT table_name, column_name, data_type FROM information_schema.columns "
                               "WHERE table_schema = 'hoctap' ORDER BY ordinal_position")
                metadata = {}
                for table, column, data_type in cursor.fetchall():
                    metadata.setdefault(table, {})[column] = data_type
                if set(TABLES) - metadata.keys():
                    raise ValueError('Run the initial Supabase migration before importing')
                # Lock all tables in a fixed order: never merge into an initialized/running app.
                cursor.execute(sql.SQL('LOCK TABLE {} IN ACCESS EXCLUSIVE MODE').format(
                    sql.SQL(', ').join(sql.Identifier('hoctap', table) for table in TABLES)))
                for table in TABLES:
                    cursor.execute(sql.SQL('SELECT COUNT(*) FROM {}').format(sql.Identifier('hoctap', table)))
                    if cursor.fetchone()[0]:
                        raise ValueError(f'Target {table} is not empty; no data was changed')
                for table in TABLES:
                    if table not in source_tables:
                        print(f'{table}: absent in source, skipped')
                        continue
                    rows = source.execute(f'SELECT * FROM "{table}"')
                    columns = [column[0] for column in rows.description]
                    if set(columns) - metadata[table].keys():
                        raise ValueError(f'Unmapped source columns in {table}')
                    insert = sql.SQL('INSERT INTO {} ({}) VALUES ({})').format(
                        sql.Identifier('hoctap', table),
                        sql.SQL(', ').join(map(sql.Identifier, columns)),
                        sql.SQL(', ').join(sql.Placeholder() for _ in columns))
                    count = 0
                    while batch := rows.fetchmany(500):
                        cursor.executemany(insert, [tuple(convert(value, metadata[table][column])
                            for column, value in zip(columns, row)) for row in batch])
                        count += len(batch)
                    cursor.execute(sql.SQL('SELECT COUNT(*) FROM {}').format(sql.Identifier('hoctap', table)))
                    if cursor.fetchone()[0] != count:
                        raise ValueError(f'Row count mismatch for {table}')
                    print(f'{table}: {count} rows verified')
                if args.apply:
                    # Explicit IDs must not collide with future identity-generated IDs.
                    for table, columns in metadata.items():
                        if table not in TABLES or columns.get('Id') not in ('integer', 'bigint'):
                            continue
                        cursor.execute('SELECT pg_get_serial_sequence(%s, %s)', (f'hoctap."{table}"', 'Id'))
                        sequence = cursor.fetchone()[0]
                        if sequence:
                            cursor.execute(sql.SQL('SELECT COALESCE(MAX("Id"), 0) + 1 FROM {}').format(
                                sql.Identifier('hoctap', table)))
                            next_id = cursor.fetchone()[0]
                            # pg_get_serial_sequence returns a PostgreSQL-quoted qualified identifier.
                            cursor.execute(sql.SQL('ALTER SEQUENCE {} RESTART WITH {}').format(
                                sql.SQL(sequence), sql.Literal(next_id)))
                    target.commit()
                    print('Import committed. Start the API to seed any new permissions.')
                else:
                    target.rollback()
                    print('Validation passed. Rolled back; rerun with --apply to commit.')


if __name__ == '__main__':
    main()
