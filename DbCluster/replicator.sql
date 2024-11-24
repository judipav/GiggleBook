CREATE ROLE replicator WITH REPLICATION LOGIN PASSWORD 'pass';
GRANT CONNECT ON DATABASE postgres TO replicator;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO replicator;


--
-- Избавляемся от постоянных ошибок в логах связанных со стандартным поведением postgresql при подключении пользователя без ключа -d пытается найти одноименную БД
CREATE DATABASE replicator
    WITH
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.utf8'
    LC_CTYPE = 'en_US.utf8'
    LOCALE_PROVIDER = 'libc'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1
    IS_TEMPLATE = False;

GRANT ALL ON DATABASE replicator TO replicator;

CHECKPOINT;  