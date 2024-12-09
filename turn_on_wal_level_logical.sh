#!/bin/bash

# Убедитесь, что PostgreSQL работает
until pg_isready -U postgres; do
  echo "Waiting for PostgreSQL to start..."
  sleep 2
done

# Добавьте необходимые параметры в postgresql.conf
echo "wal_level = logical" >> /var/lib/postgresql/data/postgresql.conf
echo "max_replication_slots = 10" >> /var/lib/postgresql/data/postgresql.conf
echo "max_wal_senders = 10" >> /var/lib/postgresql/data/postgresql.conf

# Перезагрузите настройки PostgreSQL
pg_ctl reload