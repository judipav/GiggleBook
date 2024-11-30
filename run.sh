#/bin/bash

cp ./DbCluster/Volumes/postgresql.reference ./DbCluster/Volumes/master_postgresql.conf

ADMIN_USER='admin' ADMIN_PASSWORD='admin' ADMIN_PASSWORD_HASH='$2a$14$1l.IozJx7xQRVmlkEQ32OeEEfP5mRxTpbDTCTcXRqn19gXD8YK1pO' docker-compose -f docker-compose-prometheus-grafana.yml up -d
docker-compose -f docker-compose.yml up -d
sleep 5
docker exec -it master bash -c "echo 'localhost:5432:*:replicator:pass' > ~/.pgpass"
docker exec -it master bash -c "chmod 600 ~/.pgpass"

docker cp ./DbCluster/replicator.sql master:/var/lib/postgresql/replicator.sql
docker exec -it master su - postgres -c "psql -f /var/lib/postgresql/replicator.sql"

docker exec -it master bash -c "pg_basebackup -D /var/lib/postgresql/backup -U replicator -v -P --wal-method=stream"

docker cp master:/var/lib/postgresql/backup/. ./DbCluster/Volumes/slave1/
docker cp master:/var/lib/postgresql/backup/. ./DbCluster/Volumes/slave2/

touch ./DbCluster/Volumes/slave1/standby.signal
touch ./DbCluster/Volumes/slave2/standby.signal

docker-compose -f docker-compose-slaves.yml up -d

docker stop master
echo "\nsynchronous_commit = on" >> ./DbCluster/Volumes/master_postgresql.conf
echo "\nsynchronous_standby_names = 'ANY 1 (slave1, slave2)'" >> ./DbCluster/Volumes/master_postgresql.conf

docker start master
sleep 5
docker cp ./GiggleBook/Initialize/. master:/var/lib/postgresql/restore/
docker exec -it master bash -c "chown -R postgres:postgres /var/lib/postgresql/restore/*"
docker exec -it master su - postgres -c "psql -U postgres -f /var/lib/postgresql/restore/restore.sql"

docker-compose -f docker-compose-redis.yml up -d