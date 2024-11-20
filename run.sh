#/bin/bash

docker-compose -f docker-compose.yml build
docker-compose -f docker-compose.yml up --detach --remove-orphans
sleep 5
docker exec -it master bash -c "echo 'localhost:5432:*:replicator:pass' > ~/.pgpass"
docker exec -it master bash -c "chmod 600 ~/.pgpass"

docker exec -it master bash -c "pg_basebackup -D /var/lib/postgresql/backup -U replicator -v -P --wal-method=stream"

docker cp master:/var/lib/postgresql/backup/. ./DbCluster/Volumes/slave1/
docker cp master:/var/lib/postgresql/backup/. ./DbCluster/Volumes/slave2/

touch ./DbCluster/Volumes/slave1/standby.signal
touch ./DbCluster/Volumes/slave2/standby.signal

docker-compose -f docker-compose-slaves.yml build
docker-compose -f docker-compose-slaves.yml up --detach

docker cp ./GiggleBook/Initialize/. master:/var/lib/postgresql/restore/
docker exec -it master bash -c "chown -R postgres:postgres /var/lib/postgresql/restore/*"
docker exec -it master su - postgres -c "psql -U postgres -f /var/lib/postgresql/restore/restore.sql"

curl -X POST http://admin:admin@localhost:3000/api/datasources -H "Content-Type: application/json" -d '{  "name": "Prometheus", "type": "prometheus", "url": "http://localhost:9090", "access": "proxy", "basicAuth": false, "jsonData": { "tlsSkipVerify": false } }'
