#/bin/bash

POSTGRES_PASSWORD='pass' docker-compose -f docker-compose-citus.yml up --scale citus-worker=2 -d
sleep 3
docker cp ./Chirper/init/. citus-master:/var/lib/postgresql/restore/
docker exec -it citus-master bash -c "chown -R postgres:postgres /var/lib/postgresql/restore/*"
docker exec -it citus-master su - postgres -c "psql -U postgres -f /var/lib/postgresql/restore/dialog_restore.sql"
